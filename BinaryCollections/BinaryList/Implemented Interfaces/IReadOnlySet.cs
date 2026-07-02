namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue> : IReadOnlySet<TValue>
    {
        bool IReadOnlySet<TValue>.Contains(TValue item) => Contains(in item, out _);

        bool IReadOnlySet<TValue>.IsProperSubsetOf(IEnumerable<TValue> other) => IsProperSubsetOfUnique(other);

        bool IReadOnlySet<TValue>.IsProperSupersetOf(IEnumerable<TValue> other) => IsProperSupersetOfUnique(other);

        bool IReadOnlySet<TValue>.IsSubsetOf(IEnumerable<TValue> other) => IsSubsetOfUnique(other);

        bool IReadOnlySet<TValue>.IsSupersetOf(IEnumerable<TValue> other) => IsSupersetOfUnique(other);

        bool IReadOnlySet<TValue>.Overlaps(IEnumerable<TValue> other) => Overlaps(other);

        bool IReadOnlySet<TValue>.SetEquals(IEnumerable<TValue> other) => SetEqualsUnique(other);
    }
}
