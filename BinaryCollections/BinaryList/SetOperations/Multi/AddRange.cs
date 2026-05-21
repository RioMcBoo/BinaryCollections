using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool AddRange(ReadOnlySpan<TValue> range)
        {
            if (!CheckAddRange(range.Length))
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
            if (!CheckAddRange(range.Count))
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

            if (!CheckAddRange(temp.Count))
                return false;

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return AddSortedRange(span);
        }

        public bool AddRange(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer == Comparer)
            {
                if (!CheckAddRange(range.Count))
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
            var example = range[0];
            FindIndex(ref example, out var vIndex);
            return InsertUniformRange(range, ref vIndex);
        }

        private bool InsertUniformRange(ReadOnlySpan<TValue> range, ref int vIndex)
        {
            vIndex = vIndex < 0 ? ~vIndex : vIndex;
            var info = new ArraySplitInfo(vIndex, range.Length, _count);

            if (_count == 0)
            {
                ResizeArray(ref info);
            }
            else
            {
                if (CanMoveLeft && info.LeftPart.Length <= info.RightPart.Length)
                {
                    ShiftLeftPart(ref info);
                }
                else if (CanMoveRight && info.RightPart.Length <= info.LeftPart.Length)
                {
                    ShiftRightPart(ref info);
                }
                else
                {
                    ResizeArray(ref info);
                }
            }

            range.CopyTo(_storage.AsSpan(ToRealIndex(vIndex), range.Length));
            _count += range.Length;
            _version = unchecked(++_version);

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

            InsertSortedRangeFromLeft(range, destination, newZeroRIndex);

            _storage = destination;
            return true;
        }

        private bool InsertSortedRangeFromLeft(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex - range.Length;
            return InsertSortedRangeFromLeft(range, _storage, newZeroRIndex);
        }

        private bool InsertSortedRangeFromLeft(ReadOnlySpan<TValue> range, TValue[] destination, int newZeroRIndex)
        {
            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(_zeroRIndex - newZeroRIndex == range.Length, "In-place merge requires place for the new range.");
            }

            int i = 0;
            int c = _zeroRIndex;
            int insertRIndex = newZeroRIndex;

            Debug.Assert(insertRIndex > -1, "Left buffer is not long enough");

            while (i < range.Length && c < _zeroRIndex + _count)
            {
                ref readonly TValue incomingItem = ref range[i];
                ref readonly TValue currentItem = ref _storage[c];

                switch (Compare(in currentItem, in incomingItem))
                {
                    case > 0:
                        destination[insertRIndex++] = incomingItem; i++;
                        break;
                    case < 0:
                        destination[insertRIndex++] = currentItem; c++;
                        break;
                    default:
                        destination[insertRIndex++] = incomingItem; i++;
                        destination[insertRIndex++] = currentItem; c++;
                        break;
                }
            }

            if (i < range.Length)
            {
                range[i..].CopyTo(destination.AsSpan(insertRIndex));
            }
            else if (c < _zeroRIndex + _count && _storage != destination)
            {
                Array.Copy(_storage, c, destination, insertRIndex, _zeroRIndex + _count - c);
            }

            _zeroRIndex = newZeroRIndex;
            _count += range.Length;
            _version = unchecked(++_version);
            return true;
        }

        private bool InsertSortedRangeFromRight(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex;
            return InsertSortedRangeFromRight(range, _storage, newZeroRIndex);
        }

        private bool InsertSortedRangeFromRight(ReadOnlySpan<TValue> range, TValue[] destination, int newZeroRIndex)
        {
            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(newZeroRIndex == _zeroRIndex, "In-place merge requires place for the new range.");
            }

            int i = range.Length - 1;
            int c = _zeroRIndex + _count - 1;
            int insertRIndex = newZeroRIndex + _count + range.Length - 1;

            Debug.Assert(insertRIndex < destination.Length, "Right buffer is not long enough");

            while (i >= 0 && c >= _zeroRIndex)
            {
                ref readonly TValue incomingItem = ref range[i];
                ref readonly TValue currentItem = ref _storage[c];

                switch (Compare(in currentItem, in incomingItem))
                {
                    case > 0:
                        destination[insertRIndex--] = currentItem; c--;
                        break;
                    case < 0:
                        destination[insertRIndex--] = incomingItem; i--;
                        break;
                    default:
                        destination[insertRIndex--] = currentItem; c--;
                        destination[insertRIndex--] = incomingItem; i--;
                        break;
                }
            }

            if (i >= 0)
            {
                range[..(i + 1)].CopyTo(destination.AsSpan(newZeroRIndex));
            }
            else if (c >= _zeroRIndex && _storage != destination)
            {
                Array.Copy(_storage, _zeroRIndex, destination, newZeroRIndex, c - _zeroRIndex + 1);
            }

            _zeroRIndex = newZeroRIndex;
            _count += range.Length;
            _version = unchecked(++_version);
            return true;
        }
    }
}
