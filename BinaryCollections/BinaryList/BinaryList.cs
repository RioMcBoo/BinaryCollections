using System.Runtime.CompilerServices;

namespace BinaryCollections.BinaryList
{
    public partial class BinaryList<TValue>
    {
        public int Capacity => _storage.Length;
        public int Count => _count;
        internal IComparer<TValue> Comparer { get; init; } = _defaultComparer;
        public int Version => _version;
        private int LeftBuffer => _zeroRIndex;
        private int RightBuffer => Capacity - _zeroRIndex - _count;
        private bool CanMoveLeft => LeftBuffer > 0;
        private bool CanMoveRight => RightBuffer > 0;
        public bool ContainCompareCollisions => _containCompareCollisions;
        
        private int ToVirtualIndex(int realIndex) => realIndex - _zeroRIndex;
        private int ToRealIndex(int virtualIndex) => virtualIndex + _zeroRIndex;        

        internal static IComparer<TValue> _defaultComparer;
        internal static IEqualityComparer<TValue> _equalityComparer;
        private static readonly bool ContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<TValue>();
        private static readonly bool DefaultComparerLeadsToCollisions;
        private static readonly bool IsItemTypeComparable =
            typeof(IComparable<TValue>).IsAssignableFrom(typeof(TValue)) ||
            typeof(IComparable).IsAssignableFrom(typeof(TValue));
        private static readonly bool IsItemTypeEquatable =
            typeof(IEquatable<TValue>).IsAssignableFrom(typeof(TValue));

        private const int DefaultCapacity = 4;

        static BinaryList()
        {
            bool IsReferenceType = !typeof(TValue).IsValueType;

            _defaultComparer = Comparer<TValue>.Default;
            _equalityComparer = EqualityComparer<TValue>.Default;
            DefaultComparerLeadsToCollisions = !IsItemTypeComparable || !IsItemTypeEquatable;

            if (DefaultComparerLeadsToCollisions)
            {
                if (IsReferenceType)
                {
                    _defaultComparer = new ReferenceTypeHashCodeComparer();
                }
                else if (!ContainsReferences)
                {
                    _defaultComparer = new ValueTypeHashCodeComparer();
                }
                else
                {
                    throw new InvalidOperationException("Type is not comparable");
                }
            }
        }

        public TValue this[int index] => GetItem(index);

        public bool RemoveAt(int index)
        {
            if (!CheckIndex(index))
                return false;

            return RemoveAtIndex(index);
        }

        /// <summary>
        /// Searches for the specified item and returns the index of the first occurrence within the entire collection.<br/>  
        /// If item is found, the return value is the index of the item in the list.<br/> 
        /// Otherwise, the bitwise complement of the index that this element would occupy if it were added to the list.
        /// </summary>
        public int IndexOf(TValue item)
        {
            FindLeftIndex(ref item, out int vIndex);
            return vIndex;
        }

        /// <summary>
        /// Searches for the specified item and returns the index of the last occurrence within the entire collection.<br/>  
        /// If item is found, the return value is the index of the item in the list.<br/> 
        /// Otherwise, the bitwise complement of the index that this element would occupy if it were added to the list.
        /// </summary>
        public int LastIndexOf(TValue item)
        {
            FindRightIndex(ref item, out int vIndex);
            return vIndex;
        }

        public TValue? Find(Predicate<TValue> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            // Linear search
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

        public bool Contains(TValue item) => Contains(in item, out _);

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

        public BinaryListEnumerator<TValue> GetEnumerator()
        {
            return new BinaryListEnumerator<TValue>(this, _storage, ToRealIndex(0), _count);
        }

        internal BinaryListEnumerator<TValue> GetEnumerator(int index, int count)
        {
            return new BinaryListEnumerator<TValue>(this, _storage, ToRealIndex(index), count);
        }        

        public void Clear(bool? clearArray = default)
        {
            _version = unchecked(++_version);
            _count = 0;
            _zeroRIndex = CalculateZeroIndex(_storage.Length, _count);

            if (clearArray.HasValue)
            {
                if  (clearArray.Value)
                    Array.Clear(_storage, 0, _storage.Length);
            }
            else if (ContainsReferences)
            {
                Array.Clear(_storage, 0, _storage.Length);
            }
        }
        
        public bool Contains(ref readonly TValue item, out int index) => FindIndexExactly(in item, out index);        

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
            var flags = _containCompareCollisions ? BinaryListEnumeratorFlags.ContainCompareCollisions : BinaryListEnumeratorFlags.None;
            var extracted = new BinaryListEnumerator<TValue>(this, _storage, _zeroRIndex, _count, flags);
            _storage = null!;
            return extracted;
        }
    }
}
