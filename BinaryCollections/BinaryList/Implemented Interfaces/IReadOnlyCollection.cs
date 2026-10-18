namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue> : IReadOnlyCollection<TValue>
    {
        int IReadOnlyCollection<TValue>.Count => _count;
    }
}
