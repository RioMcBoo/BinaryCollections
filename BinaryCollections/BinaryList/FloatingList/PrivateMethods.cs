using System.Numerics;
namespace BinaryCollections.BinaryList
{
    public partial class FloatingList<TValue>
    {
        protected void RemoveRangeInternal(int vIndex, int length)
        {
            _version = unchecked(++_version);

            var info = ArraySplitInfo.GetRemoveInfo(vIndex, _count, length);

            if (info.LeftPart.Length <= info.RightPart.Length)
            {
                ShiftLeftPart(in info);
            }
            else
            {
                ShiftRightPart(in info);
            }

            _count -= length;

            if (_count < 2)
                ModifyFlags(add: BinaryListFlags.NoDuplicates, remove: BinaryListFlags.UniformRange);
        }

        protected void InsertUniformRange(ReadOnlySpan<TValue> range, ref int vIndex)
        {
            _version = unchecked(++_version);

            bool ItemExists = vIndex >= 0;
            vIndex = ItemExists ? vIndex : ~vIndex;

            var info = ArraySplitInfo.GetInsertInfo(vIndex, _count, range.Length);

            bool LeftIsPriority = info.LeftPart.Length <= info.RightPart.Length;

            if (CanMoveLeft && LeftIsPriority)
            {
                ShiftLeftPart(in info);
            }
            else if (CanMoveRight && !LeftIsPriority)
            {
                ShiftRightPart(in info);
            }
            else
            {
                ResizeArray(in info);
            }

            range.CopyTo(_storage.AsSpan(ToRealIndex(vIndex), range.Length));
            _count += range.Length;

            if (ItemExists)
            {
                RemoveFlags(BinaryListFlags.NoDuplicates);

                if (_count - range.Length == 1)
                    AddFlags(BinaryListFlags.UniformRange);
            }
            else
            {
                RemoveFlags(BinaryListFlags.UniformRange);
            }
        }
        protected void RemoveAtIndex(int index)
        {
            _version = unchecked(++_version);

            var info = ArraySplitInfo.GetRemoveInfo(index, _count);

            if (info.LeftPart.Length <= info.RightPart.Length)
            {
                ShiftLeftPart(in info);
            }
            else
            {
                ShiftRightPart(in info);
            }

            _count--;

            if (_count < 2)
                ModifyFlags(add: BinaryListFlags.NoDuplicates, remove: BinaryListFlags.UniformRange);
        }

        protected void InsertAtIndex(ref readonly TValue item, ref int vIndex)
        {
            _version = unchecked(++_version);

            bool ItemExists = vIndex >= 0;
            vIndex = ItemExists ? vIndex : ~vIndex;

            var info = ArraySplitInfo.GetInsertInfo(vIndex, _count);

            bool LeftIsPriority = info.LeftPart.Length <= info.RightPart.Length;

            if (CanMoveLeft && LeftIsPriority)
            {
                ShiftLeftPart(in info);
            }
            else if (CanMoveRight && !LeftIsPriority)
            {
                ShiftRightPart(in info);
            }
            else
            {
                ResizeArray(in info);
            }

            _storage[ToRealIndex(vIndex)] = item;
            _count++;

            if (ItemExists)
            {
                RemoveFlags(BinaryListFlags.NoDuplicates);

                if (_count == 2)
                    AddFlags(BinaryListFlags.UniformRange);
            }
            else
            {
                RemoveFlags(BinaryListFlags.UniformRange);
            }
        }

        protected void ReplaceAtIndex(int oldVIndex, int newVIndex, ref readonly TValue newItem)
        {
            _version = unchecked(++_version);

            // Do replacement
            bool ItemExists = newVIndex >= 0;
            newVIndex = ItemExists ? newVIndex : ~newVIndex;

            var info = ArraySplitInfo.GetReplaceInfo(oldVIndex, newVIndex, Count);

            ReplaceItemsInArray(in info);
            _storage[ToRealIndex(newVIndex + info.ReplaceAddIndexShift)] = newItem;

            if (_count < 2)
            {
                ModifyFlags(add: BinaryListFlags.NoDuplicates, remove: BinaryListFlags.UniformRange);
            }
            else if (ItemExists)
            {
                RemoveFlags(BinaryListFlags.NoDuplicates);

                if (_count == 2)
                    AddFlags(BinaryListFlags.UniformRange);
            }
            else
            {
                RemoveFlags(BinaryListFlags.UniformRange);
            }
        }

        private void ReplaceItemsInArray(ref readonly ArraySplitInfo info)
        {
            // we take middle alignment info to see how edges will be changed
            var alignmentInfo = info.Alignment.Middle;
            bool BetterMoveMiddlePart = info.MiddlePart.Length <= info.LeftPart.Length + info.RightPart.Length;

            // Try to move middle part
            if (BetterMoveMiddlePart)
            {
                // Check boundaries
                if (info.ArraySizeDelta > 0 && RightBuffer - info.ArraySizeDelta < 0)
                    ResizeArray(in info);
                else
                    ShiftMiddlePart(in info);
            }
            else // Try to move edges
            {
                // check boundaries
                if ((LeftBuffer + alignmentInfo.LeftEdgeIndexShift < 0) || (RightBuffer - alignmentInfo.RightEdgeIndexShift < 0))
                    ResizeArray(in info);
                else
                    ShiftEdges(in info);
            }
        }

        protected void ResizeArray(int newCapacity)
        {
            TValue[] oldStorage = _storage;
            _storage = new TValue[newCapacity];

            int newZeroIndex = CalculateZeroIndex(_storage.Length, _count);
            int delta = newZeroIndex - _zeroRIndex;

            Array.Copy(oldStorage, ToRealIndex(0), _storage, ToRealIndex(delta), _count);

            _zeroRIndex = newZeroIndex;
        }

        protected void PrepareForResize(int newItemsCount, out TValue[] destination)
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

        protected void ResizeArray(ref readonly ArraySplitInfo info)
        {
            PrepareForResize(info.ArraySizeDelta, out var destination);
            ShiftArrayToCenter(_storage, destination, in info);
            _storage = destination;
        }
    }
}
