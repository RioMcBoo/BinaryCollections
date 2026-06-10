namespace BinaryCollections.BinaryList
{
    [Flags]
    public enum BinaryListFlags
    {
        None = 0x0,

        /// <summary>The range contains only duplicate elements (by the equality comparer).</summary>
        UniformRange = 0x1,

        /// <summary>The range may contain collisions (equality and sort comparers are not synchronized).</summary>
        NoCollisions = 0x2,

        /// <summary>The range may contain duplicates (by the equality comparer).</summary>
        NoDuplicates = 0x4,
    }

    public partial class BinaryList<TValue>
    {
        private BinaryListFlags _flags;
        private BinaryListComparer _comparer;
    }
}