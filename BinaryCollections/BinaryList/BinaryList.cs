using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BinaryCollections.BinaryList
{
    public partial class BinaryList<TValue> : IReadOnlyList<TValue>, ICollection<TValue>
    {
        public int Capacity => _storage.Length;
        public int Count => _count;
        internal IComparer<TValue> Comparer { get; init; } = _defaultComparer;
        public int Version => _version;
        private int LeftBuffer => _zeroRIndex;
        private int RightBuffer => Capacity - _zeroRIndex - _count;
        private bool CanMoveLeft => LeftBuffer > 0;
        private bool CanMoveRight => RightBuffer > 0;
        public bool MayContainCollisions => _mayContainCollisions;
        public bool IsReadOnly => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ToVirtualIndex(int realIndex) => realIndex - _zeroRIndex;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ToRealIndex(int virtualIndex) => virtualIndex + _zeroRIndex;        

        internal static IComparer<TValue> _defaultComparer;
        internal static IEqualityComparer<TValue> _equalityComparer;
        private static readonly bool ContainsReferences;
        private static readonly bool DefaultComparerLeadsToCollisions = true;
        private static readonly bool IsComparable =
            typeof(IComparable<TValue>).IsAssignableFrom(typeof(TValue)) ||
            typeof(IComparable).IsAssignableFrom(typeof(TValue));
        private static readonly bool IsEquatable =
            typeof(IEquatable<TValue>).IsAssignableFrom(typeof(TValue));

        private const int DefaultCapacity = 4;

        static BinaryList()
        {
            bool IsReferenceType = !typeof(TValue).IsValueType;
            ContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<TValue>();

            _defaultComparer = Comparer<TValue>.Default;
            _equalityComparer = EqualityComparer<TValue>.Default;

            if (!IsComparable)
            {
                if (IsReferenceType)
                {
                    _defaultComparer = new ReferenceTypeHashCodeComparer();
                }
                else if (!ContainsReferences)
                {
                    _defaultComparer = new ValueTypeHashCodeComparer();
                }

                DefaultComparerLeadsToCollisions = true;
            }
            else if (IsEquatable && IsEquatable)
            {
                DefaultComparerLeadsToCollisions = false;
            }            
        }

        public BinaryListEnumerator<TValue> GetRange(TValue item)
        {
            return GetRange(ref item);
        }

        public BinaryListEnumerator<TValue> GetRange(ref readonly TValue item)
        {
            var boundaries = GetBoundariesToShare(in item);
            var flags = BinaryListEnumeratorFlags.UniformRange;

            return new(this, boundaries.Source, boundaries.RealIndex, boundaries.Length, flags);
        }

        public BinaryListEnumerator<TValue> GetRange(int index, int length)
        {
            if (!CheckIndex(index, length))
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range");

            return new(this, _storage, ToRealIndex(index), length);
        }

        /// <summary>
        /// Searches for the specified item and returns the index of the first occurrence within the entire collection.<br/>  
        /// If item is found, the return value is the index of the item in the list.<br/> 
        /// Otherwise, the bitwise complement of the index that this element would occupy if it were added to the list.
        /// </summary>
        public int FindIndex(TValue item)
        {
            FindIndex(ref item, out int vIndex);
            return vIndex;
        }

        public bool FindIndex(ref readonly TValue item, out int vIndex)
        {
            int lo = 0;
            int hi = _count - 1;

            return FindIndex(in item, ref lo, ref hi, out vIndex);
        }

        public bool FindIndexExactly(ref readonly TValue item, out int vIndex)
        {
            int lo = 0;
            int hi = _count - 1;

            return FindIndexExactly(in item, ref lo, ref hi, out vIndex);
        }

        public bool FindIndexExactly(ref readonly TValue item, ref int lo, ref int hi, out int vIndex)
        {
            if (!FindIndex(in item, ref lo, ref hi, out vIndex))
                return false;

            // Find identical in right part
            for (int i = vIndex; i < Count; i++)
            {
                var result = Comparer.Compare(_storage[ToRealIndex(i)], item);

                if (result != 0)
                    break;

                if (IsIdentical(ref _storage[ToRealIndex(i)], in item))
                {
                    // Identical
                    vIndex = i;
                    return true;
                }
            }

            // Find identical in left part
            for (int i = vIndex - 1; i >= 0; i--)
            {
                var result = Comparer.Compare(_storage[ToRealIndex(i)], item);

                if (result != 0)
                    break;

                if (IsIdentical(ref _storage[ToRealIndex(i)], in item))
                {
                    // Identical
                    vIndex = i;
                    return true;
                }
            }

            vIndex = ~vIndex;
            return false;
        }

        public (int left, int right) FindBoundaries(ref readonly TValue item)
        {
            int lo, hi, i;
            int left, right, vIndex;
            (int left, int right) result = default;

            // Try find any index of the item
            lo = 0; hi = _count - 1;
            if (!FindIndex(in item, ref lo, ref hi, out i))
                return (~i, ~i);

            left = lo; right = hi; vIndex = i;

            // Find left boundary
            do { result.left = i; hi = i - 1; }
            while (FindIndex(in item, ref lo, ref hi, out i));

            lo = left; hi = right; i = vIndex;

            // Find right boundary
            do { result.right = i; lo = i + 1; }
            while (FindIndex(in item, ref lo, ref hi, out i));

            return result;
        }

        public TValue? Find(Predicate<TValue> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            for (int vIndex = 0; vIndex < _count; vIndex++)
            {
                TValue item = _storage[ToRealIndex(vIndex)];
                if (match(item))
                {
                    return item;
                }
            }

            return default;
        }

        public TValue? FindLast(Predicate<TValue> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            for (int vIndex = _count - 1; vIndex >= 0; vIndex--)
            {
                ref readonly TValue item = ref _storage[ToRealIndex(vIndex)];
                if (match(item))
                {
                    return item;
                }
            }

            return default;
        }

        public List<TValue> FindAll(Predicate<TValue> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            List<TValue> list = new List<TValue>();
            for (int vIndex = _count - 1; vIndex >= 0; vIndex--)
            {
                ref readonly TValue item = ref _storage[ToRealIndex(vIndex)];
                if (match(item))
                {
                    list.Add(item);
                }
            }

            return list;
        }

        public bool Trim()
        {
            return Resize(_count);
        }
        
        public bool Resize(int newCapacity)
        {
            if (_count < newCapacity || _storage.Length == newCapacity)
                return false;

            ResizeArray(newCapacity);
            return true;
        }

        void ICollection<TValue>.Add(TValue item) => Add(in item, out _);

        public bool Add(TValue item) => Add(ref item, out _);
        
        public bool Add(TValue item, out int index)
        {
            return Add(ref item, out index);
        }

        public bool Add(ref readonly TValue item, out int index)
        {
            FindIndex(in item, out index);
            return Insert(in item, ref index);
        }

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

                if (range.Flags.HasFlag(BinaryListEnumeratorFlags.UniformRange))
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

        public bool AddUnique(TValue item)
        {
            return AddUnique(ref item, out _);
        }

        public bool AddUnique(ref readonly TValue item, out int index)
        {
            if (FindIndexExactly(in item, out index))
                return false;

            return Insert(in item, ref index);
        }

        public bool AddRangeUnique(ReadOnlySpan<TValue> range)
        {
            if (!CheckAddRange(range.Length))
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

            if (!CheckAddRange(temp.Count))
                return false;

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return AddSortedRangeUnique(span);
        }

        public bool AddRangeUnique(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer == Comparer)
            {
                if (!CheckAddRange(range.Count))
                    return false;

                if (range.Flags.HasFlag(BinaryListEnumeratorFlags.UniformRange))
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

        public bool Remove(TValue item) => Remove(ref item, out _);

        public bool Remove(ref readonly TValue item, out int index)
        {
            if (FindIndexExactly(in item, out index))
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

        public int RemoveRange(ref readonly TValue item, out int index)
        {
            index = -1;

            var boundaries = GetBoundariesToRemove(in item);
            var vIndex = ToVirtualIndex(boundaries.RealIndex);

            if (RemoveRangeInternal(vIndex, boundaries.Length))
                index = vIndex;

            return boundaries.Length;
        }

        public bool RemoveRange(int index, int length)
        {
            if (!CheckIndex(index, length))
                return false;

            return RemoveRangeInternal(index, length);
        }

        public TValue this[int index]
        {
            get
            {
                if (CheckIndex(index))
                {
                    return _storage[ToRealIndex(index)];
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(index), "Index out of range");
                }
            }
        }

        public TValue GetItem(int index)
        {
            if (CheckIndex(index))
            {
                return _storage[ToRealIndex(index)];
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range");
            }
        }

        public bool RemoveAt(int index)
        {
            if (!CheckIndex(index))
                return false;

            var info = new ArraySplitInfo(index, false, _count);

            if (info.LeftPart.Length <= info.RightPart.Length)
            {
                ShiftLeftPart(ref info);
            }
            else if (info.RightPart.Length < info.LeftPart.Length)
            {
                ShiftRightPart(ref info);
            }

            _count--;
            _version = unchecked(++_version);

            return true;
        }

        public BinaryListEnumerator<TValue> GetEnumerator()
        {
            return new BinaryListEnumerator<TValue>(this, _storage, ToRealIndex(0), _count);
        }

        internal BinaryListEnumerator<TValue> GetEnumerator(int index, int count)
        {
            return new BinaryListEnumerator<TValue>(this, _storage, ToRealIndex(index), count);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<TValue>.Clear() => Clear();

        public void Clear(bool? clearArray = default)
        {
            _version = unchecked(++_version);
            _count = 0;
            _zeroRIndex = CalculateZeroIndex(_storage.Length, _count);

            if (clearArray.HasValue && clearArray.Value)
            {
                Array.Clear(_storage, 0, _storage.Length);
            }
            else if (ContainsReferences)
            {
                Array.Clear(_storage, 0, _storage.Length);
            }
        }

        public bool Contains(TValue item) => Contains(ref item);
        public bool Contains(ref readonly TValue item) => FindIndexExactly(in item, out _);

        public void CopyTo(TValue[] array, int arrayIndex = 0)
        {
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_storage, _zeroRIndex, array, arrayIndex, _count);
        }

        public void CopyTo(int index, TValue[] array, int arrayIndex, int count)
        {
            if (_count - index < count)
            {
                throw new ArgumentException("Not enough elements in the source array");
            }

            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_storage, ToRealIndex(index), array, arrayIndex, count);
        }

        public Span<TValue> ExtractArray(int newArraySize = -1)
        {
            _version = unchecked(++_version);
            var extracted = new Span<TValue>(_storage, _zeroRIndex, _count);

            if (newArraySize >= 0)
            {
                _count = 0;
                _storage = new TValue[DefaultCapacity];
                _zeroRIndex = CalculateZeroIndex(_storage.Length, _count);
            }
            else
                _storage = null!;

            return extracted;
        }

        public BinaryListEnumerator<TValue> ExtractArray()
        {
            _version = unchecked(++_version);
            var flags = _mayContainCollisions ? BinaryListEnumeratorFlags.MayContainCollisions : BinaryListEnumeratorFlags.None;
            var extracted = new BinaryListEnumerator<TValue>(this, _storage, _zeroRIndex, _count, flags);
            _storage = null!;
            return extracted;
        }

        public TValue[] ExtractRange(TValue item)
        {
            ExtractRange(ref item, out _, out var range);
            return range;
        }

        public bool ExtractRange(ref readonly TValue item, out int index, out TValue[] range)
        {
            index = -1;

            var boundaries = GetBoundariesToExtract(in item);
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

        public bool Replace(TValue oldItem, TValue newItem)
        {
            return Replace(ref oldItem, ref newItem, out _);
        }

        public bool Replace(int index, TValue newItem)
        {
            return Replace(index, ref newItem, out _);
        }

        public bool Replace(ref readonly TValue oldItem, ref readonly TValue newItem, out int newIndex)
        {
            return ReplaceCore(in oldItem, out newIndex, in newItem, false);
        }

        public bool Replace(int index, ref readonly TValue newItem, out int newIndex)
        {
            return ReplaceCore(index, out newIndex, in newItem, false);
        }

        public bool ReplaceUnique(TValue oldItem, TValue newItem)
        {
            return ReplaceUnique(ref oldItem, ref newItem, out _);
        }

        public bool ReplaceUnique(int index, TValue newItem)
        {
            return ReplaceUnique(index, ref newItem, out _);
        }

        public bool ReplaceUnique(ref readonly TValue oldItem, ref readonly TValue newItem, out int newIndex)
        {
            return ReplaceCore(in oldItem, out newIndex, in newItem, true);
        }

        public bool ReplaceUnique(int index, ref readonly TValue newItem, out int newIndex)
        {
            return ReplaceCore(index, out newIndex, in newItem, true);
        }

        public bool UnionWith(ReadOnlySpan<TValue> range)
        {
            if (!CheckAddRange(range.Length))
                return false;

            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return UnionWithSortedRange(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool UnionWith(IReadOnlyCollection<TValue> range)
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
                return UnionWithSortedRange(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool UnionWith(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return UnionWith(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return UnionWith(collection);
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
            return UnionWithSortedRange(span);
        }

        public bool UnionWith(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer == Comparer)
            {
                if (!CheckAddRange(range.Count))
                    return false;

                return UnionWithSortedRange(range.GetSpan());
            }

            return UnionWith(range.GetSpan());
        }

        public bool UnionWith(BinaryList<TValue> range)
        {
            return UnionWith(range.GetEnumerator());
        }

        public bool UnionWithUnique(ReadOnlySpan<TValue> range)
        {
            if (!CheckAddRange(range.Length))
                return false;

            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return UnionWithSortedRangeUnique(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool UnionWithUnique(IReadOnlyCollection<TValue> range)
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
                return UnionWithSortedRangeUnique(new ReadOnlySpan<TValue>(temp, 0, range.Count));
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

            if (!CheckAddRange(temp.Count))
                return false;

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return UnionWithSortedRangeUnique(span);
        }

        public bool UnionWithUnique(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer == Comparer)
            {
                if (!CheckAddRange(range.Count))
                    return false;

                return UnionWithSortedRangeUnique(range.GetSpan());
            }

            return UnionWithUnique(range.GetSpan());
        }

        public bool UnionWithUnique(BinaryList<TValue> range)
        {
            return UnionWithUnique(range.GetEnumerator());
        }
    }
}
