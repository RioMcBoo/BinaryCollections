namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue> : ISet<TValue>
    {
        bool ISet<TValue>.Add(TValue item) => AddUnique(in item, out _);

        void ISet<TValue>.ExceptWith(IEnumerable<TValue> other) => ExceptWithUnique(other);

        void ISet<TValue>.IntersectWith(IEnumerable<TValue> other) => IntersectWithUnique(other);

        void ISet<TValue>.SymmetricExceptWith(IEnumerable<TValue> other) => SymmetricExceptWithUnique(other);

        void ISet<TValue>.UnionWith(IEnumerable<TValue> other) => UnionWithUnique(other);

        bool ISet<TValue>.IsProperSubsetOf(IEnumerable<TValue> other) => IsProperSubsetOfUnique(other);

        bool ISet<TValue>.IsProperSupersetOf(IEnumerable<TValue> other) => IsProperSupersetOfUnique(other);

        bool ISet<TValue>.IsSubsetOf(IEnumerable<TValue> other) => IsSubsetOfUnique(other);

        bool ISet<TValue>.IsSupersetOf(IEnumerable<TValue> other) => IsSupersetOfUnique(other);

        bool ISet<TValue>.Overlaps(IEnumerable<TValue> other) => Overlaps(other);

        bool ISet<TValue>.SetEquals(IEnumerable<TValue> other) => SetEqualsUnique(other);
    }
}
