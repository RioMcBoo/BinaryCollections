using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool AddRangeUnique(ReadOnlySpan<TValue> range)
        {
            if (!CheckAddRange(range.Length))
                return false;

            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return AddSortedRangeUnique(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool AddRangeUnique(IReadOnlyCollection<TValue> range)
        {
            if (!CheckAddRange(range.Count))
                return false;

            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Count);
            try
            {
                int i = 0;
                foreach (var item in range)
                {
                    temp[i++] = item;
                }

                Array.Sort(temp, 0, range.Count, Comparer);
                return AddSortedRangeUnique(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool AddRangeUnique(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return AddRangeUnique(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return AddRangeUnique(collection);
            }

            List<TValue> temp = new List<TValue>();

            foreach (var item in range)
            {
                temp.Add(item);
            }

            if (!CheckAddRange(temp.Count))
                return false;

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return AddSortedRangeUnique(span);
        }

        public bool AddRangeUnique(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                if (!CheckAddRange(range.Count))
                    return false;

                if (range.Flags.HasFlag(BinaryListFlags.UniformRange))
                {
                    return AddUniformRangeUnique(range.GetSpan());
                }

                return AddSortedRangeUnique(range.GetSpan());
            }

            return AddRangeUnique(range.GetSpan());
        }

        public bool AddRangeUnique(BinaryList<TValue> range)
        {
            return AddRangeUnique(range.GetEnumerator());
        }

        private bool AddUniformRangeUnique(ReadOnlySpan<TValue> range)
        {
            ref readonly var example = ref range[0];
            return AddUnique(in example, out _);
        }

        private bool AddSortedRangeUnique(ReadOnlySpan<TValue> range)
        {
            if (LeftBuffer >= RightBuffer && LeftBuffer >= range.Length)
            {
                return InsertSortedRangeFromLeftUnique(range);
            }

            if (RightBuffer >= LeftBuffer && RightBuffer >= range.Length)
            {
                return InsertSortedRangeFromRightUnique(range);
            }

            return ResizeAndInsertSortedRangeUnique(range);
        }

        private bool ResizeAndInsertSortedRangeUnique(ReadOnlySpan<TValue> range)
        {
            PrepareForResize(range.Length, out var destination);

            int newZeroRIndex = CalculateZeroIndex(destination.Length, _count + range.Length);

            if (_storage == destination)
            {
                Array.Copy(destination, _zeroRIndex, destination, newZeroRIndex, _count);
                _zeroRIndex = newZeroRIndex;
                return InsertSortedRangeFromRightUnique(range);
            }

            InsertSortedRangeFromLeftUnique(range, destination, newZeroRIndex);

            _storage = destination;
            return true;
        }

        private bool InsertSortedRangeFromLeftUnique(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex - range.Length;
            return InsertSortedRangeFromLeftUnique(range, _storage, newZeroRIndex);
        }

        private bool InsertSortedRangeFromLeftUnique(ReadOnlySpan<TValue> range, TValue[] destination, int newZeroRIndex)
        {
            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(_zeroRIndex - newZeroRIndex == range.Length, "In-place merge requires place for the new range.");
            }

            // for local function usage (for optimizations) 
            Dictionary<ItemHashCounter, ItemHashCounter> tempDictionary = ContaninsCollisions ? new() : null!;

            int incomingElementsSkipped = 0;
            int rangeLength = range.Length;
            int insertRIndex = newZeroRIndex;
            int zeroIndexShift = 0;

            Debug.Assert(insertRIndex > -1, "Left buffer is not long enough");

            bool i_InRange(int i) => i < rangeLength;
            bool c_InRange(int c) => c < _zeroRIndex + _count;

            // remember - we do care only about duplicates among incomings

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

                // Clear remaining free-spots
                if (ContainsReferences && incomingElementsSkipped > 0 && _storage == destination)
                {
                    Array.Clear(_storage, insertRIndex, incomingElementsSkipped);
                }
            }
            else if (c_InRange(c))
            {
                int finishedPartLength = insertRIndex - newZeroRIndex;
                int remainingPartLength = _count + range.Length - incomingElementsSkipped - finishedPartLength;

                if (_storage != destination)
                {
                    // we just have to copy remaining items into new array
                    Array.Copy(_storage, c, destination, insertRIndex, remainingPartLength);
                }
                else if (incomingElementsSkipped > 0)
                {
                    if (finishedPartLength >= remainingPartLength)
                    {
                        // Shift remaining items due to missing duplicates
                        Array.Copy(_storage, c, _storage, insertRIndex, remainingPartLength);

                        // Clear formed dirty free-spots from right side
                        if (ContainsReferences)
                        {
                            Array.Clear(_storage, newZeroRIndex + finishedPartLength + remainingPartLength, incomingElementsSkipped);
                        }
                    }
                    else
                    {
                        // Shift finished items due to missing duplicates
                        Array.Copy(_storage, newZeroRIndex, _storage, newZeroRIndex + incomingElementsSkipped, finishedPartLength);
                        zeroIndexShift += incomingElementsSkipped;

                        // Clear formed dirty free-spots from left side
                        if (ContainsReferences)
                        {
                            Array.Clear(_storage, newZeroRIndex, incomingElementsSkipped);
                        }
                    }
                }
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
                    destination[insertRIndex++] = previous;
                    var previousCounter = new ItemHashCounter(previous);
                    previousCounter.SourceCount++;
                    tempDictionary.Add(previousCounter, previousCounter);
                    resultedRangeCurrent.length++;

                    // Add all currently existing items in collision to result, and count them.
                    while (c_InRange(c))
                    {
                        ref readonly TValue currentItem = ref _storage[c];

                        if (Compare(in currentItem, in previous) != 0)
                            break; // this is the end of duplicates, we must stop

                        previous = ref currentItem;
                        var counter = new ItemHashCounter(currentItem);

                        if (tempDictionary.ContainsKey(counter))
                            counter = tempDictionary[counter];
                        else
                            tempDictionary[counter] = counter;

                        counter.SourceCount++;
                        resultedRangeCurrent.length++;
                        destination[insertRIndex++] = currentItem;
                        c++;
                    }

                    // Add all incoming items in collision to result, and count them.
                    (int start, int length) resultedRangeIncoming = default;
                    resultedRangeIncoming.start = i;
                    while (i_InRange(i))
                    {
                        ref readonly TValue incomingItem = ref range[i];

                        if (Compare(in incomingItem, in previous) != 0)
                            break; // this is the end of duplicates, we must stop

                        previous = ref incomingItem;
                        var counter = new ItemHashCounter(incomingItem);

                        if (tempDictionary.ContainsKey(counter))
                            counter = tempDictionary[counter];
                        else
                            tempDictionary[counter] = counter;

                        counter.TargetCount++;
                        resultedRangeIncoming.length++;
                        i++;
                    }

                    int addedCount = 0;
                    foreach (var item in tempDictionary.Values)
                    {
                        int countToAdd = 0;

                        if (item.SourceCount > 0)
                        {
                            countToAdd = item.SourceCount;
                        }
                        else
                        {
                            countToAdd = 1;
                            destination[insertRIndex++] = item.Item;
                        }

                        addedCount += countToAdd;
                    }

                    incomingElementsSkipped += resultedRangeCurrent.length + resultedRangeIncoming.length - addedCount;
                }

                void ProcessMergingBothComparable(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
                {
                    ref readonly TValue firstItem = ref _storage[c++]; i++; // they are equal here, so we can move both indexes
                    incomingElementsSkipped++;
                    destination[insertRIndex++] = firstItem;

                    while (c_InRange(c))
                    {
                        ref readonly TValue currentItem = ref _storage[c];
                        if (Compare(in _storage[c], in firstItem) == 0)
                        {
                            destination[insertRIndex++] = currentItem;
                            c++;
                        }
                        else
                        {
                            break;
                        }
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
                    tempDictionary.Clear();

                    (int start, int length) resultedRangeIncoming = default;
                    resultedRangeIncoming.start = i;

                    // Add the first item to result (preparing to compare)
                    ref readonly TValue previous = ref range[i++];

                    var previousCounter = new ItemHashCounter(previous);
                    previousCounter.TargetCount++;
                    tempDictionary.Add(previousCounter, previousCounter);
                    resultedRangeIncoming.length++;

                    // Add all incoming items in collision to result, and count them.
                    while (i_InRange(i))
                    {
                        ref readonly TValue incomingItem = ref range[i];

                        if (Compare(in incomingItem, in previous) != 0)
                            break; // this is the end of duplicates, we must stop

                        previous = ref incomingItem;
                        var counter = new ItemHashCounter(incomingItem);

                        if (tempDictionary.ContainsKey(counter))
                            counter = tempDictionary[counter];
                        else
                            tempDictionary[counter] = counter;

                        counter.TargetCount++;
                        resultedRangeIncoming.length++;
                        i++;
                    }

                    int addedCount = 0;
                    foreach (var item in tempDictionary.Values)
                    {
                        int countToAdd = 1;

                        for (int j = 0; j < countToAdd; j++)
                        {
                            destination[insertRIndex++] = item.Item;
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
                ref readonly TValue firstItem = ref _storage[c++];
                destination[insertRIndex++] = firstItem;

                while (c_InRange(c))
                {
                    ref readonly TValue currentItem = ref _storage[c];
                    if (Compare(in _storage[c], in firstItem) == 0)
                    {
                        destination[insertRIndex++] = currentItem;
                        c++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private bool InsertSortedRangeFromRightUnique(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex;
            return InsertSortedRangeFromRightUnique(range, _storage, newZeroRIndex);
        }

        private bool InsertSortedRangeFromRightUnique(ReadOnlySpan<TValue> range, TValue[] destination, int newZeroRIndex)
        {
            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(newZeroRIndex == _zeroRIndex, "In-place merge requires place for the new range.");
            }
            
            // for local function usage (for optimizations) 
            Dictionary<ItemHashCounter, ItemHashCounter> tempDictionary = ContaninsCollisions ? new() : null!;

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

            // Manage remaining items
            if (i_InRange(i))
            {
                do ProcessMergingIncoming(ref insertRIndex, ref i, range);
                while (i_InRange(i));

                // Clear remaining free-spots
                if (ContainsReferences && incomingElementsSkipped > 0 && _storage == destination)
                {
                    Array.Clear(_storage, newZeroRIndex, incomingElementsSkipped);
                }
            }
            else if (c_InRange(c))
            {
                int remainingPartLength = c - _zeroRIndex + 1;
                int finishedPartLength = _count + range.Length - incomingElementsSkipped - remainingPartLength;

                if (_storage != destination)
                {
                    // we just have to copy remaining items into new array
                    Array.Copy(_storage, _zeroRIndex, destination, newZeroRIndex + incomingElementsSkipped, remainingPartLength);
                    zeroIndexShift += incomingElementsSkipped;
                }
                else if (incomingElementsSkipped > 0)
                {
                    if (finishedPartLength >= remainingPartLength)
                    {
                        // Shift remaining items due to missing duplicates
                        Array.Copy(_storage, _zeroRIndex, _storage, newZeroRIndex + incomingElementsSkipped, remainingPartLength);
                        zeroIndexShift += incomingElementsSkipped;

                        // Clear formed dirty free-spots from left side
                        if (ContainsReferences)
                        {
                            Array.Clear(_storage, newZeroRIndex, incomingElementsSkipped);
                        }
                    }
                    else
                    {
                        // Shift finished items due to missing duplicates
                        Array.Copy(_storage, insertRIndex + 1, _storage, insertRIndex + 1 - incomingElementsSkipped, finishedPartLength);

                        // Clear formed dirty free-spots from right side
                        if (ContainsReferences)
                        {
                            Array.Clear(_storage, newZeroRIndex + finishedPartLength + remainingPartLength, incomingElementsSkipped);
                        }
                    }
                }
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
                    ref readonly TValue previous = ref _storage[c--];
                    destination[insertRIndex--] = previous;
                    var previousCounter = new ItemHashCounter(previous);
                    previousCounter.SourceCount++;
                    tempDictionary.Add(previousCounter, previousCounter);
                    resultedRangeCurrent.length++;

                    // Add all currently existing items in collision to result, and count them.
                    while (c_InRange(c))
                    {
                        ref readonly TValue currentItem = ref _storage[c];

                        if (Compare(in currentItem, in previous) != 0)
                            break; // this is the end of duplicates, we must stop

                        previous = ref currentItem;
                        var counter = new ItemHashCounter(currentItem);

                        if (tempDictionary.ContainsKey(counter))
                            counter = tempDictionary[counter];
                        else
                            tempDictionary[counter] = counter;

                        counter.SourceCount++;
                        resultedRangeCurrent.length++;
                        destination[insertRIndex--] = currentItem;
                        c--;
                    }

                    // Add all incoming items in collision to result, and count them.
                    (int start, int length) resultedRangeIncoming = default;
                    resultedRangeIncoming.start = i;
                    while (i_InRange(i))
                    {
                        ref readonly TValue incomingItem = ref range[i];

                        if (Compare(in incomingItem, in previous) != 0)
                            break; // this is the end of duplicates, we must stop

                        previous = ref incomingItem;
                        var counter = new ItemHashCounter(incomingItem);

                        if (tempDictionary.ContainsKey(counter))
                            counter = tempDictionary[counter];
                        else
                            tempDictionary[counter] = counter;

                        counter.TargetCount++;
                        resultedRangeIncoming.length++;
                        i--;
                    }

                    int addedCount = 0;
                    foreach (var item in tempDictionary.Values)
                    {
                        int countToAdd = 0;

                        if (item.SourceCount > 0)
                        {
                            countToAdd = item.SourceCount;
                        }
                        else
                        {
                            countToAdd = 1;
                            destination[insertRIndex--] = item.Item;
                        }

                        addedCount += countToAdd;
                    }

                    incomingElementsSkipped += resultedRangeCurrent.length + resultedRangeIncoming.length - addedCount;
                }

                void ProcessMergingBothComparable(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
                {
                    ref readonly TValue firstItem = ref _storage[c--]; i--; // they are equal here, so we can move both indexes
                    incomingElementsSkipped++;
                    destination[insertRIndex--] = firstItem;

                    while (c_InRange(c))
                    {
                        ref readonly TValue currentItem = ref _storage[c];
                        if (Compare(in _storage[c], in firstItem) == 0)
                        {
                            destination[insertRIndex--] = currentItem;
                            c--;
                        }
                        else
                        {
                            break;
                        }
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
                    tempDictionary.Clear();

                    (int start, int length) resultedRangeIncoming = default;
                    resultedRangeIncoming.start = i;

                    // Add the first item to result (preparing to compare)
                    ref readonly TValue previous = ref range[i--];

                    var previousCounter = new ItemHashCounter(previous);
                    previousCounter.TargetCount++;
                    tempDictionary.Add(previousCounter, previousCounter);
                    resultedRangeIncoming.length++;

                    // Add all incoming items in collision to result, and count them.
                    while (i_InRange(i))
                    {
                        ref readonly TValue incomingItem = ref range[i];

                        if (Compare(in incomingItem, in previous) != 0)
                            break; // this is the end of duplicates, we must stop

                        previous = ref incomingItem;
                        var counter = new ItemHashCounter(incomingItem);

                        if (tempDictionary.ContainsKey(counter))
                            counter = tempDictionary[counter];
                        else
                            tempDictionary[counter] = counter;

                        counter.TargetCount++;
                        resultedRangeIncoming.length++;
                        i--;
                    }

                    int addedCount = 0;
                    foreach (var item in tempDictionary.Values)
                    {
                        int countToAdd = 1;

                        for (int j = 0; j < countToAdd; j++)
                        {
                            destination[insertRIndex--] = item.Item;
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
                ref readonly TValue firstItem = ref _storage[c--];
                destination[insertRIndex--] = firstItem;

                while (c_InRange(c))
                {
                    ref readonly TValue currentItem = ref _storage[c];
                    if (Compare(in _storage[c], in firstItem) == 0)
                    {
                        destination[insertRIndex--] = currentItem;
                        c--;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}
