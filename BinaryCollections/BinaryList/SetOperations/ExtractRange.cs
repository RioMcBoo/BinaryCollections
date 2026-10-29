namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public TValue[] ExtractRange(TValue item)
        {
            ExtractRange(ref item, out _, out var range);
            return range;
        }

        public TValue[] ExtractRangeByOrder(TValue item)
        {
            ExtractRange(ref item, out _, out var range, true);
            return range;
        }

        public void ExtractRange(ref readonly TValue item, out int index, out TValue[] range, bool byOrder = false)
        {
            index = -1;
            
            if (!_comparer.FindBoundaries(in item, Values, out var boundaries, byOrder))
            {
                range = null!;
                return;
            }

            range = new TValue[boundaries.Length];
            Array.Copy(_storage, ToRealIndex(boundaries.Left), range, 0, boundaries.Length);

            RemoveRangeInternal(boundaries.Left, boundaries.Length);
            _flags.CheckFor(_count);

            index = boundaries.Left;
        }
    }
}
