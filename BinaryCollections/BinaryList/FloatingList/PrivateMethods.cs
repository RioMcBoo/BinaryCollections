using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
namespace BinaryCollections.BinaryList
{
    public partial class FloatingList<TValue>
    {
        protected void RemoveAtIndex(int index)
        {
            _version = unchecked(++_version);

            var info = ArraySplitInfo.GetRemoveInfo(index, _count);

            if (info.LeftPart.Length * BackwardShiftPenalty <= info.RightPart.Length)
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

            bool LeftIsPriority = info.LeftPart.Length * BackwardShiftPenalty <= info.RightPart.Length;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ShiftArrayToCenter(ref readonly ArraySplitInfo info)
        {
            ShiftArrayToCenter(_storage, _storage, in info);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ShiftArrayToCenter(TValue[] srcArray, TValue[] destArray, ref readonly ArraySplitInfo info)
        {
            int arraySizeDelta = info.ArraySizeDelta;
            int newCount = _count + arraySizeDelta;
            int newZeroIndex = CalculateZeroIndex(destArray.Length, newCount);
            int delta = newZeroIndex - _zeroRIndex;

            ShiftArray(srcArray, destArray, in info, delta);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ShiftMiddlePart(ref readonly ArraySplitInfo info)
        {
            Debug.Assert(info.ArraySizeDelta == 0, "When replacing Middle part can be shifted only if edges are not moved");

            int delta = -info.Alignment.Middle.LeftEdgeIndexShift;
            int partIndex = ToRealIndex(info.MiddlePart.Index);
            int newPartIndex = partIndex + delta;
            int partLength = info.MiddlePart.Length;

            Array.Copy(_storage, partIndex, _storage, newPartIndex, partLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ShiftEdges(ref readonly ArraySplitInfo info)
        {
            var alignment = info.Alignment.Middle;

            // ShiftLeftPart changes zero_index, so ShiftRightPart should be called before it
            ShiftRightPart(in info.RightPart, in alignment);
            ShiftLeftPart(in info.LeftPart, in alignment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ShiftLeftPart(ref readonly ArraySplitInfo info)
        {
            ShiftLeftPart(in info.LeftPart, in info.Alignment.Right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ShiftRightPart(ref readonly ArraySplitInfo info)
        {
            ShiftRightPart(in info.RightPart, in info.Alignment.Left);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ShiftLeftPart(ref readonly PartInfo info, ref readonly AlignmentInfo alignment)
        {
            int delta = alignment.LeftEdgeIndexShift;
            int partIndex = ToRealIndex(info.Index);
            int newPartIndex = partIndex + delta;
            int partLength = info.Length;

            Array.Copy(_storage, partIndex, _storage, newPartIndex, partLength);

            if (ContainsReferences && delta > 0)
                Array.Clear(_storage, _zeroRIndex, delta);

            _zeroRIndex += delta;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ShiftRightPart(ref readonly PartInfo info, ref readonly AlignmentInfo alignment)
        {
            int delta = alignment.RightEdgeIndexShift;
            int partIndex = ToRealIndex(info.Index);
            int newPartIndex = partIndex + delta;
            int partLength = info.Length;

            Array.Copy(_storage, partIndex, _storage, newPartIndex, partLength);

            if (ContainsReferences && delta < 0)
                Array.Clear(_storage, newPartIndex + partLength, -delta);
        }
    }
}
