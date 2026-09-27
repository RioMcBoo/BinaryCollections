using System.Diagnostics;

namespace BinaryCollections.BinaryList
{
    public sealed partial class BinaryList<TValue> : FloatingList<TValue>
    {
        public BinaryListComparer Comparer => _comparer;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool ContaninsCollisions => _comparer.LeadsToCollisions;

        internal static IComparer<TValue> DefaultComparer = Comparer<TValue>.Default;
        private static readonly bool DefaultComparerLeadsToCollisions;

        static BinaryList()
        {
            bool IsReferenceType = !typeof(TValue).IsValueType;
            DefaultComparerLeadsToCollisions = !ItemType<TValue>.IsComparable || !ItemType<TValue>.IsEquatable;

            if (DefaultComparerLeadsToCollisions)
            {
                if (IsReferenceType)
                {
                    DefaultComparer = new ReferenceTypeHashCodeComparer();
                }
                else if (!ItemType<TValue>.ContainsReferences)
                {
                    DefaultComparer = new ValueTypeHashCodeComparer();
                }
                else
                {
                    // we will require a comparer from user
                }
            }
        }

        public bool Contains(TValue item) => Contains(in item, out _);
        public bool Contains(ref readonly TValue item, out int index) => FindIndexExactly(in item, Values, Comparer, out index);

        /// <summary>
        /// Searches for the specified item and returns the index of the first occurrence within the entire collection.<br/>  
        /// If item is found, the return value is the index of the item in the list.<br/> 
        /// Otherwise, the bitwise complement of the index that this element would occupy if it were added to the list.
        /// </summary>
        public int IndexOf(TValue item)
        {
            FindLeftIndex(in item, Values, Comparer, out int vIndex);
            return vIndex;
        }

        /// <summary>
        /// Searches for the specified item and returns the index of the last occurrence within the entire collection.<br/>  
        /// If item is found, the return value is the index of the item in the list.<br/> 
        /// Otherwise, the bitwise complement of the index that this element would occupy if it were added to the list.
        /// </summary>
        public int LastIndexOf(TValue item)
        {
            FindRightIndex(in item, Values, Comparer, out int vIndex);
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
            var extracted = new BinaryListEnumerator<TValue>(this, _storage, _zeroRIndex, _count);
            _storage = null!;
            return extracted;
        }
    }
}
