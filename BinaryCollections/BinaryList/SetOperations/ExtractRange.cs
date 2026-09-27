using System.Buffers;
namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public TValue[] ExtractRange(TValue item)
        {
            ExtractRange(ref item, out _, out var range);
            return range;
        }

        public bool ExtractRange(ref readonly TValue item, out int index, out TValue[] range)
        {
            index = -1;

            var boundaries = GetBoundariesToExtract(in item);
            if (boundaries.Length == 0)
            {
                range = null!;
                return false;
            }

            range = new TValue[boundaries.Length];
            Array.Copy(boundaries.Source, boundaries.Index, range, 0, boundaries.Length);

            if (RemoveRangeInternal(ToVirtualIndex(boundaries.Index), boundaries.Length))
            {
                index = ToVirtualIndex(boundaries.Index);
            }

            if (_count < 2)
                ModifyFlags(add: BinaryListFlags.NoDuplicates, remove: BinaryListFlags.UniformRange);

            return true;
        }

        private ArrayRange GetBoundariesToExtract(ref readonly TValue item)
        {
            var (left, right) = FindBoundaries(in item, Values, Comparer);

            if (left < 0 || right < 0)
                return new();

            int collisionStartIndex = ToRealIndex(left);
            int collisionLength = right - left + 1;

            return ExtractRangeFromCollisions(in item, collisionStartIndex, collisionLength);

            ArrayRange ExtractRangeFromCollisions(ref readonly TValue item, int rIndex, int length)
            {
                if (ContainsCollisions)
                {
                    // We need to group duplicates within collisions together
                    TValue[] temp = ArrayPool<TValue>.Shared.Rent(length);
                    try
                    {
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
                            else
                            {
                                temp[t++] = currItem;
                            }
                        }

                        Array.Copy(temp, 0, _storage, write, t);

                        rIndex = write;
                        length -= write - rIndex;
                    }
                    finally
                    {
                        ArrayPool<TValue>.Shared.Return(temp, true);
                    }
                }

                return new ArrayRange(rIndex, length, _storage);
            }
        }
    }
}
