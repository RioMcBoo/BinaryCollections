namespace BinaryCollections.BinaryList
{
    public sealed partial class BinaryList<TValue> : FloatingList<TValue>
    {
        private bool ContaninsCollisions => _comparerLeadsToCollisions && DefaultComparerLeadsToCollisions;
        internal IComparer<TValue> Comparer { get; init; } = _defaultComparer;
        public bool ComparerLeadsToCollisions => _comparerLeadsToCollisions;

        internal static IComparer<TValue> _defaultComparer = Comparer<TValue>.Default;
        private static readonly bool DefaultComparerLeadsToCollisions;
        private static readonly bool IsItemTypeComparable =
            typeof(IComparable<TValue>).IsAssignableFrom(typeof(TValue)) ||
            typeof(IComparable).IsAssignableFrom(typeof(TValue));
        private static readonly bool IsItemTypeEquatable =
            typeof(IEquatable<TValue>).IsAssignableFrom(typeof(TValue));

        static BinaryList()
        {
            bool IsReferenceType = !typeof(TValue).IsValueType;
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
                    // we will require a comparer from user
                }
            }
        }        

        public bool Contains(TValue item) => Contains(in item, out _);
        public bool Contains(ref readonly TValue item, out int index) => FindIndexExactly(in item, out index);

        /// <summary>
        /// Searches for the specified item and returns the index of the first occurrence within the entire collection.<br/>  
        /// If item is found, the return value is the index of the item in the list.<br/> 
        /// Otherwise, the bitwise complement of the index that this element would occupy if it were added to the list.
        /// </summary>
        public int IndexOf(TValue item)
        {
            FindLeftIndex(in item, out int vIndex);
            return vIndex;
        }

        /// <summary>
        /// Searches for the specified item and returns the index of the last occurrence within the entire collection.<br/>  
        /// If item is found, the return value is the index of the item in the list.<br/> 
        /// Otherwise, the bitwise complement of the index that this element would occupy if it were added to the list.
        /// </summary>
        public int LastIndexOf(TValue item)
        {
            FindRightIndex(in item, out int vIndex);
            return vIndex;
        }

        public BinaryListEnumerator<TValue> GetEnumerator()
        {
            return new BinaryListEnumerator<TValue>(this, _storage, ToRealIndex(0), _count);
        }

        internal BinaryListEnumerator<TValue> GetEnumerator(int index, int count)
        {
            return new BinaryListEnumerator<TValue>(this, _storage, ToRealIndex(index), count);
        }

        public BinaryListEnumerator<TValue> ExtractArray()
        {
            _version = unchecked(++_version);
            var flags = _comparerLeadsToCollisions ? BinaryListEnumeratorFlags.ComparerLeadsToCollisions : BinaryListEnumeratorFlags.None;
            var extracted = new BinaryListEnumerator<TValue>(this, _storage, _zeroRIndex, _count, flags);
            _storage = null!;
            return extracted;
        }
    }
}
