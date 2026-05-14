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

        private class ItemHashCounter : IEquatable<ItemHashCounter>
        {
            public TValue Item;
            public int IncomingCount;
            public int CurrentCount;

            public ItemHashCounter(TValue item) { Item = item; }

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
                if (obj is ItemHashCounter other)
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

        internal static bool IsIdentical(ref readonly TValue currentItem, ref readonly TValue item)
        {
            return _equalityComparer.Equals(currentItem, item);
        }
    }
}
