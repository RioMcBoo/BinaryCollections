namespace BinaryCollections.BinaryList
{
    public sealed partial class BinaryListComparer<TValue> : IComparer<TValue>
    {
        private readonly bool _leadsToCollisions;
        private readonly IComparer<TValue> _comparer;

        public readonly bool LeadsToCollisions;

        public BinaryListComparer(out BinaryListFlags flags, IComparer<TValue> comparer = null!, bool? comparerLeadsToCollisions = null)
        {
            if (comparer is BinaryListComparer<TValue> binaryListComparer)
            {
                _comparer = binaryListComparer._comparer;
                _leadsToCollisions = binaryListComparer._leadsToCollisions;

                if (comparerLeadsToCollisions.HasValue && comparerLeadsToCollisions.Value == true)
                {
                    // We only allow the stricter rule
                    _leadsToCollisions = true;
                }
            }
            else if (ReferenceEquals(comparer, ItemType<TValue>.DefaultComparer) || comparer == null)
            {
                _comparer = comparer ?? ItemType<TValue>.DefaultComparer;
                _leadsToCollisions = ItemType<TValue>.DefaultComparer == null;

                if (comparerLeadsToCollisions.HasValue && comparerLeadsToCollisions.Value == true)
                {
                    // We only allow the stricter rule
                    _leadsToCollisions = true;
                }
            }
            else
            {
                _comparer = comparer;

                if (comparerLeadsToCollisions.HasValue)
                {
                    _leadsToCollisions = comparerLeadsToCollisions.Value;
                }
                else
                {
                    // Custom comparers are assumed to potentially lead to collisions by default
                    _leadsToCollisions = true;
                }
            }

            flags = LeadsToCollisions ? BinaryListFlags.None : BinaryListFlags.NoCollisions;

            LeadsToCollisions = _leadsToCollisions && ItemType<TValue>.DefaultComparer == null;
        }

        /// <returns>
        ///     <para>A value that indicates the relative order of the objects being compared. The return value has these meanings:</para>
        ///     <list type="table">
        ///         <item>
        ///             <term>Less than zero</term>
        ///             <description><paramref name="x" /> is smaller than <paramref name="y" />.</description>
        ///         </item>
        ///         <item>
        ///             <term>Zero</term>
        ///             <description><paramref name="x" /> equal to <paramref name="y" />.</description>
        ///         </item>
        ///         <item>
        ///             <term>Greater than zero</term>
        ///             <description><paramref name="x" /> is greater than <paramref name="y" />.</description>
        ///         </item>
        ///     </list>
        /// </returns>
        public int Compare(TValue? x, TValue? y)
        {
            int compareResult;

            if (_comparer is not null)
            {
                compareResult = _comparer.Compare(x, y);

                if (compareResult != 0)
                    return compareResult;
            }

            if (ItemType<TValue>.IsReference)
            {
                if (x is null && y is null)
                {
                    return 0;
                }
                else
                {
                    if (x is null) return -1;
                    if (y is null) return 1;
                }
            }

            compareResult = ItemType<TValue>.EqualityComparer.GetHashCode(x).CompareTo(ItemType<TValue>.EqualityComparer.GetHashCode(y));

            if (compareResult != 0)
                return compareResult;

            if (_leadsToCollisions && ItemType<TValue>.DefaultComparer != null)
            {
                compareResult = ItemType<TValue>.DefaultComparer.Compare(x, y);
            }

            return compareResult;
        }

        public override bool Equals(object? obj)
        {
            if (obj is BinaryListComparer<TValue> other)
            {
                return (this._comparer == other._comparer) && (this._leadsToCollisions == other._leadsToCollisions);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_comparer, _leadsToCollisions);
        }

        public static bool operator ==(BinaryListComparer<TValue>? left, BinaryListComparer<TValue>? right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left is null || right is null)
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(BinaryListComparer<TValue>? left, BinaryListComparer<TValue>? right)
        {
            return !(left == right);
        }
    }
}