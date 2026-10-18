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

            if (ContainsReferences && srcArray == destArray)
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
    }
}
