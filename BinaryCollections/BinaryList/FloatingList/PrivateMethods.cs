using System.Numerics;
namespace BinaryCollections.BinaryList
{
    public partial class FloatingList<TValue>
    {
        protected void ClearArray(TValue[] array, int vIndex, int length)
        {
            if (length > 0)
            {
                Array.Clear(array, ToRealIndex(vIndex), length);
            }
        }

        protected bool RemoveAtIndex(int index)
        {
            var removeInfo = new RemoveInfo(index, _count);
            var info = removeInfo.Data;

            if (info.LeftPart.Length < info.RightPart.Length)
            {
                ShiftLeftPart(in info);
            }
            else if (info.RightPart.Length < info.LeftPart.Length)
            {
                ShiftRightPart(in info);
            }
            else if (LeftBuffer <= RightBuffer)
            {
                ShiftLeftPart(in info);
            }
            else
            {
                ShiftRightPart(in info);
            }

            _count--;
            _version = unchecked(++_version);

            return true;
        }

        protected bool Insert(ref readonly TValue item, ref int vIndex)
        {
            if (_count == Array.MaxLength)
                return false;

            vIndex = vIndex < 0 ? ~vIndex : vIndex;

            if (_count > 0)
            {
                var insertInfo = new InsertInfo(vIndex, _count);
                var info = insertInfo.Data;

                if (CanMoveLeft && info.LeftPart.Length <= info.RightPart.Length)
                {
                    ShiftLeftPart(in info);
                }
                else if (CanMoveRight && info.RightPart.Length <= info.LeftPart.Length)
                {
                    ShiftRightPart(in info);
                }
                else
                {
                    ResizeArray(in info);
                }
            }

            _storage[ToRealIndex(vIndex)] = item;
            _count++;
            _version = unchecked(++_version);

            return true;
        }

        protected bool Move(TValue[] srcArray, int srcVIndex, TValue[] destArray, int destVIndex, int length)
        {
            if (length > 0)
            {
                Array.Copy(srcArray, ToRealIndex(srcVIndex), destArray, ToRealIndex(destVIndex), length);
                return true;
            }

            return false;
        }

        protected void MoveArray(TValue[] srcArray, TValue[] destArray, int delta)
        {
            Move(srcArray, 0, destArray, delta, _count);
        }

        protected void ResizeArray(int newCapacity)
        {
            TValue[] oldStorage = _storage;
            _storage = new TValue[newCapacity];

            int newZeroIndex = CalculateZeroIndex(_storage.Length, _count);
            int delta = newZeroIndex - _zeroRIndex;

            MoveArray(oldStorage, _storage, delta);

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

        protected void ShiftArrayToCenter(ref readonly ArraySplitInfo info)
        {
            ShiftArrayToCenter(_storage, _storage, in info);
        }

        protected void ShiftArrayToCenter(TValue[] srcArray, TValue[] destArray, ref readonly ArraySplitInfo info)
        {
            int arraySizeDelta = info.ArraySizeDelta;
            int newCount = _count + arraySizeDelta;
            int newZeroIndex = CalculateZeroIndex(destArray.Length, newCount);
            int delta = newZeroIndex - _zeroRIndex;

            ShiftArray(srcArray, destArray, in info, delta);
        }

        protected void ShiftMiddlePart(ref readonly ArraySplitInfo info)
        {
            ShiftArray(_storage, _storage, in info, 0);
        }

        protected void ShiftEdges(ref readonly ArraySplitInfo info)
        {
            ShiftArray(_storage, _storage, in info, info.Alignment.Middle.LeftEdgeIndexShift);
        }

        protected void ShiftLeftPart(ref readonly ArraySplitInfo info)
        {
            ShiftArray(_storage, _storage, in info, info.Alignment.Right.LeftEdgeIndexShift);
        }

        protected void ShiftRightPart(ref readonly ArraySplitInfo info)
        {
            ShiftArray(_storage, _storage, in info, info.Alignment.Left.LeftEdgeIndexShift);
        }
    }
}
