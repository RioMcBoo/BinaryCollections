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

        private bool SetEqualsSorted(ReadOnlySpan<TValue> range, BinaryListFlags flags = BinaryListFlags.None)
        {
            Iterator source = new(Values, Flags);
            Iterator target = new(range, flags);
            return SetEqualsSorted(source, target, _comparer);
        }

        private static bool SetEqualsSorted(Iterator source, Iterator target, BinaryListComparer comparer)
        {
            if (source.Length != target.Length)
                return false;

            if (source.Length == 0 && target.Length == 0)
                return true;

            // for local function usage (for optimizations) 
            Dictionary<TValue?, HeapCounter> tempDictionary = comparer.LeadsToCollisions ? new() : null!;            

            target.SetIndex(target.LowerLimit);
            source.SetIndex(source.LowerLimit);

            while (!target.OutOfRange && !source.OutOfRange)
            {
                switch (comparer.Compare(source.Current, target.Current))
                {
                    case > 0:
                    case < 0:
                        return false;
                    case 0:
                        if (!ProcessBoth(ref source, ref target))
                            return false;
                        break;
                }
            }

            return source.OutOfRange == target.OutOfRange;

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
                        if (item.TargetCount != item.SourceCount)
                            return false;
                    }

                    return true;
                }

                bool ProcessBothComparable(ref Iterator source, ref Iterator target)
                {
                    ref readonly TValue previous = ref source.Current;
                    RefCounter counter = new(1, 1);

                    while (source.Next(1) + target.Next(1) == 2)
                    {
                        bool endOfDuplicates = false;

                        if (comparer.Compare(source.Current, previous) == 0)
                        {
                            counter.SourceCount++;
                        }
                        else
                        {
                            endOfDuplicates = true;
                        }

                        if (comparer.Compare(target.Current, previous) == 0)
                        {
                            counter.TargetCount++;
                        }
                        else
                        {
                            endOfDuplicates = true;
                        }

                        if (counter.TargetCount != counter.SourceCount)
                        {
                            return false;
                        }

                        if (endOfDuplicates)
                        {
                            break;
                        }
                    }

                    return source.OutOfRange == target.OutOfRange;
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
