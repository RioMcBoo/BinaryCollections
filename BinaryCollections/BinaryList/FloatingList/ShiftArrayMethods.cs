using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BinaryCollections.BinaryList
{
    public partial class FloatingList<TValue>
    {
        protected void ShiftArray(TValue[] srcArray, TValue[] destArray, ref readonly ArraySplitInfo info, int delta)
        {
            // we move the array relative to the zero index, so this is related to left alignment
            AlignmentInfo alignmentInfo = info.Alignment.Left;
            /*alignmentInfo.LeftEdgeIndexShift == 0 (always, in this case)*/

            int newZeroRIndex = delta + _zeroRIndex;

            if (delta < 0)
            {
                ShiftArrayLeft(srcArray, destArray, in info, delta + 0 /*[alignmentInfo.LeftEdgeIndexShift]*/);
            }
            else
            {
                ShiftArrayRight(srcArray, destArray, in info, delta + alignmentInfo.RightEdgeIndexShift);
            }

            if (ItemType<TValue>.ContainsReferences && srcArray == destArray)
            {
                int cleanLength_ShiftLeft = -delta - alignmentInfo.RightEdgeIndexShift;
                int cleanIndex_ShiftLeft = _count + delta + alignmentInfo.RightEdgeIndexShift;

                int cleanLength_ShiftRight = delta /* + [alignmentInfo.LeftEdgeIndexShift]*/;
                int cleanIndex_ShiftRight = 0 /* - [alignmentInfo.LeftEdgeIndexShift]*/;

                ClearArray(destArray, cleanIndex_ShiftLeft, cleanLength_ShiftLeft);
                ClearArray(destArray, cleanIndex_ShiftRight, cleanLength_ShiftRight);
            }

            _zeroRIndex = newZeroRIndex;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void ClearArray(TValue[] array, int vIndex, int length)
            {
                if (length > 0)
                {
                    Array.Clear(array, ToRealIndex(vIndex), length);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ShiftArrayLeft(TValue[] srcArray, TValue[] destArray, ref readonly ArraySplitInfo info, int delta)
        {
            Array.Copy(srcArray, ToRealIndex(info.LeftPart.Index), destArray, ToRealIndex(info.LeftPart.Index + delta), info.LeftPart.Length);
            delta += info.LeftPoint.Count;

            Array.Copy(srcArray, ToRealIndex(info.MiddlePart.Index), destArray, ToRealIndex(info.MiddlePart.Index + delta), info.MiddlePart.Length);
            delta += info.RightPoint.Count;

            Array.Copy(srcArray, ToRealIndex(info.RightPart.Index), destArray, ToRealIndex(info.RightPart.Index + delta), info.RightPart.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ShiftArrayRight(TValue[] srcArray, TValue[] destArray, ref readonly ArraySplitInfo info, int delta)
        {
            Array.Copy(srcArray, ToRealIndex(info.RightPart.Index), destArray, ToRealIndex(info.RightPart.Index + delta), info.RightPart.Length);
            delta -= info.RightPoint.Count;

            Array.Copy(srcArray, ToRealIndex(info.MiddlePart.Index), destArray, ToRealIndex(info.MiddlePart.Index + delta), info.MiddlePart.Length);
            delta -= info.LeftPoint.Count;

            Array.Copy(srcArray, ToRealIndex(info.LeftPart.Index), destArray, ToRealIndex(info.LeftPart.Index + delta), info.LeftPart.Length);
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

            if (ItemType<TValue>.ContainsReferences && delta > 0)
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

            if (ItemType<TValue>.ContainsReferences && delta < 0)
                Array.Clear(_storage, newPartIndex + partLength, -delta);
        }
    }
}
