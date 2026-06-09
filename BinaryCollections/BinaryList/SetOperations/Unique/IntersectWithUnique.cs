using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool IntersectWithUnique(ReadOnlySpan<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return IntersectWithSortedUnique(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool IntersectWithUnique(IReadOnlyCollection<TValue> range)
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
                return IntersectWithSortedUnique(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool IntersectWithUnique(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return IntersectWithUnique(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return IntersectWithUnique(collection);
            }

            List<TValue> temp = new List<TValue>();

            foreach (var item in range)
            {
                temp.Add(item);
            }

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return IntersectWithSortedUnique(span);
        }

        public bool IntersectWithUnique(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                return IntersectWithSortedUnique(range.GetSpan());
            }

            return IntersectWithUnique(range.GetSpan());
        }

        public bool IntersectWithUnique(BinaryList<TValue> range)
        {
            return IntersectWithUnique(range.GetEnumerator());
        }

        private bool IntersectWithSortedUnique(ReadOnlySpan<TValue> range)
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
                return IntersectWithFromLeftUnique(range);
            }

            if (RightBuffer >= LeftBuffer && RightBuffer >= range.Length)
            {
                return IntersectWithFromRightUnique(range);
            }

            return ResizeAndIntersectWithSortedUnique(range);
        }

        private bool ResizeAndIntersectWithSortedUnique(ReadOnlySpan<TValue> range)
        {
            PrepareForResize(range.Length, out var destination);

            int newZeroRIndex = CalculateZeroIndex(destination.Length, _count + range.Length);

            if (_storage == destination)
            {
                Array.Copy(destination, _zeroRIndex, destination, newZeroRIndex, _count);
                _zeroRIndex = newZeroRIndex;
                return IntersectWithFromRightUnique(range);
            }

            IntersectWithFromLeftUnique(range, destination, newZeroRIndex);

            _storage = destination;
            return true;
        }

        private bool IntersectWithFromLeftUnique(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex - range.Length;
            return IntersectWithFromLeftUnique(range, _storage, newZeroRIndex);
        }

        private bool IntersectWithFromLeftUnique(ReadOnlySpan<TValue> range, TValue[] destination, int newZeroRIndex)
        {
            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(_zeroRIndex - newZeroRIndex == range.Length, "In-place merge requires place for the new range.");
            }

            // for local function usage (for optimizations) 
            Dictionary<TValue?, ItemCounter> tempDictionary = ContaninsCollisions ? new() : null!;

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

            // Manage remaining items
            if (i_InRange(i))
            {
                incomingElementsSkipped += range.Length - i;
            }
            else if (c_InRange(c))
            {
                incomingElementsSkipped += _zeroRIndex + _count - c;
            }

            // Clear remaining free-spots
            if (ContainsReferences && incomingElementsSkipped > 0 && _storage == destination)
            {
                Array.Clear(_storage, insertRIndex, incomingElementsSkipped);
            }

            _zeroRIndex = newZeroRIndex + zeroIndexShift;
            _count += range.Length - incomingElementsSkipped;
            _version = unchecked(++_version);
            return true;

            void ProcessMergingBoth(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                if (ContaninsCollisions)
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

                    tempDictionary.Add(previous, new ItemCounter(sourceCount: 1));
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
                            counter = new ItemCounter();
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
                            counter = new ItemCounter();
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
                        if (item.Value.TargetCount > 0 && item.Value.SourceCount > 0)
                        {
                            addedCount++;
                            destination[insertRIndex++] = item.Key;
                        }
                    }

                    incomingElementsSkipped += resultedRangeCurrent.length + resultedRangeIncoming.length - addedCount;
                }

                void ProcessMergingBothComparable(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
                {
                    ref readonly TValue firstItem = ref _storage[c++]; i++; // they are equal here, so we can move both indexes
                    incomingElementsSkipped++;
                    destination[insertRIndex++] = firstItem;

                    while (c_InRange(c) && Compare(in _storage[c], in firstItem) == 0)
                    {
                        incomingElementsSkipped++;
                        c++;
                    }

                    while (i_InRange(i) && Compare(in range[i], in firstItem) == 0)
                    {
                        incomingElementsSkipped++;
                        i++;
                    }
                }
            }

            void ProcessMergingIncoming(ref int insertRIndex, ref int i, ReadOnlySpan<TValue> range)
            {
                if (ContaninsCollisions)
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
                    incomingElementsSkipped++;

                    while (i_InRange(i) && Compare(in range[i], in firstItem) == 0)
                    {
                        incomingElementsSkipped++; i++;
                    }
                }

                void ProcessMergingIncomingComparable(ref int insertRIndex, ref int i, ReadOnlySpan<TValue> range)
                {
                    ProcessMergingIncomingDefault(ref insertRIndex, ref i, range);
                }
            }

            void ProcessMergingCurrent(ref int insertRIndex, ref int c)
            {
                if (ContaninsCollisions)
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
                    incomingElementsSkipped++;

                    while (c_InRange(c) && Compare(in _storage[c], in firstItem) == 0)
                    {
                        incomingElementsSkipped++; c++;
                    }
                }

                void ProcessMergingCurrentComparable(ref int insertRIndex, ref int c)
                {
                    ProcessMergingCurrentDefault(ref insertRIndex, ref c);
                }
            }
        }

        private bool IntersectWithFromRightUnique(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex;
            return IntersectWithFromRightUnique(range, _storage, newZeroRIndex);
        }

        private bool IntersectWithFromRightUnique(ReadOnlySpan<TValue> range, TValue[] destination, int newZeroRIndex)
        {
            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(newZeroRIndex == _zeroRIndex, "In-place merge requires place for the new range.");
            }

            // for local function usage (for optimizations) 
            Dictionary<TValue?, ItemCounter> tempDictionary = ContaninsCollisions ? new() : null!;

            int incomingElementsSkipped = 0;
            int rangeLength = range.Length;
            int insertRIndex = newZeroRIndex + _count + range.Length - 1;
            int zeroIndexShift = 0;

            Debug.Assert(insertRIndex < destination.Length, "Right buffer is not long enough");

            bool i_InRange(int i) => i >= 0;
            bool c_InRange(int c) => c >= _zeroRIndex;

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

            // Manage remaining items
            if (i_InRange(i))
            {
                incomingElementsSkipped += i + 1;
            }
            else if (c_InRange(c))
            {
                incomingElementsSkipped += c + 1 - _zeroRIndex;
            }

            // Clear remaining free-spots
            if (ContainsReferences && incomingElementsSkipped > 0 && _storage == destination)
            {
                Array.Clear(_storage, newZeroRIndex, incomingElementsSkipped);
            }

            zeroIndexShift += incomingElementsSkipped;

            _zeroRIndex = newZeroRIndex + zeroIndexShift;
            _count += range.Length - incomingElementsSkipped;
            _version = unchecked(++_version);
            return true;

            void ProcessMergingBoth(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                if (ContaninsCollisions)
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

                    tempDictionary.Add(previous, new ItemCounter(sourceCount: 1));
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
                            counter = new ItemCounter();
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
                            counter = new ItemCounter();
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
                        if (item.Value.TargetCount > 0 && item.Value.SourceCount > 0)
                        {
                            addedCount++;
                            destination[insertRIndex--] = item.Key;
                        }
                    }

                    incomingElementsSkipped += resultedRangeCurrent.length + resultedRangeIncoming.length - addedCount;
                }

                void ProcessMergingBothComparable(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
                {
                    ref readonly TValue firstItem = ref _storage[c--]; i--; // they are equal here, so we can move both indexes
                    incomingElementsSkipped++;
                    destination[insertRIndex--] = firstItem;

                    while (c_InRange(c) && Compare(in _storage[c], in firstItem) == 0)
                    {
                        incomingElementsSkipped++;
                        c--;
                    }

                    while (i_InRange(i) && Compare(in range[i], in firstItem) == 0)
                    {
                        incomingElementsSkipped++;
                        i--;
                    }
                }
            }

            void ProcessMergingIncoming(ref int insertRIndex, ref int i, ReadOnlySpan<TValue> range)
            {
                if (ContaninsCollisions)
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
                    incomingElementsSkipped++;

                    while (i_InRange(i) && Compare(in range[i], in firstItem) == 0)
                    {
                        incomingElementsSkipped++; i--;
                    }
                }

                void ProcessMergingIncomingComparable(ref int insertRIndex, ref int i, ReadOnlySpan<TValue> range)
                {
                    ProcessMergingIncomingDefault(ref insertRIndex, ref i, range);
                }
            }

            void ProcessMergingCurrent(ref int insertRIndex, ref int c)
            {
                if (ContaninsCollisions)
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
                    incomingElementsSkipped++;

                    while (c_InRange(c) && Compare(in _storage[c], in firstItem) == 0)
                    {
                        incomingElementsSkipped++; c--;
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
