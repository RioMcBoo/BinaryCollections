using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CustomCollections.BinaryList
{
    public partial class BinaryList<TValue> : IReadOnlyList<TValue>, IReadOnlyCollection<TValue>, ICollection<TValue>
    {
        public TValue[] ExtractRange(TValue item)
        {
            ExtractRange(ref item, out _, out var range);
            return range;
        }

        public bool ExtractRange(ref TValue item, out int index, out TValue[] range)
        {
            index = -1;

            var boundaries = GetBoundariesToExtract(ref item);
            if (boundaries.Length == 0)
            {
                range = null!;
                return false;
            }

            range = new TValue[boundaries.Length];
            Array.Copy(boundaries.Source, boundaries.RealIndex, range, 0, boundaries.Length);

            if (RemoveRangeInternal(ToVirtualIndex(boundaries.RealIndex), boundaries.Length))
            {
                index = ToVirtualIndex(boundaries.RealIndex);
            }

            return true;
        }

        public bool Remove(TValue item) => Remove(ref item, out _);
        public bool Remove(ref TValue item, out int index)
        {
            if (FindIndexUnique(ref item, out index))
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        public int RemoveRange(TValue item)
        {
            return RemoveRange(ref item, out _);
        }

        public int RemoveRange(ref TValue item, out int index)
        {
            index = -1;

            var boundaries = GetBoundariesToRemove(ref item);

            if (RemoveRangeInternal(ToVirtualIndex(boundaries.RealIndex), boundaries.Length))
                index = ToVirtualIndex(boundaries.RealIndex);

            return boundaries.Length;
        }

        public bool RemoveRange(int index, int length)
        {
            if (!CheckIndex(index, length))
                return false;

            return RemoveRangeInternal(index, length);
        }

        private bool RemoveRangeInternal(int vIndex, int length)
        {
            if (length > 0)
            {
                var info = new ArraySplitInfo(new Point(vIndex, -length), _storage.Length);
                ShiftArray(_storage, _storage, ref info, 0);
                _count -= length;

                _version = unchecked(++_version);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckAddRange(int rangeCount)
        {
            if (rangeCount <= 0)
                return false;

            if (rangeCount > Array.MaxLength - _count)
                return false;

            return true;
        }

        public bool AddRange(BinaryListEnumerator<TValue> range)
        {
            if (!CheckAddRange(range.Count))
                return false;

            if (range.Comparer == Comparer)
            {
                if (range.Flags.HasFlag(BinaryListEnumeratorFlags.UniformRange))
                {
                    return AddUniformRange(range.GetSpan());
                }

                return AddSortedRange(range.GetSpan());
            }

            var span = range.GetSpan();
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(span.Length);
            try
            {
                span.CopyTo(temp);
                Array.Sort(temp, 0, span.Length, Comparer);
                return AddSortedRange(new ReadOnlySpan<TValue>(temp, 0, span.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp);
            }
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
            return InsertSortedRangeFromLeft(range, _storage, 0);
        }

        private bool InsertSortedRangeFromLeft(ReadOnlySpan<TValue> range, TValue[] destination, int movingIndexDelta)
        {
            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(movingIndexDelta == 0, "In-place merge requires zero delta.");
            }

            int i = 0;
            int c = _zeroRIndex;
            int newZeroRIndex = movingIndexDelta + _zeroRIndex - range.Length;
            int insertRIndex = newZeroRIndex;

            while (i < range.Length && c < _zeroRIndex + _count)
            {
                ref readonly TValue incomingItem = ref range[i];
                ref readonly TValue currentItem = ref _storage[c];

                switch (Comparer.Compare(currentItem, incomingItem))
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
            else if (c < _zeroRIndex + _count && _storage == destination)
            {
                Array.Copy(_storage, c, _storage, insertRIndex, _zeroRIndex + _count - c);
            }

            _zeroRIndex = newZeroRIndex;
            _count += range.Length;
            _version = unchecked(++_version);
            return true;
        }

        private bool InsertSortedRangeFromRight(ReadOnlySpan<TValue> range)
        {
            return InsertSortedRangeFromRight(range, _storage, 0);
        }

        private bool InsertSortedRangeFromRight(ReadOnlySpan<TValue> range, TValue[] destination, int movingIndexDelta)
        {
            if (ReferenceEquals(_storage, destination))
            {
                Debug.Assert(movingIndexDelta == 0, "In-place merge requires zero delta.");
            }

            int i = range.Length - 1;
            int c = _zeroRIndex + _count - 1;
            int newZeroRIndex = movingIndexDelta + _zeroRIndex;
            int insertRIndex = newZeroRIndex + _count + range.Length - 1;

            while (i >= 0 && c >= _zeroRIndex)
            {
                ref readonly TValue incomingItem = ref range[i];
                ref readonly TValue currentItem = ref _storage[c];

                switch (Comparer.Compare(currentItem, incomingItem))
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
            else if (c >= _zeroRIndex && _storage == destination)
            {
                Array.Copy(_storage, _zeroRIndex, _storage, newZeroRIndex, c - _zeroRIndex + 1);
            }

            _zeroRIndex = newZeroRIndex;
            _count += range.Length;
            _version = unchecked(++_version);
            return true;
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

        private bool AddUnsortedRange(ReadOnlySpan<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return AddSortedRange(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp);
            }
        }

        public bool AddRange(ReadOnlySpan<TValue> range)
        {
            if (!CheckAddRange(range.Length))
                return false;

            return AddUnsortedRange(range);
        }

        public bool AddRange(IReadOnlyCollection<TValue> range)
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
                return AddSortedRange(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
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

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return AddSortedRange(span);
        }

        public BinaryListEnumerator<TValue> GetRange(TValue item)
        {
            return GetRange(ref item);
        }

        public BinaryListEnumerator<TValue> GetRange(ref TValue item)
        {
            var boundaries = GetBoundariesToShare(ref item);

            return new(this, boundaries.Source, boundaries.RealIndex, boundaries.Length, BinaryListEnumeratorFlags.UniformRange);
        }

        public BinaryListEnumerator<TValue> GetRange(int index, int length)
        {
            if (!CheckIndex(index, length))
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range");

            return new(this, _storage, ToRealIndex(index), length);
        }

        private ListRangeInfo GetBoundariesToRemove(ref TValue item)
        {
            var (left, right) = FindBoundaries(ref item);

            if (left < 0)
                return new();

            TValue[] source = _storage;
            int initialRIndex = ToRealIndex(left);
            int lengthInitial = right - left + 1;

            RemoveRangeFromCollisions(ref item, ref initialRIndex, ref lengthInitial, out source);

            return new(initialRIndex, lengthInitial, source);

            void RemoveRangeFromCollisions(ref TValue item, ref int rIndex, ref int length, out TValue[] source)
            {
                int write = rIndex;
                _version = unchecked(++_version);

                for (int read = rIndex; read < rIndex + length; read++)
                {
                    ref var currItem = ref _storage[read];
                    if (!IsIdentical(ref currItem, ref item))
                    {
                        _storage[write] = currItem;
                        write++;
                    }
                }

                source = _storage;
                int keptCountInBlock = write - rIndex;

                rIndex = write;
                length = lengthInitial - keptCountInBlock;
            }
        }

        private ListRangeInfo GetBoundariesToExtract(ref TValue item)
        {
            var (left, right) = FindBoundaries(ref item);

            if (left < 0)
                return new();

            TValue[] source = _storage;
            int initialRIndex = ToRealIndex(left);
            int lengthInitial = right - left + 1;

            ExtractRangeFromCollisions(ref item, ref initialRIndex, ref lengthInitial, out source);

            return new(initialRIndex, lengthInitial, source);

            void ExtractRangeFromCollisions(ref TValue item, ref int rIndex, ref int length, out TValue[] source)
            {
                int write = rIndex;
                _version = unchecked(++_version);

                TValue[] temp = ArrayPool<TValue>.Shared.Rent(length); int t = 0;

                try
                {
                    for (int read = rIndex; read < rIndex + length; read++)
                    {
                        ref var currItem = ref _storage[read];
                        if (!IsIdentical(ref currItem, ref item))
                        {
                            _storage[write] = currItem;
                            write++;
                        }
                        else
                        {
                            temp[t++] = currItem;
                        }
                    }

                    Array.Copy(temp, 0, _storage, write, t);
                }
                finally
                {
                    ArrayPool<TValue>.Shared.Return(temp);
                }

                source = _storage;
                int keptCountInBlock = write - rIndex;

                rIndex = write;
                length = lengthInitial - keptCountInBlock;
            }
        }

        private ListRangeInfo GetBoundariesToShare(ref TValue item)
        {
            var (left, right) = FindBoundaries(ref item);

            if (left < 0)
                return new();

            TValue[] source = _storage;
            int initialRIndex = ToRealIndex(left);
            int lengthInitial = right - left + 1;

            CopyRangeFromCollisions(ref item, ref initialRIndex, ref lengthInitial, out source);

            return new(initialRIndex, lengthInitial, source);

            void CopyRangeFromCollisions(ref TValue item, ref int rIndex, ref int length, out TValue[] source)
            {
                source = new TValue[length];
                int write = 0;

                for (int read = rIndex; read < rIndex + length; read++)
                {
                    ref var currItem = ref _storage[read];
                    if (IsIdentical(ref currItem, ref item))
                    {
                        source[write] = currItem;
                        write++;
                    }
                }

                rIndex = 0;
                length = write;
            }
        }
    }
}
