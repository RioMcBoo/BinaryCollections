namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue> : ISet<TValue>
    {
        bool ISet<TValue>.Add(TValue item) => Add(in item, out _);

        void ISet<TValue>.ExceptWith(IEnumerable<TValue> other)
        {
            throw new NotImplementedException();
        }

        void ISet<TValue>.IntersectWith(IEnumerable<TValue> other) => IntersectWithUnique(other);

        void ISet<TValue>.SymmetricExceptWith(IEnumerable<TValue> other) => SymmetricExceptWithUnique(other);

        void ISet<TValue>.UnionWith(IEnumerable<TValue> other) => UnionWithUnique(other);

        bool ISet<TValue>.IsProperSubsetOf(IEnumerable<TValue> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<TValue>.IsProperSupersetOf(IEnumerable<TValue> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<TValue>.IsSubsetOf(IEnumerable<TValue> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<TValue>.IsSupersetOf(IEnumerable<TValue> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<TValue>.Overlaps(IEnumerable<TValue> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<TValue>.SetEquals(IEnumerable<TValue> other)
        {
            throw new NotImplementedException();
        }
    }
}
