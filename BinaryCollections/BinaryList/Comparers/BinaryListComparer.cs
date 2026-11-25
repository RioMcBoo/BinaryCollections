using ExceptionHelper;

namespace BinaryCollections.BinaryList
{
    public sealed partial class BinaryListComparer<TValue> : IComparer<TValue>, IEquatable<BinaryListComparer<TValue>>
    {
        public static readonly BinaryListComparer<TValue> Default = new();

        private readonly IComparer<TValue> _orderComparer;
        private readonly IComparer<TValue>? _identityComparer;

        public BinaryListComparer(IComparer<TValue> orderComparer, IComparer<TValue> identityComparer)
        {
            if (orderComparer is null)            
                Throw.ArgumentNullException(nameof(BinaryListComparer<TValue>), nameof(orderComparer));

            if (identityComparer is null)
                Throw.ArgumentNullException(nameof(BinaryListComparer<TValue>), nameof(identityComparer));

            if (orderComparer is BinaryListComparer<TValue> binaryComparer)
                orderComparer = binaryComparer._orderComparer;

            if (identityComparer is BinaryListComparer<TValue> binaryIdentityComparer)
                identityComparer = binaryIdentityComparer._orderComparer;

            _orderComparer = orderComparer;
            _identityComparer = identityComparer;

            if (Equals(_orderComparer, _identityComparer))
                _identityComparer = null;            
        }

        public BinaryListComparer(IComparer<TValue> orderComparer) : this(orderComparer, ItemType<TValue>.DefaultComparer) { }

        public BinaryListComparer() : this(ItemType<TValue>.DefaultComparer, ItemType<TValue>.DefaultComparer) { }

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
        public int CompareByOrder(TValue? x, TValue? y) => _orderComparer.Compare(x, y);

        /// <inheritdoc cref="Compare(TValue?, TValue?)"/>
        public int Compare(TValue? x, TValue? y)
        {
            int compareResult = _orderComparer.Compare(x, y);

            if (compareResult != 0 || _identityComparer is null)
                return compareResult;

            return _identityComparer.Compare(x, y);
        }

        public bool Equals(BinaryListComparer<TValue> obj)
        {
            if (obj is null)
                return false;

            return object.Equals(_orderComparer, obj._orderComparer) && object.Equals(_identityComparer, obj._identityComparer);
        }

        public override bool Equals(object? obj)
        {
            if (obj is BinaryListComparer<TValue> other)
            {
                return this.Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_orderComparer, _identityComparer);
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