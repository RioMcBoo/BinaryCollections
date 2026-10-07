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

        private bool IntersectWithFromLeft(ReadOnlySpan<TValue> range, TValue[] destination, int newZeroRIndex)
        {
            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(_zeroRIndex - newZeroRIndex == range.Length, "In-place merge requires place for the new range.");
            }

            // for local function usage (for optimizations) 
            Dictionary<TValue?, HeapCounter> tempDictionary = ContainsCollisions ? new() : null!;

            int incomingElementsSkipped = 0;
            int rangeLength = range.Length;
            int insertRIndex = newZeroRIndex;
            int zeroIndexShift = 0;

            Debug.Assert(insertRIndex > -1, "Left buffer is not long enough");

            bool i_InRange(int i) => i < rangeLength;
            bool c_InRange(int c) => c < _zeroRIndex + _count;

            // General merge loop
            int i = 0;
            int c = _zeroRIndex;

            while (i_InRange(i) && c_InRange(c))
            {
                ref readonly TValue incomingItem = ref range[i];
                ref readonly TValue currentItem = ref _storage[c];

                switch (Compare(in currentItem, in incomingItem))
                {
                    case > 0:
                        ProcessMergingIncoming(ref insertRIndex, ref i, range);
                        break;
                    case < 0:
                        ProcessMergingCurrent(ref insertRIndex, ref c);
                        break;
                    case 0:
                        ProcessMergingBoth(ref insertRIndex, ref i, ref c, range);
                        break;
                }
            }

            incomingElementsSkipped = newZeroRIndex + _count + range.Length - insertRIndex;

            // Clear remaining free-spots
            if (ContainsReferences && incomingElementsSkipped > 0 && _storage == destination)
            {
                Array.Clear(_storage, insertRIndex, incomingElementsSkipped);
            }

            _zeroRIndex = newZeroRIndex + zeroIndexShift;
            _count += range.Length - incomingElementsSkipped;
            _version = unchecked(++_version);

            if (_count < 2)
                ModifyFlags(add: BinaryListFlags.NoDuplicates, remove: BinaryListFlags.UniformRange);

            return true;

            void ProcessMergingBoth(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                if (ContainsCollisions)
                {
                    ProcessMergingBothDefault(ref insertRIndex, ref i, ref c, range);
                }
                else
                {
                    ProcessMergingBothComparable(ref insertRIndex, ref i, ref c, range);
                }

                void ProcessMergingBothDefault(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
                {
                    tempDictionary.Clear();

                    (int start, int length) resultedRangeCurrent = default;
                    resultedRangeCurrent.start = c;

                    // Add the first item to result (preparing to compare)
                    ref readonly TValue previous = ref _storage[c++];

                    tempDictionary.Add(previous, new HeapCounter(sourceCount: 1));
                    resultedRangeCurrent.length++;

                    // Add all currently existing items in collision to result, and count them.
                    while (c_InRange(c))
                    {
                        ref readonly TValue currentItem = ref _storage[c];

                        if (Compare(in currentItem, in previous) != 0)
                            break; // this is the end of duplicates, we must stop

                        previous = ref currentItem;

                        if (!tempDictionary.TryGetValue(currentItem, out var counter))
                        {
                            counter = new HeapCounter();
                            tempDictionary[currentItem] = counter;
                        }

                        counter.SourceCount++;
                        resultedRangeCurrent.length++;
                        c++;
                    }

                    // Add all incoming items in collision to result, and count them.
                    (int start, int length) resultedRangeIncoming = default;
                    resultedRangeIncoming.start = i;
                    do
                    {
                        ref readonly TValue incomingItem = ref range[i];

                        if (Compare(in incomingItem, in previous) != 0)
                            break; // this is the end of duplicates, we must stop

                        previous = ref incomingItem;

                        if (!tempDictionary.TryGetValue(incomingItem, out var counter))
                        {
                            counter = new HeapCounter();
                            tempDictionary[incomingItem] = counter;
                        }

                        counter.TargetCount++;
                        resultedRangeIncoming.length++;
                        i++;
                    }
                    while (i_InRange(i));

                    int addedCount = 0;
                    foreach (var item in tempDictionary)
                    {
                        int countToAdd = Math.Min(item.Value.SourceCount, item.Value.TargetCount);

                        for (int j = 0; j < countToAdd; j++)
                        {
                            destination[insertRIndex++] = item.Key;
                        }

                        addedCount += countToAdd;
                    }
                }

                void ProcessMergingBothComparable(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
                {
                    ref readonly TValue firstItem = ref _storage[c++]; i++; // they are equal here, so we can move both indexes
                    destination[insertRIndex++] = firstItem;

                    while (true)
                    {
                        int hasItemInRow = 0;

                        if (c_InRange(c) && Compare(in _storage[c], in firstItem) == 0)
                        {
                            hasItemInRow++; c++;
                        }

                        if (i_InRange(i) && Compare(in range[i], in firstItem) == 0)
                        {
                            hasItemInRow++; i++;
                        }

                        switch (hasItemInRow)
                        {
                            case 0:
                                return;
                            case 1:
                                return;
                            case 2:
                                destination[insertRIndex++] = firstItem;
                                continue;
                        }
                    }
                }
            }

            void ProcessMergingIncoming(ref int insertRIndex, ref int i, ReadOnlySpan<TValue> range)
            {
                if (ContainsCollisions)
                {
                    ProcessMergingIncomingDefault(ref insertRIndex, ref i, range);
                }
                else
                {
                    ProcessMergingIncomingComparable(ref insertRIndex, ref i, range);
                }

                void ProcessMergingIncomingDefault(ref int insertRIndex, ref int i, ReadOnlySpan<TValue> range)
                {
                    ref readonly TValue firstItem = ref range[i++];

                    while (i_InRange(i) && Compare(in range[i], in firstItem) == 0)
                    {
                        i++;
                    }
                }

                void ProcessMergingIncomingComparable(ref int insertRIndex, ref int i, ReadOnlySpan<TValue> range)
                {
                    ProcessMergingIncomingDefault(ref insertRIndex, ref i, range);
                }
            }

            void ProcessMergingCurrent(ref int insertRIndex, ref int c)
            {
                if (ContainsCollisions)
                {
                    ProcessMergingCurrentDefault(ref insertRIndex, ref c);
                }
                else
                {
                    ProcessMergingCurrentComparable(ref insertRIndex, ref c);
                }

                void ProcessMergingCurrentDefault(ref int insertRIndex, ref int c)
                {
                    ref readonly TValue firstItem = ref _storage[c++];

                    while (c_InRange(c) && Compare(in _storage[c], in firstItem) == 0)
                    {
                        c++;
                    }
                }

                void ProcessMergingCurrentComparable(ref int insertRIndex, ref int c)
                {
                    ProcessMergingCurrentDefault(ref insertRIndex, ref c);
                }
            }
        }

        private bool IntersectWithFromRight(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex;
            return IntersectWithFromRight(range, _storage, newZeroRIndex);
        }

        private bool IntersectWithFromRight(ReadOnlySpan<TValue> range, TValue[] destination, int newZeroRIndex)
        {
            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(newZeroRIndex == _zeroRIndex, "In-place merge requires place for the new range.");
            }

            // for local function usage (for optimizations) 
            Dictionary<TValue?, HeapCounter> tempDictionary = ContainsCollisions ? new() : null!;

            int incomingElementsSkipped = 0;
            int rangeLength = range.Length;
            int insertRIndex = newZeroRIndex + _count + range.Length - 1;
            int zeroIndexShift = 0;

            Debug.Assert(insertRIndex < destination.Length, "Right buffer is not long enough");

            bool i_InRange(int i) => i >= 0;
            bool c_InRange(int c) => c >= _zeroRIndex;

            // remember - we do care only about duplicates among incomings

            // General merge loop
            int i = range.Length - 1;
            int c = _zeroRIndex + _count - 1;

            while (i_InRange(i) && c_InRange(c))
            {
                ref readonly TValue incomingItem = ref range[i];
                ref readonly TValue currentItem = ref _storage[c];

                switch (Compare(in currentItem, in incomingItem))
                {
                    case > 0:
                        ProcessMergingCurrent(ref insertRIndex, ref c);
                        break;
                    case < 0:
                        ProcessMergingIncoming(ref insertRIndex, ref i, range);
                        break;
                    default:
                        ProcessMergingBoth(ref insertRIndex, ref i, ref c, range);
                        break;
                }
            }

            incomingElementsSkipped = (insertRIndex + 1) - newZeroRIndex;
            zeroIndexShift += incomingElementsSkipped;

            // Clear remaining free-spots
            if (ContainsReferences && incomingElementsSkipped > 0 && _storage == destination)
            {
                Array.Clear(_storage, newZeroRIndex, incomingElementsSkipped);
            }

            _zeroRIndex = newZeroRIndex + zeroIndexShift;
            _count += range.Length - incomingElementsSkipped;
            _version = unchecked(++_version);

            if (_count < 2)
                ModifyFlags(add: BinaryListFlags.NoDuplicates, remove: BinaryListFlags.UniformRange);

            return true;

            void ProcessMergingBoth(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                if (ContainsCollisions)
                {
                    ProcessMergingBothDefault(ref insertRIndex, ref i, ref c, range);
                }
                else
                {
                    ProcessMergingBothComparable(ref insertRIndex, ref i, ref c, range);
                }

                void ProcessMergingBothDefault(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
                {
                    tempDictionary.Clear();

                    (int start, int length) resultedRangeCurrent = default;
                    resultedRangeCurrent.start = c;

                    // Add the first item to result (preparing to compare)
                    ref readonly TValue previous = ref _storage[c--];

                    tempDictionary.Add(previous, new HeapCounter(sourceCount: 1));
                    resultedRangeCurrent.length++;

                    // Add all currently existing items in collision to result, and count them.
                    while (c_InRange(c))
                    {
                        ref readonly TValue currentItem = ref _storage[c];

                        if (Compare(in currentItem, in previous) != 0)
                            break; // this is the end of duplicates, we must stop

                        previous = ref currentItem;

                        if (!tempDictionary.TryGetValue(currentItem, out var counter))
                        {
                            counter = new HeapCounter();
                            tempDictionary[currentItem] = counter;
                        }

                        counter.SourceCount++;
                        resultedRangeCurrent.length++;
                        c--;
                    }

                    // Add all incoming items in collision to result, and count them.
                    (int start, int length) resultedRangeIncoming = default;
                    resultedRangeIncoming.start = i;
                    do
                    {
                        ref readonly TValue incomingItem = ref range[i];

                        if (Compare(in incomingItem, in previous) != 0)
                            break; // this is the end of duplicates, we must stop

                        previous = ref incomingItem;

                        if (!tempDictionary.TryGetValue(incomingItem, out var counter))
                        {
                            counter = new HeapCounter();
                            tempDictionary[incomingItem] = counter;
                        }

                        counter.TargetCount++;
                        resultedRangeIncoming.length++;
                        i--;
                    }
                    while (i_InRange(i));

                    int addedCount = 0;
                    foreach (var item in tempDictionary)
                    {
                        int countToAdd = Math.Min(item.Value.SourceCount, item.Value.TargetCount);

                        for (int j = 0; j < countToAdd; j++)
                        {
                            destination[insertRIndex--] = item.Key;
                        }

                        addedCount += countToAdd;
                    }
                }

                void ProcessMergingBothComparable(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
                {
                    ref readonly TValue firstItem = ref _storage[c--]; i--; // they are equal here, so we can move both indexes
                    destination[insertRIndex--] = firstItem;

                    while (true)
                    {
                        int hasItemInRow = 0;

                        if (c_InRange(c) && Compare(in _storage[c], in firstItem) == 0)
                        {
                            hasItemInRow++; c--;
                        }

                        if (i_InRange(i) && Compare(in range[i], in firstItem) == 0)
                        {
                            hasItemInRow++; i--;
                        }

                        switch (hasItemInRow)
                        {
                            case 0:
                                return;
                            case 1:
                                return;
                            case 2:
                                destination[insertRIndex--] = firstItem;
                                continue;
                        }
                    }
                }
            }
            
            void ProcessMergingIncoming(ref int insertRIndex, ref int i, ReadOnlySpan<TValue> range)
            {
                if (ContainsCollisions)
                {
                    ProcessMergingIncomingDefault(ref insertRIndex, ref i, range);
                }
                else
                {
                    ProcessMergingIncomingComparable(ref insertRIndex, ref i, range);
                }

                void ProcessMergingIncomingDefault(ref int insertRIndex, ref int i, ReadOnlySpan<TValue> range)
                {
                    ref readonly TValue firstItem = ref range[i--];

                    while (i_InRange(i) && Compare(in range[i], in firstItem) == 0)
                    {
                        i--;
                    }
                }

                void ProcessMergingIncomingComparable(ref int insertRIndex, ref int i, ReadOnlySpan<TValue> range)
                {
                    ProcessMergingIncomingDefault(ref insertRIndex, ref i, range);
                }
            }

            void ProcessMergingCurrent(ref int insertRIndex, ref int c)
            {
                if (ContainsCollisions)
                {
                    ProcessMergingCurrentDefault(ref insertRIndex, ref c);
                }
                else
                {
                    ProcessMergingCurrentComparable(ref insertRIndex, ref c);
                }

                void ProcessMergingCurrentDefault(ref int insertRIndex, ref int c)
                {
                    ref readonly TValue firstItem = ref _storage[c--];

                    while (c_InRange(c) && Compare(in _storage[c], in firstItem) == 0)
                    {
                        c--;
                    }
                }

                void ProcessMergingCurrentComparable(ref int insertRIndex, ref int c)
                {
                    ProcessMergingCurrentDefault(ref insertRIndex, ref c);
                }
            }
        }
    }
}
