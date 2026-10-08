using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool IntersectWith(ReadOnlySpan<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return IntersectWithSorted(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool IntersectWith(IReadOnlyCollection<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Count);
            try
            {
                int i = 0;
                foreach (var item in range)
                {
                    temp[i++] = item;
                }

                Array.Sort(temp, 0, range.Count, Comparer);
                return IntersectWithSorted(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool IntersectWith(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return IntersectWith(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return IntersectWith(collection);
            }

            List<TValue> temp = new List<TValue>();

            foreach (var item in range)
            {
                temp.Add(item);
            }

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return IntersectWithSorted(span);
        }

        public bool IntersectWith(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                return IntersectWithSorted(range.GetSpan());
            }

            return IntersectWith(range.GetSpan());
        }

        public bool IntersectWith(BinaryList<TValue> range)
        {
            return IntersectWith(range.GetEnumerator());
        }

        private bool IntersectWithSorted(ReadOnlySpan<TValue> range)
        {
            if (_count <= 0)
                return false;

            if (range.Length <= 0)
            {
                Clear();
                return true;
            }

            if (LeftBuffer >= RightBuffer && LeftBuffer >= range.Length)
            {
                return IntersectWithFromLeft(range);
            }

            if (RightBuffer >= LeftBuffer && RightBuffer >= range.Length)
            {
                return IntersectWithFromRight(range);
            }

            return ResizeAndIntersectWithSorted(range);
        }

        private bool ResizeAndIntersectWithSorted(ReadOnlySpan<TValue> range)
        {
            PrepareForResize(range.Length, out var destination);

            int newZeroRIndex = CalculateZeroIndex(destination.Length, _count + range.Length);

            if (_storage == destination)
            {
                Array.Copy(destination, _zeroRIndex, destination, newZeroRIndex, _count);
                _zeroRIndex = newZeroRIndex;
                return IntersectWithFromRight(range);
            }
#if TEST
            if (_count % 2 == 0)
            {
                // Because this case does not arise in practice,
                // yet we must maintain the code for the eventuality that it might suddenly become active.
                IntersectWithFromRight(range, destination, newZeroRIndex);
            }
            else
            {
                IntersectWithFromLeft(range, destination, newZeroRIndex);
            }
#else
            IntersectWithFromLeft(range, destination, newZeroRIndex);
#endif
            _storage = destination;
            return true;
        }

        private bool IntersectWithFromLeft(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex - range.Length;
            return IntersectWithFromLeft(range, _storage, newZeroRIndex);
        }

        private bool IntersectWithFromLeft(ReadOnlySpan<TValue> second, TValue[] destination, int newZeroRIndex)
        {
            var first = _storage;

            if (ReferenceEquals(first, destination))
            {
                Debug.Assert(_zeroRIndex - newZeroRIndex == second.Length, "In-place merge requires place for the new range.");
            }

            // for local function usage (for optimizations) 
            Dictionary<TValue?, HeapCounter> tempDictionary = ContainsCollisions ? new() : null!;

            int incomingElementsSkipped = 0;
            int rangeLength = second.Length;
            int d = newZeroRIndex;
            int zeroIndexShift = 0;

            Debug.Assert(d > -1, "Left buffer is not long enough");

            bool s_InRange(int s) => s < rangeLength;
            bool f_InRange(int f) => f < _zeroRIndex + _count;

            // General merge loop
            int s = 0;
            int f = _zeroRIndex;

            while (s_InRange(s) && f_InRange(f))
            {
                ref readonly TValue secondItem = ref second[s];
                ref readonly TValue firstItem = ref first[f];

                switch (Compare(in firstItem, in secondItem))
                {
                    case > 0:
                        ProcessMergingSecond(ref d, ref s, second);
                        break;
                    case < 0:
                        ProcessMergingFirst(ref d, ref f);
                        break;
                    case 0:
                        ProcessMergingBoth(ref d, ref s, ref f, second);
                        break;
                }
            }

            incomingElementsSkipped = newZeroRIndex + _count + second.Length - d;

            // Clear remaining free-spots
            if (ContainsReferences && incomingElementsSkipped > 0 && first == destination)
            {
                Array.Clear(first, d, incomingElementsSkipped);
            }

            _zeroRIndex = newZeroRIndex + zeroIndexShift;
            _count += second.Length - incomingElementsSkipped;
            _version = unchecked(++_version);

            if (_count < 2)
                ModifyFlags(add: BinaryListFlags.NoDuplicates, remove: BinaryListFlags.UniformRange);

            return true;

            void ProcessMergingBoth(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
            {
                if (ContainsCollisions)
                {
                    ProcessMergingBothDefault(ref d, ref s, ref f, second);
                }
                else
                {
                    ProcessMergingBothComparable(ref d, ref s, ref f, second);
                }

                void ProcessMergingBothDefault(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
                {
                    tempDictionary.Clear();

                    (int start, int length) resultedRangeFirst = default;
                    resultedRangeFirst.start = f;

                    // Add the first item to result (preparing to compare)
                    ref readonly TValue previousItem = ref first[f++];

                    tempDictionary.Add(previousItem, new HeapCounter(firstCount: 1));
                    resultedRangeFirst.length++;

                    // Add all currently existing items in collision to result, and count them.
                    while (f_InRange(f))
                    {
                        ref readonly TValue currentItem = ref first[f];

                        if (Compare(in currentItem, in previousItem) != 0)
                            break; // this is the end of duplicates, we must stop

                        previousItem = ref currentItem;

                        if (!tempDictionary.TryGetValue(currentItem, out var counter))
                        {
                            counter = new HeapCounter();
                            tempDictionary[currentItem] = counter;
                        }

                        counter.FirstCount++;
                        resultedRangeFirst.length++;
                        f++;
                    }

                    // Add all incoming items in collision to result, and count them.
                    (int start, int length) resultedRangeSecond = default;
                    resultedRangeSecond.start = s;
                    do
                    {
                        ref readonly TValue currentItem = ref second[s];

                        if (Compare(in currentItem, in previousItem) != 0)
                            break; // this is the end of duplicates, we must stop

                        previousItem = ref currentItem;

                        if (!tempDictionary.TryGetValue(currentItem, out var counter))
                        {
                            counter = new HeapCounter();
                            tempDictionary[currentItem] = counter;
                        }

                        counter.SecondCount++;
                        resultedRangeSecond.length++;
                        s++;
                    }
                    while (s_InRange(s));

                    int addedCount = 0;
                    foreach (var item in tempDictionary)
                    {
                        int countToAdd = Math.Min(item.Value.FirstCount, item.Value.SecondCount);

                        for (int j = 0; j < countToAdd; j++)
                        {
                            destination[d++] = item.Key;
                        }

                        addedCount += countToAdd;
                    }
                }

                void ProcessMergingBothComparable(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
                {
                    ref readonly TValue initialItem = ref first[f++]; s++; // they are equal here, so we can move both indexes
                    destination[d++] = initialItem;

                    while (true)
                    {
                        int hasItemInRow = 0;

                        if (f_InRange(f) && Compare(in first[f], in initialItem) == 0)
                        {
                            hasItemInRow++; f++;
                        }

                        if (s_InRange(s) && Compare(in second[s], in initialItem) == 0)
                        {
                            hasItemInRow++; s++;
                        }

                        switch (hasItemInRow)
                        {
                            case 0:
                                return;
                            case 1:
                                return;
                            case 2:
                                destination[d++] = initialItem;
                                continue;
                        }
                    }
                }
            }

            void ProcessMergingSecond(ref int d, ref int s, ReadOnlySpan<TValue> second)
            {
                if (ContainsCollisions)
                {
                    ProcessMergingSecondDefault(ref d, ref s, second);
                }
                else
                {
                    ProcessMergingSecondComparable(ref d, ref s, second);
                }

                void ProcessMergingSecondDefault(ref int d, ref int s, ReadOnlySpan<TValue> second)
                {
                    ref readonly TValue initialItem = ref second[s++];

                    while (s_InRange(s) && Compare(in second[s], in initialItem) == 0)
                    {
                        s++;
                    }
                }

                void ProcessMergingSecondComparable(ref int d, ref int s, ReadOnlySpan<TValue> second)
                {
                    ProcessMergingSecondDefault(ref d, ref s, second);
                }
            }

            void ProcessMergingFirst(ref int d, ref int f)
            {
                if (ContainsCollisions)
                {
                    ProcessMergingFirstDefault(ref d, ref f);
                }
                else
                {
                    ProcessMergingFirstComparable(ref d, ref f);
                }

                void ProcessMergingFirstDefault(ref int d, ref int f)
                {
                    ref readonly TValue initialItem = ref first[f++];

                    while (f_InRange(f) && Compare(in first[f], in initialItem) == 0)
                    {
                        f++;
                    }
                }

                void ProcessMergingFirstComparable(ref int d, ref int f)
                {
                    ProcessMergingFirstDefault(ref d, ref f);
                }
            }
        }

        private bool IntersectWithFromRight(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex;
            return IntersectWithFromRight(range, _storage, newZeroRIndex);
        }

        private bool IntersectWithFromRight(ReadOnlySpan<TValue> second, TValue[] destination, int newZeroRIndex)
        {
            var first = _storage;

            if (ReferenceEquals(first, destination))
            {
                Debug.Assert(newZeroRIndex == _zeroRIndex, "In-place merge requires place for the new range.");
            }

            // for local function usage (for optimizations) 
            Dictionary<TValue?, HeapCounter> tempDictionary = ContainsCollisions ? new() : null!;

            int incomingElementsSkipped = 0;
            int rangeLength = second.Length;
            int d = newZeroRIndex + _count + second.Length - 1;
            int zeroIndexShift = 0;

            Debug.Assert(d < destination.Length, "Right buffer is not long enough");

            bool s_InRange(int s) => s >= 0;
            bool f_InRange(int f) => f >= _zeroRIndex;

            // remember - we do care only about duplicates among incomings

            // General merge loop
            int s = second.Length - 1;
            int f = _zeroRIndex + _count - 1;

            while (s_InRange(s) && f_InRange(f))
            {
                ref readonly TValue secondItem = ref second[s];
                ref readonly TValue firstItem = ref first[f];

                switch (Compare(in firstItem, in secondItem))
                {
                    case > 0:
                        ProcessMergingFirst(ref d, ref f);
                        break;
                    case < 0:
                        ProcessMergingSecond(ref d, ref s, second);
                        break;
                    default:
                        ProcessMergingBoth(ref d, ref s, ref f, second);
                        break;
                }
            }

            incomingElementsSkipped = (d + 1) - newZeroRIndex;
            zeroIndexShift += incomingElementsSkipped;

            // Clear remaining free-spots
            if (ContainsReferences && incomingElementsSkipped > 0 && first == destination)
            {
                Array.Clear(first, newZeroRIndex, incomingElementsSkipped);
            }

            _zeroRIndex = newZeroRIndex + zeroIndexShift;
            _count += second.Length - incomingElementsSkipped;
            _version = unchecked(++_version);

            if (_count < 2)
                ModifyFlags(add: BinaryListFlags.NoDuplicates, remove: BinaryListFlags.UniformRange);

            return true;

            void ProcessMergingBoth(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
            {
                if (ContainsCollisions)
                {
                    ProcessMergingBothDefault(ref d, ref s, ref f, second);
                }
                else
                {
                    ProcessMergingBothComparable(ref d, ref s, ref f, second);
                }

                void ProcessMergingBothDefault(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
                {
                    tempDictionary.Clear();

                    (int start, int length) resultedRangeFirst = default;
                    resultedRangeFirst.start = f;

                    // Add the first item to result (preparing to compare)
                    ref readonly TValue previousItem = ref first[f--];

                    tempDictionary.Add(previousItem, new HeapCounter(firstCount: 1));
                    resultedRangeFirst.length++;

                    // Add all currently existing items in collision to result, and count them.
                    while (f_InRange(f))
                    {
                        ref readonly TValue currentItem = ref first[f];

                        if (Compare(in currentItem, in previousItem) != 0)
                            break; // this is the end of duplicates, we must stop

                        previousItem = ref currentItem;

                        if (!tempDictionary.TryGetValue(currentItem, out var counter))
                        {
                            counter = new HeapCounter();
                            tempDictionary[currentItem] = counter;
                        }

                        counter.FirstCount++;
                        resultedRangeFirst.length++;
                        f--;
                    }

                    // Add all incoming items in collision to result, and count them.
                    (int start, int length) resultedRangeSecond = default;
                    resultedRangeSecond.start = s;
                    do
                    {
                        ref readonly TValue currentItem = ref second[s];

                        if (Compare(in currentItem, in previousItem) != 0)
                            break; // this is the end of duplicates, we must stop

                        previousItem = ref currentItem;

                        if (!tempDictionary.TryGetValue(currentItem, out var counter))
                        {
                            counter = new HeapCounter();
                            tempDictionary[currentItem] = counter;
                        }

                        counter.SecondCount++;
                        resultedRangeSecond.length++;
                        s--;
                    }
                    while (s_InRange(s));

                    int addedCount = 0;
                    foreach (var item in tempDictionary)
                    {
                        int countToAdd = Math.Min(item.Value.FirstCount, item.Value.SecondCount);

                        for (int j = 0; j < countToAdd; j++)
                        {
                            destination[d--] = item.Key;
                        }

                        addedCount += countToAdd;
                    }
                }

                void ProcessMergingBothComparable(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
                {
                    ref readonly TValue initialItem = ref first[f--]; s--; // they are equal here, so we can move both indexes
                    destination[d--] = initialItem;

                    while (true)
                    {
                        int hasItemInRow = 0;

                        if (f_InRange(f) && Compare(in first[f], in initialItem) == 0)
                        {
                            hasItemInRow++; f--;
                        }

                        if (s_InRange(s) && Compare(in second[s], in initialItem) == 0)
                        {
                            hasItemInRow++; s--;
                        }

                        switch (hasItemInRow)
                        {
                            case 0:
                                return;
                            case 1:
                                return;
                            case 2:
                                destination[d--] = initialItem;
                                continue;
                        }
                    }
                }
            }
            
            void ProcessMergingSecond(ref int d, ref int s, ReadOnlySpan<TValue> second)
            {
                if (ContainsCollisions)
                {
                    ProcessMergingSecondDefault(ref d, ref s, second);
                }
                else
                {
                    ProcessMergingSecondComparable(ref d, ref s, second);
                }

                void ProcessMergingSecondDefault(ref int d, ref int s, ReadOnlySpan<TValue> second)
                {
                    ref readonly TValue initialItem = ref second[s--];

                    while (s_InRange(s) && Compare(in second[s], in initialItem) == 0)
                    {
                        s--;
                    }
                }

                void ProcessMergingSecondComparable(ref int d, ref int s, ReadOnlySpan<TValue> second)
                {
                    ProcessMergingSecondDefault(ref d, ref s, second);
                }
            }

            void ProcessMergingFirst(ref int d, ref int f)
            {
                if (ContainsCollisions)
                {
                    ProcessMergingFirstDefault(ref d, ref f);
                }
                else
                {
                    ProcessMergingFirstComparable(ref d, ref f);
                }

                void ProcessMergingFirstDefault(ref int d, ref int f)
                {
                    ref readonly TValue initialItem = ref first[f--];

                    while (f_InRange(f) && Compare(in first[f], in initialItem) == 0)
                    {
                        f--;
                    }
                }

                void ProcessMergingFirstComparable(ref int d, ref int f)
                {
                    ProcessMergingFirstDefault(ref d, ref f);
                }
            }
        }
    }
}
