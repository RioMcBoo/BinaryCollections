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
            Array.Copy(boundaries.Source, boundaries.RealIndex, range, 0, boundaries.Length);

            if (RemoveRangeInternal(ToVirtualIndex(boundaries.RealIndex), boundaries.Length))
            {
                index = ToVirtualIndex(boundaries.RealIndex);
            }

            return true;
        }

        private ListRangeInfo GetBoundariesToExtract(ref readonly TValue item)
        {
            var (left, right) = FindBoundaries(in item, Values);

            if (left < 0)
                return new();

            TValue[] source = _storage;
            int initialRIndex = ToRealIndex(left);
            int lengthInitial = right - left + 1;

            ExtractRangeFromCollisions(in item, ref initialRIndex, ref lengthInitial, out source);

            return new(initialRIndex, lengthInitial, source);

            void ExtractRangeFromCollisions(ref readonly TValue item, ref int rIndex, ref int length, out TValue[] source)
            {
                int write = rIndex;
                _version = unchecked(++_version);

                TValue[] temp = ArrayPool<TValue>.Shared.Rent(length); int t = 0;

                try
                {
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
                }
                finally
                {
                    ArrayPool<TValue>.Shared.Return(temp, true);
                }

                source = _storage;
                int keptCountInBlock = write - rIndex;

                rIndex = write;
                length = lengthInitial - keptCountInBlock;
            }
        }
    }
}
