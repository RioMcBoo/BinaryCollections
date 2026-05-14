namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue> : IReadOnlySet<TValue>
    {
        bool IReadOnlySet<TValue>.Contains(TValue item) => Contains(in item, out _);

        bool IReadOnlySet<TValue>.IsProperSubsetOf(IEnumerable<TValue> other)
        {
            throw new NotImplementedException();
        }

        bool IReadOnlySet<TValue>.IsProperSupersetOf(IEnumerable<TValue> other)
        {
            throw new NotImplementedException();
        }

        bool IReadOnlySet<TValue>.IsSubsetOf(IEnumerable<TValue> other)
        {
            throw new NotImplementedException();
        }

        bool IReadOnlySet<TValue>.IsSupersetOf(IEnumerable<TValue> other)
        {
            throw new NotImplementedException();
        }

        bool IReadOnlySet<TValue>.Overlaps(IEnumerable<TValue> other)
        {
            throw new NotImplementedException();
        }

        bool IReadOnlySet<TValue>.SetEquals(IEnumerable<TValue> other)
        {
            throw new NotImplementedException();
        }
    }
}
