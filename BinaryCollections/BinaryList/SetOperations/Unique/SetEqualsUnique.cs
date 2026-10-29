using System.Buffers;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool SetEqualsUnique(ReadOnlySpan<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return SetEqualsSortedUnique(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool SetEqualsUnique(IReadOnlyCollection<TValue> range)
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
                return SetEqualsSortedUnique(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool SetEqualsUnique(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return SetEqualsUnique(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return SetEqualsUnique(collection);
            }

            List<TValue> temp = new List<TValue>();

            foreach (var item in range)
            {
                temp.Add(item);
            }

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return SetEqualsSortedUnique(span);
        }

        public bool SetEqualsUnique(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                if (Flags == DuplicateDensity.None &&
                    range.Flags == DuplicateDensity.None &&
                    Count != range.Count)
                {
                    return false;
                }

                return SetEqualsSortedUnique(range.GetSpan(), range.Flags);
            }

            return SetEqualsUnique(range.GetSpan());
        }

        public bool SetEqualsUnique(BinaryList<TValue> range)
        {
            return SetEqualsUnique(range.GetEnumerator());
        }

        private bool SetEqualsSortedUnique(ReadOnlySpan<TValue> range, DuplicateDensity flags = DuplicateDensity.Possible)
        {
            Iterator first = new(Values, Flags);
            Iterator second = new(range, flags);
            return SetEqualsSortedUnique(ref first, ref second, _comparer);
        }

        private static bool SetEqualsSortedUnique(ref Iterator first, ref Iterator second, BinaryListComparer<TValue> comparer)
        {
            if (first.Length == 0 && second.Length == 0)
                return true;     

            second.SetIndex(second.LowerLimit);
            first.SetIndex(first.LowerLimit);

            while (!second.OutOfRange && !first.OutOfRange)
            {
                switch (comparer.Compare(first.Current, second.Current))
                {
                    case > 0:
                    case < 0:
                        return false;
                    case 0:
                        if (!ProcessBoth(ref first, ref second))
                            return false;
                        break;
                }
            }

            return first.OutOfRange == second.OutOfRange;

            bool ProcessBoth(ref Iterator first, ref Iterator second)
            {
                ref readonly TValue previousItem = ref first.Current;
                RefCounter counter = new(1, 1);

                while (first.Next(1) == 1)
                {
                    if (comparer.Compare(first.Current, previousItem) != 0)
                        break;
                }

                while (second.Next(1) == 1)
                {
                    if (comparer.Compare(second.Current, previousItem) != 0)
                        break;
                }

                return first.OutOfRange == second.OutOfRange;
            }
        }
    }
}
