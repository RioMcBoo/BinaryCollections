using System.Buffers;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool IsSubsetOf(ReadOnlySpan<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return IsSubsetOfSorted(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool IsSubsetOf(IReadOnlyCollection<TValue> range)
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
                return IsSubsetOfSorted(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool IsSubsetOf(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return IsSubsetOf(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return IsSubsetOf(collection);
            }

            List<TValue> temp = new List<TValue>();

            foreach (var item in range)
            {
                temp.Add(item);
            }

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return IsSubsetOfSorted(span);
        }

        public bool IsSubsetOf(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                return IsSubsetOfSorted(range.GetSpan(), range.Flags);
            }

            return IsSubsetOf(range.GetSpan());
        }

        public bool IsSubsetOf(BinaryList<TValue> range)
        {
            return IsSubsetOf(range.GetEnumerator());
        }

        private bool IsSubsetOfSorted(ReadOnlySpan<TValue> range, BinaryListFlags flags = BinaryListFlags.None)
        {
            if (Count == 0 && range.Length == 0)
                return true;

            if (Count == 0)
                return true;

            if (range.Length == 0)
                return false;

            if (range.Length < Count)
                return false;

            Iterator source = new(Values, Flags);
            Iterator target = new(range, flags);
            return IsSubsetOfSorted(source, target, _comparer, out bool _, out bool _);
        }

        private static bool IsSubsetOfSorted(Iterator source, Iterator target, BinaryListComparer comparer, out bool hasExtraDuplicates, out bool hasExtraUniqueElements)
        {
            hasExtraDuplicates = false;
            hasExtraUniqueElements = false;

            // for local function usage (for optimizations) 
            Dictionary<TValue?, HeapCounter> tempDictionary = comparer.LeadsToCollisions ? new() : null!;

            if (!CheckLeftBoundary(ref target, ref source, ref hasExtraUniqueElements))
                return false;

            if (!CheckRightBoundary(ref target, ref source, ref hasExtraUniqueElements))
                return false;

            target.SetIndex(target.LowerLimit);
            source.SetIndex(source.LowerLimit);

            while (!target.OutOfRange && !source.OutOfRange)
            {
                switch (comparer.Compare(source.Current, target.Current))
                {
                    case > 0:
                        ProcessTarget(ref target, ref hasExtraUniqueElements);
                        break;
                    case < 0:
                        return false;
                    case 0:
                        if (!ProcessBoth(ref source, ref target, ref hasExtraDuplicates))
                            return false;
                        break;
                }
            }

            return true;

            bool CheckLeftBoundary(ref Iterator target, ref Iterator source, ref bool hasExtraUniqueElements)
            {
                ref readonly TValue targetItem = ref target[0];
                ref readonly TValue sourceItem = ref source[0];

                switch (comparer.Compare(sourceItem, targetItem))
                {
                    case > 0:
                        return SkipTargetTo(in sourceItem, ref target, ref hasExtraUniqueElements);
                    case < 0:
                        return false; // out of incoming range
                    case 0:
                        return true;
                }

                bool SkipTargetTo(ref readonly TValue item, ref Iterator target, ref bool hasExtraUniqueElements)
                {
                    if (!FindLeftIndex(in item, target.Span, comparer, out int i))
                        return false;

                    target.LowerLimit = i;
                    hasExtraUniqueElements = true;
                    return true;
                }
            }

            bool CheckRightBoundary(ref Iterator target, ref Iterator source, ref bool hasExtraUniqueElements)
            {
                ref readonly TValue targetItem = ref target[^1];
                ref readonly TValue sourceItem = ref source[^1];

                switch (comparer.Compare(sourceItem, targetItem))
                {
                    case < 0:
                        return SkipTargetTo(in sourceItem, ref target, ref hasExtraUniqueElements);
                    case > 0:
                        return false; // out of incoming range
                    case 0:
                        return true;
                }

                bool SkipTargetTo(ref readonly TValue item, ref Iterator target, ref bool hasExtraUniqueElements)
                {
                    if (!FindRightIndex(in item, target.Span, comparer, out int i))
                        return false;

                    target.UpperLimit = i;
                    hasExtraUniqueElements = true;
                    return true;
                }
            }

            bool ProcessBoth(ref Iterator source, ref Iterator target, ref bool hasExtraDuplicates)
            {
                if (comparer.LeadsToCollisions)
                {
                    return ProcessBothDefault(ref source, ref target, ref hasExtraDuplicates);
                }
                else
                {
                    return ProcessBothComparable(ref source, ref target, ref hasExtraDuplicates);
                }

                bool ProcessBothDefault(ref Iterator source, ref Iterator target, ref bool hasExtraDuplicates)
                {
                    tempDictionary.Clear();

                    // Add the first item to result (preparing to compare)
                    ref readonly TValue previous = ref source.Current;
                    source++;
                    tempDictionary.Add(previous, new HeapCounter(sourceCount: 1));

                    while (!source.OutOfRange)
                    {
                        ref readonly TValue sourceItem = ref source.Current;

                        if (comparer.Compare(sourceItem, previous) != 0)
                            break; // this is the end of duplicates, we must stop

                        previous = ref sourceItem;

                        if (!tempDictionary.TryGetValue(sourceItem, out var counter))
                        {
                            counter = new HeapCounter();
                            tempDictionary[sourceItem] = counter;
                        }

                        counter.SourceCount++;
                        source++;
                    }

                    while (!target.OutOfRange)
                    {
                        ref readonly TValue targetItem = ref target.Current;

                        if (comparer.Compare(targetItem, previous) != 0)
                            break; // this is the end of duplicates, we must stop

                        previous = ref targetItem;

                        if (!tempDictionary.TryGetValue(targetItem, out var counter))
                        {
                            counter = new HeapCounter();
                            tempDictionary[targetItem] = counter;
                        }

                        counter.TargetCount++;
                        target++;
                    }

                    foreach (var item in tempDictionary.Values)
                    {
                        if (item.SourceCount > item.TargetCount)
                            return false;

                        if (item.TargetCount > item.SourceCount)
                            hasExtraDuplicates = true;
                    }

                    return true;
                }

                bool ProcessBothComparable(ref Iterator source, ref Iterator target, ref bool hasExtraDuplicates)
                {
                    ref readonly TValue firstItem = ref source.Current;
                    source++; target++; // they are equal here, so we can move both indexes

                    int sourceAmount = 1;
                    int targetAmount = 1;

                    while (!source.OutOfRange)
                    {
                        if (comparer.Compare(source.Current, firstItem) != 0)
                            break; // this is the end of duplicates, we must stop

                        sourceAmount++;
                        source++;
                    }

                    while (!target.OutOfRange)
                    {
                        if (comparer.Compare(target.Current, firstItem) != 0)
                            break; // this is the end of duplicates, we must stop

                        targetAmount++;
                        target++;
                    }

                    if (sourceAmount > targetAmount)
                    {
                        return false;
                    }

                    if (targetAmount > sourceAmount)
                    {
                        hasExtraDuplicates = true;
                    }

                    return true;
                }
            }

            void ProcessTarget(ref Iterator target, ref bool hasExtraUniqueElements)
            {
                ref readonly TValue prevItem = ref target.Current;
                hasExtraUniqueElements = true;

                while (target.Next(1) == 1)
                {
                    if (comparer.Compare(target.Current, prevItem) != 0)
                        return;
                }
            }
        }
    }
}
