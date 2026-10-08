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

            Iterator first = new(Values, Flags);
            Iterator second = new(range, flags);
            return IsSubsetOfSorted(first, second, _comparer, out bool _, out bool _);
        }

        private static bool IsSubsetOfSorted(Iterator first, Iterator second, BinaryListComparer comparer, out bool hasExtraDuplicates, out bool hasExtraUniqueElements)
        {
            hasExtraDuplicates = false;
            hasExtraUniqueElements = false;

            // for local function usage (for optimizations) 
            Dictionary<TValue?, HeapCounter> tempDictionary = comparer.LeadsToCollisions ? new() : null!;

            if (!CheckLeftBoundary(ref second, ref first, ref hasExtraUniqueElements))
                return false;

            if (!CheckRightBoundary(ref second, ref first, ref hasExtraUniqueElements))
                return false;

            second.SetIndex(second.LowerLimit);
            first.SetIndex(first.LowerLimit);

            while (!second.OutOfRange && !first.OutOfRange)
            {
                switch (comparer.Compare(first.Current, second.Current))
                {
                    case > 0:
                        ProcessSecond(ref second, ref hasExtraUniqueElements);
                        break;
                    case < 0:
                        return false;
                    case 0:
                        if (!ProcessBoth(ref first, ref second, ref hasExtraDuplicates))
                            return false;
                        break;
                }
            }

            return true;

            bool CheckLeftBoundary(ref Iterator second, ref Iterator first, ref bool hasExtraUniqueElements)
            {
                ref readonly TValue secondItem = ref second[0];
                ref readonly TValue firstItem = ref first[0];

                switch (comparer.Compare(firstItem, secondItem))
                {
                    case > 0:
                        return SkipSecondTo(in firstItem, ref second, ref hasExtraUniqueElements);
                    case < 0:
                        return false; // out of incoming range
                    case 0:
                        return true;
                }

                bool SkipSecondTo(ref readonly TValue item, ref Iterator second, ref bool hasExtraUniqueElements)
                {
                    if (!FindLeftIndex(in item, second.Span, comparer, out int i))
                        return false;

                    second.LowerLimit = i;
                    hasExtraUniqueElements = true;
                    return true;
                }
            }

            bool CheckRightBoundary(ref Iterator second, ref Iterator first, ref bool hasExtraUniqueElements)
            {
                ref readonly TValue secondItem = ref second[^1];
                ref readonly TValue firstItem = ref first[^1];

                switch (comparer.Compare(firstItem, secondItem))
                {
                    case < 0:
                        return SkipSecondTo(in firstItem, ref second, ref hasExtraUniqueElements);
                    case > 0:
                        return false; // out of incoming range
                    case 0:
                        return true;
                }

                bool SkipSecondTo(ref readonly TValue item, ref Iterator second, ref bool hasExtraUniqueElements)
                {
                    if (!FindRightIndex(in item, second.Span, comparer, out int i))
                        return false;

                    second.UpperLimit = i;
                    hasExtraUniqueElements = true;
                    return true;
                }
            }

            bool ProcessBoth(ref Iterator first, ref Iterator second, ref bool hasExtraDuplicates)
            {
                if (comparer.LeadsToCollisions)
                {
                    return ProcessBothDefault(ref first, ref second, ref hasExtraDuplicates);
                }
                else
                {
                    return ProcessBothComparable(ref first, ref second, ref hasExtraDuplicates);
                }

                bool ProcessBothDefault(ref Iterator first, ref Iterator second, ref bool hasExtraDuplicates)
                {
                    tempDictionary.Clear();

                    // Add the first item to result (preparing to compare)
                    ref readonly TValue previousItem = ref first.Current;
                    first++;
                    tempDictionary.Add(previousItem, new HeapCounter(firstCount: 1));

                    while (!first.OutOfRange)
                    {
                        ref readonly TValue currentItem = ref first.Current;

                        if (comparer.Compare(currentItem, previousItem) != 0)
                            break; // this is the end of duplicates, we must stop

                        previousItem = ref currentItem;

                        if (!tempDictionary.TryGetValue(currentItem, out var counter))
                        {
                            counter = new HeapCounter();
                            tempDictionary[currentItem] = counter;
                        }

                        counter.FirstCount++;
                        first++;
                    }

                    while (!second.OutOfRange)
                    {
                        ref readonly TValue currentItem = ref second.Current;

                        if (comparer.Compare(currentItem, previousItem) != 0)
                            break; // this is the end of duplicates, we must stop

                        previousItem = ref currentItem;

                        if (!tempDictionary.TryGetValue(currentItem, out var counter))
                        {
                            counter = new HeapCounter();
                            tempDictionary[currentItem] = counter;
                        }

                        counter.SecondCount++;
                        second++;
                    }

                    foreach (var item in tempDictionary.Values)
                    {
                        if (item.FirstCount > item.SecondCount)
                            return false;

                        if (item.SecondCount > item.FirstCount)
                            hasExtraDuplicates = true;
                    }

                    return true;
                }

                bool ProcessBothComparable(ref Iterator first, ref Iterator second, ref bool hasExtraDuplicates)
                {
                    ref readonly TValue initialItem = ref first.Current;
                    first++; second++; // they are equal here, so we can move both indexes

                    int sourceAmount = 1;
                    int targetAmount = 1;

                    while (!first.OutOfRange)
                    {
                        if (comparer.Compare(first.Current, initialItem) != 0)
                            break; // this is the end of duplicates, we must stop

                        sourceAmount++;
                        first++;
                    }

                    while (!second.OutOfRange)
                    {
                        if (comparer.Compare(second.Current, initialItem) != 0)
                            break; // this is the end of duplicates, we must stop

                        targetAmount++;
                        second++;
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

            void ProcessSecond(ref Iterator second, ref bool hasExtraUniqueElements)
            {
                ref readonly TValue previousItem = ref second.Current;
                hasExtraUniqueElements = true;

                while (second.Next(1) == 1)
                {
                    if (comparer.Compare(second.Current, previousItem) != 0)
                        return;
                }
            }
        }
    }
}
