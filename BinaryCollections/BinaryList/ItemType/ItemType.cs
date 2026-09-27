using System.Runtime.CompilerServices;

namespace BinaryCollections.BinaryList
{
    internal static class ItemType<TValue>
    {
        internal static readonly bool ContainsReferences =
            RuntimeHelpers.IsReferenceOrContainsReferences<TValue>();

        internal static readonly bool IsComparable =
            typeof(IComparable<TValue>).IsAssignableFrom(typeof(TValue)) ||
            typeof(IComparable).IsAssignableFrom(typeof(TValue));

        internal static readonly bool IsEquatable =
            typeof(IEquatable<TValue>).IsAssignableFrom(typeof(TValue));
    }
}
