using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool ExceptWith(ReadOnlySpan<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return ExceptWithSorted(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool ExceptWith(IReadOnlyCollection<TValue> range)
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
                return ExceptWithSorted(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool ExceptWith(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return ExceptWith(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return ExceptWith(collection);
            }

            List<TValue> temp = new List<TValue>();

            foreach (var item in range)
            {
                temp.Add(item);
            }
            
            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return ExceptWithSorted(span);
        }

        public bool ExceptWith(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                return ExceptWithSorted(range.GetSpan());
            }

            return ExceptWith(range.GetSpan());
        }

        public bool ExceptWith(BinaryList<TValue> range)
        {
            return ExceptWith(range.GetEnumerator());
        }

        private bool ExceptWithSorted(ReadOnlySpan<TValue> range)
        {
            if (range.Length <= 0 || _count <= 0)
                return false;

            if (LeftBuffer >= RightBuffer && LeftBuffer >= range.Length)
            {
                return ExceptWithFromLeft(range);
            }

            if (RightBuffer >= LeftBuffer && RightBuffer >= range.Length)
            {
                return ExceptWithFromRight(range);
            }

            return ResizeAndExceptWithSorted(range);
        }

        private bool ResizeAndExceptWithSorted(ReadOnlySpan<TValue> range)
        {
            PrepareForResize(range.Length, out var destination);

            int newZeroRIndex = CalculateZeroIndex(destination.Length, _count + range.Length);

            if (_storage == destination)
            {
                Array.Copy(destination, _zeroRIndex, destination, newZeroRIndex, _count);
                _zeroRIndex = newZeroRIndex;
                return ExceptWithFromRight(range);
            }
#if TEST
            if (_count % 2 == 0)
            {
                // Because this case does not arise in practice,
                // yet we must maintain the code for the eventuality that it might suddenly become active.
                ExceptWithFromRight(range, destination, newZeroRIndex);
            }
            else
            {
                ExceptWithFromLeft(range, destination, newZeroRIndex);
            }
#else
            ExceptWithFromLeft(range, destination, newZeroRIndex);
#endif
            _storage = destination;
            return true;
        }

        private bool ExceptWithFromLeft(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex - range.Length;
            return ExceptWithFromLeft(range, _storage, newZeroRIndex);
        }

        private bool ExceptWithFromLeft(ReadOnlySpan<TValue> second, TValue[] destination, int newZeroRIndex)
        {
            _version = unchecked(++_version);

            var first = _storage;

            if (ReferenceEquals(first, destination))
            {
                Debug.Assert(_zeroRIndex - newZeroRIndex == second.Length, "In-place merge requires place for the new range.");
            }

            // for local function usage (for optimizations) 
            Dictionary<TValue?, HeapCounter> tempDictionary = _comparer.LeadsToCollisions ? new() : null!;

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

                switch (_comparer.Compare(firstItem, secondItem))
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

            // Manage remaining items
            if (s_InRange(s))
            {
                do ProcessMergingSecond(ref d, ref s, second);
                while (s_InRange(s));

                incomingElementsSkipped = newZeroRIndex + _count + second.Length - d;

                // Clear remaining free-spots
                if (ItemType<TValue>.ContainsReferences && incomingElementsSkipped > 0 && first == destination)
                {
                    Array.Clear(first, d, incomingElementsSkipped);
                }
            }
            else if (f_InRange(f))
            {
                int remainingPartLength = _zeroRIndex + _count - f;
                int finishedPartLength = d - newZeroRIndex;

                incomingElementsSkipped = _count + second.Length - remainingPartLength - finishedPartLength;

                if (first != destination)
                {
                    // we just have to copy remaining items into new array
                    Array.Copy(first, f, destination, d, remainingPartLength);
                }
                else if (incomingElementsSkipped > 0)
                {
                    if (finishedPartLength >= remainingPartLength)
                    {
                        // Shift remaining items due to missing duplicates
                        Array.Copy(first, f, first, d, remainingPartLength);

                        // Clear formed dirty free-spots from right side
                        if (ItemType<TValue>.ContainsReferences)
                        {
                            Array.Clear(first, newZeroRIndex + finishedPartLength + remainingPartLength, incomingElementsSkipped);
                        }
                    }
                    else
                    {
                        // Shift finished items due to missing duplicates
                        Array.Copy(first, newZeroRIndex, first, newZeroRIndex + incomingElementsSkipped, finishedPartLength);
                        zeroIndexShift += incomingElementsSkipped;

                        // Clear formed dirty free-spots from left side
                        if (ItemType<TValue>.ContainsReferences)
                        {
                            Array.Clear(first, newZeroRIndex, incomingElementsSkipped);
                        }
                    }
                }
            }
            else
            {
                incomingElementsSkipped = newZeroRIndex + _count + second.Length - d;
                // Clear remaining free-spots
                if (ItemType<TValue>.ContainsReferences && incomingElementsSkipped > 0 && first == destination)
                {
                    Array.Clear(first, d, incomingElementsSkipped);
                }
            }

            _zeroRIndex = newZeroRIndex + zeroIndexShift;
            _count += second.Length - incomingElementsSkipped;

            _flags.CheckFor(_count);

            return true;

            void ProcessMergingBoth(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
            {
                if (_comparer.LeadsToCollisions)
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

                        if (_comparer.Compare(currentItem, previousItem) != 0)
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

                        if (_comparer.Compare(currentItem, previousItem) != 0)
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
                        int countToAdd = item.Value.FirstCount - item.Value.SecondCount;
                        if (countToAdd < 0)
                            countToAdd = 0;

                        for (int j = 0; j < countToAdd; j++)
                        {
                            destination[d++] = item.Key;
                        }

                        addedCount += countToAdd;
                    }
                }

                void ProcessMergingBothComparable(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
                {
                    f++; s++; // they are equal here, so we can move both indexes
                }
            }

            void ProcessMergingSecond(ref int d, ref int s, ReadOnlySpan<TValue> second)
            {
                if (_comparer.LeadsToCollisions)
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

                    while (s_InRange(s) && _comparer.Compare(second[s], initialItem) == 0)
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
                if (_comparer.LeadsToCollisions)
                {
                    ProcessMergingFirstDefault(ref d, ref f);
                }
                else
                {
                    ProcessMergingFirstComparable(ref d, ref f);
                }

                void ProcessMergingFirstDefault(ref int d, ref int f)
                {
                    destination[d++] = first[f++];
                }

                void ProcessMergingFirstComparable(ref int d, ref int f)
                {
                    ProcessMergingFirstDefault(ref d, ref f);
                }
            }
        }

        private bool ExceptWithFromRight(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex;
            return ExceptWithFromRight(range, _storage, newZeroRIndex);
        }

        private bool ExceptWithFromRight(ReadOnlySpan<TValue> second, TValue[] destination, int newZeroRIndex)
        {
            _version = unchecked(++_version);

            var first = _storage;

            if (ReferenceEquals(first, destination))
            {
                Debug.Assert(newZeroRIndex == _zeroRIndex, "In-place merge requires place for the new range.");
            }

            // for local function usage (for optimizations) 
            Dictionary<TValue?, HeapCounter> tempDictionary = _comparer.LeadsToCollisions ? new() : null!;

            int incomingElementsSkipped = 0;
            int rangeLength = second.Length;
            int d = newZeroRIndex + _count + second.Length - 1;
            int zeroIndexShift = 0;

            Debug.Assert(d < destination.Length, "Right buffer is not long enough");

            bool s_InRange(int s) => s >= 0;
            bool f_InRange(int f) => f >= _zeroRIndex;

            // General merge loop
            int s = second.Length - 1;
            int f = _zeroRIndex + _count - 1;

            while (s_InRange(s) && f_InRange(f))
            {
                ref readonly TValue secondItem = ref second[s];
                ref readonly TValue firstItem = ref first[f];

                switch (_comparer.Compare(firstItem, secondItem))
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

            // Manage remaining items
            if (s_InRange(s))
            {
                do ProcessMergingSecond(ref d, ref s, second);
                while (s_InRange(s));

                incomingElementsSkipped = (d + 1) - newZeroRIndex;
                zeroIndexShift += incomingElementsSkipped;

                // Clear remaining free-spots
                if (ItemType<TValue>.ContainsReferences && incomingElementsSkipped > 0 && first == destination)
                {
                    Array.Clear(first, newZeroRIndex, incomingElementsSkipped);
                }
            }
            else if (f_InRange(f))
            {
                int remainingPartLength = (f + 1) - _zeroRIndex;
                int finishedPartLength = _count + second.Length - (d + 1 - newZeroRIndex);

                incomingElementsSkipped = _count + second.Length - remainingPartLength - finishedPartLength;

                if (first != destination)
                {
                    // we just have to copy remaining items into new array
                    Array.Copy(first, _zeroRIndex, destination, newZeroRIndex + incomingElementsSkipped, remainingPartLength);
                    zeroIndexShift += incomingElementsSkipped;
                }
                else if (incomingElementsSkipped > 0)
                {
                    if (finishedPartLength >= remainingPartLength)
                    {
                        // Shift remaining items due to missing duplicates
                        Array.Copy(first, _zeroRIndex, first, newZeroRIndex + incomingElementsSkipped, remainingPartLength);
                        zeroIndexShift += incomingElementsSkipped;

                        // Clear formed dirty free-spots from left side
                        if (ItemType<TValue>.ContainsReferences)
                        {
                            Array.Clear(first, newZeroRIndex, incomingElementsSkipped);
                        }
                    }
                    else
                    {
                        // Shift finished items due to missing duplicates
                        Array.Copy(first, d + 1, first, d + 1 - incomingElementsSkipped, finishedPartLength);

                        // Clear formed dirty free-spots from right side
                        if (ItemType<TValue>.ContainsReferences)
                        {
                            Array.Clear(first, newZeroRIndex + finishedPartLength + remainingPartLength, incomingElementsSkipped);
                        }
                    }
                }
            }
            else
            {
                incomingElementsSkipped = (d + 1) - newZeroRIndex;
                zeroIndexShift += incomingElementsSkipped;

                // Clear remaining free-spots
                if (ItemType<TValue>.ContainsReferences && incomingElementsSkipped > 0 && first == destination)
                {
                    Array.Clear(first, newZeroRIndex, incomingElementsSkipped);
                }
            }

            _zeroRIndex = newZeroRIndex + zeroIndexShift;
            _count += second.Length - incomingElementsSkipped;

            _flags.CheckFor(_count);

            return true;

            void ProcessMergingBoth(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
            {
                if (_comparer.LeadsToCollisions)
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

                        if (_comparer.Compare(currentItem, previousItem) != 0)
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

                        if (_comparer.Compare(currentItem, previousItem) != 0)
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
                        int countToAdd = item.Value.FirstCount - item.Value.SecondCount;
                        if (countToAdd < 0)
                            countToAdd = 0;

                        for (int j = 0; j < countToAdd; j++)
                        {
                            destination[d--] = item.Key;
                        }

                        addedCount += countToAdd;
                    }
                }

                void ProcessMergingBothComparable(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
                {
                    f--; s--; // they are equal here, so we can move both indexes
                }
            }
            
            void ProcessMergingSecond(ref int d, ref int s, ReadOnlySpan<TValue> second)
            {
                if (_comparer.LeadsToCollisions)
                {
                    ProcessMergingSecondDefault(ref d, ref s, second);
                }
                else
                {
                    ProcessMergingSecondComparable(ref d, ref s, second);
                }

                void ProcessMergingSecondDefault(ref int d, ref int s, ReadOnlySpan<TValue> second)
                {
                    ProcessMergingSecondComparable(ref d, ref s, second);
                }

                void ProcessMergingSecondComparable(ref int d, ref int s, ReadOnlySpan<TValue> second)
                {
                    ref readonly TValue initialItem = ref second[s--];

                    while (s_InRange(s) && _comparer.Compare(second[s], initialItem) == 0)
                    {
                        s--;
                    }
                }
            }

            void ProcessMergingFirst(ref int d, ref int f)
            {
                if (_comparer.LeadsToCollisions)
                {
                    ProcessMergingFirstDefault(ref d, ref f);
                }
                else
                {
                    ProcessMergingFirstComparable(ref d, ref f);
                }

                void ProcessMergingFirstDefault(ref int d, ref int f)
                {
                    ProcessMergingFirstComparable(ref d, ref f);
                }

                void ProcessMergingFirstComparable(ref int d, ref int f)
                {
                    destination[d--] = first[f--];
                }
            }
        }
    }
}
