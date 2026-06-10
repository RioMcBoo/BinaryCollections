using System.Buffers;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool IsSupersetOfUnique(ReadOnlySpan<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return IsSupersetOfSortedUnique(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool IsSupersetOfUnique(IReadOnlyCollection<TValue> range)
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
                return IsSupersetOfSortedUnique(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool IsSupersetOfUnique(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return IsSupersetOfUnique(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return IsSupersetOfUnique(collection);
            }

            List<TValue> temp = new List<TValue>();

            foreach (var item in range)
            {
                temp.Add(item);
            }

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return IsSupersetOfSortedUnique(span);
        }

        public bool IsSupersetOfUnique(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                if (Flags.HasFlag(BinaryListFlags.NoDuplicates) &&
                    range.Flags.HasFlag(BinaryListFlags.NoDuplicates) &&
                    range.Count > Count)
                {
                    return false;
                }

                return IsSupersetOfSortedUnique(range.GetSpan(), range.Flags);
            }

            return IsSupersetOfUnique(range.GetSpan());
        }

        public bool IsSupersetOfUnique(BinaryList<TValue> range)
        {
            return IsSupersetOfUnique(range.GetEnumerator());
        }

        private bool IsSupersetOfSortedUnique(ReadOnlySpan<TValue> range, BinaryListFlags flags = BinaryListFlags.None)
        {
            if (Count == 0 && range.Length == 0)
                return true;

            if (Count == 0)
                return false;

            if (range.Length == 0)
                return true;

            Iterator target = new(Values, Flags);
            Iterator source = new(range, flags);
            return IsSubsetOfSortedUnique(source, target, _comparer, out bool _, out bool _);
        }
    }
}
