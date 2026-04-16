using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CustomCollections.BinaryList
{
    public partial class BinaryList<TValue> : IReadOnlyList<TValue>, IReadOnlyCollection<TValue>, ICollection<TValue>
    {
        private class ValueTypeEqualityComparer : IEqualityComparer<TValue>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool Equals(TValue? x, TValue? y)
            {
                return new ReadOnlySpan<byte>(Unsafe.AsPointer(ref Unsafe.AsRef(in x)), Unsafe.SizeOf<TValue>())
                    .SequenceEqual(new ReadOnlySpan<byte>(Unsafe.AsPointer(ref Unsafe.AsRef(in y)), Unsafe.SizeOf<TValue>()));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe int GetHashCode([DisallowNull] TValue obj)
            {
                var bytes = new ReadOnlySpan<byte>(Unsafe.AsPointer(ref Unsafe.AsRef(in obj)), Unsafe.SizeOf<TValue>());
                HashCode hc = new HashCode();
                hc.AddBytes(bytes);
                return hc.ToHashCode();
            }
        }

        private class ReferenceTypeEqualityComparer: IEqualityComparer<TValue>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(TValue? x, TValue? y)
            {
                return ReferenceEquals(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode([DisallowNull] TValue obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
