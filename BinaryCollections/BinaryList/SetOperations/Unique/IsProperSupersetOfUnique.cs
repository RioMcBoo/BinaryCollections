using System.Buffers;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool IsProperSupersetOfUnique(ReadOnlySpan<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return IsProperSupersetOfSortedUnique(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool IsProperSupersetOfUnique(IReadOnlyCollection<TValue> range)
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
                return IsProperSupersetOfSortedUnique(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool IsProperSupersetOfUnique(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return IsProperSupersetOfUnique(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return IsProperSupersetOfUnique(collection);
            }

            List<TValue> temp = new List<TValue>();

            foreach (var item in range)
            {
                temp.Add(item);
            }

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return IsProperSupersetOfSortedUnique(span);
        }

        public bool IsProperSupersetOfUnique(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                if (Flags.HasFlag(BinaryListFlags.NoDuplicates) &&
                    range.Flags.HasFlag(BinaryListFlags.NoDuplicates) &&
                    range.Count >= Count)
                {
                    return false;
                }

                return IsProperSupersetOfSortedUnique(range.GetSpan(), range.Flags);
            }

            return IsProperSupersetOfUnique(range.GetSpan());
        }

        public bool IsProperSupersetOfUnique(BinaryList<TValue> range)
        {
            return IsProperSupersetOfUnique(range.GetEnumerator());
        }

        private bool IsProperSupersetOfSortedUnique(ReadOnlySpan<TValue> range, BinaryListFlags flags = BinaryListFlags.None)
        {
            if (Count == 0 && range.Length == 0)
                return false;

            if (Count == 0)
                return false;

            if (range.Length == 0)
                return true;

            Iterator second = new(Values, Flags);
            Iterator first = new(range, flags);

            if (IsSubsetOfSortedUnique(ref first, ref second, _comparer, out bool hasExtraDuplicates, out bool hasExtraUniqueElements))
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
