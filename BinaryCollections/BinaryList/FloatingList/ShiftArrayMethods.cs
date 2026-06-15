namespace BinaryCollections.BinaryList
{
    public partial class FloatingList<TValue>
    {
        protected void ShiftArray(TValue[] srcArray, TValue[] destArray, ref readonly ArraySplitInfo info, int delta)
        {
            // we move the array relative to the zero index, so this is related to left alignment
            AlignmentInfo alignmentInfo = info.Alignment.Left;

            int newZeroRIndex = delta + _zeroRIndex;

            if (delta < 0)
            {
                ShiftArrayLeft(srcArray, destArray, in info, delta + alignmentInfo.LeftEdgeIndexShift);
            }
            else
            {
                ShiftArrayRight(srcArray, destArray, in info, delta + alignmentInfo.RightEdgeIndexShift);
            }

            if (ContainsReferences && srcArray == destArray)
            {
                int cleanLength_ShiftLeft = -delta - alignmentInfo.RightEdgeIndexShift;
                int cleanIndex_ShiftLeft = _count + delta + alignmentInfo.RightEdgeIndexShift;

                int cleanLength_ShiftRight = delta;
                int cleanIndex_ShiftRight = 0;

                ClearArray(destArray, cleanIndex_ShiftLeft, cleanLength_ShiftLeft);
                ClearArray(destArray, cleanIndex_ShiftRight, cleanLength_ShiftRight);
            }

            _zeroRIndex = newZeroRIndex;
        }

        protected void ShiftArrayLeft(TValue[] srcArray, TValue[] destArray, ref readonly ArraySplitInfo info, int delta)
        {
            Move(srcArray, info.LeftPart.Index, destArray, info.LeftPart.Index + delta, info.LeftPart.Length);
            delta += info.LeftPoint.Count;

            Move(srcArray, info.MiddlePart.Index, destArray, info.MiddlePart.Index + delta, info.MiddlePart.Length);
            delta += info.RightPoint.Count;

            Move(srcArray, info.RightPart.Index, destArray, info.RightPart.Index + delta, info.RightPart.Length);
        }

        protected void ShiftArrayRight(TValue[] srcArray, TValue[] destArray, ref readonly ArraySplitInfo info, int delta)
        {
            Move(srcArray, info.RightPart.Index, destArray, info.RightPart.Index + delta, info.RightPart.Length);
            delta -= info.RightPoint.Count;

            Move(srcArray, info.MiddlePart.Index, destArray, info.MiddlePart.Index + delta, info.MiddlePart.Length);
            delta -= info.LeftPoint.Count;

            Move(srcArray, info.LeftPart.Index, destArray, info.LeftPart.Index + delta, info.LeftPart.Length);
        }
    }
}
