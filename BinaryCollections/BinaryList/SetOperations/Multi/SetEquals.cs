using System.Buffers;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool SetEquals(ReadOnlySpan<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return SetEqualsSorted(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool SetEquals(IReadOnlyCollection<TValue> range)
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
                return SetEqualsSorted(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool SetEquals(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return SetEquals(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return SetEquals(collection);
            }

            List<TValue> temp = new List<TValue>();

            foreach (var item in range)
            {
                temp.Add(item);
            }

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return SetEqualsSorted(span);
        }

        public bool SetEquals(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                return SetEqualsSorted(range.GetSpan(), range.Flags);
            }

            return SetEquals(range.GetSpan());
        }

        public bool SetEquals(BinaryList<TValue> range)
        {
            return SetEquals(range.GetEnumerator());
        }

        private bool SetEqualsSorted(ReadOnlySpan<TValue> range, DuplicateDensity flags = DuplicateDensity.Possible)
        {
            Iterator first = new(Values, Flags);
            Iterator second = new(range, flags);
            return SetEqualsSorted(first, second, _comparer);
        }

        private static bool SetEqualsSorted(Iterator first, Iterator second, BinaryListComparer<TValue> comparer)
        {
            if (first.Length != second.Length)
                return false;

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
                ref readonly TValue previous = ref first.Current;
                RefCounter counter = new(1, 1);

                while (first.Next(1) + second.Next(1) == 2)
                {
                    bool endOfDuplicates = false;

                    if (comparer.Compare(first.Current, previous) == 0)
                    {
                        counter.FirstCount++;
                    }
                    else
                    {
                        endOfDuplicates = true;
                    }

                    if (comparer.Compare(second.Current, previous) == 0)
                    {
                        counter.SecondCount++;
                    }
                    else
                    {
                        endOfDuplicates = true;
                    }

                    if (counter.SecondCount != counter.FirstCount)
                    {
                        return false;
                    }

                    if (endOfDuplicates)
                    {
                        break;
                    }
                }

                return first.OutOfRange == second.OutOfRange;
            }
        }
    }
}
