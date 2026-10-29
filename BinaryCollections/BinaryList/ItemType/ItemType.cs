using System.Runtime.CompilerServices;

namespace BinaryCollections.BinaryList
{
    internal static class ItemType<TValue>
    {
        internal static readonly Comparer<TValue> DefaultComparer = null!;

        internal static readonly bool ContainsReferences =
            RuntimeHelpers.IsReferenceOrContainsReferences<TValue>();

        static ItemType()
        {
            Type type = typeof(TValue);

            bool IsComparable(Type type)
            {
                return typeof(IComparable<TValue>).IsAssignableFrom(type) ||
                       typeof(IComparable).IsAssignableFrom(type) ||
                       (Nullable.GetUnderlyingType(type) is { } u &&
                           (typeof(IComparable<>).MakeGenericType(u).IsAssignableFrom(u) ||
                            typeof(IComparable).IsAssignableFrom(u)));
            }

            if (IsComparable(type))
            {
                if (typeof(TValue) == typeof(string))
                {
                    // The default comparer if "string" violates the `IComparable` contract.
                    DefaultComparer = (Comparer<TValue>)(object)new OrdinalStringComparer();
                }
                else
                {
                    DefaultComparer = Comparer<TValue>.Default;
                }
            }
        }
    }
}
