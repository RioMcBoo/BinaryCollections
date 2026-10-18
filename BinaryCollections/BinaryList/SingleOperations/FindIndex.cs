using System.Runtime.CompilerServices;
using ExceptionHelper;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        private static bool FindIndex(ref readonly TValue item, ReadOnlySpan<TValue> span, BinaryListComparer comparer, out int vIndex)
        {
            int lo = 0;
            int hi = span.Length - 1;

            return BinarySearchFor(in item, span, comparer, ref lo, ref hi, out vIndex);
        }

        private static bool FindLeftIndex(ref readonly TValue item, ReadOnlySpan<TValue> span, BinaryListComparer comparer, out int index)
        {
            int lo = 0;
            int hi = span.Length - 1;

            if (!BinarySearchFor(in item, span, comparer, ref lo, ref hi, out index))
            {
                return false;
            }

            index = FindLeftBoundIndex(in item, span, comparer, ref lo, ref hi, ref index);

            return true;
        }

        private static bool FindRightIndex(ref readonly TValue item, ReadOnlySpan<TValue> span, BinaryListComparer comparer, out int index)
        {
            int lo = 0;
            int hi = span.Length - 1;

            if (!BinarySearchFor(in item, span, comparer, ref lo, ref hi, out index))
            {
                return false;
            }

            index = FindRightBoundIndex(in item, span, comparer, ref lo, ref hi, ref index);

            return true;
        }

        private static bool FindIndexExactly(ref readonly TValue item, ReadOnlySpan<TValue> span, BinaryListComparer comparer, out int vIndex)
        {
            if (!FindIndex(in item, span, comparer, out vIndex))
                return false;

            // Find identical in right part
            for (int i = vIndex; i < span.Length; i++)
            {
                var result = comparer.Compare(span[i], item);

                if (result != 0)
                    break;

                if (IsIdentical(in span[i], in item))
                {
                    vIndex = i;
                    return true;
                }
            }

            // Find identical in left part
            for (int i = vIndex - 1; i >= 0; i--)
            {
                var result = comparer.Compare(span[i], item);

                if (result != 0)
                    break;

                if (IsIdentical(in span[i], in item))
                {
                    vIndex = i;
                    return true;
                }
            }

            vIndex = ~vIndex;
            return false;
        }

        public static Boundaries FindBoundaries(ref readonly TValue item, ReadOnlySpan<TValue> span, BinaryListComparer comparer)
        {
            // Try find any index of the item
            int lo = 0;
            int hi = span.Length - 1;
            if (!BinarySearchFor(in item, span, comparer, ref lo, ref hi, out int i))
                return new(i, i);

            // Save state for found point
            int savedLo = lo;
            int savedHi = hi;
            int savedI = i;

            int leftBound = FindLeftBoundIndex(in item, span, comparer, ref lo, ref hi, ref i);

            // Restore state of found point
            lo = savedLo;
            hi = savedHi;
            i = savedI;

            // Find right boundary
            int rightBound = FindRightBoundIndex(in item, span, comparer, ref lo, ref hi, ref i);

            return new(leftBound, rightBound);
        }

        private static int FindLeftBoundIndex(ref readonly TValue item, ReadOnlySpan<TValue> span, BinaryListComparer comparer, ref int lo, ref int hi, ref int i)
        {
            int index = 0;

            do { index = i; hi = i - 1; }
            while (BinarySearchFor(in item, span, comparer, ref lo, ref hi, out i));

            return index;
        }

        private static int FindRightBoundIndex(ref readonly TValue item, ReadOnlySpan<TValue> span, BinaryListComparer comparer, ref int lo, ref int hi, ref int i)
        {
            int index = 0;

            do { index = i; lo = i + 1; }
            while (BinarySearchFor(in item, span, comparer, ref lo, ref hi, out i));

            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool BinarySearchFor(ref readonly TValue item, ReadOnlySpan<TValue> span, BinaryListComparer comparer, ref int lo, ref int hi, out int i)
        {
            while (lo <= hi)
            {
                //// PERF: `lo` or `hi` will never be negative inside the loop,
                //       so computing median using uints is safe since we know
                //       `length <= int.MaxValue`, and indices are >= 0
                //       and thus cannot overflow an uint.
                //       Saves one subtraction per loop compared to

                //  1 -->   int i = lo + (hi - lo) / 2;
                //  2 -->   int i = (int)(((uint)hi + (uint)lo) >> 1);
                //  3 -->   int i = (hi + lo) >>> 1;

                i = unchecked((hi + lo) >>> 1);

                int compareResult = comparer.Compare(span[i], item);

                if (compareResult == 0)
                {
                    return true;
                }
                else if (compareResult > 0)
                {
                    hi = i - 1;
                }
                else
                {
                    lo = i + 1;
                }
            }

            i = ~lo;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFoundIndexOutOfRange(int index, int count)
        {
            if (index > 0)
                return index >= count;

            if (index < 0)
                return ~index == 0 || ~index == count;

            Throw.ArgumentException(nameof(IsFoundIndexOutOfRange), "index is wrong");
            return false;
        }

        /// <returns>
        ///     <para>A value that indicates the relative order of the objects being compared. The return value has these meanings:</para>
        ///     <list type="table">
        ///         <item>
        ///             <term>Less than zero</term>
        ///             <description><paramref name="current" /> is smaller than <paramref name="incoming" />.</description>
        ///         </item>
        ///         <item>
        ///             <term>Zero</term>
        ///             <description><paramref name="current" /> equal to <paramref name="incoming" />.</description>
        ///         </item>
        ///         <item>
        ///             <term>Greater than zero</term>
        ///             <description><paramref name="current" /> is greater than <paramref name="incoming" />.</description>
        ///         </item>
        ///     </list>
        /// </returns>
        private int Compare(ref readonly TValue current, ref readonly TValue incoming) => _comparer.Compare(current, incoming);
    }
}
