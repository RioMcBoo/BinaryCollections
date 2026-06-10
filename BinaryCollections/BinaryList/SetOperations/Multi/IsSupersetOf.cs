using System.Buffers;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool IsSupersetOf(ReadOnlySpan<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return IsSupersetOfSorted(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool IsSupersetOf(IReadOnlyCollection<TValue> range)
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
                return IsSupersetOfSorted(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool IsSupersetOf(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return IsSupersetOf(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return IsSupersetOf(collection);
            }

            List<TValue> temp = new List<TValue>();

            foreach (var item in range)
            {
                temp.Add(item);
            }

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return IsSupersetOfSorted(span);
        }

        public bool IsSupersetOf(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                return IsSupersetOfSorted(range.GetSpan(), range.Flags);
            }

            return IsSupersetOf(range.GetSpan());
        }

        public bool IsSupersetOf(BinaryList<TValue> range)
        {
            return IsSupersetOf(range.GetEnumerator());
        }

        private bool IsSupersetOfSorted(ReadOnlySpan<TValue> range, BinaryListFlags flags = BinaryListFlags.None)
        {
            if (Count == 0 && range.Length == 0)
                return true;

            if (Count == 0)
                return false;

            if (range.Length == 0)
                return true;

            if (range.Length > Count)
                return false;

            Iterator target = new(Values, Flags);
            Iterator source = new(range, flags);

            return IsSubsetOfSorted(source, target, _comparer, out bool _, out bool _);
        }
    }
}
