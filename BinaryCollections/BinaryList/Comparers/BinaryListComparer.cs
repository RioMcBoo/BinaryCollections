
namespace BinaryCollections.BinaryList
{
    public partial class BinaryList<TValue>
    {
        public class BinaryListComparer : IComparer<TValue>
        {
            private readonly bool _leadsToCollisions;
            private readonly IComparer<TValue> _comparer;

            public bool LeadsToCollisions => _leadsToCollisions && DefaultComparerLeadsToCollisions;

            public BinaryListComparer(ref BinaryListFlags flags, IComparer<TValue> comparer = null!, bool? comparerLeadsToCollisions = null)
            {
                if (comparer is BinaryListComparer binaryListComparer)
                {
                    _comparer = binaryListComparer._comparer;
                    _leadsToCollisions = binaryListComparer._leadsToCollisions;

                    if (comparerLeadsToCollisions.HasValue && comparerLeadsToCollisions.Value == true)
                    {
                        // We only allow the stricter rule
                        _leadsToCollisions = true;
                    }
                }
                else if (ReferenceEquals(comparer, _defaultComparer) || comparer == null)
                {
                    _comparer = comparer ?? _defaultComparer;
                    _leadsToCollisions = DefaultComparerLeadsToCollisions;

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

                flags = LeadsToCollisions ? flags | BinaryListFlags.MayContainCollisions : flags & ~BinaryListFlags.MayContainCollisions;
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

                if (_leadsToCollisions && !DefaultComparerLeadsToCollisions)
                {
                    compareResult = _defaultComparer.Compare(x, y);
                }

                return compareResult;
            }

            public override bool Equals(object? obj)
            {
                if (obj is BinaryListComparer other)
                {
                    return (this._comparer == other._comparer) && (this._leadsToCollisions == other._leadsToCollisions);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_comparer, _leadsToCollisions);
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
