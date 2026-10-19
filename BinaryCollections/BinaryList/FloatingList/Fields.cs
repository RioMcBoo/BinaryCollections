namespace BinaryCollections.BinaryList
{
    /// <summary>The range may contain duplicates (from the perspective of the <see cref="BinaryListComparer{TValue}.Compare(TValue?, TValue?)"/>). <br/> <br/>
    /// If <see cref="BinaryListComparer{TValue}.LeadsToCollisions"/>, they are also considered duplicates. <br/> <br/>
    /// Collisions are a sort of duplicates, but duplicates do not necessarily involve collisions. <br/> <br/>
    /// Duplicates may simply be ordinary copies.</summary>
    [Flags]
    public enum DuplicateDensity
    {
        None = 0x0,

        /// <summary>The range may contain duplicates (from the perspective of the <see cref="BinaryListComparer{TValue}.Compare(TValue?, TValue?)"/>).</summary>
        Possible = 0x1,

        /// <summary>The range contains only duplicates (from the perspective of the <see cref="BinaryListComparer{TValue}.Compare(TValue?, TValue?)"/>).</summary>
        Full = 0x2,
        
    }

    public partial class FloatingList<TValue>
    {
        protected TValue[] _storage;
        protected int _zeroRIndex;
        protected int _count;
        protected int _version;
        protected DuplicateDensity _flags;
    }
}
