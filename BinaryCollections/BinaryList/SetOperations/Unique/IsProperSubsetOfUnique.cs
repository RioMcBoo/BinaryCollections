using System.Buffers;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool IsProperSubsetOfUnique(ReadOnlySpan<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return IsProperSubsetOfSortedUnique(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool IsProperSubsetOfUnique(IReadOnlyCollection<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Count);
            try
            {
                int i = 0;
                foreach (var item in range)
                {
                    temp[i++] = item;
                }

                Array.Sort(temp, 0, range.Count, Comparer);
                return IsProperSubsetOfSortedUnique(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool IsProperSubsetOfUnique(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return IsProperSubsetOfUnique(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return IsProperSubsetOfUnique(collection);
            }

            List<TValue> temp = new List<TValue>();

            foreach (var item in range)
            {
                temp.Add(item);
            }

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return IsProperSubsetOfSortedUnique(span);
        }

        public bool IsProperSubsetOfUnique(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                if (Flags.HasFlag(BinaryListFlags.NoDuplicates) &&
                    range.Flags.HasFlag(BinaryListFlags.NoDuplicates) &&
                    range.Count <= Count)
                {
                    return false;
                }

                return IsProperSubsetOfSortedUnique(range.GetSpan(), range.Flags);
            }

            return IsProperSubsetOfUnique(range.GetSpan());
        }

        public bool IsProperSubsetOfUnique(BinaryList<TValue> range)
        {
            return IsProperSubsetOfUnique(range.GetEnumerator());
        }

        private bool IsProperSubsetOfSortedUnique(ReadOnlySpan<TValue> range, BinaryListFlags flags = BinaryListFlags.None)
        {
            if (Count == 0 && range.Length == 0)
                return false;

            if (Count == 0)
                return true;

            if (range.Length == 0)
                return false;

            Iterator source = new(Values, Flags);
            Iterator target = new(range, flags);

            if (IsSubsetOfSortedUnique(source, target, _comparer, out bool hasExtraDuplicates, out bool hasExtraUniqueElements))
            {
                return hasExtraUniqueElements;
            }
            else
            {
                return false;
            }
        }
    }
}
