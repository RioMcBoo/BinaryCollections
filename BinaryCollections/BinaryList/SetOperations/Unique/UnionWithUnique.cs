using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ExceptionHelper;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool UnionWithUnique(ReadOnlySpan<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return UnionWithSortedUnique(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool UnionWithUnique(IReadOnlyCollection<TValue> range)
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
                return UnionWithSortedUnique(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool UnionWithUnique(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return UnionWithUnique(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return UnionWithUnique(collection);
            }

            List<TValue> temp = new List<TValue>();

            foreach (var item in range)
            {
                temp.Add(item);
            }

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return UnionWithSortedUnique(span);
        }

        public bool UnionWithUnique(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                return UnionWithSortedUnique(range.GetSpan());
            }

            return UnionWithUnique(range.GetSpan());
        }

        public bool UnionWithUnique(BinaryList<TValue> range)
        {
            return UnionWithUnique(range.GetEnumerator());
        }

        private bool UnionWithSortedUnique(ReadOnlySpan<TValue> range)
        {
            if (range.Length <= 0 && _count <= 1)
                return false;

            if (!CheckCountOverload(range.Length))
            {
                if (!UnionWithSortedUniqueSafeTest(range))
                    Throw.CollectionOverloadException(nameof(UnionWithSortedUnique));
            }

            if (LeftBuffer >= RightBuffer && LeftBuffer >= range.Length)
            {
                return UnionWithFromLeftUnique(range);
            }

            if (RightBuffer >= LeftBuffer && RightBuffer >= range.Length)
            {
                return UnionWithFromRightUnique(range);
            }

            return ResizeAndUnionWithSortedUnique(range);
        }

        private bool UnionWithSortedUniqueSafeTest(ReadOnlySpan<TValue> range)
        {
            throw new NotImplementedException();
        }

        private bool ResizeAndUnionWithSortedUnique(ReadOnlySpan<TValue> range)
        {
            PrepareForResize(range.Length, out var destination);

            int newZeroRIndex = CalculateZeroIndex(destination.Length, _count + range.Length);

            if (_storage == destination)
            {
                Array.Copy(destination, _zeroRIndex, destination, newZeroRIndex, _count);
                _zeroRIndex = newZeroRIndex;
                return UnionWithFromRightUnique(range);
            }
#if TEST
            if (_count % 2 == 0)
            {
                // Because this case does not arise in practice,
                // yet we must maintain the code for the eventuality that it might suddenly become active.
                UnionWithFromRightUnique(range, destination, newZeroRIndex);
            }
            else
            {
                UnionWithFromLeftUnique(range, destination, newZeroRIndex);
            }
#else
            UnionWithFromLeftUnique(range, destination, newZeroRIndex);
#endif
            _storage = destination;
            return true;
        }

        private bool UnionWithFromLeftUnique(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex - range.Length;
            return UnionWithFromLeftUnique(range, _storage, newZeroRIndex);
        }

        private bool UnionWithFromLeftUnique(ReadOnlySpan<TValue> second, TValue[] destination, int newZeroRIndex)
        {
            _version = unchecked(++_version);

            var first = _storage;

            if (ReferenceEquals(first, destination))
            {
                Debug.Assert(_zeroRIndex - newZeroRIndex == second.Length, "In-place merge requires place for the new range.");
            }

            int incomingElementsSkipped = 0;
            int rangeLength = second.Length;
            int d = newZeroRIndex;
            int zeroIndexShift = 0;

            Debug.Assert(d > -1, "Left buffer is not long enough");

            bool s_InRange(int s) => s < rangeLength;
            bool f_InRange(int f) => f < _zeroRIndex + _count;

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
            }
            else if (f_InRange(f))
            {
                do ProcessMergingFirst(ref d, ref f);
                while (f_InRange(f));
            }

            incomingElementsSkipped = newZeroRIndex + _count + second.Length - d;

            // Clear remaining free-spots
            if (ItemType<TValue>.ContainsReferences && incomingElementsSkipped > 0 && first == destination)
            {
                Array.Clear(first, d, incomingElementsSkipped);
            }

            _zeroRIndex = newZeroRIndex + zeroIndexShift;
            _count += second.Length - incomingElementsSkipped;

            _flags.Remove(DuplicateDensity.Possible | DuplicateDensity.Full);

            return true;

            void ProcessMergingBoth(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
            {
                ref readonly TValue initialItem = ref first[f++]; s++; // they are equal here, so we can move both indexes

                destination[d++] = initialItem;

                while (f_InRange(f) && _comparer.Compare(first[f], initialItem) == 0)
                {
                    f++;
                }

                while (s_InRange(s) && _comparer.Compare(second[s], initialItem) == 0)
                {
                    s++;
                }
            }

            void ProcessMergingSecond(ref int d, ref int s, ReadOnlySpan<TValue> second)
            {
                ref readonly TValue initialItem = ref second[s++];
                destination[d++] = initialItem;

                while (s_InRange(s) && _comparer.Compare(second[s], initialItem) == 0)
                {
                    s++;
                }
            }

            void ProcessMergingFirst(ref int d, ref int f)
            {
                ref readonly TValue initialItem = ref first[f++];
                destination[d++] = initialItem;

                while (f_InRange(f) && _comparer.Compare(first[f], initialItem) == 0)
                {
                    f++;
                }
            }
        }

        private bool UnionWithFromRightUnique(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex;
            return UnionWithFromRightUnique(range, _storage, newZeroRIndex);
        }

        private bool UnionWithFromRightUnique(ReadOnlySpan<TValue> second, TValue[] destination, int newZeroRIndex)
        {
            _version = unchecked(++_version);

            var first = _storage;

            if (ReferenceEquals(first, destination))
            {
                Debug.Assert(newZeroRIndex == _zeroRIndex, "In-place merge requires place for the new range.");
            }

            int incomingElementsSkipped = 0;
            int rangeLength = second.Length;
            int d = newZeroRIndex + _count + second.Length - 1;
            int zeroIndexShift = 0;

            Debug.Assert(d < destination.Length, "Right buffer is not long enough");

            bool s_InRange(int s) => s >= 0;
            bool f_InRange(int f) => f >= _zeroRIndex;

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
            }
            else if (f_InRange(f))
            {
                do ProcessMergingFirst(ref d, ref f);
                while (f_InRange(f));
            }

            incomingElementsSkipped = (d + 1) - newZeroRIndex;
            zeroIndexShift += incomingElementsSkipped;

            // Clear remaining free-spots
            if (ItemType<TValue>.ContainsReferences && incomingElementsSkipped > 0 && first == destination)
            {
                Array.Clear(first, newZeroRIndex, incomingElementsSkipped);
            }

            _zeroRIndex = newZeroRIndex + zeroIndexShift;
            _count += second.Length - incomingElementsSkipped;

            _flags.Remove(DuplicateDensity.Possible | DuplicateDensity.Full);

            return true;

            void ProcessMergingBoth(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
            {
                ref readonly TValue initialItem = ref first[f--]; s--; // they are equal here, so we can move both indexes

                destination[d--] = initialItem;

                while (f_InRange(f) && _comparer.Compare(first[f], initialItem) == 0)
                {
                    f--;
                }

                while (s_InRange(s) && _comparer.Compare(second[s], initialItem) == 0)
                {
                    s--;
                }
            }

            void ProcessMergingSecond(ref int d, ref int s, ReadOnlySpan<TValue> second)
            {
                ref readonly TValue initialItem = ref second[s--];
                destination[d--] = initialItem;

                while (s_InRange(s) && _comparer.Compare(second[s], initialItem) == 0)
                {
                    s--;
                }
            }

            void ProcessMergingFirst(ref int d, ref int f)
            {
                ref readonly TValue initialItem = ref first[f--];
                destination[d--] = initialItem;

                while (f_InRange(f) && _comparer.Compare(first[f], initialItem) == 0)
                {
                    f--;
                }
            }
        }
    }
}
