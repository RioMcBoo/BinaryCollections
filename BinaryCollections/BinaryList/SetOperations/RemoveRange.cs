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
                var info = ArraySplitInfo.GetRemoveInfo(vIndex, _count, length);

                if (info.LeftPart.Length * BackwardShiftPenalty <= info.RightPart.Length)
                {
                    ShiftLeftPart(in info);
                }
                else
                {
                    ShiftRightPart(in info);
                }

                _count -= length;
                _version = unchecked(++_version);

                if (_count < 2)
                    ModifyFlags(add: BinaryListFlags.NoDuplicates, remove: BinaryListFlags.UniformRange);

                return true;
            }

            return false;
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
