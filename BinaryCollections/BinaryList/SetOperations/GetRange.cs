using ExceptionHelper;

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
            if (_count == 0)
                return GetEnumerator();

            if (HasFlag(BinaryListFlags.UniformRange) && IsIdentical(in _storage[ToRealIndex(0)], in item))
                return GetEnumerator();

            var boundaries = GetBoundariesToShare(in item);
            return new(this, boundaries.Source, boundaries.Index, boundaries.Length, BinaryListFlags.UniformRange);
        }

        public BinaryListEnumerator<TValue> GetRange(int index, int length)
        {
            if (!CheckIndex(index, length))
                Throw.ArgumentException(nameof(GetRange), $"{nameof(index)} or {nameof(length)} is out of range");

            return new(this, _storage, ToRealIndex(index), length);
        }

        private ArrayRange GetBoundariesToShare(ref readonly TValue item)
        {
            var (left, right) = FindBoundaries(in item, Values, Comparer);

            if (left < 0 || right < 0)
                return new();

            int collisionStartIndex = ToRealIndex(left);
            int collisionLength = right - left + 1;

            return ExtractRangeFromCollisions(in item, collisionStartIndex, collisionLength);

            ArrayRange ExtractRangeFromCollisions(ref readonly TValue item, int rIndex, int length)
            {
                TValue[] source;

                if (ContainsCollisions)
                {
                    // We cannot make changes to the current array.
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
                else
                {
                    source = _storage;
                }

                return new(rIndex, length, source);
            }
        }
    }
}
