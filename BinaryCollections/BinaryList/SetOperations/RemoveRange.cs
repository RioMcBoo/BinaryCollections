namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public int RemoveRange(TValue item)
        {
            return RemoveRange(ref item, out _);
        }

        public int RemoveRange(ref readonly TValue item, out int index)
        {
            index = -1;

            var boundaries = GetBoundariesToRemove(in item);
            var vIndex = ToVirtualIndex(boundaries.Index);

            if (RemoveRangeInternal(vIndex, boundaries.Length))
                index = vIndex;

            return boundaries.Length;
        }

        public bool RemoveRange(int index, int length)
        {
            if (!CheckIndex(index, length))
                return false;

            return RemoveRangeInternal(index, length);
        }

        private bool RemoveRangeInternal(int vIndex, int length)
        {
            if (length > 0)
            {
                var removeInfo = new RemoveInfo(vIndex, _count, length);
                var info = removeInfo.Data;

                if (info.LeftPart.Length <= info.RightPart.Length)
                {
                    ShiftLeftPart(ref info);
                }
                else if (info.RightPart.Length < info.LeftPart.Length)
                {
                    ShiftRightPart(ref info);
                }

                _count -= length;
                _version = unchecked(++_version);

                return true;
            }

            return false;
        }

        private ArrayRange GetBoundariesToRemove(ref readonly TValue item)
        {
            var (left, right) = FindBoundaries(in item, Values, Comparer);

            if (left < 0 || right < 0)
                return new();

            int collisionStartIndex = ToRealIndex(left);
            int collisionLength = right - left + 1;

            return ExtractRangeFromCollisions(in item, collisionStartIndex, collisionLength);

            ArrayRange ExtractRangeFromCollisions(ref readonly TValue item, int rIndex, int length)
            {
                if (ContaninsCollisions)
                {
                    // We need to group other items within collisions together

                    int t = 0;
                    int write = rIndex;
                    _version = unchecked(++_version);

                    for (int read = rIndex; read < rIndex + length; read++)
                    {
                        ref readonly var currItem = ref _storage[read];
                        if (!IsIdentical(in currItem, in item))
                        {
                            _storage[write] = currItem;
                            write++;
                        }
                    }

                    rIndex = write;
                    length -= write - rIndex;
                }

                return new ArrayRange(rIndex, length, _storage);
            }
        }
    }
}
