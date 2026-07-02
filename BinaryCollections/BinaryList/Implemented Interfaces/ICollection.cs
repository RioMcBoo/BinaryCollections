namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue> : ICollection<TValue>
    {
        bool ICollection<TValue>.IsReadOnly => false;

        void ICollection<TValue>.Add(TValue item) => Add(in item, out _);

        void ICollection<TValue>.Clear() => Clear();

        bool ICollection<TValue>.Contains(TValue item) => Contains(in item, out _);

        void ICollection<TValue>.CopyTo(TValue[] array, int arrayIndex) => CopyTo(array, arrayIndex);

        bool ICollection<TValue>.Remove(TValue item) => Remove(in item, out _);
    }
}
