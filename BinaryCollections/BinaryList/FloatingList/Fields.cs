namespace BinaryCollections.BinaryList
{
    public partial class FloatingList<TValue>
    {
        protected TValue[] _storage;
        protected int _zeroRIndex;
        protected int _count;
        protected int _version;
    }
}
