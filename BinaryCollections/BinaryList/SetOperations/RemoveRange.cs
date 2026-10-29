namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public int RemoveRange(TValue item)
        {
            return RemoveRange(ref item, out _);
        }

        public int RemoveRangeByOrder(TValue item)
        {
            return RemoveRange(ref item, out _, true);
        }

        public int RemoveRange(ref readonly TValue item, out int index, bool byOrder = false)
        {
            index = -1;

            if (!_comparer.FindBoundaries(in item, Values, out var boundaries, byOrder))
                return 0;

            index = ToVirtualIndex(boundaries.Left);
            RemoveRangeInternal(index, boundaries.Length);               

            return boundaries.Length;
        }

        public bool RemoveRange(int index, int length)
        {
            if (!CheckIndex(index, length))
                return false;

            RemoveRangeInternal(index, length);
            return true;
        }
    }
}
