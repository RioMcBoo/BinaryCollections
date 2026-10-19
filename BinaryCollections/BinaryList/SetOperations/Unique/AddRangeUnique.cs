using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool AddRangeUnique(ReadOnlySpan<TValue> range)
        {
            if (!CheckCountOverload(range.Length))
                return false;

            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return AddSortedRangeUnique(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool AddRangeUnique(IReadOnlyCollection<TValue> range)
        {
            if (!CheckCountOverload(range.Count))
                return false;

            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Count);
            try
            {
                int i = 0;
                foreach (var item in range)
                {
                    temp[i++] = item;
                }

                Array.Sort(temp, 0, range.Count, Comparer);
                return AddSortedRangeUnique(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool AddRangeUnique(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return AddRangeUnique(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return AddRangeUnique(collection);
            }

            List<TValue> temp = new List<TValue>();

            foreach (var item in range)
            {
                temp.Add(item);
            }

            if (!CheckCountOverload(temp.Count))
                return false;

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return AddSortedRangeUnique(span);
        }

        public bool AddRangeUnique(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                if (!CheckCountOverload(range.Count))
                    return false;

                if (range.Flags.HasFlag(BinaryListFlags.UniformRange))
                {
                    return AddUniformRangeUnique(range.GetSpan());
                }

                return AddSortedRangeUnique(range.GetSpan());
            }

            return AddRangeUnique(range.GetSpan());
        }

        public bool AddRangeUnique(BinaryList<TValue> range)
        {
            return AddRangeUnique(range.GetEnumerator());
        }

        private bool AddUniformRangeUnique(ReadOnlySpan<TValue> range)
        {
            if (range.Length <= 0)
                return false;

            ref readonly var example = ref range[0];
            return AddUnique(in example, out _);
        }

        private bool AddSortedRangeUnique(ReadOnlySpan<TValue> range)
        {
            if (range.Length <= 0)
                return false;

            if (LeftBuffer >= RightBuffer && LeftBuffer >= range.Length)
            {
                return InsertSortedRangeFromLeftUnique(range);
            }

            if (RightBuffer >= LeftBuffer && RightBuffer >= range.Length)
            {
                return InsertSortedRangeFromRightUnique(range);
            }

            return ResizeAndInsertSortedRangeUnique(range);
        }

        private bool ResizeAndInsertSortedRangeUnique(ReadOnlySpan<TValue> range)
        {
            PrepareForResize(range.Length, out var destination);

            int newZeroRIndex = CalculateZeroIndex(destination.Length, _count + range.Length);

            if (_storage == destination)
            {
                Array.Copy(destination, _zeroRIndex, destination, newZeroRIndex, _count);
                _zeroRIndex = newZeroRIndex;
                return InsertSortedRangeFromRightUnique(range);
            }

#if TEST
            if (_count % 2 == 0)
            {
                // Because this case does not arise in practice,
                // yet we must maintain the code for the eventuality that it might suddenly become active.
                InsertSortedRangeFromRightUnique(range, destination, newZeroRIndex);
            }
            else
            {
                InsertSortedRangeFromLeftUnique(range, destination, newZeroRIndex);
            }
#else
            InsertSortedRangeFromLeftUnique(range, destination, newZeroRIndex);
#endif
            _storage = destination;
            return true;
        }

        private bool InsertSortedRangeFromLeftUnique(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex - range.Length;
            return InsertSortedRangeFromLeftUnique(range, _storage, newZeroRIndex);
        }

        private bool InsertSortedRangeFromLeftUnique(ReadOnlySpan<TValue> second, TValue[] destination, int newZeroRIndex)
        {
            _version = unchecked(++_version);

            var first = _storage;

            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(_zeroRIndex - newZeroRIndex == second.Length, "In-place merge requires place for the new range.");
            }

            // for local function usage (for optimizations) 
            Dictionary<TValue?, HeapCounter> tempDictionary = _comparer.LeadsToCollisions ? new() : null!;

            int incomingElementsSkipped = 0;
            int rangeLength = second.Length;
            int d = newZeroRIndex;
            int zeroIndexShift = 0;

            Debug.Assert(d > -1, "Left buffer is not long enough");

            bool s_InRange(int s) => s < rangeLength;
            bool f_InRange(int f) => f < _zeroRIndex + _count;

            // remember - we do care only about duplicates among incomings

            // General merge loop
            int s = 0;
            int f = _zeroRIndex;

            while (s_InRange(s) && f_InRange(f))
            {
                ref readonly TValue secondItem = ref second[s];
                ref readonly TValue firstItem = ref first[f];

                switch (_comparer.Compare(firstItem, secondItem))
                {
                    case > 0:
                        ProcessMergingSecond(ref d, ref s, second);
                        break;
                    case < 0:
                        ProcessMergingFirst(ref d, ref f);
                        break;
                    case 0:
                        ProcessMergingBoth(ref d, ref s, ref f, second);
                        break;
                }
            }

            // Manage remaining items
            if (s_InRange(s))
            {
                do ProcessMergingSecond(ref d, ref s, second);
                while (s_InRange(s));

                incomingElementsSkipped = newZeroRIndex + _count + second.Length - d;

                // Clear remaining free-spots
                if (ItemType<TValue>.ContainsReferences && incomingElementsSkipped > 0 && first == destination)
                {
                    Array.Clear(first, d, incomingElementsSkipped);
                }
            }
            else if (f_InRange(f))
            {
                int remainingPartLength = _zeroRIndex + _count - f;
                int finishedPartLength = d - newZeroRIndex;

                incomingElementsSkipped = _count + second.Length - remainingPartLength - finishedPartLength;

                if (first != destination)
                {
                    // we just have to copy remaining items into new array
                    Array.Copy(first, f, destination, d, remainingPartLength);
                }
                else if (incomingElementsSkipped > 0)
                {
                    if (finishedPartLength >= remainingPartLength)
                    {
                        // Shift remaining items due to missing duplicates
                        Array.Copy(first, f, first, d, remainingPartLength);

                        // Clear formed dirty free-spots from right side
                        if (ItemType<TValue>.ContainsReferences)
                        {
                            Array.Clear(first, newZeroRIndex + finishedPartLength + remainingPartLength, incomingElementsSkipped);
                        }
                    }
                    else
                    {
                        // Shift finished items due to missing duplicates
                        Array.Copy(first, newZeroRIndex, first, newZeroRIndex + incomingElementsSkipped, finishedPartLength);
                        zeroIndexShift += incomingElementsSkipped;

                        // Clear formed dirty free-spots from left side
                        if (ItemType<TValue>.ContainsReferences)
                        {
                            Array.Clear(first, newZeroRIndex, incomingElementsSkipped);
                        }
                    }
                }
            }
            else
            {
                incomingElementsSkipped = newZeroRIndex + _count + second.Length - d;

                // Clear remaining free-spots
                if (ItemType<TValue>.ContainsReferences && incomingElementsSkipped > 0 && first == destination)
                {
                    Array.Clear(first, d, incomingElementsSkipped);
                }
            }

            _zeroRIndex = newZeroRIndex + zeroIndexShift;
            _count += second.Length - incomingElementsSkipped;

            if (incomingElementsSkipped < second.Length)
                RemoveFlags(BinaryListFlags.UniformRange);

            return true;

            void ProcessMergingBoth(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
            {
                if (_comparer.LeadsToCollisions)
                {
                    ProcessMergingBothDefault(ref d, ref s, ref f, second);
                }
                else
                {
                    ProcessMergingBothComparable(ref d, ref s, ref f, second);
                }

                void ProcessMergingBothDefault(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
                {
                    tempDictionary.Clear();

                    (int start, int length) resultedRangeFirst = default;
                    resultedRangeFirst.start = f;

                    // Add the first item to result (preparing to compare)
                    ref readonly TValue previousItem = ref first[f++];
                    destination[d++] = previousItem;

                    tempDictionary.Add(previousItem, new HeapCounter(firstCount: 1));
                    resultedRangeFirst.length++;

                    // Add all currently existing items in collision to result, and count them.
                    while (f_InRange(f))
                    {
                        ref readonly TValue currentItem = ref first[f];

                        if (_comparer.Compare(currentItem, previousItem) != 0)
                            break; // this is the end of duplicates, we must stop

                        previousItem = ref currentItem;

                        if (!tempDictionary.TryGetValue(currentItem, out var counter))
                        {
                            counter = new HeapCounter();
                            tempDictionary[currentItem] = counter;
                        }

                        counter.FirstCount++;
                        resultedRangeFirst.length++;
                        destination[d++] = currentItem;
                        f++;
                    }

                    // Add all incoming items in collision to result, and count them.
                    (int start, int length) resultedRangeSecond = default;
                    resultedRangeSecond.start = s;
                    while (s_InRange(s))
                    {
                        ref readonly TValue currentItem = ref second[s];

                        if (_comparer.Compare(currentItem, previousItem) != 0)
                            break; // this is the end of duplicates, we must stop

                        previousItem = ref currentItem;

                        if (!tempDictionary.TryGetValue(currentItem, out var counter))
                        {
                            counter = new HeapCounter();
                            tempDictionary[currentItem] = counter;
                        }

                        counter.SecondCount++;
                        resultedRangeSecond.length++;
                        s++;
                    }

                    int addedCount = 0;
                    foreach (var item in tempDictionary)
                    {
                        int countToAdd = 0;

                        if (item.Value.FirstCount > 0)
                        {
                            countToAdd = item.Value.FirstCount;
                        }
                        else
                        {
                            countToAdd = 1;
                            destination[d++] = item.Key;
                        }

                        addedCount += countToAdd;
                    }
                }

                void ProcessMergingBothComparable(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
                {
                    ref readonly TValue initialItem = ref first[f++]; s++; // they are equal here, so we can move both indexes

                    destination[d++] = initialItem;

                    while (f_InRange(f))
                    {
                        ref readonly TValue currentItem = ref first[f];
                        if (_comparer.Compare(first[f], initialItem) == 0)
                        {
                            destination[d++] = currentItem;
                            f++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    while (s_InRange(s) && _comparer.Compare(second[s], initialItem) == 0)
                    {
                        s++;
                    }
                }
            }

            void ProcessMergingSecond(ref int d, ref int s, ReadOnlySpan<TValue> second)
            {
                if (_comparer.LeadsToCollisions)
                {
                    ProcessMergingSecondDefault(ref d, ref s, second);
                }
                else
                {
                    ProcessMergingSecondComparable(ref d, ref s, second);
                }

                void ProcessMergingSecondDefault(ref int d, ref int s, ReadOnlySpan<TValue> second)
                {
                    tempDictionary.Clear();

                    (int start, int length) resultedRangeSecond = default;
                    resultedRangeSecond.start = s;

                    // Add the first item to result (preparing to compare)
                    ref readonly TValue previousItem = ref second[s++];

                    tempDictionary.Add(previousItem, new HeapCounter(secondCount: 1));
                    resultedRangeSecond.length++;

                    // Add all incoming items in collision to result, and count them.
                    while (s_InRange(s))
                    {
                        ref readonly TValue currentItem = ref second[s];

                        if (_comparer.Compare(currentItem, previousItem) != 0)
                            break; // this is the end of duplicates, we must stop

                        previousItem = ref currentItem;

                        if (!tempDictionary.TryGetValue(currentItem, out var counter))
                        {
                            counter = new HeapCounter();
                            tempDictionary[currentItem] = counter;
                        }

                        counter.SecondCount++;
                        resultedRangeSecond.length++;
                        s++;
                    }

                    int addedCount = 0;
                    foreach (var item in tempDictionary)
                    {
                        int countToAdd = 1;

                        for (int j = 0; j < countToAdd; j++)
                        {
                            destination[d++] = item.Key;
                        }

                        addedCount += countToAdd;
                    }
                }

                void ProcessMergingSecondComparable(ref int d, ref int s, ReadOnlySpan<TValue> second)
                {
                    ref readonly TValue initialItem = ref second[s++];
                    destination[d++] = initialItem;

                    while (s_InRange(s) && _comparer.Compare(second[s], initialItem) == 0)
                    {
                        s++;
                    }
                }
            }

            void ProcessMergingFirst(ref int d, ref int f)
            {
                ref readonly TValue initialItem = ref first[f++];
                destination[d++] = initialItem;

                while (f_InRange(f))
                {
                    ref readonly TValue currentItem = ref first[f];
                    if (_comparer.Compare(first[f], initialItem) == 0)
                    {
                        destination[d++] = currentItem;
                        f++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private bool InsertSortedRangeFromRightUnique(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex;
            return InsertSortedRangeFromRightUnique(range, _storage, newZeroRIndex);
        }

        private bool InsertSortedRangeFromRightUnique(ReadOnlySpan<TValue> second, TValue[] destination, int newZeroRIndex)
        {
            _version = unchecked(++_version);

            var first = _storage;

            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(newZeroRIndex == _zeroRIndex, "In-place merge requires place for the new range.");
            }
            
            // for local function usage (for optimizations) 
            Dictionary<TValue?, HeapCounter> tempDictionary = _comparer.LeadsToCollisions ? new() : null!;

            int incomingElementsSkipped = 0;
            int rangeLength = second.Length;
            int d = newZeroRIndex + _count + second.Length - 1;
            int zeroIndexShift = 0;

            Debug.Assert(d < destination.Length, "Right buffer is not long enough");

            bool s_InRange(int s) => s >= 0;
            bool f_InRange(int f) => f >= _zeroRIndex;

            // remember - we do care only about duplicates among incomings

            // General merge loop
            int s = second.Length - 1;
            int f = _zeroRIndex + _count - 1;

            while (s_InRange(s) && f_InRange(f))
            {
                ref readonly TValue secondItem = ref second[s];
                ref readonly TValue firstItem = ref first[f];

                switch (_comparer.Compare(firstItem, secondItem))
                {
                    case > 0:
                        ProcessMergingFirst(ref d, ref f);
                        break;
                    case < 0:
                        ProcessMergingSecond(ref d, ref s, second);
                        break;
                    default:
                        ProcessMergingBoth(ref d, ref s, ref f, second);
                        break;
                }
            }

            // Manage remaining items
            if (s_InRange(s))
            {
                do ProcessMergingSecond(ref d, ref s, second);
                while (s_InRange(s));

                incomingElementsSkipped = (d + 1) - newZeroRIndex;
                zeroIndexShift += incomingElementsSkipped;

                // Clear remaining free-spots
                if (ItemType<TValue>.ContainsReferences && incomingElementsSkipped > 0 && first == destination)
                {
                    Array.Clear(first, newZeroRIndex, incomingElementsSkipped);
                }
            }
            else if (f_InRange(f))
            {
                int remainingPartLength = (f + 1) - _zeroRIndex;
                int finishedPartLength = _count + second.Length - (d + 1 - newZeroRIndex);
                incomingElementsSkipped = _count + second.Length - remainingPartLength - finishedPartLength;

                if (first != destination)
                {
                    // we just have to copy remaining items into new array
                    Array.Copy(first, _zeroRIndex, destination, newZeroRIndex + incomingElementsSkipped, remainingPartLength);
                    zeroIndexShift += incomingElementsSkipped;
                }
                else if (incomingElementsSkipped > 0)
                {
                    if (finishedPartLength >= remainingPartLength)
                    {
                        // Shift remaining items due to missing duplicates
                        Array.Copy(first, _zeroRIndex, first, newZeroRIndex + incomingElementsSkipped, remainingPartLength);
                        zeroIndexShift += incomingElementsSkipped;

                        // Clear formed dirty free-spots from left side
                        if (ItemType<TValue>.ContainsReferences)
                        {
                            Array.Clear(first, newZeroRIndex, incomingElementsSkipped);
                        }
                    }
                    else
                    {
                        // Shift finished items due to missing duplicates
                        Array.Copy(first, d + 1, first, d + 1 - incomingElementsSkipped, finishedPartLength);

                        // Clear formed dirty free-spots from right side
                        if (ItemType<TValue>.ContainsReferences)
                        {
                            Array.Clear(first, newZeroRIndex + finishedPartLength + remainingPartLength, incomingElementsSkipped);
                        }
                    }
                }
            }
            else
            {
                incomingElementsSkipped = (d + 1) - newZeroRIndex;
                zeroIndexShift += incomingElementsSkipped;

                // Clear remaining free-spots
                if (ItemType<TValue>.ContainsReferences && incomingElementsSkipped > 0 && first == destination)
                {
                    Array.Clear(first, newZeroRIndex, incomingElementsSkipped);
                }
            }

            _zeroRIndex = newZeroRIndex + zeroIndexShift;
            _count += second.Length - incomingElementsSkipped;

            if (incomingElementsSkipped < second.Length)
                RemoveFlags(BinaryListFlags.UniformRange);

            return true;

            void ProcessMergingBoth(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
            {
                if (_comparer.LeadsToCollisions)
                {
                    ProcessMergingBothDefault(ref d, ref s, ref f, second);
                }
                else
                {
                    ProcessMergingBothComparable(ref d, ref s, ref f, second);
                }

                void ProcessMergingBothDefault(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
                {
                    tempDictionary.Clear();

                    (int start, int length) resultedRangeCurrent = default;
                    resultedRangeCurrent.start = f;

                    // Add the first item to result (preparing to compare)
                    ref readonly TValue previousItem = ref first[f--];
                    destination[d--] = previousItem;

                    tempDictionary.Add(previousItem, new HeapCounter(firstCount: 1));
                    resultedRangeCurrent.length++;

                    // Add all currently existing items in collision to result, and count them.
                    while (f_InRange(f))
                    {
                        ref readonly TValue currentItem = ref first[f];

                        if (_comparer.Compare(currentItem, previousItem) != 0)
                            break; // this is the end of duplicates, we must stop

                        previousItem = ref currentItem;

                        if (!tempDictionary.TryGetValue(currentItem, out var counter))
                        {
                            counter = new HeapCounter();
                            tempDictionary[currentItem] = counter;
                        }

                        counter.FirstCount++;
                        resultedRangeCurrent.length++;
                        destination[d--] = currentItem;
                        f--;
                    }

                    // Add all incoming items in collision to result, and count them.
                    (int start, int length) resultedRangeIncoming = default;
                    resultedRangeIncoming.start = s;
                    while (s_InRange(s))
                    {
                        ref readonly TValue currentItem = ref second[s];

                        if (_comparer.Compare(currentItem, previousItem) != 0)
                            break; // this is the end of duplicates, we must stop

                        previousItem = ref currentItem;

                        if (!tempDictionary.TryGetValue(currentItem, out var counter))
                        {
                            counter = new HeapCounter();
                            tempDictionary[currentItem] = counter;
                        }

                        counter.SecondCount++;
                        resultedRangeIncoming.length++;
                        s--;
                    }

                    int addedCount = 0;
                    foreach (var item in tempDictionary)
                    {
                        int countToAdd = 0;

                        if (item.Value.FirstCount > 0)
                        {
                            countToAdd = item.Value.FirstCount;
                        }
                        else
                        {
                            countToAdd = 1;
                            destination[d--] = item.Key;
                        }

                        addedCount += countToAdd;
                    }
                }

                void ProcessMergingBothComparable(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
                {
                    ref readonly TValue initialItem = ref first[f--]; s--; // they are equal here, so we can move both indexes

                    destination[d--] = initialItem;

                    while (f_InRange(f))
                    {
                        ref readonly TValue currentItem = ref first[f];
                        if (_comparer.Compare(first[f], initialItem) == 0)
                        {
                            destination[d--] = currentItem;
                            f--;
                        }
                        else
                        {
                            break;
                        }
                    }

                    while (s_InRange(s) && _comparer.Compare(second[s], initialItem) == 0)
                    {
                        s--;
                    }
                }
            }

            void ProcessMergingSecond(ref int d, ref int s, ReadOnlySpan<TValue> second)
            {
                if (_comparer.LeadsToCollisions)
                {
                    ProcessMergingSecondDefault(ref d, ref s, second);
                }
                else
                {
                    ProcessMergingSecondComparable(ref d, ref s, second);
                }

                void ProcessMergingSecondDefault(ref int d, ref int s, ReadOnlySpan<TValue> second)
                {
                    tempDictionary.Clear();

                    (int start, int length) resultedRangeIncoming = default;
                    resultedRangeIncoming.start = s;

                    // Add the first item to result (preparing to compare)
                    ref readonly TValue previousItem = ref second[s--];

                    tempDictionary.Add(previousItem, new HeapCounter(secondCount: 1));
                    resultedRangeIncoming.length++;

                    // Add all incoming items in collision to result, and count them.
                    while (s_InRange(s))
                    {
                        ref readonly TValue currentItem = ref second[s];

                        if (_comparer.Compare(currentItem, previousItem) != 0)
                            break; // this is the end of duplicates, we must stop

                        previousItem = ref currentItem;

                        if (!tempDictionary.TryGetValue(currentItem, out var counter))
                        {
                            counter = new HeapCounter();
                            tempDictionary[currentItem] = counter;
                        }

                        counter.SecondCount++;
                        resultedRangeIncoming.length++;
                        s--;
                    }

                    int addedCount = 0;
                    foreach (var item in tempDictionary)
                    {
                        int countToAdd = 1;

                        for (int j = 0; j < countToAdd; j++)
                        {
                            destination[d--] = item.Key;
                        }

                        addedCount += countToAdd;
                    }
                }

                void ProcessMergingSecondComparable(ref int d, ref int s, ReadOnlySpan<TValue> second)
                {
                    ref readonly TValue initialItem = ref second[s--];
                    destination[d--] = initialItem;

                    while (s_InRange(s) && _comparer.Compare(second[s], initialItem) == 0)
                    {
                        s--;
                    }
                }
            }

            void ProcessMergingFirst(ref int d, ref int f)
            {
                ref readonly TValue initialItem = ref first[f--];
                destination[d--] = initialItem;

                while (f_InRange(f))
                {
                    ref readonly TValue currentItem = ref first[f];
                    if (_comparer.Compare(first[f], initialItem) == 0)
                    {
                        destination[d--] = currentItem;
                        f--;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}
