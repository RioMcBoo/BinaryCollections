using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BinaryCollections.BinaryList
{
    public partial class FloatingList<TValue>
    {
        protected readonly struct Part
        {
            readonly public int VIndex;
            readonly public int Length;

            public Part(int vIndex, int length)
            {
                VIndex = vIndex;
                Length = length;
            }
        }

        protected readonly struct Point
        {
            readonly public int VIndex;
            readonly public int ArraySizeDelta;
            public int PrevPartLength(int prevPartIndex) => VIndex - prevPartIndex;
            public int NextPartIndex => VIndex + (NewItemtPushes ? 0 : -ArraySizeDelta);
            public bool NewItemtPushes => ArraySizeDelta > 0;

            public Point(int vIndex, bool addItem)
            {
                VIndex = vIndex;
                ArraySizeDelta = addItem ? 1 : -1;
            }

            public Point(int vIndex, int arraySizeDelta)
            {
                VIndex = vIndex;
                ArraySizeDelta = arraySizeDelta;
            }
        }

        protected ref struct ArraySplitInfo
        {
            public Part LeftPart { get; private set; }
            public Point LeftPoint { get; private set; }
            public Part MiddPart { get; private set; }
            public Point RightPoint { get; private set; }
            public Part RightPart { get; private set; }
            public int ArraySizeDelta
            {
                get
                {
                    return (LeftPoint.ArraySizeDelta + RightPoint.ArraySizeDelta) switch
                    {
                        0 => 0,
                        _ => LeftPoint.ArraySizeDelta,
                    };
                }
            }

            public ArraySplitInfo(int removeVIndex, int addVIndex, int arrayLength)
            {
                Point leftPoint = new(removeVIndex, false);
                Point rightPoint = new(addVIndex, true);
                Initialize(leftPoint, rightPoint, arrayLength);
            }

            public ArraySplitInfo(int vIndex, bool add, int arrayLength)
            {
                Point leftPoint, rightPoint;
                leftPoint = rightPoint = new(vIndex, add);
                Initialize(leftPoint, rightPoint, arrayLength);
            }

            public ArraySplitInfo(Point point, int arrayLength)
            {
                Point leftPoint, rightPoint;
                leftPoint = rightPoint = point;
                Initialize(leftPoint, rightPoint, arrayLength);
            }

            private void Initialize(Point leftPoint, Point rightPoint, int arrayLength)
            {
                if (leftPoint.VIndex > rightPoint.VIndex)
                    (leftPoint, rightPoint) = (rightPoint, leftPoint);

                int leftPartVIndex = 0;
                int leftPartLength = leftPoint.VIndex;

                int middPartIndex = leftPoint.NextPartIndex;
                int middPartLength = rightPoint.PrevPartLength(middPartIndex);

                int rightPartVIndex = rightPoint.NextPartIndex;
                int rightPartLength = arrayLength - rightPoint.NextPartIndex;

                LeftPart = new(leftPartVIndex, leftPartLength);
                MiddPart = new(middPartIndex, middPartLength);
                RightPart = new(rightPartVIndex, rightPartLength);

                LeftPoint = leftPoint;
                RightPoint = rightPoint;
            }
        }

        protected int ToVirtualIndex(int realIndex) => realIndex - _zeroRIndex;
        protected int ToRealIndex(int virtualIndex) => virtualIndex + _zeroRIndex;

        protected bool CheckCountOverload(int rangeCount)
        {
            if (rangeCount > Array.MaxLength - _count)
            {
                return false;
            }

            return true;
        }

        protected void ThrowOverloadException()
        {
            throw new InvalidOperationException("Operation would result in an overflow.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CheckVersion(int version)
        {
            if (version != _version)
                ThrowInvalidOperation();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOperation()
        {
            throw new InvalidOperationException("Array was changed");
        }

        [Obsolete("Only for debugging purposes. If the element is a value type, it will be difficult to find boundaries", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public TValue[] ExtractRawArray(int newArraySize = -1)
        {
            _version = unchecked(++_version);
            TValue[] extracted = _storage;

            if (newArraySize >= 0)
            {
                _count = 0;
                _storage = new TValue[DefaultCapacity];
                _zeroRIndex = CalculateZeroIndex(_storage.Length, _count);
            }
            else
                _storage = null!;

            return extracted;
        }

        public TValue GetItem(int index)
        {
            if (CheckIndex(index))
            {
                return _storage[ToRealIndex(index)];
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range");
            }
        }

        [Obsolete("This method is dangerous because it allows you to change the internal state of collection elements, point to deleted or changed array! Use it only if you understand the consequences..", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref TValue GetItemRef(int index)
        {
            if (CheckIndex(index))
            {
                return ref _storage[ToRealIndex(index)];
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range");
            }
        }

        protected static int CalculateZeroIndex(int capacity, int count)
        {
            if (capacity < 0 || count < 0 || capacity < count)
            {
                throw new ArgumentOutOfRangeException(nameof(CalculateZeroIndex), "capacity < 0 || count < 0 || capacity < count");
            }

            return (capacity - count) / 2;
        }

        protected bool CheckIndex(int vIndex, int length = 1)
        {
            if (vIndex < 0 || vIndex + length > _count)
            {
                return false;
            }

            return true;
        }
    }
}
