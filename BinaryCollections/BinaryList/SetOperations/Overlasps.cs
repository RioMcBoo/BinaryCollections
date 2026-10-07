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

        private bool OverlapsSorted(ReadOnlySpan<TValue> range, BinaryListFlags flags = BinaryListFlags.None)
        {
            Iterator source = new(Values, Flags);
            Iterator target = new(range, flags);
            return OverlapsSorted(source, target, _comparer);
        }

        private static bool OverlapsSorted(Iterator source, Iterator target, BinaryListComparer comparer)
        {
            if (source.Length == 0 || target.Length == 0)
                return false;

            // for local function usage (for optimizations) 
            Dictionary<TValue?, HeapCounter> tempDictionary = comparer.LeadsToCollisions ? new() : null!;            

            if (!CheckLeftBoundary(ref target, ref source))
                return false;

            if (!CheckRightBoundary(ref target, ref source))
                return false;

            target.SetIndex(target.LowerLimit);
            source.SetIndex(source.LowerLimit);

            while (!target.OutOfRange && !source.OutOfRange)
            {
                switch (comparer.Compare(source.Current, target.Current))
                {
                    case > 0:
                        ProcessTarget(ref target);
                        break;
                    case < 0:
                        ProcessSource(ref source);
                        break;
                    case 0:
                        if (ProcessBoth(ref source, ref target))
                            return true;
                        break;
                }
            }

            return false;

            bool CheckLeftBoundary(ref Iterator target, ref Iterator source)
            {
                ref readonly TValue targetItem = ref target[0];
                ref readonly TValue sourceItem = ref source[0];

                switch (comparer.Compare(sourceItem, targetItem))
                {
                    case > 0:
                        return SkipTargetTo(in sourceItem, ref target);
                    case < 0:
                        return SkipSourceTo(in targetItem, ref source);
                    case 0:
                        return true;
                }

                bool SkipTargetTo(ref readonly TValue item, ref Iterator target)
                {
                    if (!FindLeftIndex(in item, target.Span, comparer, out int i))
                    {
                        if (IsFoundIndexOutOfRange(i, target.Span.Length))
                            return false;

                        i = ~i;
                    }

                    target.LowerLimit = i;
                    return true;
                }

                bool SkipSourceTo(ref readonly TValue item, ref Iterator source)
                {
                    if (!FindLeftIndex(in item, source.Span, comparer, out int i))
                    {
                        if (IsFoundIndexOutOfRange(i, source.Span.Length))
                            return false;

                        i = ~i;
                    }

                    source.LowerLimit = i;
                    return true;
                }
            }

            bool CheckRightBoundary(ref Iterator target, ref Iterator source)
            {
                ref readonly TValue targetItem = ref target[^1];
                ref readonly TValue sourceItem = ref source[^1];

                switch (comparer.Compare(sourceItem, targetItem))
                {
                    case < 0:
                        return SkipTargetTo(in sourceItem, ref target);
                    case > 0:
                        return SkipSourceTo(in targetItem, ref source);
                    case 0:
                        return true;
                }

                bool SkipTargetTo(ref readonly TValue item, ref Iterator target)
                {
                    if (!FindRightIndex(in item, target.Span, comparer, out int i))
                    {
                        if (IsFoundIndexOutOfRange(i, target.Span.Length))
                            return false;

                        i = ~i;
                    }

                    target.UpperLimit = i;
                    return true;
                }

                bool SkipSourceTo(ref readonly TValue item, ref Iterator source)
                {
                    if (!FindRightIndex(in item, source.Span, comparer, out int i))
                    {
                        if (IsFoundIndexOutOfRange(i, source.Span.Length))
                            return false;

                        i = ~i;
                    }

                    source.UpperLimit = i;
                    return true;
                }
            }

            bool ProcessBoth(ref Iterator source, ref Iterator target)
            {
                if (comparer.LeadsToCollisions)
                {
                    return ProcessBothDefault(ref source, ref target);
                }
                else
                {
                    return ProcessBothComparable(ref source, ref target);
                }

                bool ProcessBothDefault(ref Iterator source, ref Iterator target)
                {
                    tempDictionary.Clear();

                    // Add the first item to result (preparing to compare)
                    ref readonly TValue previous = ref source.Current;

                    GetCounter(in source.Current).SourceCount++;
                    GetCounter(in target.Current).TargetCount++;

                    while (source.Next(1) == 1)
                    {
                        ref readonly TValue sourceItem = ref source.Current;

                        if (comparer.Compare(sourceItem, previous) != 0)
                            break; // this is the end of duplicates, we must stop

                        GetCounter(in sourceItem).SourceCount++;
                    }

                    while (target.Next(1) == 1)
                    {
                        ref readonly TValue targetItem = ref target.Current;

                        if (comparer.Compare(targetItem, previous) != 0)
                            break; // this is the end of duplicates, we must stop

                        GetCounter(in targetItem).TargetCount++;
                    }

                    foreach (var item in tempDictionary.Values)
                    {
                        if (item.TargetCount > 0 && item.SourceCount > 0)
                            return true;
                    }

                    return false;
                }

                bool ProcessBothComparable(ref Iterator source, ref Iterator target)
                {
                    return true;
                }
            }

            void ProcessTarget(ref Iterator target)
            {
                ref readonly TValue prevItem = ref target.Current;

                while (target.Next(1) == 1)
                {
                    if (comparer.Compare(target.Current, prevItem) != 0)
                        return;
                }
            }

            void ProcessSource(ref Iterator source)
            {
                ref readonly TValue prevItem = ref source.Current;

                while (source.Next(1) == 1)
                {
                    if (comparer.Compare(source.Current, prevItem) != 0)
                        return;
                }
            }

            HeapCounter GetCounter(ref readonly TValue item)
            {
                if (!tempDictionary.TryGetValue(item, out var counter))
                {
                    counter = new HeapCounter();
                    tempDictionary[item] = counter;
                }

                return counter;
            }
        }
    }
}
