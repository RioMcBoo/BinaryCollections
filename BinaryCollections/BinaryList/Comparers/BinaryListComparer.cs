
namespace BinaryCollections.BinaryList
{
    public partial class BinaryList<TValue>
    {
        public class BinaryListComparer : IComparer<TValue>
        {
            public bool LeadsToCollisions { get; init; }
            internal readonly IComparer<TValue> _comparer;

            public bool ContainsCollisions => LeadsToCollisions && DefaultComparerLeadsToCollisions;

            public BinaryListComparer(ref BinaryListFlags flags, IComparer<TValue> comparer = null!, bool? comparerLeadsToCollisions = null)
            {
                if (comparer is BinaryListComparer binaryListComparer)
                {
                    _comparer = binaryListComparer._comparer;
                    LeadsToCollisions = binaryListComparer.LeadsToCollisions;

                    if (comparerLeadsToCollisions.HasValue && comparerLeadsToCollisions.Value == true)
                    {
                        // We only allow the stricter rule
                        LeadsToCollisions = true;
                    }
                }
                else if (ReferenceEquals(comparer, _defaultComparer) || comparer == null)
                {
                    _comparer = comparer ?? _defaultComparer;
                    LeadsToCollisions = DefaultComparerLeadsToCollisions;

                    if (comparerLeadsToCollisions.HasValue && comparerLeadsToCollisions.Value == true)
                    {
                        // We only allow the stricter rule
                        LeadsToCollisions = true;
                    }
                }
                else
                {
                    _comparer = comparer;

                    if (comparerLeadsToCollisions.HasValue)
                    {
                        LeadsToCollisions = comparerLeadsToCollisions.Value;
                    }
                    else
                    {
                        // Custom comparers are assumed to potentially lead to collisions by default
                        LeadsToCollisions = true;
                    }
                }

                flags = ContainsCollisions ? flags | BinaryListFlags.MayContainCollisions : flags & ~BinaryListFlags.MayContainCollisions;
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
                int compareResult = _comparer.Compare(x, y);

                if (LeadsToCollisions && !DefaultComparerLeadsToCollisions)
                {
                    compareResult = _defaultComparer.Compare(x, y);
                }

                return compareResult;
            }

            public override bool Equals(object? obj)
            {
                if (obj is BinaryListComparer other)
                {
                    return (this._comparer == other._comparer) && (this.LeadsToCollisions == other.LeadsToCollisions);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_comparer, LeadsToCollisions);
            }

            public static bool operator ==(BinaryListComparer? left, BinaryListComparer? right)
            {
                if (left is null || right is null)
                    return false;

                if (ReferenceEquals(left, right))
                    return true;
                
                return left.Equals(right);
            }

            public static bool operator !=(BinaryListComparer? left, BinaryListComparer? right)
            {
                return !(left == right);
            }
        }        
    }
}
