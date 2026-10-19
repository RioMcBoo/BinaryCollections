using System.Buffers;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool IsProperSubsetOf(ReadOnlySpan<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return IsProperSubsetOfSorted(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool IsProperSubsetOf(IReadOnlyCollection<TValue> range)
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
                return IsProperSubsetOfSorted(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool IsProperSubsetOf(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return IsProperSubsetOf(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return IsProperSubsetOf(collection);
            }

            List<TValue> temp = new List<TValue>();

            foreach (var item in range)
            {
                temp.Add(item);
            }

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return IsProperSubsetOfSorted(span);
        }

        public bool IsProperSubsetOf(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                return IsProperSubsetOfSorted(range.GetSpan(), range.Flags);
            }

            return IsProperSubsetOf(range.GetSpan());
        }

        public bool IsProperSubsetOf(BinaryList<TValue> range)
        {
            return IsProperSubsetOf(range.GetEnumerator());
        }

        private bool IsProperSubsetOfSorted(ReadOnlySpan<TValue> range, DuplicateDensity flags = DuplicateDensity.Possible)
        {
            if (Count == 0 && range.Length == 0)
                return false;

            if (Count == 0)
                return true;

            if (range.Length == 0)
                return false;

            if (range.Length <= Count)
                return false;

            Iterator first = new(Values, Flags);
            Iterator second = new(range, flags);

            if (IsSubsetOfSorted(first, second, _comparer, out bool hasExtraDuplicates, out bool hasExtraUniqueElements))
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
