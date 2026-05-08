using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BinaryCollections.BinaryList
{
    public partial class BinaryList<TValue>
    {
        private ref struct BinarySearchState
        {
            public int lo;
            public int hi;
            public int i;
        }

        private readonly ref struct ListRangeInfo
        {
            readonly public int RealIndex;
            readonly public int Length;
            readonly public TValue[] Source;

            public ListRangeInfo(int realIndex, int length, TValue[] source)
            {
                RealIndex = realIndex;
                Length = length;
                Source = source;
            }
        }

        private readonly struct Part
        {
            readonly public int VIndex;
            readonly public int Length;

            public Part(int vIndex, int length)
            {
                VIndex = vIndex;
                Length = length;
            }
        }

        private readonly struct Point
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

        private ref struct ArraySplitInfo
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

        private struct ItemBinaryCounter : IComparable<ItemBinaryCounter>, IEquatable<ItemBinaryCounter>
        {
            public TValue Item;
            public int IncomingCount;
            public int CurrentCount;

            public ItemBinaryCounter(TValue item) { Item = item; }

            public int CompareTo(ItemBinaryCounter other)
            {
                return _defaultComparer.Compare(Item, other.Item);
            }

            public bool Equals(ItemBinaryCounter other)
            {
                return _equalityComparer.Equals(Item, other.Item);
            }
        }

        private class ItemHashCounter : IComparable<ItemHashCounter>, IEquatable<ItemHashCounter>
        {
            public TValue Item;
            public int IncomingCount;
            public int CurrentCount;

            public ItemHashCounter(TValue item) { Item = item; }

            public int CompareTo(ItemHashCounter? other)
            {
                return _defaultComparer.Compare(Item, other!.Item);
            }

            public bool Equals(ItemHashCounter? other)
            {
                return _equalityComparer.Equals(Item, other!.Item);
            }

            public override int GetHashCode()
            {
                return Item?.GetHashCode() ?? 0;
            }

            public override bool Equals(object? obj)
            {
                if (obj is ItemBinaryCounter other)
                {
                    return Equals(other);
                }

                return false;
            }
        }

        private ref struct SourceInfo
        {
            public ReadOnlySpan<TValue> Array;
            public int Index;
            public int Count;
        }

        private ref struct LoopInfo
        {
            public int IndexDirection;
            public SourceInfo Current;
            public SourceInfo Incoming;
            public SourceInfo Destination;
        }

        private bool CheckAddRange(int rangeCount)
        {
            if (rangeCount <= 0)
                return false;

            if (rangeCount > Array.MaxLength - _count)
                return false;

            return true;
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

        [Obsolete("This method is dangerous because it allows you to change the internal state of collection elements. Use it only if you understand the consequences..", false)]
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

        private static int CalculateZeroIndex(int capacity, int count)
        {
            if (capacity < 0 || count < 0 || capacity < count)
            {
                throw new ArgumentOutOfRangeException(nameof(CalculateZeroIndex), "capacity < 0 || count < 0 || capacity < count");
            }

            return (capacity - count) / 2;
        }

        internal static bool IsIdentical(ref readonly TValue currentItem, ref readonly TValue item)
        {
            return _equalityComparer.Equals(currentItem, item);
        }

        private bool CheckIndex(int vIndex, int length = 1)
        {
            if (vIndex < 0 || vIndex + length > _count)
            {
                return false;
            }

            return true;
        }
    }
}
