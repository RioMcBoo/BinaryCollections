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

            if (boundaries.Length < 1)
                return 0;

            index = ToVirtualIndex(boundaries.Index);
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

        private ArrayRange GetBoundariesToRemove(ref readonly TValue item)
        {
            var boundaries = FindBoundaries(in item, Values, Comparer);

            if (boundaries.Left < 0 || boundaries.Right < 0)
                return new();

            int collisionStartIndex = ToRealIndex(boundaries.Left);
            int collisionLength = boundaries.Right - boundaries.Left + 1;

            return ExtractRangeFromCollisions(in item, collisionStartIndex, collisionLength);

            ArrayRange ExtractRangeFromCollisions(ref readonly TValue item, int rIndex, int length)
            {
                if (ContainsCollisions)
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
