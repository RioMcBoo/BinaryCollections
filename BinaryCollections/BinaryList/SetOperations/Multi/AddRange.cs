using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool AddRange(ReadOnlySpan<TValue> range)
        {
            if (!CheckCountOverload(range.Length))
                return false;

            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return AddSortedRange(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool AddRange(IReadOnlyCollection<TValue> range)
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
                return AddSortedRange(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool AddRange(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return AddRange(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return AddRange(collection);
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
            return AddSortedRange(span);
        }

        public bool AddRange(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                if (!CheckCountOverload(range.Count))
                    return false;

                if (range.Flags.HasFlag(BinaryListFlags.UniformRange))
                {
                    return AddUniformRange(range.GetSpan());
                }

                return AddSortedRange(range.GetSpan());
            }

            return AddRange(range.GetSpan());
        }

        public bool AddRange(BinaryList<TValue> range)
        {
            return AddRange(range.GetEnumerator());
        }

        private bool AddSortedRange(ReadOnlySpan<TValue> range)
        {
            if (range.Length <= 0)
                return false;

            if (LeftBuffer >= RightBuffer && LeftBuffer >= range.Length)
            {
                return InsertSortedRangeFromLeft(range);
            }

            if (RightBuffer >= LeftBuffer && RightBuffer >= range.Length)
            {
                return InsertSortedRangeFromRight(range);
            }

            return ResizeAndInsertSortedRange(range);
        }

        private bool AddUniformRange(ReadOnlySpan<TValue> range)
        {
            if (range.Length <= 0)
                return false;

            var example = range[0];
            _comparer.FindIndex(ref example, Values, out var vIndex);
            InsertUniformRange(range, ref vIndex);
            return true;
        }

        private bool ResizeAndInsertSortedRange(ReadOnlySpan<TValue> range)
        {
            PrepareForResize(range.Length, out var destination);

            int newZeroRIndex = CalculateZeroIndex(destination.Length, _count + range.Length);

            if (_storage == destination)
            {
                Array.Copy(destination, _zeroRIndex, destination, newZeroRIndex, _count);
                _zeroRIndex = newZeroRIndex;
                return InsertSortedRangeFromRight(range);
            }
#if TEST
            if (_count % 2 == 0)
            {
                // Because this case does not arise in practice,
                // yet we must maintain the code for the eventuality that it might suddenly become active.
                InsertSortedRangeFromRight(range, destination, newZeroRIndex);
            }
            else
            {
                InsertSortedRangeFromLeft(range, destination, newZeroRIndex);
            }
#else
            InsertSortedRangeFromLeft(range, destination, newZeroRIndex);
#endif
            _storage = destination;
            return true;
        }

        private bool InsertSortedRangeFromLeft(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex - range.Length;
            return InsertSortedRangeFromLeft(range, _storage, newZeroRIndex);
        }

        private bool InsertSortedRangeFromLeft(ReadOnlySpan<TValue> second, TValue[] destination, int newZeroRIndex)
        {
            _version = unchecked(++_version);

            var first = _storage;

            if (ReferenceEquals(first, destination))
            {
                Debug.Assert(_zeroRIndex - newZeroRIndex == second.Length, "In-place merge requires place for the new range.");
            }

            int s = 0;
            int f = _zeroRIndex;
            int d = newZeroRIndex;

            Debug.Assert(d > -1, "Left buffer is not long enough");

            while (s < second.Length && f < _zeroRIndex + _count)
            {
                ref readonly TValue secondItem = ref second[s];
                ref readonly TValue firstItem = ref first[f];

                switch (_comparer.Compare(firstItem, secondItem))
                {
                    case > 0:
                        destination[d++] = secondItem; s++;
                        break;
                    case < 0:
                        destination[d++] = firstItem; f++;
                        break;
                    default:
                        destination[d++] = secondItem; s++;
                        destination[d++] = firstItem; f++;
                        break;
                }
            }

            if (s < second.Length)
            {
                second[s..].CopyTo(destination.AsSpan(d));
            }
            else if (f < _zeroRIndex + _count && _storage != destination)
            {
                Array.Copy(_storage, f, destination, d, _zeroRIndex + _count - f);
            }

            _zeroRIndex = newZeroRIndex;
            _count += second.Length;

            RemoveFlags(BinaryListFlags.NoDuplicates | BinaryListFlags.UniformRange);

            return true;
        }

        private bool InsertSortedRangeFromRight(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex;
            return InsertSortedRangeFromRight(range, _storage, newZeroRIndex);
        }

        private bool InsertSortedRangeFromRight(ReadOnlySpan<TValue> second, TValue[] destination, int newZeroRIndex)
        {
            _version = unchecked(++_version);

            var first = _storage;

            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(newZeroRIndex == _zeroRIndex, "In-place merge requires place for the new range.");
            }

            int s = second.Length - 1;
            int f = _zeroRIndex + _count - 1;
            int insertRIndex = newZeroRIndex + _count + second.Length - 1;

            Debug.Assert(insertRIndex < destination.Length, "Right buffer is not long enough");

            while (s >= 0 && f >= _zeroRIndex)
            {
                ref readonly TValue secondItem = ref second[s];
                ref readonly TValue firstItem = ref first[f];

                switch (_comparer.Compare(firstItem, secondItem))
                {
                    case > 0:
                        destination[insertRIndex--] = firstItem; f--;
                        break;
                    case < 0:
                        destination[insertRIndex--] = secondItem; s--;
                        break;
                    default:
                        destination[insertRIndex--] = firstItem; f--;
                        destination[insertRIndex--] = secondItem; s--;
                        break;
                }
            }

            if (s >= 0)
            {
                second[..(s + 1)].CopyTo(destination.AsSpan(newZeroRIndex));
            }
            else if (f >= _zeroRIndex && _storage != destination)
            {
                Array.Copy(_storage, _zeroRIndex, destination, newZeroRIndex, f - _zeroRIndex + 1);
            }

            _zeroRIndex = newZeroRIndex;
            _count += second.Length;

            RemoveFlags(BinaryListFlags.NoDuplicates | BinaryListFlags.UniformRange);

            return true;
        }
    }
}
