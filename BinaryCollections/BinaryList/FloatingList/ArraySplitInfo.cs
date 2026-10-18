using ExceptionHelper;
using System.Diagnostics;

namespace BinaryCollections.BinaryList
{
    public partial class FloatingList<TValue>
    {
        protected readonly struct PointInfo
        {
            public readonly int Index;
            public readonly int Count;

            public PointInfo(int index, int count = 0)
            {
                Index = index;
                Count = count;
            }
        }

        protected readonly struct PartInfo
        {
            public readonly int Index;
            public readonly int Length;

            public PartInfo(int index, int length)
            {
                Index = index;
                Length = length < 0 ? 0 : length;
            }
        }

        protected readonly struct AlignmentInfo
        {
            public readonly int LeftEdgeIndexShift;
            public readonly int RightEdgeIndexShift;

            public AlignmentInfo(int leftEdgeIndexShift, int rightEdgeIndexShift)
            {
                LeftEdgeIndexShift = leftEdgeIndexShift;
                RightEdgeIndexShift = rightEdgeIndexShift;
            }
        }

        protected readonly struct AlignmentInfoContainer
        {
            public readonly AlignmentInfo Middle;
            public readonly AlignmentInfo Left;
            public readonly AlignmentInfo Right;

            public AlignmentInfoContainer(AlignmentInfo middle, AlignmentInfo left, AlignmentInfo right)
            {
                Middle = middle;
                Left = left;
                Right = right;
            }
        }

        protected readonly struct ArraySplitInfo
        {
            public readonly int ReplaceAddIndexShift;
            public readonly int ArraySizeDelta;
            public readonly AlignmentInfoContainer Alignment;
            public readonly PartInfo LeftPart;
            public readonly PointInfo LeftPoint;
            public readonly PartInfo MiddlePart;
            public readonly PointInfo RightPoint;
            public readonly PartInfo RightPart;

            public static ArraySplitInfo GetRemoveInfo(int vIndex, int arrayLength, int count = 1)
            {
                PointInfo removePoint = new(vIndex, -count);
                PointInfo insertPoint = new(vIndex, 0);
                return new ArraySplitInfo(removePoint, insertPoint, arrayLength);
            }

            public static ArraySplitInfo GetInsertInfo(int vIndex, int arrayLength, int count = 1)
            {
                PointInfo removePoint = new(vIndex, 0);
                PointInfo insertPoint = new(vIndex, count);
                return new ArraySplitInfo(removePoint, insertPoint, arrayLength);
            }

            public static ArraySplitInfo GetReplaceInfo(int removeVIndex, int insertVIndex, int arrayLength)
            {
                PointInfo removePoint = new(removeVIndex, -1);
                PointInfo insertPoint = new(insertVIndex, 1);
                return new ArraySplitInfo(removePoint, insertPoint, arrayLength);
            }

            ArraySplitInfo(PointInfo removePoint, PointInfo insertPoint, int arrayLength)
            {
                // Checking arguments for compliance 1
                Debug.Assert(removePoint.Count != insertPoint.Count,
                    "BinaryList.FloatingList.ArraySplitInfo: Invalid operation combination");

                // Checking arguments for compliance 2
                Debug.Assert(removePoint.Count < 0 || insertPoint.Count > 0,
                    "BinaryList.FloatingList.ArraySplitInfo: Invalid operation combination");

                // If one operation is missing, its index must be equal to the another operation 1. 
                Debug.Assert(removePoint.Count != 0 || removePoint.Index == insertPoint.Index,
                    "BinaryList.FloatingList.ArraySplitInfo.removePoint: Invalid Index");

                // If one operation is missing, its index must be equal to the another operation 2.
                Debug.Assert(insertPoint.Count != 0 || insertPoint.Index == removePoint.Index,
                    "BinaryList.FloatingList.ArraySplitInfo.insertPoint: Invalid Index");

                var leftPoint = removePoint;
                var rightPoint = insertPoint;

                // Sort points by index and operations
                if (leftPoint.Index > rightPoint.Index)
                {
                    (leftPoint, rightPoint) = (rightPoint, leftPoint);
                }

                (int Index, int Length) leftPart, middlePart, rightPart;
                int replaceAddIndexShift;

                if (leftPoint.Count > 0 || rightPoint.Count < 0)
                {
                    leftPart.Index = 0;
                    leftPart.Length = leftPoint.Index;

                    middlePart.Index = leftPoint.Index;
                    middlePart.Length = rightPoint.Index - middlePart.Index;

                    rightPart.Index = rightPoint.Index - rightPoint.Count;
                    rightPart.Length = arrayLength - rightPart.Index;

                    replaceAddIndexShift = 0;
                }
                else if (leftPoint.Count < 0 || rightPoint.Count > 0)
                {
                    leftPart.Index = 0;
                    leftPart.Length = leftPoint.Index;

                    middlePart.Index = leftPoint.Index - leftPoint.Count;
                    middlePart.Length = rightPoint.Index - middlePart.Index;

                    rightPart.Index = middlePart.Index > rightPoint.Index ? middlePart.Index : rightPoint.Index;
                    rightPart.Length = arrayLength - rightPart.Index;

                    var indexDiffTemp = leftPoint.Index - rightPoint.Index;
                    replaceAddIndexShift = leftPoint.Count > indexDiffTemp ? leftPoint.Count : indexDiffTemp;
                }
                else
                {
                    Throw.InvalidOperationException(nameof(ArraySplitInfo), "Checking was not fully implemented");
                    return;
                }

                var alignmentMiddle = new AlignmentInfo(
                        leftEdgeIndexShift: -leftPoint.Count,
                        rightEdgeIndexShift: rightPoint.Count
                        );
                var alignmentLeft = new AlignmentInfo(
                        leftEdgeIndexShift: 0,
                        rightEdgeIndexShift: rightPoint.Count + leftPoint.Count
                        );
                var alignmentRight = new AlignmentInfo(
                        leftEdgeIndexShift: -rightPoint.Count - leftPoint.Count,
                        rightEdgeIndexShift: 0
                        );

                Alignment = new(alignmentMiddle, alignmentLeft, alignmentRight);

                LeftPart = new PartInfo(leftPart.Index, leftPart.Length);
                LeftPoint = leftPoint;
                MiddlePart = new PartInfo(middlePart.Index, middlePart.Length);
                RightPoint = rightPoint;
                RightPart = new PartInfo(rightPart.Index, rightPart.Length);

                ReplaceAddIndexShift = replaceAddIndexShift;
                ArraySizeDelta = LeftPoint.Count + RightPoint.Count;
            }
        }
    }
}

