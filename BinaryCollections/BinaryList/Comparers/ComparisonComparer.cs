namespace BinaryCollections.BinaryList
{
    public sealed class ComparisonComparer<TValue> : IComparer<TValue>
    {
        private readonly Comparison<TValue> _comparison;
        public ComparisonComparer(Comparison<TValue> comparison)
        {
            _comparison = comparison;
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
        public int Compare(TValue? x, TValue? y) { return _comparison(x, y); }

        public static implicit operator ComparisonComparer<TValue>(Comparison<TValue> comparison)
            => comparison is null ? null! : new(comparison);
    }
}
