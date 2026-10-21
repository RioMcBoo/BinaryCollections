using System.Runtime.CompilerServices;

namespace BinaryCollections.BinaryList
{
    public sealed partial class BinaryListComparer<TValue> : IInformativeComparer<TValue>
    {
        private readonly bool _comparerCorrespondsToEquals;
        private readonly IComparer<TValue> _comparer;

        internal readonly bool LeadsToCollisions;
        public bool CorrespondsToEquals
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !LeadsToCollisions;
        }

        /// <param name="comparer">
        /// An optional <see cref="IComparer{T}"/> to use for element ordering. 
        /// Can be an instance of <see cref="BinaryListComparer{TValue}"/>, an <see cref="IInformativeComparer{T}"/>, 
        /// or an <see cref="IComparable{T}"/>-based comparer. If <see langword="null"/>, the default <see cref="IComparable{T}"/> logic for <typeparamref name="TValue"/> is used.
        /// </param>
        /// <param name="comparerCorrespondsToEquals">
        /// An optional boolean flag indicating whether a return value of 0 from the <paramref name="comparer"/>.Compare() method 
        /// signifies absolute identity of the elements. 
        /// <para>
        /// Safety rules for this parameter:
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// <b>Downgrade only:</b> A user cannot force a higher level of trust. If an <see cref="IInformativeComparer{T}"/> 
        /// declares itself unsafe (<see cref="IInformativeComparer{T}.CorrespondsToEquals"/> <see langword="is"></see> <see langword="false"/>), 
        /// passing <see langword="true"/> here will not override it. Trust can only be reduced, never increased.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <b>User override for raw comparers:</b> If a raw, non-informative comparer is provided, the user is allowed 
        /// to explicitly declare its safety via this parameter. However, if no explicit flag is given, or <paramref name="comparer"/> <see langword="is"/> <see langword="null"/>, the system refuses to blindly trust it, 
        /// treating it as potentially leading to collisions.
        /// </description>
        /// </item>
        /// </list>
        /// </para>
        /// </param>
        public BinaryListComparer(IComparer<TValue> comparer = null!, bool? comparerCorrespondsToEquals = null)
        {
            if (comparer is BinaryListComparer<TValue> binaryListComparer)
            {
                _comparer = binaryListComparer._comparer;
                _comparerCorrespondsToEquals = binaryListComparer._comparerCorrespondsToEquals;
            }
            else if (comparer is IInformativeComparer<TValue> informativeComparer)
            {
                _comparer = informativeComparer;
                _comparerCorrespondsToEquals = informativeComparer.CorrespondsToEquals;
            }
            else if (ReferenceEquals(comparer, ItemType<TValue>.DefaultComparer))
            {
                _comparer = null!;
                _comparerCorrespondsToEquals = false;
            }
            else
            {
                _comparer = comparer;

                if (comparerCorrespondsToEquals.HasValue)
                {
                    _comparerCorrespondsToEquals = comparerCorrespondsToEquals.Value;
                }
                else
                {
                    // Custom comparers are assumed to potentially lead to collisions by default
                    _comparerCorrespondsToEquals = false;
                }
            }

            if (comparerCorrespondsToEquals.HasValue && comparerCorrespondsToEquals.Value == false)
            {
                // We only allow the stricter rule
                _comparerCorrespondsToEquals = false;
            }

            LeadsToCollisions = !_comparerCorrespondsToEquals && ItemType<TValue>.DefaultComparer is null;
        }

        /// <returns>
        ///     <para>A value that indicates the relative order of the objects being compared. The return value has these meanings:</para>
        ///     <list type="table">
        ///         <item>
        ///             <term>Less than zero</term>
        ///             <description><paramref name="currentItem" /> is smaller than <paramref name="item" />.</description>
        ///         </item>
        ///         <item>
        ///             <term>Zero</term>
        ///             <description><paramref name="currentItem" /> equal to <paramref name="item" />.</description>
        ///         </item>
        ///         <item>
        ///             <term>Greater than zero</term>
        ///             <description><paramref name="currentItem" /> is greater than <paramref name="item" />.</description>
        ///         </item>
        ///     </list>
        /// </returns>
        public int Compare(TValue? currentItem, TValue? item)
        {
            int compareResult = 0;

            if (_comparer is not null)
            {
                compareResult = _comparer.Compare(currentItem, item);

                if (compareResult != 0)
                    return compareResult;
            }

            if (!_comparerCorrespondsToEquals || _comparer is null)
            {
                compareResult = ItemType<TValue>.EqualityComparer.GetHashCode(currentItem).CompareTo(ItemType<TValue>.EqualityComparer.GetHashCode(item));

                if (compareResult != 0)
                    return compareResult;

                if (ItemType<TValue>.DefaultComparer is not null)
                {
                    compareResult = ItemType<TValue>.DefaultComparer.Compare(currentItem, item);
                }
            }

            return compareResult;
        }

        public override bool Equals(object? obj)
        {
            if (obj is BinaryListComparer<TValue> other)
            {
                return (this._comparer == other._comparer) && (this._comparerCorrespondsToEquals == other._comparerCorrespondsToEquals);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_comparer, _comparerCorrespondsToEquals);
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