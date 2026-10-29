using System.Buffers;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool Overlaps(ReadOnlySpan<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return OverlapsSorted(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool Overlaps(IReadOnlyCollection<TValue> range)
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
                return OverlapsSorted(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool Overlaps(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return Overlaps(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return Overlaps(collection);
            }

            List<TValue> temp = new List<TValue>();

            foreach (var item in range)
            {
                temp.Add(item);
            }

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return OverlapsSorted(span);
        }

        public bool Overlaps(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                return OverlapsSorted(range.GetSpan(), range.Flags);
            }

            return Overlaps(range.GetSpan());
        }

        public bool Overlaps(BinaryList<TValue> range)
        {
            return Overlaps(range.GetEnumerator());
        }

        private bool OverlapsSorted(ReadOnlySpan<TValue> range, DuplicateDensity flags = DuplicateDensity.Possible)
        {
            Iterator source = new(Values, Flags);
            Iterator target = new(range, flags);
            return OverlapsSorted(source, target, _comparer);
        }

        private static bool OverlapsSorted(Iterator first, Iterator second, BinaryListComparer<TValue> comparer)
        {
            if (first.Length == 0 || second.Length == 0)
                return false;

            if (!CheckLeftBoundary(ref second, ref first))
                return false;

            if (!CheckRightBoundary(ref second, ref first))
                return false;

            second.SetIndex(second.LowerLimit);
            first.SetIndex(first.LowerLimit);

            while (!second.OutOfRange && !first.OutOfRange)
            {
                switch (comparer.Compare(first.Current, second.Current))
                {
                    case > 0:
                        ProcessSecond(ref second);
                        break;
                    case < 0:
                        ProcessFirst(ref first);
                        break;
                    case 0:
                        if (ProcessBoth(ref first, ref second))
                            return true;
                        break;
                }
            }

            return false;

            bool CheckLeftBoundary(ref Iterator second, ref Iterator first)
            {
                ref readonly TValue secondItem = ref second[0];
                ref readonly TValue firstItem = ref first[0];

                switch (comparer.Compare(firstItem, secondItem))
                {
                    case > 0:
                        return SkipSecondTo(in firstItem, ref second);
                    case < 0:
                        return SkipFirstTo(in secondItem, ref first);
                    case 0:
                        return true;
                }

                bool SkipSecondTo(ref readonly TValue item, ref Iterator second)
                {
                    if (!comparer.FindLeftBound(in item, second.Span, out int i))
                    {
                        if (IsFoundIndexOutOfRange(i, second.Span.Length))
                            return false;

                        i = ~i;
                    }

                    second.LowerLimit = i;
                    return true;
                }

                bool SkipFirstTo(ref readonly TValue item, ref Iterator first)
                {
                    if (!comparer.FindLeftBound(in item, first.Span, out int i))
                    {
                        if (IsFoundIndexOutOfRange(i, first.Span.Length))
                            return false;

                        i = ~i;
                    }

                    first.LowerLimit = i;
                    return true;
                }
            }

            bool CheckRightBoundary(ref Iterator second, ref Iterator first)
            {
                ref readonly TValue secondItem = ref second[^1];
                ref readonly TValue firstItem = ref first[^1];

                switch (comparer.Compare(firstItem, secondItem))
                {
                    case < 0:
                        return SkipTargetTo(in firstItem, ref second);
                    case > 0:
                        return SkipSourceTo(in secondItem, ref first);
                    case 0:
                        return true;
                }

                bool SkipTargetTo(ref readonly TValue item, ref Iterator second)
                {
                    if (!comparer.FindRightBound(in item, second.Span, out int i))
                    {
                        if (IsFoundIndexOutOfRange(i, second.Span.Length))
                            return false;

                        i = ~i;
                    }

                    second.UpperLimit = i;
                    return true;
                }

                bool SkipSourceTo(ref readonly TValue item, ref Iterator first)
                {
                    if (!comparer.FindRightBound(in item, first.Span, out int i))
                    {
                        if (IsFoundIndexOutOfRange(i, first.Span.Length))
                            return false;

                        i = ~i;
                    }

                    first.UpperLimit = i;
                    return true;
                }
            }

            bool ProcessBoth(ref Iterator first, ref Iterator second)
            {
                return true;
            }

            void ProcessSecond(ref Iterator second)
            {
                ref readonly TValue previousItem = ref second.Current;

                while (second.Next(1) == 1)
                {
                    if (comparer.Compare(second.Current, previousItem) != 0)
                        return;
                }
            }

            void ProcessFirst(ref Iterator first)
            {
                ref readonly TValue previousItem = ref first.Current;

                while (first.Next(1) == 1)
                {
                    if (comparer.Compare(first.Current, previousItem) != 0)
                        return;
                }
            }
        }
    }
}
