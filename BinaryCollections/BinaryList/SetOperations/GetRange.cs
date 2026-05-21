namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public BinaryListEnumerator<TValue> GetRange(TValue item)
        {
            return GetRange(ref item);
        }

        public BinaryListEnumerator<TValue> GetRange(ref readonly TValue item)
        {
            var boundaries = GetBoundariesToShare(in item);
            return new(this, boundaries.Source, boundaries.RealIndex, boundaries.Length, BinaryListFlags.UniformRange);
        }

        public BinaryListEnumerator<TValue> GetRange(int index, int length)
        {
            if (!CheckIndex(index, length))
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range");

            return new(this, _storage, ToRealIndex(index), length);
        }

        private ListRangeInfo GetBoundariesToShare(ref readonly TValue item)
        {
            var (left, right) = FindBoundaries(in item);

            if (left < 0)
                return new();

            TValue[] source = _storage;
            int initialRIndex = ToRealIndex(left);
            int lengthInitial = right - left + 1;

            CopyRangeFromCollisions(in item, ref initialRIndex, ref lengthInitial, out source);

            return new(initialRIndex, lengthInitial, source);

            void CopyRangeFromCollisions(ref readonly TValue item, ref int rIndex, ref int length, out TValue[] source)
            {
                source = new TValue[length];
                int write = 0;

                for (int read = rIndex; read < rIndex + length; read++)
                {
                    ref readonly var currItem = ref _storage[read];
                    if (IsIdentical(in currItem, in item))
                    {
                        source[write] = currItem;
                        write++;
                    }
                }

                rIndex = 0;
                length = write;
            }
        }
    }
}
