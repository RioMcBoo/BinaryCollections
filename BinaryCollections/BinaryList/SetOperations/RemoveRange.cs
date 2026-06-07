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
            var vIndex = ToVirtualIndex(boundaries.RealIndex);

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
                var info = new ArraySplitInfo(new Point(vIndex, -length), _count);

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

        private ListRangeInfo GetBoundariesToRemove(ref readonly TValue item)
        {
            var (left, right) = FindBoundaries(in item, Values, Comparer);

            if (left < 0)
                return new();

            TValue[] source = _storage;
            int initialRIndex = ToRealIndex(left);
            int lengthInitial = right - left + 1;

            RemoveRangeFromCollisions(in item, ref initialRIndex, ref lengthInitial, out source);

            return new(initialRIndex, lengthInitial, source);

            void RemoveRangeFromCollisions(ref readonly TValue item, ref int rIndex, ref int length, out TValue[] source)
            {
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

                source = _storage;
                int keptCountInBlock = write - rIndex;

                rIndex = write;
                length = lengthInitial - keptCountInBlock;
            }
        }
    }
}
