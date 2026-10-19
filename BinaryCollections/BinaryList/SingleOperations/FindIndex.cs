using System.Runtime.CompilerServices;

namespace BinaryCollections.BinaryList
{
    public sealed partial class BinaryListComparer<TValue>
    {
        public ref struct Boundaries
        {
            public int Left;
            public int Right;

            public Boundaries(int left, int right)
            {
                Left = left;
                Right = right;
            }
        }

        public bool FindIndex(ref readonly TValue item, ReadOnlySpan<TValue> span, out int vIndex)
        {
            int lo = 0;
            int hi = span.Length - 1;

            return BinarySearchFor(in item, span, ref lo, ref hi, out vIndex);
        }

        public bool FindLeftBound(ref readonly TValue item, ReadOnlySpan<TValue> span, out int index)
        {
            int lo = 0;
            int hi = span.Length - 1;

            if (!BinarySearchFor(in item, span, ref lo, ref hi, out index))
            {
                return false;
            }

            index = FindLeftBound(in item, span, ref lo, ref hi, ref index);

            return true;
        }

        public bool FindRightBound(ref readonly TValue item, ReadOnlySpan<TValue> span, out int index)
        {
            int lo = 0;
            int hi = span.Length - 1;

            if (!BinarySearchFor(in item, span, ref lo, ref hi, out index))
            {
                return false;
            }

            index = FindRightBound(in item, span, ref lo, ref hi, ref index);

            return true;
        }

        public bool FindIndexExactly(ref readonly TValue item, ReadOnlySpan<TValue> span, out int vIndex)
        {
            if (!FindIndex(in item, span, out vIndex))
                return false;

            // Find identical in right part
            for (int i = vIndex; i < span.Length; i++)
            {
                var result = Compare(span[i], item);

                if (result != 0)
                    break;

                if (ItemType<TValue>.IsIdentical(in span[i], in item))
                {
                    vIndex = i;
                    return true;
                }
            }

            // Find identical in left part
            for (int i = vIndex - 1; i >= 0; i--)
            {
                var result = Compare(span[i], item);

                if (result != 0)
                    break;

                if (ItemType<TValue>.IsIdentical(in span[i], in item))
                {
                    vIndex = i;
                    return true;
                }
            }

            vIndex = ~vIndex;
            return false;
        }

        public Boundaries FindBoundaries(ref readonly TValue item, ReadOnlySpan<TValue> span)
        {
            // Try find any index of the item
            int lo = 0;
            int hi = span.Length - 1;
            if (!BinarySearchFor(in item, span, ref lo, ref hi, out int i))
                return new(i, i);

            // Save state for found point
            int savedLo = lo;
            int savedHi = hi;
            int savedI = i;

            int leftBound = FindLeftBound(in item, span, ref lo, ref hi, ref i);

            // Restore state of found point
            lo = savedLo;
            hi = savedHi;
            i = savedI;

            // Find right boundary
            int rightBound = FindRightBound(in item, span, ref lo, ref hi, ref i);

            return new(leftBound, rightBound);
        }

        private int FindLeftBound(ref readonly TValue item, ReadOnlySpan<TValue> span, ref int lo, ref int hi, ref int i)
        {
            int index;

            do { index = i; hi = i - 1; }
            while (BinarySearchFor(in item, span, ref lo, ref hi, out i));

            return index;
        }

        private int FindRightBound(ref readonly TValue item, ReadOnlySpan<TValue> span, ref int lo, ref int hi, ref int i)
        {
            int index;

            do { index = i; lo = i + 1; }
            while (BinarySearchFor(in item, span, ref lo, ref hi, out i));

            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool BinarySearchFor(ref readonly TValue item, ReadOnlySpan<TValue> span, ref int lo, ref int hi, out int i)
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

                int compareResult = Compare(span[i], item);

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
    }
}
