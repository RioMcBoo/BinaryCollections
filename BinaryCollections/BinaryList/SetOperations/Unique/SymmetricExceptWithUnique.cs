using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ExceptionHelper;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool SymmetricExceptWithUnique(ReadOnlySpan<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return SymmetricExceptWithSortedUnique(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool SymmetricExceptWithUnique(IReadOnlyCollection<TValue> range)
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
                return SymmetricExceptWithSortedUnique(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool SymmetricExceptWithUnique(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return SymmetricExceptWithUnique(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return SymmetricExceptWithUnique(collection);
            }

            List<TValue> temp = new List<TValue>();

            foreach (var item in range)
            {
                temp.Add(item);
            }

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return SymmetricExceptWithSortedUnique(span);
        }

        public bool SymmetricExceptWithUnique(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                return SymmetricExceptWithSortedUnique(range.GetSpan());
            }

            return SymmetricExceptWithUnique(range.GetSpan());
        }

        public bool SymmetricExceptWithUnique(BinaryList<TValue> range)
        {
            return SymmetricExceptWithUnique(range.GetEnumerator());
        }

        private bool SymmetricExceptWithSortedUnique(ReadOnlySpan<TValue> range)
        {
            if (range.Length <= 0 && _count <= 0)
                return false;

            if (range.Length <= 0)
                return false;

            if (!CheckCountOverload(range.Length))
            {
                if (!SymmetricExceptWithSortedUniqueSafeTest(range))
                    Throw.CollectionOverloadException(nameof(SymmetricExceptWithSortedUnique));
            }

            if (LeftBuffer >= RightBuffer && LeftBuffer >= range.Length)
            {
                return SymmetricExceptWithFromLeftUnique(range);
            }

            if (RightBuffer >= LeftBuffer && RightBuffer >= range.Length)
            {
                return SymmetricExceptWithFromRightUnique(range);
            }

            return ResizeAndSymmetricExceptWithSortedUnique(range);
        }

        private bool SymmetricExceptWithSortedUniqueSafeTest(ReadOnlySpan<TValue> range)
        {
            throw new NotImplementedException();
        }

        private bool ResizeAndSymmetricExceptWithSortedUnique(ReadOnlySpan<TValue> range)
        {
            PrepareForResize(range.Length, out var destination);

            int newZeroRIndex = CalculateZeroIndex(destination.Length, _count + range.Length);

            if (_storage == destination)
            {
                Array.Copy(destination, _zeroRIndex, destination, newZeroRIndex, _count);
                _zeroRIndex = newZeroRIndex;
                return SymmetricExceptWithFromRightUnique(range);
            }

            SymmetricExceptWithFromLeftUnique(range, destination, newZeroRIndex);

            _storage = destination;
            return true;
        }

        private bool SymmetricExceptWithFromLeftUnique(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex - range.Length;
            return SymmetricExceptWithFromLeftUnique(range, _storage, newZeroRIndex);
        }

        private bool SymmetricExceptWithFromLeftUnique(ReadOnlySpan<TValue> range, TValue[] destination, int newZeroRIndex)
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

            // Manage remaining items
            if (i_InRange(i))
            {
                do ProcessMergingIncoming(ref insertRIndex, ref i, range);
                while (i_InRange(i));
            }
            else if (c_InRange(c))
            {
                do ProcessMergingCurrent(ref insertRIndex, ref c);
                while (c_InRange(c));
            }

            // Clear remaining free-spots
            if (ContainsReferences && incomingElementsSkipped > 0 && _storage == destination)
            {
                Array.Clear(_storage, insertRIndex, incomingElementsSkipped);
            }

            _zeroRIndex = newZeroRIndex + zeroIndexShift;
            _count += range.Length - incomingElementsSkipped;
            _version = unchecked(++_version);
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
                        if (item.Value.TargetCount == 0 || item.Value.SourceCount == 0)
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
                    incomingElementsSkipped+=2;

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
                    tempDictionary.Clear();

                    (int start, int length) resultedRangeIncoming = default;
                    resultedRangeIncoming.start = i;

                    // Add the first item to result (preparing to compare)
                    ref readonly TValue previous = ref range[i++];

                    tempDictionary.Add(previous, new HeapCounter(targetCount: 1));
                    resultedRangeIncoming.length++;

                    // Add all incoming items in collision to result, and count them.
                    while (i_InRange(i))
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

                    int addedCount = 0;
                    foreach (var item in tempDictionary)
                    {
                        int countToAdd = 1;

                        for (int j = 0; j < countToAdd; j++)
                        {
                            destination[insertRIndex++] = item.Key;
                        }

                        addedCount += countToAdd;
                    }

                    incomingElementsSkipped += resultedRangeIncoming.length - addedCount;
                }

                void ProcessMergingIncomingComparable(ref int insertRIndex, ref int i, ReadOnlySpan<TValue> range)
                {
                    ref readonly TValue firstItem = ref range[i++];
                    destination[insertRIndex++] = firstItem;

                    while (i_InRange(i) && Compare(in range[i], in firstItem) == 0)
                    {
                        incomingElementsSkipped++;
                        i++;
                    }
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
                    tempDictionary.Clear();

                    (int start, int length) resultedRangeCurrent = default;
                    resultedRangeCurrent.start = c;

                    // Add the first item to result (preparing to compare)
                    ref readonly TValue previous = ref _storage[c++];

                    tempDictionary.Add(previous, new HeapCounter(sourceCount: 1));
                    resultedRangeCurrent.length++;

                    // Add all currently existing items in collision to result, and count them..
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

                    int addedCount = 0;
                    foreach (var item in tempDictionary)
                    {
                        int countToAdd = 1;

                        for (int j = 0; j < countToAdd; j++)
                        {
                            destination[insertRIndex++] = item.Key;
                        }

                        addedCount += countToAdd;
                    }

                    incomingElementsSkipped += resultedRangeCurrent.length - addedCount;
                }

                void ProcessMergingCurrentComparable(ref int insertRIndex, ref int c)
                {
                    ref readonly TValue firstItem = ref _storage[c++];
                    destination[insertRIndex++] = firstItem;

                    while (c_InRange(c) && Compare(in _storage[c], in firstItem) == 0)
                    {
                        incomingElementsSkipped++;
                        c++;
                    }
                }
            }
        }

        private bool SymmetricExceptWithFromRightUnique(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex;
            return SymmetricExceptWithFromRightUnique(range, _storage, newZeroRIndex);
        }

        private bool SymmetricExceptWithFromRightUnique(ReadOnlySpan<TValue> range, TValue[] destination, int newZeroRIndex)
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
                do ProcessMergingIncoming(ref insertRIndex, ref i, range);
                while (i_InRange(i));
            }
            else if (c_InRange(c))
            {
                do ProcessMergingCurrent(ref insertRIndex, ref c);
                while (c_InRange(c));
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
                        if (item.Value.TargetCount == 0 || item.Value.SourceCount == 0)
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
                    incomingElementsSkipped += 2;

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
                    tempDictionary.Clear();

                    (int start, int length) resultedRangeIncoming = default;
                    resultedRangeIncoming.start = i;

                    // Add the first item to result (preparing to compare)
                    ref readonly TValue previous = ref range[i--];

                    tempDictionary.Add(previous, new HeapCounter(targetCount: 1));
                    resultedRangeIncoming.length++;

                    // Add all incoming items in collision to result, and count them.
                    while (i_InRange(i))
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

                    int addedCount = 0;
                    foreach (var item in tempDictionary)
                    {
                        int countToAdd = 1;

                        for (int j = 0; j < countToAdd; j++)
                        {
                            destination[insertRIndex--] = item.Key;
                        }

                        addedCount += countToAdd;
                    }

                    incomingElementsSkipped += resultedRangeIncoming.length - addedCount;
                }

                void ProcessMergingIncomingComparable(ref int insertRIndex, ref int i, ReadOnlySpan<TValue> range)
                {
                    ref readonly TValue firstItem = ref range[i--];
                    destination[insertRIndex--] = firstItem;

                    while (i_InRange(i) && Compare(in range[i], in firstItem) == 0)
                    {
                        incomingElementsSkipped++;
                        i--;
                    }
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
                    tempDictionary.Clear();

                    (int start, int length) resultedRangeCurrent = default;
                    resultedRangeCurrent.start = c;

                    // Add the first item to result (preparing to compare)
                    ref readonly TValue previous = ref _storage[c--];

                    tempDictionary.Add(previous, new HeapCounter(sourceCount: 1));
                    resultedRangeCurrent.length++;

                    // Add all currently existing items in collision to result, and count them..
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

                    int addedCount = 0;
                    foreach (var item in tempDictionary)
                    {
                        int countToAdd = 1;

                        for (int j = 0; j < countToAdd; j++)
                        {
                            destination[insertRIndex--] = item.Key;
                        }

                        addedCount += countToAdd;
                    }

                    incomingElementsSkipped += resultedRangeCurrent.length - addedCount;
                }

                void ProcessMergingCurrentComparable(ref int insertRIndex, ref int c)
                {
                    ref readonly TValue firstItem = ref _storage[c--];
                    destination[insertRIndex--] = firstItem;

                    while (c_InRange(c) && Compare(in _storage[c], in firstItem) == 0)
                    {
                        incomingElementsSkipped++;
                        c--;
                    }
                }
            }
        }
    }
}
