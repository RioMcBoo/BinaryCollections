using System.Numerics;
namespace BinaryCollections.BinaryList
{
    public partial class FloatingList<TValue>
    {
        protected bool RemoveAtIndex(int index)
        {
            var info = new ArraySplitInfo(index, false, _count);

            if (info.LeftPart.Length <= info.RightPart.Length)
            {
                ShiftLeftPart(ref info);
            }
            else if (info.RightPart.Length < info.LeftPart.Length)
            {
                ShiftRightPart(ref info);
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

        protected void ResizeArray(ref ArraySplitInfo info)
        {
            PrepareForResize(info.ArraySizeDelta, out var destination);
            ShiftArrayToCenter(_storage, destination, ref info);
            _storage = destination;
        }

        protected void ShiftArrayToCenter(ref ArraySplitInfo info)
        {
            ShiftArrayToCenter(_storage, _storage, ref info);
        }

        protected void ShiftArrayToCenter(TValue[] srcArray, TValue[] destArray, ref ArraySplitInfo info)
        {
            int arraySizeDelta = info.ArraySizeDelta;
            int newCount = _count + arraySizeDelta;
            int newZeroIndex = CalculateZeroIndex(destArray.Length, newCount);
            int delta = newZeroIndex - _zeroRIndex;

            ShiftArray(srcArray, destArray, ref info, delta);
        }

        protected void ShiftArray(TValue[] srcArray, TValue[] destArray, ref ArraySplitInfo info, int delta)
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

        protected void ClearArray(TValue[] array, int vIndex, int length)
        {
            if (length > 0)
            {
                Array.Clear(array, ToRealIndex(vIndex), length);
            }
        }

        protected void ShiftArrayLeft(TValue[] srcArray, TValue[] destArray, ref ArraySplitInfo info, int delta)
        {
            Move(srcArray, info.LeftPart.VIndex, destArray, info.LeftPart.VIndex + delta, info.LeftPart.Length);
            delta += info.LeftPoint.ArraySizeDelta;

            if (Move(srcArray, info.MiddPart.VIndex, destArray, info.MiddPart.VIndex + delta, info.MiddPart.Length))
                delta += info.RightPoint.ArraySizeDelta;

            Move(srcArray, info.RightPart.VIndex, destArray, info.RightPart.VIndex + delta, info.RightPart.Length);
        }

        protected void ShiftArrayRight(TValue[] srcArray, TValue[] destArray, ref ArraySplitInfo info, int delta)
        {
            Move(srcArray, info.RightPart.VIndex, destArray, info.RightPart.VIndex + delta, info.RightPart.Length);
            delta -= info.RightPoint.ArraySizeDelta;

            if (Move(srcArray, info.MiddPart.VIndex, destArray, info.MiddPart.VIndex + delta, info.MiddPart.Length))
                delta -= info.LeftPoint.ArraySizeDelta;

            Move(srcArray, info.LeftPart.VIndex, destArray, info.LeftPart.VIndex + delta, info.LeftPart.Length);
        }

        protected void ShiftMiddlePart(ref ArraySplitInfo info, out int addItemIndexDelta)
        {
            int delta = info.LeftPoint.ArraySizeDelta;
            Move(_storage, info.MiddPart.VIndex, _storage, info.MiddPart.VIndex + delta, info.MiddPart.Length);
            addItemIndexDelta = info.LeftPoint.NewItemtPushes ? -1 : 0;
        }

        protected void ShiftEdges(ref ArraySplitInfo info)
        {
            // ShiftLeftPart changes zero_index, so ShiftRightPart should be called before it
            ShiftRightPart(ref info);
            ShiftLeftPart(ref info);
        }

        protected void ShiftLeftPart(ref ArraySplitInfo info)
        {
            int delta = -info.LeftPoint.ArraySizeDelta;
            Move(_storage, info.LeftPart.VIndex, _storage, info.LeftPart.VIndex + delta, info.LeftPart.Length);

            if (ContainsReferences && delta > 0)
                Array.Clear(_storage, _zeroRIndex, delta);

            _zeroRIndex += delta;
        }

        protected void ShiftRightPart(ref ArraySplitInfo info)
        {
            int delta = info.RightPoint.ArraySizeDelta;
            Move(_storage, info.RightPart.VIndex, _storage, info.RightPart.VIndex + delta, info.RightPart.Length);

            if (ContainsReferences && delta < 0)
                Array.Clear(_storage, _zeroRIndex + _count + delta, -delta);
        }
    }
}
