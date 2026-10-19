using System.Runtime.CompilerServices;

namespace BinaryCollections.BinaryList
{
    internal static class ItemType<TValue>
    {
        internal static readonly EqualityComparer<TValue> EqualityComparer = null!;
        internal static readonly Comparer<TValue> DefaultComparer = null!;

        internal static readonly bool ContainsReferences =
            RuntimeHelpers.IsReferenceOrContainsReferences<TValue>();

        internal static readonly bool IsReference = !typeof(TValue).IsValueType;

        static ItemType()
        {
            Type type = typeof(TValue);

            bool hasCustomEquals = type.GetMethod("Equals", [type])?.DeclaringType != typeof(object);
            bool hasCustomGetHashCode = type.GetMethod("GetHashCode", Type.EmptyTypes)?.DeclaringType != typeof(object);

            bool IsComparable =
                typeof(IComparable<TValue>).IsAssignableFrom(type) ||
                typeof(IComparable).IsAssignableFrom(type);

            bool IsEquatable =
                typeof(IEquatable<TValue>).IsAssignableFrom(type) ||
                (hasCustomEquals && hasCustomGetHashCode) ||
                (!hasCustomEquals && !hasCustomGetHashCode && (IsReference || !ContainsReferences));

            if (IsComparable)
            {
                DefaultComparer = Comparer<TValue>.Default;
            }

            if (IsEquatable)
            {
                EqualityComparer = EqualityComparer<TValue>.Default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsIdentical(ref readonly TValue currentItem, ref readonly TValue item)
        {
            return EqualityComparer.Equals(currentItem, item);
        }
    }
}
