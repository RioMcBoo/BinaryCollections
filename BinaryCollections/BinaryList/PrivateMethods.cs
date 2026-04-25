using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    public partial class BinaryList<TValue>
    {
        private bool Insert(ref readonly TValue item, ref int vIndex)
        {
            if (_count == Array.MaxLength)
                return false;

            vIndex = vIndex < 0 ? ~vIndex : vIndex;

            if (_count > 0)
            {
                var info = new ArraySplitInfo(vIndex, true, _count);

                if (CanMoveLeft && info.LeftPart.Length <= info.RightPart.Length)
                {
                    ShiftLeftPart(ref info);
                }
                else if (CanMoveRight && info.RightPart.Length <= info.LeftPart.Length)
                {
                    ShiftRightPart(ref info);
                }
                else
                {
                    ResizeArray(ref info);
                }
            }

            _storage[ToRealIndex(vIndex)] = item;
            _count++;
            _version = unchecked(++_version);

            return true;
        }

        private bool RemoveRangeInternal(int vIndex, int length)
        {
            if (length > 0)
            {
                var info = new ArraySplitInfo(new Point(vIndex, -length), _count);

                if (info.LeftPart.Length <= info.RightPart.Length)
                {
                    ShiftLeftPart(ref info);
                }
                else if (info.RightPart.Length < info.LeftPart.Length)
                {
                    ShiftRightPart(ref info);
                }

                _count -= length;
                _version = unchecked(++_version);

                return true;
            }

            return false;
        }

        private bool AddSortedRange(ReadOnlySpan<TValue> range)
        {
            if (LeftBuffer >= RightBuffer && LeftBuffer >= range.Length)
            {
                return InsertSortedRangeFromLeft(range);
            }

            if (RightBuffer >= LeftBuffer && RightBuffer >= range.Length)
            {
                return InsertSortedRangeFromRight(range);
            }

            return ResizeAndInsertSortedRange(range);
        }

        private bool AddUniformRange(ReadOnlySpan<TValue> range)
        {
            var example = range[0];
            FindIndex(ref example, out var vIndex);
            return InsertUniformRange(range, ref vIndex);
        }

        private bool InsertUniformRange(ReadOnlySpan<TValue> range, ref int vIndex)
        {
            vIndex = vIndex < 0 ? ~vIndex : vIndex;
            var info = new ArraySplitInfo(vIndex, range.Length, _count);

            if (_count == 0)
            {
                ResizeArray(ref info);
            }
            else
            {
                if (CanMoveLeft && info.LeftPart.Length <= info.RightPart.Length)
                {
                    ShiftLeftPart(ref info);
                }
                else if (CanMoveRight && info.RightPart.Length <= info.LeftPart.Length)
                {
                    ShiftRightPart(ref info);
                }
                else
                {
                    ResizeArray(ref info);
                }
            }

            range.CopyTo(_storage.AsSpan(ToRealIndex(vIndex), range.Length));
            _count += range.Length;
            _version = unchecked(++_version);

            return true;
        }

        private bool ResizeAndInsertSortedRange(ReadOnlySpan<TValue> range)
        {
            PrepareForResize(range.Length, out var destination);

            int newZeroRIndex = CalculateZeroIndex(destination.Length, _count + range.Length);

            if (_storage == destination)
            {
                Array.Copy(destination, _zeroRIndex, destination, newZeroRIndex, _count);
                _zeroRIndex = newZeroRIndex;
                return InsertSortedRangeFromRight(range);
            }

            InsertSortedRangeFromLeft(range, destination, newZeroRIndex);

            _storage = destination;
            return true;
        }

        private bool InsertSortedRangeFromLeft(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex - range.Length;
            return InsertSortedRangeFromLeft(range, _storage, newZeroRIndex);
        }

        private bool InsertSortedRangeFromLeft(ReadOnlySpan<TValue> range, TValue[] destination, int newZeroRIndex)
        {
            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(_zeroRIndex - newZeroRIndex == range.Length, "In-place merge requires place for the new range.");
            }

            int i = 0;
            int c = _zeroRIndex;
            int insertRIndex = newZeroRIndex;

            Debug.Assert(insertRIndex > -1, "Left buffer is not long enough");

            while (i < range.Length && c < _zeroRIndex + _count)
            {
                ref readonly TValue incomingItem = ref range[i];
                ref readonly TValue currentItem = ref _storage[c];

                switch (Comparer.Compare(currentItem, incomingItem))
                {
                    case > 0:
                        destination[insertRIndex++] = incomingItem; i++;
                        break;
                    case < 0:
                        destination[insertRIndex++] = currentItem; c++;
                        break;
                    default:
                        destination[insertRIndex++] = incomingItem; i++;
                        destination[insertRIndex++] = currentItem; c++;
                        break;
                }
            }

            if (i < range.Length)
            {
                range[i..].CopyTo(destination.AsSpan(insertRIndex));
            }
            else if (c < _zeroRIndex + _count && _storage != destination)
            {
                Array.Copy(_storage, c, destination, insertRIndex, _zeroRIndex + _count - c);
            }

            _zeroRIndex = newZeroRIndex;
            _count += range.Length;
            _version = unchecked(++_version);
            return true;
        }

        private bool InsertSortedRangeFromRight(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex;
            return InsertSortedRangeFromRight(range, _storage, newZeroRIndex);
        }

        private bool InsertSortedRangeFromRight(ReadOnlySpan<TValue> range, TValue[] destination, int newZeroRIndex)
        {
            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(newZeroRIndex == _zeroRIndex, "In-place merge requires place for the new range.");
            }

            int i = range.Length - 1;
            int c = _zeroRIndex + _count - 1;
            int insertRIndex = newZeroRIndex + _count + range.Length - 1;

            Debug.Assert(insertRIndex < destination.Length, "Right buffer is not long enough");

            while (i >= 0 && c >= _zeroRIndex)
            {
                ref readonly TValue incomingItem = ref range[i];
                ref readonly TValue currentItem = ref _storage[c];

                switch (Comparer.Compare(currentItem, incomingItem))
                {
                    case > 0:
                        destination[insertRIndex--] = currentItem; c--;
                        break;
                    case < 0:
                        destination[insertRIndex--] = incomingItem; i--;
                        break;
                    default:
                        destination[insertRIndex--] = currentItem; c--;
                        destination[insertRIndex--] = incomingItem; i--;
                        break;
                }
            }

            if (i >= 0)
            {
                range[..(i + 1)].CopyTo(destination.AsSpan(newZeroRIndex));
            }
            else if (c >= _zeroRIndex && _storage != destination)
            {
                Array.Copy(_storage, _zeroRIndex, destination, newZeroRIndex, c - _zeroRIndex + 1);
            }

            _zeroRIndex = newZeroRIndex;
            _count += range.Length;
            _version = unchecked(++_version);
            return true;
        }

        private ListRangeInfo GetBoundariesToRemove(ref readonly TValue item)
        {
            var (left, right) = FindBoundaries(in item);

            if (left < 0)
                return new();

            TValue[] source = _storage;
            int initialRIndex = ToRealIndex(left);
            int lengthInitial = right - left + 1;

            RemoveRangeFromCollisions(in item, ref initialRIndex, ref lengthInitial, out source);

            return new(initialRIndex, lengthInitial, source);

            void RemoveRangeFromCollisions(ref readonly TValue item, ref int rIndex, ref int length, out TValue[] source)
            {
                int write = rIndex;
                _version = unchecked(++_version);

                for (int read = rIndex; read < rIndex + length; read++)
                {
                    ref readonly var currItem = ref _storage[read];
                    if (!IsIdentical(in currItem, in item))
                    {
                        _storage[write] = currItem;
                        write++;
                    }
                }

                source = _storage;
                int keptCountInBlock = write - rIndex;

                rIndex = write;
                length = lengthInitial - keptCountInBlock;
            }
        }

        private ListRangeInfo GetBoundariesToExtract(ref readonly TValue item)
        {
            var (left, right) = FindBoundaries(in item);

            if (left < 0)
                return new();

            TValue[] source = _storage;
            int initialRIndex = ToRealIndex(left);
            int lengthInitial = right - left + 1;

            ExtractRangeFromCollisions(in item, ref initialRIndex, ref lengthInitial, out source);

            return new(initialRIndex, lengthInitial, source);

            void ExtractRangeFromCollisions(ref readonly TValue item, ref int rIndex, ref int length, out TValue[] source)
            {
                int write = rIndex;
                _version = unchecked(++_version);

                TValue[] temp = ArrayPool<TValue>.Shared.Rent(length); int t = 0;

                try
                {
                    for (int read = rIndex; read < rIndex + length; read++)
                    {
                        ref readonly var currItem = ref _storage[read];
                        if (!IsIdentical(in currItem, in item))
                        {
                            _storage[write] = currItem;
                            write++;
                        }
                        else
                        {
                            temp[t++] = currItem;
                        }
                    }

                    Array.Copy(temp, 0, _storage, write, t);
                }
                finally
                {
                    ArrayPool<TValue>.Shared.Return(temp, true);
                }

                source = _storage;
                int keptCountInBlock = write - rIndex;

                rIndex = write;
                length = lengthInitial - keptCountInBlock;
            }
        }

        private ListRangeInfo GetBoundariesToShare(ref readonly TValue item)
        {
            var (left, right) = FindBoundaries(in item);

            if (left < 0)
                return new();

            TValue[] source = _storage;
            int initialRIndex = ToRealIndex(left);
            int lengthInitial = right - left + 1;

            CopyRangeFromCollisions(in item, ref initialRIndex, ref lengthInitial, out source);

            return new(initialRIndex, lengthInitial, source);

            void CopyRangeFromCollisions(ref readonly TValue item, ref int rIndex, ref int length, out TValue[] source)
            {
                source = new TValue[length];
                int write = 0;

                for (int read = rIndex; read < rIndex + length; read++)
                {
                    ref readonly var currItem = ref _storage[read];
                    if (IsIdentical(in currItem, in item))
                    {
                        source[write] = currItem;
                        write++;
                    }
                }

                rIndex = 0;
                length = write;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool FindIndex(ref readonly TValue item, ref int lo, ref int hi, out int i)
        {
            while (lo <= hi)
            {
                //// PERF: `lo` or `hi` will never be negative inside the loop,
                //       so computing median using uints is safe since we know
                //       `length <= int.MaxValue`, and indices are >= 0
                //       and thus cannot overflow an uint.
                //       Saves one subtraction per loop compared to
                //       `int i = lo + (hi - lo) / 2;`
                //        int i = (int)(((uint)hi + (uint)lo) >> 1);
                i = unchecked((hi + lo) >>> 1);

                int compareResult = Comparer.Compare(_storage[ToRealIndex(i)], item);

                if (compareResult == 0)
                {
                    return true;
                }
                else if (compareResult > 0)
                {
                    hi = i - 1;
                }
                else
                {
                    lo = i + 1;
                }
            }

            i = ~lo;
            return false;
        }

        private bool Move(TValue[] srcArray, int srcVIndex, TValue[] destArray, int destVIndex, int length)
        {
            if (length > 0)
            {
                Array.Copy(srcArray, ToRealIndex(srcVIndex), destArray, ToRealIndex(destVIndex), length);
                return true;
            }

            return false;
        }

        private void MoveArray(TValue[] srcArray, TValue[] destArray, int delta)
        {
            Move(srcArray, 0, destArray, delta, _count);
        }


        private void ResizeArray(int newCapacity)
        {
            TValue[] oldStorage = _storage;
            _storage = new TValue[newCapacity];

            int newZeroIndex = CalculateZeroIndex(_storage.Length, _count);
            int delta = newZeroIndex - _zeroRIndex;

            MoveArray(oldStorage, _storage, delta);

            _zeroRIndex = newZeroIndex;
        }

        private void PrepareForResize(int newItemsCount, out TValue[] destination)
        {
            destination = _storage;

            int required = _count + newItemsCount + (Capacity >> 2);
            int newCapacity = (int)BitOperations.RoundUpToPowerOf2((uint)required);

            if (newCapacity == 0 || newCapacity > Array.MaxLength)
                newCapacity = Array.MaxLength;

            if (newCapacity < DefaultCapacity)
                newCapacity = DefaultCapacity;

            if (newCapacity > _storage.Length)
                destination = new TValue[newCapacity];
        }

        private void ResizeArray(ref ArraySplitInfo info)
        {
            PrepareForResize(info.ArraySizeDelta, out var destination);
            ShiftArrayToCenter(_storage, destination, ref info);
            _storage = destination;
        }

        private void ShiftArrayToCenter(ref ArraySplitInfo info)
        {
            ShiftArrayToCenter(_storage, _storage, ref info);
        }

        private void ShiftArrayToCenter(TValue[] srcArray, TValue[] destArray, ref ArraySplitInfo info)
        {
            int arraySizeDelta = info.ArraySizeDelta;
            int newCount = _count + arraySizeDelta;
            int newZeroIndex = CalculateZeroIndex(destArray.Length, newCount);
            int delta = newZeroIndex - _zeroRIndex;

            ShiftArray(srcArray, destArray, ref info, delta);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ShiftArray(TValue[] srcArray, TValue[] destArray, ref ArraySplitInfo info, int delta)
        {
            int arraySizeDelta = info.ArraySizeDelta;
            int newZeroRIndex = delta + _zeroRIndex;

            if (delta < 0)
                ShiftArrayLeft(srcArray, destArray, ref info, delta);
            else
                ShiftArrayRight(srcArray, destArray, ref info, delta + arraySizeDelta);

            if (ContainsReferences && srcArray == destArray)
            {
                int cleanLength_ShiftLeft = -delta - arraySizeDelta;
                int cleanIndex_ShiftLeft = _count + delta + arraySizeDelta;

                int cleanLength_ShiftRight = delta;
                int cleanIndex_ShiftRight = _zeroRIndex;

                ClearArray(destArray, cleanIndex_ShiftRight, cleanLength_ShiftRight);
                ClearArray(destArray, cleanIndex_ShiftLeft, cleanLength_ShiftLeft);
            }

            _zeroRIndex = newZeroRIndex;
        }

        private void ClearArray(TValue[] array, int vIndex, int length)
        {
            if (length > 0)
            {
                Array.Clear(array, ToRealIndex(vIndex), length);
            }
        }

        private void ShiftArrayLeft(TValue[] srcArray, TValue[] destArray, ref ArraySplitInfo info, int delta)
        {
            Move(srcArray, info.LeftPart.VIndex, destArray, info.LeftPart.VIndex + delta, info.LeftPart.Length);
            delta += info.LeftPoint.ArraySizeDelta;

            if (Move(srcArray, info.MiddPart.VIndex, destArray, info.MiddPart.VIndex + delta, info.MiddPart.Length))
                delta += info.RightPoint.ArraySizeDelta;

            Move(srcArray, info.RightPart.VIndex, destArray, info.RightPart.VIndex + delta, info.RightPart.Length);
        }

        private void ShiftArrayRight(TValue[] srcArray, TValue[] destArray, ref ArraySplitInfo info, int delta)
        {
            Move(srcArray, info.RightPart.VIndex, destArray, info.RightPart.VIndex + delta, info.RightPart.Length);
            delta -= info.RightPoint.ArraySizeDelta;

            if (Move(srcArray, info.MiddPart.VIndex, destArray, info.MiddPart.VIndex + delta, info.MiddPart.Length))
                delta -= info.LeftPoint.ArraySizeDelta;

            Move(srcArray, info.LeftPart.VIndex, destArray, info.LeftPart.VIndex + delta, info.LeftPart.Length);
        }

        private void ShiftMiddlePart(ref ArraySplitInfo info, out int addItemIndexDelta)
        {
            int delta = info.LeftPoint.ArraySizeDelta;
            Move(_storage, info.MiddPart.VIndex, _storage, info.MiddPart.VIndex + delta, info.MiddPart.Length);
            addItemIndexDelta = info.LeftPoint.NewItemtPushes ? -1 : 0;
        }

        private void ShiftEdges(ref ArraySplitInfo info)
        {
            // ShiftLeftPart changes zero_index, so ShiftRightPart should be called before it
            ShiftRightPart(ref info);
            ShiftLeftPart(ref info);
        }

        private void ShiftLeftPart(ref ArraySplitInfo info)
        {
            int delta = -info.LeftPoint.ArraySizeDelta;
            Move(_storage, info.LeftPart.VIndex, _storage, info.LeftPart.VIndex + delta, info.LeftPart.Length);

            if (ContainsReferences && delta > 0)
                Array.Clear(_storage, _zeroRIndex, delta);

            _zeroRIndex += delta;
        }

        private void ShiftRightPart(ref ArraySplitInfo info)
        {
            int delta = info.RightPoint.ArraySizeDelta;
            Move(_storage, info.RightPart.VIndex, _storage, info.RightPart.VIndex + delta, info.RightPart.Length);

            if (ContainsReferences && delta < 0)
                Array.Clear(_storage, _zeroRIndex + _count + delta, -delta);
        }

        private void ReplaceItemsInArray(ref ArraySplitInfo info, out int addItemIndexDelta)
        {
            addItemIndexDelta = 0;
            bool NeedsMoveMiddlePart = info.MiddPart.Length < info.LeftPart.Length + info.RightPart.Length;

            if (NeedsMoveMiddlePart || Capacity == Count)
            {
                ShiftMiddlePart(ref info, out addItemIndexDelta);
            }
            else if ((info.LeftPoint.NewItemtPushes && !CanMoveLeft) || (info.RightPoint.NewItemtPushes && !CanMoveRight))
            {
                ShiftArrayToCenter(ref info);
            }
            else
            {
                ShiftEdges(ref info);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ReplaceCore(ref readonly TValue oldItem, out int newVIndex, ref readonly TValue newItem, bool unique)
        {
            FindIndexExactly(in oldItem, out var oldVIndex);
            return ReplaceCore(oldVIndex, out newVIndex, in newItem, unique);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ReplaceCore(int oldVIndex, out int newVIndex, ref readonly TValue newItem, bool unique)
        {
            if (FindIndexExactly(in newItem, out newVIndex) && unique)
                return false;

            if (!CheckIndex(oldVIndex))
                return false;

            _version = unchecked(++_version);

            if (oldVIndex == newVIndex && newVIndex > -1)
            {
                // oldItem and newItem have same index, so we can replace them easily.
                _storage[ToRealIndex(oldVIndex)] = newItem;
                return true;
            }

            // Do replacement
            newVIndex = newVIndex < 0 ? ~newVIndex : newVIndex;
            var info = new ArraySplitInfo(oldVIndex, newVIndex, Count);
            ReplaceItemsInArray(ref info, out int addItemIndexDelta);

            _storage[ToRealIndex(newVIndex + addItemIndexDelta)] = newItem;

            return true;
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

        private bool UnionWithSortedRange(ReadOnlySpan<TValue> range)
        {
            if (LeftBuffer >= RightBuffer && LeftBuffer >= range.Length)
            {
                return UnionWithFromLeft(range);
            }

            if (RightBuffer >= LeftBuffer && RightBuffer >= range.Length)
            {
                return UnionWithFromRight(range);
            }

            return ResizeAndUnionWithSortedRange(range);
        }

        private bool ResizeAndUnionWithSortedRange(ReadOnlySpan<TValue> range)
        {
            PrepareForResize(range.Length, out var destination);

            int newZeroRIndex = CalculateZeroIndex(destination.Length, _count + range.Length);

            if (_storage == destination)
            {
                Array.Copy(destination, _zeroRIndex, destination, newZeroRIndex, _count);
                _zeroRIndex = newZeroRIndex;
                return UnionWithFromRight(range);
            }

            UnionWithFromLeft(range, destination, newZeroRIndex);

            _storage = destination;
            return true;
        }

        private bool UnionWithFromLeft(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex - range.Length;
            return UnionWithFromLeft(range, _storage, newZeroRIndex);
        }

        private bool UnionWithFromLeft(ReadOnlySpan<TValue> range, TValue[] destination, int newZeroRIndex)
        {
            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(_zeroRIndex - newZeroRIndex == range.Length, "In-place merge requires place for the new range.");
            }

            BinaryList<ItemBinaryCounter> tempList = null!; // for local function usage (for optimizations)
            Dictionary<ItemHashCounter, ItemHashCounter> tempDictionary = null!; // for local function usage (for optimizations)            

            if (MayContainCollisions)
            {
                if (DefaultComparerLeadsToCollisions)
                    tempDictionary = new();
                else
                    tempList = new();
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
                if (_mayContainCollisions)
                {
                    if (DefaultComparerLeadsToCollisions)
                        ProcessMergingDuplicatesDefault(ref insertRIndex, ref i, ref c, range);
                    else
                        ProcessMergingDuplicatesComparable(ref insertRIndex, ref i, ref c, range);
                }
                else
                {
                    ProcessMergingDuplicatesPure(ref insertRIndex, ref i, ref c, range);
                }
            }

            void ProcessMergingDuplicatesDefault(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                tempDictionary.Clear();

                (int start, int length) resultedRangeCurrent = default;
                resultedRangeCurrent.start = c;

                // Add the first item to result (preparing to compare)
                ref readonly TValue previous = ref _storage[c++];

                var previousCounter = new ItemHashCounter(previous);
                previousCounter.CurrentCount++;
                tempDictionary.Add(previousCounter, previousCounter);
                resultedRangeCurrent.length++;

                // Add all currently existing items in collision to result, and count them.
                while (c_InRange(c))
                {
                    ref readonly TValue currentItem = ref _storage[c];

                    if (Comparer.Compare(currentItem, previous) != 0)
                        break; // this is the end of duplicates, we must stop

                    previous = ref currentItem;
                    var counter = new ItemHashCounter(currentItem);

                    if (tempDictionary.ContainsKey(counter))
                        counter = tempDictionary[counter];
                    else
                        tempDictionary[counter] = counter;

                    counter.CurrentCount++;
                    resultedRangeCurrent.length++;
                    c++;
                }

                // Add all incoming items in collision to result, and count them.
                (int start, int length) resultedRangeIncoming = default;
                resultedRangeIncoming.start = i;
                do
                {
                    ref readonly TValue incomingItem = ref range[i];

                    if (Comparer.Compare(incomingItem, previous) != 0)
                        break; // this is the end of duplicates, we must stop

                    previous = ref incomingItem;
                    var counter = new ItemHashCounter(incomingItem);

                    if (tempDictionary.ContainsKey(counter))
                        counter = tempDictionary[counter];
                    else
                        tempDictionary[counter] = counter;

                    counter.IncomingCount++;
                    resultedRangeIncoming.length++;
                    i++;
                }
                while (i_InRange(i));

                int addedCount = 0;
                foreach (var item in tempDictionary.Values)
                {
                    int countToAdd = Math.Max(item.CurrentCount, item.IncomingCount);
                    addedCount += countToAdd;

                    for (int j = 0; j < countToAdd; j++)
                    {
                        destination[insertRIndex++] = item.Item;
                    }
                }

                incomingElementsSkipped += resultedRangeCurrent.length + resultedRangeIncoming.length - addedCount;
            }

            void ProcessMergingDuplicatesComparable(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                tempList.Clear(false);

                (int start, int length) resultedRangeCurrent = default;
                resultedRangeCurrent.start = c;

                // Add the first item to result (preparing to compare)
                ref readonly TValue previous = ref _storage[c++];

                var previousCounter = new ItemBinaryCounter(previous);
                previousCounter.CurrentCount++;
                tempList.AddUnique(ref previousCounter, out _);
                resultedRangeCurrent.length++;

                // Add all currently existing items in collision to result, and count them.
                while (c_InRange(c))
                {
                    ref readonly TValue currentItem = ref _storage[c];

                    if (Comparer.Compare(currentItem, previous) != 0)
                        break; // this is the end of duplicates, we must stop

                    previous = ref currentItem;
                    var newCounter = new ItemBinaryCounter(currentItem);

                    ref ItemBinaryCounter counter = ref newCounter;
                    if (!tempList.AddUnique(ref counter, out var index))
                        counter = ref tempList.GetItemRef(index);

                    counter.CurrentCount++;
                    resultedRangeCurrent.length++;
                    c++;
                }

                // Add all incoming items in collision to result, and count them.
                (int start, int length) resultedRangeIncoming = default;
                resultedRangeIncoming.start = i;
                do
                {
                    ref readonly TValue incomingItem = ref range[i];

                    if (Comparer.Compare(incomingItem, previous) != 0)
                        break; // this is the end of duplicates, we must stop

                    previous = ref incomingItem;
                    var newCounter = new ItemBinaryCounter(incomingItem);

                    ref ItemBinaryCounter counter = ref newCounter;
                    if (!tempList.AddUnique(ref counter, out var index))
                        counter = ref tempList.GetItemRef(index);

                    counter.IncomingCount++;
                    resultedRangeIncoming.length++;
                    i++;
                }
                while (i_InRange(i));

                int addedCount = 0;
                foreach (var item in tempList)
                {
                    int countToAdd = Math.Max(item.CurrentCount, item.IncomingCount);
                    addedCount += countToAdd;

                    for (int j = 0; j < countToAdd; j++)
                    {
                        destination[insertRIndex++] = item.Item;
                    }
                }

                incomingElementsSkipped += resultedRangeCurrent.length + resultedRangeIncoming.length - addedCount;
            }

            void ProcessMergingDuplicatesPure(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                ref readonly TValue firstItem = ref _storage[c++]; i++; // they are equal here, so we can move both indexes
                incomingElementsSkipped++;
                destination[insertRIndex++] = firstItem;

                bool currentDone = false;
                bool incomingDone = false;

                while (true)
                {
                    int hasItemInRow = 0;

                    if (!currentDone && c_InRange(c) && Comparer.Compare(_storage[c], firstItem) == 0)
                    {
                        hasItemInRow++; c++;
                    }
                    else
                    {
                        currentDone = true;
                    }

                    if (!incomingDone && i_InRange(i) && Comparer.Compare(range[i], firstItem) == 0)
                    {
                        hasItemInRow++; i++;
                    }
                    else
                    {
                        incomingDone = true;
                    }

                    if (hasItemInRow > 0)
                    {
                        destination[insertRIndex++] = firstItem;

                        if (hasItemInRow == 2)
                            incomingElementsSkipped++;

                        continue;
                    }

                    break;
                }
            }

            void ProcessMergingIncoming(ref int insertRIndex, ref int i, ReadOnlySpan<TValue> range)
            {
                destination[insertRIndex++] = range[i++];
            }

            void ProcessMergingCurrent(ref int insertRIndex, ref int c)
            {
                destination[insertRIndex++] = _storage[c++];
            }
        }

        private bool UnionWithFromRight(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex;
            return UnionWithFromRight(range, _storage, newZeroRIndex);
        }

        private bool UnionWithFromRight(ReadOnlySpan<TValue> range, TValue[] destination, int newZeroRIndex)
        {
            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(newZeroRIndex == _zeroRIndex, "In-place merge requires place for the new range.");
            }

            BinaryList<ItemBinaryCounter> tempList = null!; // for local function usage (for optimizations)
            Dictionary<ItemHashCounter, ItemHashCounter> tempDictionary = null!; // for local function usage (for optimizations)            

            if (MayContainCollisions)
            {
                if (DefaultComparerLeadsToCollisions)
                    tempDictionary = new();
                else
                    tempList = new();
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
                if (_mayContainCollisions)
                {
                    if (DefaultComparerLeadsToCollisions)
                        ProcessMergingDuplicatesDefault(ref insertRIndex, ref i, ref c, range);
                    else
                        ProcessMergingDuplicatesComparable(ref insertRIndex, ref i, ref c, range);
                }
                else
                {
                    ProcessMergingDuplicatesPure(ref insertRIndex, ref i, ref c, range);
                }
            }

            void ProcessMergingDuplicatesDefault(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                tempDictionary.Clear();

                (int start, int length) resultedRangeCurrent = default;
                resultedRangeCurrent.start = c;

                // Add the first item to result (preparing to compare)
                ref readonly TValue previous = ref _storage[c--];

                var previousCounter = new ItemHashCounter(previous);
                previousCounter.CurrentCount++;
                tempDictionary.Add(previousCounter, previousCounter);
                resultedRangeCurrent.length++;

                // Add all currently existing items in collision to result, and count them.
                while (c_InRange(c))
                {
                    ref readonly TValue currentItem = ref _storage[c];

                    if (Comparer.Compare(currentItem, previous) != 0)
                        break; // this is the end of duplicates, we must stop

                    previous = ref currentItem;
                    var counter = new ItemHashCounter(currentItem);

                    if (tempDictionary.ContainsKey(counter))
                        counter = tempDictionary[counter];
                    else
                        tempDictionary[counter] = counter;

                    counter.CurrentCount++;
                    resultedRangeCurrent.length++;
                    c--;
                }

                // Add all incoming items in collision to result, and count them.
                (int start, int length) resultedRangeIncoming = default;
                resultedRangeIncoming.start = i;
                do
                {
                    ref readonly TValue incomingItem = ref range[i];

                    if (Comparer.Compare(incomingItem, previous) != 0)
                        break; // this is the end of duplicates, we must stop

                    previous = ref incomingItem;
                    var counter = new ItemHashCounter(incomingItem);

                    if (tempDictionary.ContainsKey(counter))
                        counter = tempDictionary[counter];
                    else
                        tempDictionary[counter] = counter;

                    counter.IncomingCount++;
                    resultedRangeIncoming.length++;
                    i--;
                }
                while (i_InRange(i));

                int addedCount = 0;
                foreach (var item in tempDictionary.Values)
                {
                    int countToAdd = Math.Max(item.CurrentCount, item.IncomingCount);
                    addedCount += countToAdd;

                    for (int j = 0; j < countToAdd; j++)
                    {
                        destination[insertRIndex--] = item.Item;
                    }
                }

                incomingElementsSkipped += resultedRangeCurrent.length + resultedRangeIncoming.length - addedCount;
            }

            void ProcessMergingDuplicatesComparable(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                tempList.Clear(false);

                (int start, int length) resultedRangeCurrent = default;
                resultedRangeCurrent.start = c;

                // Add the first item to result (preparing to compare)
                ref readonly TValue previous = ref _storage[c--];

                var previousCounter = new ItemBinaryCounter(previous);
                previousCounter.CurrentCount++;
                tempList.AddUnique(ref previousCounter, out _);
                
                resultedRangeCurrent.length++;

                // Add all currently existing items in collision to result, and count them.
                while (c_InRange(c))
                {
                    ref readonly TValue currentItem = ref _storage[c];

                    if (Comparer.Compare(currentItem, previous) != 0)
                        break; // this is the end of duplicates, we must stop

                    previous = ref currentItem;
                    var newCounter = new ItemBinaryCounter(currentItem);

                    ref ItemBinaryCounter counter = ref newCounter;
                    if (!tempList.AddUnique(ref counter, out var index))
                        counter = ref tempList.GetItemRef(index);

                    counter.CurrentCount++;
                    resultedRangeCurrent.length++;
                    c--;
                }

                // Add all incoming items in collision to result, and count them.
                (int start, int length) resultedRangeIncoming = default;
                resultedRangeIncoming.start = i;
                do
                {
                    ref readonly TValue incomingItem = ref range[i];

                    if (Comparer.Compare(incomingItem, previous) != 0)
                        break; // this is the end of duplicates, we must stop

                    previous = ref incomingItem;
                    var newCounter = new ItemBinaryCounter(incomingItem);

                    ref ItemBinaryCounter counter = ref newCounter;
                    if (!tempList.AddUnique(ref counter, out var index))
                        counter = ref tempList.GetItemRef(index);

                    counter.IncomingCount++;
                    resultedRangeIncoming.length++;
                    i--;
                }
                while (i_InRange(i));

                int addedCount = 0;
                foreach (var item in tempList)
                {
                    int countToAdd = Math.Max(item.CurrentCount, item.IncomingCount);
                    addedCount += countToAdd;

                    for (int j = 0; j < countToAdd; j++)
                    {
                        destination[insertRIndex--] = item.Item;
                    }
                }

                incomingElementsSkipped += resultedRangeCurrent.length + resultedRangeIncoming.length - addedCount;
            }

            void ProcessMergingDuplicatesPure(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                ref readonly TValue firstItem = ref _storage[c--]; i--; // they are equal here, so we can move both indexes
                incomingElementsSkipped++;
                destination[insertRIndex--] = firstItem;

                bool currentDone = false;
                bool incomingDone = false;

                while (true)
                {
                    int hasItemInRow = 0;

                    if (!currentDone && c_InRange(c) && Comparer.Compare(_storage[c], firstItem) == 0)
                    {
                        hasItemInRow++; c--;
                    }
                    else
                    {
                        currentDone = true;
                    }

                    if (!incomingDone && i_InRange(i) && Comparer.Compare(range[i], firstItem) == 0)
                    {
                        hasItemInRow++; i--;
                    }
                    else
                    {
                        incomingDone = true;
                    }

                    if (hasItemInRow > 0)
                    {
                        destination[insertRIndex--] = firstItem;

                        if (hasItemInRow == 2)
                            incomingElementsSkipped++;

                        continue;
                    }

                    break;
                }
            }            

            void ProcessMergingIncoming(ref int insertRIndex, ref int i, ReadOnlySpan<TValue> range)
            {
                destination[insertRIndex--] = range[i--];
            }

            void ProcessMergingCurrent(ref int insertRIndex, ref int c)
            {
                destination[insertRIndex--] = _storage[c--];
            }
        }

        private bool UnionWithSortedRangeUnique(ReadOnlySpan<TValue> range)
        {
            if (LeftBuffer >= RightBuffer && LeftBuffer >= range.Length)
            {
                return UnionWithFromLeftUnique(range);
            }

            if (RightBuffer >= LeftBuffer && RightBuffer >= range.Length)
            {
                return UnionWithFromRightUnique(range);
            }

            return ResizeAndUnionWithSortedRangeUnique(range);
        }

        private bool ResizeAndUnionWithSortedRangeUnique(ReadOnlySpan<TValue> range)
        {
            PrepareForResize(range.Length, out var destination);

            int newZeroRIndex = CalculateZeroIndex(destination.Length, _count + range.Length);

            if (_storage == destination)
            {
                Array.Copy(destination, _zeroRIndex, destination, newZeroRIndex, _count);
                _zeroRIndex = newZeroRIndex;
                return UnionWithFromRightUnique(range);
            }

            UnionWithFromLeftUnique(range, destination, newZeroRIndex);

            _storage = destination;
            return true;
        }

        private bool UnionWithFromLeftUnique(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex - range.Length;
            return UnionWithFromLeftUnique(range, _storage, newZeroRIndex);
        }

        private bool UnionWithFromLeftUnique(ReadOnlySpan<TValue> range, TValue[] destination, int newZeroRIndex)
        {
            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(_zeroRIndex - newZeroRIndex == range.Length, "In-place merge requires place for the new range.");
            }

            BinaryList<ItemBinaryCounter> tempList = null!; // for local function usage (for optimizations)
            Dictionary<ItemHashCounter, ItemHashCounter> tempDictionary = null!; // for local function usage (for optimizations)            

            if (MayContainCollisions)
            {
                if (DefaultComparerLeadsToCollisions)
                    tempDictionary = new();
                else
                    tempList = new();
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
            return true;

            void ProcessMergingDuplicates(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                if (_mayContainCollisions)
                {
                    if (DefaultComparerLeadsToCollisions)
                        ProcessMergingDuplicatesDefault(ref insertRIndex, ref i, ref c, range);
                    else
                        ProcessMergingDuplicatesComparable(ref insertRIndex, ref i, ref c, range);
                }
                else
                {
                    ProcessMergingDuplicatesPure(ref insertRIndex, ref i, ref c, range);
                }
            }

            void ProcessMergingDuplicatesDefault(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                tempDictionary.Clear();

                (int start, int length) resultedRangeCurrent = default;
                resultedRangeCurrent.start = c;

                // Add the first item to result (preparing to compare)
                ref readonly TValue previous = ref _storage[c++];

                var previousCounter = new ItemHashCounter(previous);
                previousCounter.CurrentCount++;
                tempDictionary.Add(previousCounter, previousCounter);
                resultedRangeCurrent.length++;

                // Add all currently existing items in collision to result, and count them.
                while (c_InRange(c))
                {
                    ref readonly TValue currentItem = ref _storage[c];

                    if (Comparer.Compare(currentItem, previous) != 0)
                        break; // this is the end of duplicates, we must stop

                    previous = ref currentItem;
                    var counter = new ItemHashCounter(currentItem);

                    if (tempDictionary.ContainsKey(counter))
                        counter = tempDictionary[counter];
                    else
                        tempDictionary[counter] = counter;

                    counter.CurrentCount++;
                    resultedRangeCurrent.length++;
                    c++;
                }

                // Add all incoming items in collision to result, and count them.
                (int start, int length) resultedRangeIncoming = default;
                resultedRangeIncoming.start = i;
                do
                {
                    ref readonly TValue incomingItem = ref range[i];

                    if (Comparer.Compare(incomingItem, previous) != 0)
                        break; // this is the end of duplicates, we must stop

                    previous = ref incomingItem;
                    var counter = new ItemHashCounter(incomingItem);

                    if (tempDictionary.ContainsKey(counter))
                        counter = tempDictionary[counter];
                    else
                        tempDictionary[counter] = counter;

                    counter.IncomingCount++;
                    resultedRangeIncoming.length++;
                    i++;
                }
                while (i_InRange(i));

                int addedCount = 0;
                foreach (var item in tempDictionary.Values)
                {
                    int countToAdd = 1;
                    addedCount += countToAdd;

                    for (int j = 0; j < countToAdd; j++)
                    {
                        destination[insertRIndex++] = item.Item;
                    }
                }

                incomingElementsSkipped += resultedRangeCurrent.length + resultedRangeIncoming.length - addedCount;
            }

            void ProcessMergingDuplicatesComparable(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                tempList.Clear(false);

                (int start, int length) resultedRangeCurrent = default;
                resultedRangeCurrent.start = c;

                // Add the first item to result (preparing to compare)
                ref readonly TValue previous = ref _storage[c++];

                var previousCounter = new ItemBinaryCounter(previous);
                previousCounter.CurrentCount++;
                tempList.AddUnique(ref previousCounter, out _);
                resultedRangeCurrent.length++;

                // Add all currently existing items in collision to result, and count them.
                while (c_InRange(c))
                {
                    ref readonly TValue currentItem = ref _storage[c];

                    if (Comparer.Compare(currentItem, previous) != 0)
                        break; // this is the end of duplicates, we must stop

                    previous = ref currentItem;
                    var newCounter = new ItemBinaryCounter(currentItem);

                    ref ItemBinaryCounter counter = ref newCounter;
                    if (!tempList.AddUnique(ref counter, out var index))
                        counter = ref tempList.GetItemRef(index);

                    counter.CurrentCount++;
                    resultedRangeCurrent.length++;
                    c++;
                }

                // Add all incoming items in collision to result, and count them.
                (int start, int length) resultedRangeIncoming = default;
                resultedRangeIncoming.start = i;
                do
                {
                    ref readonly TValue incomingItem = ref range[i];

                    if (Comparer.Compare(incomingItem, previous) != 0)
                        break; // this is the end of duplicates, we must stop

                    previous = ref incomingItem;
                    var newCounter = new ItemBinaryCounter(incomingItem);

                    ref ItemBinaryCounter counter = ref newCounter;
                    if (!tempList.AddUnique(ref counter, out var index))
                        counter = ref tempList.GetItemRef(index);

                    counter.IncomingCount++;
                    resultedRangeIncoming.length++;
                    i++;
                }
                while (i_InRange(i));

                int addedCount = 0;
                foreach (var item in tempList)
                {
                    int countToAdd = 1;
                    addedCount += countToAdd;

                    for (int j = 0; j < countToAdd; j++)
                    {
                        destination[insertRIndex++] = item.Item;
                    }
                }

                incomingElementsSkipped += resultedRangeCurrent.length + resultedRangeIncoming.length - addedCount;
            }

            void ProcessMergingDuplicatesPure(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                ref readonly TValue firstItem = ref _storage[c++]; i++; // they are equal here, so we can move both indexes
                incomingElementsSkipped++;
                destination[insertRIndex++] = firstItem;

                while (c_InRange(c) && Comparer.Compare(_storage[c], firstItem) == 0)
                {
                    incomingElementsSkipped++;
                    c++;
                }

                while (i_InRange(i) && Comparer.Compare(range[i], firstItem) == 0)
                {
                    incomingElementsSkipped++;
                    i++;
                }
            }

            void ProcessMergingIncoming(ref int insertRIndex, ref int i, ReadOnlySpan<TValue> range)
            {               
                ref readonly TValue firstItem = ref range[i++];
                destination[insertRIndex++] = firstItem;

                while (i_InRange(i) && Comparer.Compare(range[i], firstItem) == 0)
                {
                    incomingElementsSkipped++;
                    i++;
                }
            }

            void ProcessMergingCurrent(ref int insertRIndex, ref int c)
            {
                ref readonly TValue firstItem = ref _storage[c++];
                destination[insertRIndex++] = firstItem;

                while (c_InRange(c) && Comparer.Compare(_storage[c], firstItem) == 0)
                {
                    incomingElementsSkipped++;
                    c++;
                }
            }
        }

        private bool UnionWithFromRightUnique(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex;
            return UnionWithFromRightUnique(range, _storage, newZeroRIndex);
        }

        private bool UnionWithFromRightUnique(ReadOnlySpan<TValue> range, TValue[] destination, int newZeroRIndex)
        {
            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(newZeroRIndex == _zeroRIndex, "In-place merge requires place for the new range.");
            }

            BinaryList<ItemBinaryCounter> tempList = null!; // for local function usage (for optimizations)
            Dictionary<ItemHashCounter, ItemHashCounter> tempDictionary = null!; // for local function usage (for optimizations)            

            if (MayContainCollisions)
            {
                if (DefaultComparerLeadsToCollisions)
                    tempDictionary = new();
                else
                    tempList = new();
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
            return true;

            void ProcessMergingDuplicates(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                if (_mayContainCollisions)
                {
                    if (DefaultComparerLeadsToCollisions)
                        ProcessMergingDuplicatesDefault(ref insertRIndex, ref i, ref c, range);
                    else
                        ProcessMergingDuplicatesComparable(ref insertRIndex, ref i, ref c, range);
                }
                else
                {
                    ProcessMergingDuplicatesPure(ref insertRIndex, ref i, ref c, range);
                }
            }

            void ProcessMergingDuplicatesDefault(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                tempDictionary.Clear();

                (int start, int length) resultedRangeCurrent = default;
                resultedRangeCurrent.start = c;

                // Add the first item to result (preparing to compare)
                ref readonly TValue previous = ref _storage[c--];

                var previousCounter = new ItemHashCounter(previous);
                previousCounter.CurrentCount++;
                tempDictionary.Add(previousCounter, previousCounter);
                resultedRangeCurrent.length++;

                // Add all currently existing items in collision to result, and count them.
                while (c_InRange(c))
                {
                    ref readonly TValue currentItem = ref _storage[c];

                    if (Comparer.Compare(currentItem, previous) != 0)
                        break; // this is the end of duplicates, we must stop

                    previous = ref currentItem;
                    var counter = new ItemHashCounter(currentItem);

                    if (tempDictionary.ContainsKey(counter))
                        counter = tempDictionary[counter];
                    else
                        tempDictionary[counter] = counter;

                    counter.CurrentCount++;
                    resultedRangeCurrent.length++;
                    c--;
                }

                // Add all incoming items in collision to result, and count them.
                (int start, int length) resultedRangeIncoming = default;
                resultedRangeIncoming.start = i;
                do
                {
                    ref readonly TValue incomingItem = ref range[i];

                    if (Comparer.Compare(incomingItem, previous) != 0)
                        break; // this is the end of duplicates, we must stop

                    previous = ref incomingItem;
                    var counter = new ItemHashCounter(incomingItem);

                    if (tempDictionary.ContainsKey(counter))
                        counter = tempDictionary[counter];
                    else
                        tempDictionary[counter] = counter;

                    counter.IncomingCount++;
                    resultedRangeIncoming.length++;
                    i--;
                }
                while (i_InRange(i));

                int addedCount = 0;
                foreach (var item in tempDictionary.Values)
                {
                    int countToAdd = 1;
                    addedCount += countToAdd;

                    for (int j = 0; j < countToAdd; j++)
                    {
                        destination[insertRIndex--] = item.Item;
                    }
                }

                incomingElementsSkipped += resultedRangeCurrent.length + resultedRangeIncoming.length - addedCount;
            }

            void ProcessMergingDuplicatesComparable(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                tempList.Clear(false);

                (int start, int length) resultedRangeCurrent = default;
                resultedRangeCurrent.start = c;

                // Add the first item to result (preparing to compare)
                ref readonly TValue previous = ref _storage[c--];

                var previousCounter = new ItemBinaryCounter(previous);
                previousCounter.CurrentCount++;
                tempList.AddUnique(ref previousCounter, out _);

                resultedRangeCurrent.length++;

                // Add all currently existing items in collision to result, and count them.
                while (c_InRange(c))
                {
                    ref readonly TValue currentItem = ref _storage[c];

                    if (Comparer.Compare(currentItem, previous) != 0)
                        break; // this is the end of duplicates, we must stop

                    previous = ref currentItem;
                    var newCounter = new ItemBinaryCounter(currentItem);

                    ref ItemBinaryCounter counter = ref newCounter;
                    if (!tempList.AddUnique(ref counter, out var index))
                        counter = ref tempList.GetItemRef(index);

                    counter.CurrentCount++;
                    resultedRangeCurrent.length++;
                    c--;
                }

                // Add all incoming items in collision to result, and count them.
                (int start, int length) resultedRangeIncoming = default;
                resultedRangeIncoming.start = i;
                do
                {
                    ref readonly TValue incomingItem = ref range[i];

                    if (Comparer.Compare(incomingItem, previous) != 0)
                        break; // this is the end of duplicates, we must stop

                    previous = ref incomingItem;
                    var newCounter = new ItemBinaryCounter(incomingItem);

                    ref ItemBinaryCounter counter = ref newCounter;
                    if (!tempList.AddUnique(ref counter, out var index))
                        counter = ref tempList.GetItemRef(index);

                    counter.IncomingCount++;
                    resultedRangeIncoming.length++;
                    i--;
                }
                while (i_InRange(i));

                int addedCount = 0;
                foreach (var item in tempList)
                {
                    int countToAdd = 1;
                    addedCount += countToAdd;

                    for (int j = 0; j < countToAdd; j++)
                    {
                        destination[insertRIndex--] = item.Item;
                    }
                }

                incomingElementsSkipped += resultedRangeCurrent.length + resultedRangeIncoming.length - addedCount;
            }

            void ProcessMergingDuplicatesPure(ref int insertRIndex, ref int i, ref int c, ReadOnlySpan<TValue> range)
            {
                ref readonly TValue firstItem = ref _storage[c--]; i--; // they are equal here, so we can move both indexes
                incomingElementsSkipped++;
                destination[insertRIndex--] = firstItem;

                while (c_InRange(c) && Comparer.Compare(_storage[c], firstItem) == 0)
                {
                    incomingElementsSkipped++;
                    c--;
                }

                while (i_InRange(i) && Comparer.Compare(range[i], firstItem) == 0)
                {
                    incomingElementsSkipped++;
                    i--;
                }
            }

            void ProcessMergingIncoming(ref int insertRIndex, ref int i, ReadOnlySpan<TValue> range)
            {
                ref readonly TValue firstItem = ref range[i--];
                destination[insertRIndex--] = firstItem;

                while (i_InRange(i) && Comparer.Compare(range[i], firstItem) == 0)
                {
                    incomingElementsSkipped++;
                    i--;
                }
            }

            void ProcessMergingCurrent(ref int insertRIndex, ref int c)
            {
                ref readonly TValue firstItem = ref _storage[c--];
                destination[insertRIndex--] = firstItem;

                while (c_InRange(c) && Comparer.Compare(_storage[c], firstItem) == 0)
                {
                    incomingElementsSkipped++;
                    c--;
                }
            }
        }
    }
}
