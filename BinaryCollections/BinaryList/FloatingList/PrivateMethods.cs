using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
namespace BinaryCollections.BinaryList
{
    public partial class FloatingList<TValue>
    {
        protected bool RemoveAtIndex(int index)
        {
            var removeInfo = new RemoveInfo(index, _count);
            var info = removeInfo.Data;

            if (info.LeftPart.Length * BackwardShiftPenalty <= info.RightPart.Length)
            {
                ShiftLeftPart(in info);
            }
            else
            {
                ShiftRightPart(in info);
            }

            _count--;
            _version = unchecked(++_version);

            if (_count < 2)
                ModifyFlags(add: BinaryListFlags.NoDuplicates, remove: BinaryListFlags.UniformRange);

            return true;
        }

        protected bool Insert(ref readonly TValue item, ref int vIndex)
        {
            if (_count == Array.MaxLength)
                return false;

            bool ItemExists = vIndex >= 0;
            vIndex = ItemExists ? vIndex : ~vIndex;

            var insertInfo = new InsertInfo(vIndex, _count);
            var info = insertInfo.Data;

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
            _version = unchecked(++_version);

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

            return true;
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

            int delta = -info.Alignment.Middle.RightEdgeIndexShift;
            Array.Copy(_storage, ToRealIndex(info.MiddlePart.Index), _storage, ToRealIndex(info.MiddlePart.Index + delta), info.MiddlePart.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ShiftEdges(ref readonly ArraySplitInfo info)
        {
            // ShiftLeftPart changes zero_index, so ShiftRightPart should be called before it
            ShiftRightPart(in info.RightPart, in info.Alignment.Middle);
            ShiftLeftPart(in info.LeftPart, in info.Alignment.Middle);
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
            Array.Copy(_storage, ToRealIndex(info.Index), _storage, ToRealIndex(info.Index + delta), info.Length);

            if (ContainsReferences && delta > 0)
                Array.Clear(_storage, _zeroRIndex, delta);

            _zeroRIndex += delta;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ShiftRightPart(ref readonly PartInfo info, ref readonly AlignmentInfo alignment)
        {
            int delta = alignment.RightEdgeIndexShift;
            Array.Copy(_storage, ToRealIndex(info.Index), _storage, ToRealIndex(info.Index + delta), info.Length);

            if (ContainsReferences && delta < 0)
                Array.Clear(_storage, _zeroRIndex + _count + delta, -delta);
        }
    }
}
