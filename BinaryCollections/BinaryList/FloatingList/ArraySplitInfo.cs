
namespace BinaryCollections.BinaryList
{
    public partial class FloatingList<TValue>
    {
        protected enum Operation
        {
            None,
            Insert,
            Remove,
        }

        protected readonly struct PointInfo
        {
            public readonly int Index;
            public readonly int Count;
            public readonly Operation Operation;

            public PointInfo(int index, int count = 0)
            {
                Index = index;
                Count = count;
                Operation = Count switch
                {
                    > 0 => Operation.Insert,
                    < 0 => Operation.Remove,
                    _ => Operation.None,
                };
            }

            public PointInfo(int index, Operation operation)
            {
                Index = index;
                Operation = operation;
                Count = operation switch
                {
                    Operation.Insert => 1,
                    Operation.Remove => -1,
                    _ => 0,
                };
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

        protected struct AlignmentInfoContainer
        {
            public AlignmentInfo Middle;
            public AlignmentInfo Left;
            public AlignmentInfo Right;
        }

        protected readonly ref struct ReplaceInfo
        {
            public readonly ArraySplitInfo Data;

            public ReplaceInfo(int removeVIndex, int insertVIndex, int arrayLength)
            {
                PointInfo removePoint = new(removeVIndex, Operation.Remove);
                PointInfo insertPoint = new(insertVIndex, Operation.Insert);
                Data = new ArraySplitInfo(removePoint, insertPoint, arrayLength);
            }
        }

        protected readonly ref struct InsertInfo
        {
            public readonly ArraySplitInfo Data;

            public InsertInfo(int vIndex, int arrayLength, int count = 1)
            {
                PointInfo removePoint = new(vIndex, Operation.None);
                PointInfo insertPoint = new(vIndex, count);
                Data = new ArraySplitInfo(removePoint, insertPoint, arrayLength);
            }
        }

        protected readonly ref struct RemoveInfo
        {
            public readonly ArraySplitInfo Data;

            public RemoveInfo(int vIndex, int arrayLength, int count = 1)
            {
                PointInfo removePoint = new(vIndex, -count);
                PointInfo insertPoint = new(vIndex, Operation.None);
                Data = new ArraySplitInfo(removePoint, insertPoint, arrayLength);
            }
        }

        protected readonly ref struct ArraySplitInfo
        {
            private readonly int _addIndexShift;

            public readonly AlignmentInfoContainer Alignment;
            public readonly PartInfo LeftPart;
            public readonly PointInfo LeftPoint;
            public readonly PartInfo MiddlePart;
            public readonly PointInfo RightPoint;
            public readonly PartInfo RightPart;

            public int ArraySizeDelta => LeftPoint.Count + RightPoint.Count;
            public int AddIndexShift => _addIndexShift;

            public ArraySplitInfo(PointInfo removePoint, PointInfo insertPoint, int arrayLength)
            {
                // Checking arguments for compliance
                if (removePoint.Operation == insertPoint.Operation
                    || (removePoint.Operation != Operation.Remove && insertPoint.Operation != Operation.Insert))
                {
                    throw new ArgumentException("BinaryList.FloatingList.OperationInfo: Invalid operation combination");
                }

                // If one operation is missing, its index must be equal to the another operation.
                if (removePoint.Operation == Operation.None && removePoint.Index != insertPoint.Index)
                {
                    throw new ArgumentException("BinaryList.FloatingList.OperationInfo.removePoint: Invalid Index");
                }

                // If one operation is missing, its index must be equal to the another operation.
                if (insertPoint.Operation == Operation.None && insertPoint.Index != removePoint.Index)
                {
                    throw new ArgumentException("BinaryList.FloatingList.OperationInfo.insertPoint: Invalid Index");
                }

                // Checking for array out of bounds when removing items from the array
                if (arrayLength - removePoint.Index + removePoint.Count < 0)
                {
                    throw new ArgumentException("BinaryList.FloatingList.OperationInfo: Invalid Remove items count");
                }

                var leftPoint = removePoint;
                var rightPoint = insertPoint;

                // Sort points by index and operations
                if (leftPoint.Index > rightPoint.Index)
                {
                    (leftPoint, rightPoint) = (rightPoint, leftPoint);
                }

                (int Index, int Length) leftPart, middlePart, rightPart;
                int addIndexShift;

                if (leftPoint.Operation == Operation.Insert || rightPoint.Operation == Operation.Remove)
                {
                    leftPart.Index = 0;
                    leftPart.Length = leftPoint.Index;

                    middlePart.Index = leftPoint.Index;
                    middlePart.Length = rightPoint.Index - middlePart.Index;

                    rightPart.Index = rightPoint.Index - rightPoint.Count;
                    rightPart.Length = arrayLength - rightPart.Index;

                    addIndexShift = 0;
                }
                else if (leftPoint.Operation == Operation.Remove || rightPoint.Operation == Operation.Insert)
                {
                    leftPart.Index = 0;
                    leftPart.Length = leftPoint.Index;

                    middlePart.Index = leftPoint.Index - leftPoint.Count;
                    middlePart.Length = rightPoint.Index - middlePart.Index;

                    rightPart.Index = middlePart.Index > rightPoint.Index ? middlePart.Index : rightPoint.Index;
                    rightPart.Length = arrayLength - rightPart.Index;

                    var indexDiffTemp = leftPoint.Index - rightPoint.Index;
                    addIndexShift = leftPoint.Count > indexDiffTemp ? leftPoint.Count : indexDiffTemp;
                }
                else
                {
                    throw new InvalidOperationException("BinaryList.FloatingList.OperationInfo: Checking was not fully implemented");
                }

                Alignment.Middle = new FloatingList<TValue>.AlignmentInfo(
                        leftEdgeIndexShift: -leftPoint.Count,
                        rightEdgeIndexShift: rightPoint.Count
                        );
                Alignment.Left = new FloatingList<TValue>.AlignmentInfo(
                        leftEdgeIndexShift: 0,
                        rightEdgeIndexShift: rightPoint.Count + leftPoint.Count
                        );
                Alignment.Right = new FloatingList<TValue>.AlignmentInfo(
                        leftEdgeIndexShift: -rightPoint.Count - leftPoint.Count,
                        rightEdgeIndexShift: 0
                        );

                LeftPart = new PartInfo(leftPart.Index, leftPart.Length);
                LeftPoint = leftPoint;
                MiddlePart = new PartInfo(middlePart.Index, middlePart.Length);
                RightPoint = rightPoint;
                RightPart = new PartInfo(rightPart.Index, rightPart.Length);
                _addIndexShift = addIndexShift;
            }
        }
    }
}

