namespace BinaryCollections.BinaryList
{
    public partial class BinaryList<TValue>
    {
        private class ValueTypeHashCodeComparer : IComparer<TValue>
        {
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
                if (BinaryList<TValue>._equalityComparer.Equals(x, y)) return 0;
                return BinaryList<TValue>._equalityComparer.GetHashCode(x).CompareTo(BinaryList<TValue>._equalityComparer.GetHashCode(y));
            }
        }

        private class ReferenceTypeHashCodeComparer : IComparer<TValue>
        {
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
            /// 
            public int Compare(TValue? x, TValue? y)
            {
                if (BinaryList<TValue>._equalityComparer.Equals(x, y)) return 0;
                if (x is null) return -1;
                if (y is null) return 1;

                return BinaryList<TValue>._equalityComparer.GetHashCode(x).CompareTo(BinaryList<TValue>._equalityComparer.GetHashCode(y));
            }
        }
    }
}
