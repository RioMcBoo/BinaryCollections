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

        private class ItemCounter
        {
            public int TargetCount;
            public int SourceCount;

            public ItemCounter(int targetCount = 0, int sourceCount = 0)
            {
                TargetCount = targetCount;
                SourceCount = sourceCount;
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
