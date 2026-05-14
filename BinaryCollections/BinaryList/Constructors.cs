namespace BinaryCollections.BinaryList
{
    public partial class BinaryList<TValue>
    {
        /// <param name="containCompareCollisions">
        /// Manually sets the collision policy. A "collision" occurs when <c>Compare(A, B) == 0</c> but <c>Equals(A, B) == false</c>.
        /// </param>
        /// <remarks>
        /// <para>
        /// <b>Technical Requirement:</b> For maximum performance in set-, range- and other operations (Union, Intersect, Remove, FindIndexExactly etc.), 
        /// the sorting comparer and the equality comparer MUST be collision-free (i.e., return the same result for equal elements).
        /// </para>
        /// <para>
        /// You can manually set <paramref name="containCompareCollisions"/> to <see langword="false"/> if you guarantee that your <paramref name="comparer"/> 
        /// has no collisions, even if the list's internal heuristics suggest otherwise.
        /// </para>
        /// <para>If left as <see langword="null"/>, the list auto-determines the policy:</para>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///             <b>Default Comparer:</b> <see langword="false"/> (Collision-free) only if <typeparamref name="TValue"/> 
        ///             implements <see cref="IComparable{TValue}"/>.
        ///             It is suggested that the sorting comparer and the equality comparer return the same result for equal elements.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///             <b>Custom Comparer:</b> <see langword="true"/> (Potential collisions), as custom logic 
        ///             often sorts by a subset of fields (e.g., sorting by 'Price' while 'Id' differs).
        ///         </description>
        ///     </item>
        /// </list>
        /// <para>
        /// <warning>
        ///     <b>Warning:</b> Forcing this flag to <see langword="false"/> when collisions actually exist or 
        ///                 <see cref="IComparable{TValue}"/> is implemented but not synchronized with the equality comparer
        ///                 will lead to undefined behavior in set operations, including data loss and incorrect 
        ///                 results in <c>Union</c>, <c>Intersect</c>, <c>Except</c>, <c>GetRange</c>, <c>ExtractRange</c>.
        /// </warning>
        /// </para>
        ///     <para>
        ///         <b>Remember:</b> A collision-free state is guaranteed only if:
        ///         <c>(Compare(a, b) == 0) == Equals(a, b)</c> for all possible elements.
        ///     </para>
        /// </remarks>
        public BinaryList(IEnumerable<TValue> initialValues, IComparer<TValue> comparer = null!, int capacity = DefaultCapacity, bool? containCompareCollisions = default)
        {
            if (initialValues is IReadOnlyCollection<TValue> collection)
            {
                capacity = capacity > collection.Count ? capacity : collection.Count;
            }

            if (initialValues is BinaryListEnumerator<TValue> enumerator)
            {
                _containCompareCollisions = containCompareCollisions ?? (comparer is null ? enumerator.ContainCompareCollisions : true);
                Comparer = comparer is null ? enumerator.Comparer : comparer;
            }
            else
            {
                _containCompareCollisions = containCompareCollisions ?? (comparer is null ? DefaultComparerLeadsToCollisions : true);
                Comparer = comparer ?? Comparer;
            }

            _storage = new TValue[capacity];
            _zeroRIndex = CalculateZeroIndex(capacity, 0);

            AddRange(initialValues);
        }

        /// <inheritdoc cref="BinaryList{TValue}(IEnumerable{TValue}, IComparer{TValue}, int, bool?)"/>
        public BinaryList(ReadOnlySpan<TValue> initialValues, IComparer<TValue> comparer = null!, int capacity = DefaultCapacity, bool? containCompareCollisions = default)
        {
            capacity = capacity > initialValues.Length ? capacity : initialValues.Length;

            _containCompareCollisions = containCompareCollisions ?? (comparer is null ? DefaultComparerLeadsToCollisions : true);
            Comparer = comparer ?? Comparer;

            _storage = new TValue[capacity];
            _zeroRIndex = CalculateZeroIndex(capacity, 0);

            AddRange(initialValues);
        }

        /// <inheritdoc cref="BinaryList{TValue}(IEnumerable{TValue}, IComparer{TValue}, int, bool?)"/>
        public BinaryList(IEnumerable<TValue> initialValues, Comparison<TValue> comparison, int capacity = DefaultCapacity, bool? containCompareCollisions = default)
            : this(initialValues, comparison is null ? null! : new ComparsionComparer(comparison), capacity, containCompareCollisions) { }

        /// <inheritdoc cref="BinaryList{TValue}(IEnumerable{TValue}, IComparer{TValue}, int, bool?)"/>
        public BinaryList(ReadOnlySpan<TValue> initialValues, Comparison<TValue> comparison, int capacity = DefaultCapacity, bool? containCompareCollisions = default)
            : this(initialValues, comparison is null ? null! : new ComparsionComparer(comparison), capacity, containCompareCollisions) { }

        /// <inheritdoc cref="BinaryList{TValue}(IEnumerable{TValue}, IComparer{TValue}, int, bool?)"/>
        public BinaryList(IComparer<TValue> comparer, int capacity = DefaultCapacity, bool? containCompareCollisions = default)
        {
            capacity = capacity > 0 ? (capacity > Array.MaxLength ? Array.MaxLength : capacity) : DefaultCapacity;
            _storage = new TValue[capacity];
            _zeroRIndex = CalculateZeroIndex(capacity, 0);
            Comparer = comparer ?? Comparer;
            _containCompareCollisions = containCompareCollisions ?? (comparer is null ? DefaultComparerLeadsToCollisions : true);
        }

        /// <inheritdoc cref="BinaryList{TValue}(IEnumerable{TValue}, IComparer{TValue}, int, bool?)"/>
        public BinaryList(Comparison<TValue> comparison, int capacity = DefaultCapacity, bool? containCompareCollisions = default)
        : this(comparison is null ? null! : new ComparsionComparer(comparison), capacity, containCompareCollisions) { }

        /// <inheritdoc cref="BinaryList{TValue}(IEnumerable{TValue}, IComparer{TValue}, int, bool?)"/>
        public BinaryList(int capacity = DefaultCapacity, bool? containCompareCollisions = default)
        {
            capacity = capacity > 0 ? (capacity > Array.MaxLength ? Array.MaxLength : capacity) : DefaultCapacity;
            _storage = new TValue[capacity];
            _zeroRIndex = CalculateZeroIndex(capacity, 0);
            _containCompareCollisions = containCompareCollisions ?? DefaultComparerLeadsToCollisions;
        }
        
        /// <inheritdoc cref="BinaryList{TValue}(IEnumerable{TValue}, IComparer{TValue}, int, bool?)"/>
        public BinaryList(bool? containCompareCollisions)
        {
            _storage = new TValue[DefaultCapacity];
            _zeroRIndex = CalculateZeroIndex(DefaultCapacity, 0);
            _containCompareCollisions = containCompareCollisions ?? DefaultComparerLeadsToCollisions;
        }
    }
}
