namespace BinaryCollections.BinaryList
{
    public partial class BinaryList<TValue>
    {
        private TValue[] _storage;
        private int _zeroRIndex;
        private int _count;
        private int _version;
        private readonly bool _mayContainCollisions;
    }
}
