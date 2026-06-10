using System.Buffers;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool IsProperSupersetOf(ReadOnlySpan<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return IsProperSupersetOfSorted(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool IsProperSupersetOf(IReadOnlyCollection<TValue> range)
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
                return IsProperSupersetOfSorted(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool IsProperSupersetOf(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return IsProperSupersetOf(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return IsProperSupersetOf(collection);
            }

            List<TValue> temp = new List<TValue>();

            foreach (var item in range)
            {
                temp.Add(item);
            }

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return IsProperSupersetOfSorted(span);
        }

        public bool IsProperSupersetOf(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                return IsProperSupersetOfSorted(range.GetSpan(), range.Flags);
            }

            return IsProperSupersetOf(range.GetSpan());
        }

        public bool IsProperSupersetOf(BinaryList<TValue> range)
        {
            return IsProperSupersetOf(range.GetEnumerator());
        }

        private bool IsProperSupersetOfSorted(ReadOnlySpan<TValue> range, BinaryListFlags flags = BinaryListFlags.None)
        {
            if (Count == 0 && range.Length == 0)
                return false;

            if (Count == 0)
                return false;

            if (range.Length == 0)
                return true;

            if (range.Length >= Count)
                return false;

            Iterator target = new(Values, Flags);
            Iterator source = new(range, flags);

            if (IsSubsetOfSorted(source, target, _comparer, out bool hasExtraDuplicates, out bool hasExtraUniqueElements))
            {
                return hasExtraUniqueElements || hasExtraDuplicates;
            }
            else
            {
                return false;
            }
        }
    }
}
