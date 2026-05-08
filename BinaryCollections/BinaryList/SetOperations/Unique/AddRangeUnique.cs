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
            if (range.Comparer == Comparer)
            {
                if (!CheckAddRange(range.Count))
                    return false;

                if (range.Flags.HasFlag(BinaryListEnumeratorFlags.UniformRange))
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

                switch (Comparer.Compare(currentItem, incomingItem))
                {
                    case > 0:
                        ProcessMergingIncoming(ref insertRIndex, ref i, range);
                        break;
                    case < 0:
                        ProcessMergingCurrent(ref insertRIndex, ref c);
                        break;
                    case 0:
                        ProcessMergingDuplicates(ref insertRIndex, ref i, ref c, range);
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

            void ProcessMergingDuplicates(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                (int start, int length, int index) resultedRange = default;
                resultedRange.start = insertRIndex;

                // Add the first item to result (preparing to compare)
                ref readonly TValue previous = ref _storage[c++];
                destination[insertRIndex++] = previous;
                resultedRange.length++;

                while (c_InRange(c)) // Add all currently existing items in collision to result, and count them.
                {  // (we do care only about duplicates from incoming)
                    ref readonly TValue existedItem = ref _storage[c];

                    if (Comparer.Compare(existedItem, previous) != 0)
                        break; // this is the end of duplicates, we must stop

                    previous = ref existedItem;
                    destination[insertRIndex++] = existedItem;
                    resultedRange.length++;

                    c++;
                }

                do // Add incoming items if they are not added yet
                {
                    bool alreadyAdded = false;
                    ref readonly TValue incomingItem = ref range[i];

                    if (Comparer.Compare(incomingItem, previous) != 0)
                        return; // this is the end of duplicates, we must stop

                    if (_mayContainCollisions)
                    {
                        resultedRange.index = resultedRange.start;

                        do // Look for existing item in collisions
                        {
                            ref readonly TValue currentItem = ref destination[resultedRange.index++];

                            if (_equalityComparer.Equals(currentItem, incomingItem))
                            {
                                alreadyAdded = true;
                                break; // this item is already in result, we must stop
                            }

                        }
                        while (resultedRange.index < resultedRange.start + resultedRange.length);
                    }
                    else
                    {
                        alreadyAdded = true;
                    }

                    if (!alreadyAdded)
                    {
                        destination[insertRIndex++] = incomingItem;
                        resultedRange.length++;
                    }
                    else
                    {
                        incomingElementsSkipped++;
                    }

                    i++;
                }
                while (i_InRange(i));
            }

            void ProcessMergingIncoming(ref int insertRIndex, ref int i, ReadOnlySpan<TValue> range)
            {
                (int start, int length, int index) resultedRange = default;
                resultedRange.start = insertRIndex;

                // Add the first item to result (preparing to compare)
                ref readonly TValue previous = ref range[i++];
                destination[insertRIndex++] = previous;
                resultedRange.length++;

                while (i_InRange(i)) // Add incoming items if they are not added yet
                {
                    bool alreadyAdded = false;
                    ref readonly TValue incomingItem = ref range[i];

                    if (Comparer.Compare(incomingItem, previous) != 0)
                        return; // this is the end of duplicates, we must stop

                    previous = ref incomingItem;

                    if (_mayContainCollisions)
                    {
                        resultedRange.index = resultedRange.start;

                        do // Look for existing item in collisions
                        {
                            ref readonly TValue currentItem = ref destination[resultedRange.index++];

                            if (_equalityComparer.Equals(currentItem, incomingItem))
                            {
                                alreadyAdded = true;
                                break; // this item is already in result, we must stop
                            }

                        }
                        while (resultedRange.index < resultedRange.start + resultedRange.length);
                    }
                    else
                    {
                        alreadyAdded = true;
                    }

                    if (!alreadyAdded)
                    {
                        destination[insertRIndex++] = incomingItem;
                        resultedRange.length++;
                    }
                    else
                    {
                        incomingElementsSkipped++;
                    }

                    i++;
                }
            }

            void ProcessMergingCurrent(ref int insertRIndex, ref int c)
            {
                destination[insertRIndex++] = _storage[c++];
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

                switch (Comparer.Compare(currentItem, incomingItem))
                {
                    case > 0:
                        ProcessMergingCurrent(ref insertRIndex, ref c);
                        break;
                    case < 0:
                        ProcessMergingIncoming(ref insertRIndex, ref i, range);
                        break;
                    default:
                        ProcessMergingDuplicates(ref insertRIndex, ref i, ref c, range);
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

            void ProcessMergingDuplicates(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                (int start, int length, int index) resultedRange = default;
                resultedRange.start = insertRIndex;

                // Add the first item to result (preparing to compare)
                ref readonly TValue previous = ref _storage[c--];
                destination[insertRIndex--] = previous;
                resultedRange.length++;

                while (c_InRange(c)) // Add all currently existing items in collision to result, and count them.
                {  // (we do care only about duplicates from incoming)
                    ref readonly TValue existedItem = ref _storage[c];

                    if (Comparer.Compare(existedItem, previous) != 0)
                        break; // this is the end of duplicates, we must stop

                    previous = ref existedItem;
                    destination[insertRIndex--] = existedItem;
                    resultedRange.length++;

                    c--;
                }

                do // Add incoming items if they are not added yet
                {
                    bool alreadyAdded = false;
                    ref readonly TValue incomingItem = ref range[i];

                    if (Comparer.Compare(incomingItem, previous) != 0)
                        return; // this is the end of duplicates, we must stop

                    if (_mayContainCollisions)
                    {
                        resultedRange.index = resultedRange.start;

                        do // Look for existing item in collisions
                        {
                            ref readonly TValue currentItem = ref destination[resultedRange.index++];

                            if (_equalityComparer.Equals(currentItem, incomingItem))
                            {
                                alreadyAdded = true;
                                break; // this item is already in result, we must stop
                            }

                        }
                        while (resultedRange.index < resultedRange.start + resultedRange.length);
                    }
                    else
                    {
                        alreadyAdded = true;
                    }

                    if (!alreadyAdded)
                    {
                        destination[insertRIndex--] = incomingItem;
                        resultedRange.length++;
                    }
                    else
                    {
                        incomingElementsSkipped++;
                    }

                    i--;
                }
                while (i_InRange(i));
            }

            void ProcessMergingIncoming(ref int insertRIndex, ref int i, ReadOnlySpan<TValue> range)
            {
                (int start, int length, int index) resultedRange = default;
                resultedRange.start = insertRIndex;

                // Add the first item to result (preparing to compare)
                ref readonly TValue previous = ref range[i--];
                destination[insertRIndex--] = previous;
                resultedRange.length++;

                while (i_InRange(i)) // Add incoming items if they are not added yet
                {
                    bool alreadyAdded = false;
                    ref readonly TValue incomingItem = ref range[i];

                    if (Comparer.Compare(incomingItem, previous) != 0)
                        return; // this is the end of duplicates, we must stop

                    previous = ref incomingItem;

                    if (_mayContainCollisions)
                    {
                        resultedRange.index = resultedRange.start;

                        do // Look for existing item in collisions
                        {
                            ref readonly TValue currentItem = ref destination[resultedRange.index++];

                            if (_equalityComparer.Equals(currentItem, incomingItem))
                            {
                                alreadyAdded = true;
                                break; // this item is already in result, we must stop
                            }

                        }
                        while (resultedRange.index < resultedRange.start + resultedRange.length);
                    }
                    else
                    {
                        alreadyAdded = true;
                    }

                    if (!alreadyAdded)
                    {
                        destination[insertRIndex--] = incomingItem;
                        resultedRange.length++;
                    }
                    else
                    {
                        incomingElementsSkipped++;
                    }

                    i++;
                }
            }

            void ProcessMergingCurrent(ref int insertRIndex, ref int c)
            {
                destination[insertRIndex--] = _storage[c--];
            }
        }
    }
}
