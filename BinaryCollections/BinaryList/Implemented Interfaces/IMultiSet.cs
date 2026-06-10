namespace System.Collections.Generic
{
    /// <summary>
    /// Generic collection that guarantees the uniqueness of its elements, as defined
    /// by some comparer. It also supports basic set operations such as Union, Intersection,
    /// Complement and Exclusive Complement.
    /// </summary>
    public interface IMultiSet<T> : ICollection<T>
    {
        //Add ITEM to the set, return true if added, false if duplicate
        new bool Add(T item);

        //Transform this set into its union with the IEnumerable<T> other
        void UnionWith(IEnumerable<T> other);

        //Transform this set into its intersection with the IEnumerable<T> other
        void IntersectWith(IEnumerable<T> other);

        //Transform this set so it contains no elements that are also in other
        void ExceptWith(IEnumerable<T> other);

        //Transform this set so it contains elements initially in this or in other, but not both
        void SymmetricExceptWith(IEnumerable<T> other);

        //Check if this set is a subset of other
        bool IsSubsetOf(IEnumerable<T> other);

        //Check if this set is a superset of other
        bool IsSupersetOf(IEnumerable<T> other);

        //Check if this set is a subset of other, but not the same as it
        bool IsProperSupersetOf(IEnumerable<T> other);

        //Check if this set is a superset of other, but not the same as it
        bool IsProperSubsetOf(IEnumerable<T> other);

        //Check if this set has any elements in common with other
        bool Overlaps(IEnumerable<T> other);

        //Check if this set contains the same and only the same elements as other
        bool SetEquals(IEnumerable<T> other);
    }
}

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue> : IMultiSet<TValue>
    {
        bool IMultiSet<TValue>.Add(TValue item) => Add(in item, out _);

        void IMultiSet<TValue>.ExceptWith(IEnumerable<TValue> other) => ExceptWith(other);

        void IMultiSet<TValue>.IntersectWith(IEnumerable<TValue> other) => IntersectWith(other);

        void IMultiSet<TValue>.SymmetricExceptWith(IEnumerable<TValue> other) => SymmetricExceptWith(other);

        void IMultiSet<TValue>.UnionWith(IEnumerable<TValue> other) => UnionWith(other);

        bool IMultiSet<TValue>.IsProperSubsetOf(IEnumerable<TValue> other) => IsProperSubsetOf(other);

        bool IMultiSet<TValue>.IsProperSupersetOf(IEnumerable<TValue> other) => IsProperSupersetOf(other);

        bool IMultiSet<TValue>.IsSubsetOf(IEnumerable<TValue> other) => IsSubsetOf(other);

        bool IMultiSet<TValue>.IsSupersetOf(IEnumerable<TValue> other) => IsSupersetOf(other);

        bool IMultiSet<TValue>.Overlaps(IEnumerable<TValue> other) => Overlaps(other);

        bool IMultiSet<TValue>.SetEquals(IEnumerable<TValue> other) => SetEquals(other);
    }
}
