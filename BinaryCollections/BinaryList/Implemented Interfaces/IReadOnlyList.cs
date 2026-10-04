namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue> : IReadOnlyList<TValue>
    {
        TValue IReadOnlyList<TValue>.this[int index] => GetItemRef(index);
    }
}
