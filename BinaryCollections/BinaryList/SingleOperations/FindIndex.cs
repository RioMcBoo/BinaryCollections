using System.Runtime.CompilerServices;

namespace BinaryCollections.BinaryList
{
    public sealed partial class BinaryListComparer<TValue>
    {
        public struct Boundaries
        {
            public int Left;
            public int Right;
            public int Length => Right - Left;

            public Boundaries(int left, int right)
            {
                Left = left;
                Right = right;
            }
        }

        public bool FindIndex(ref readonly TValue item, ReadOnlySpan<TValue> span, out int vIndex, bool byOrder = false)
        {
            int lo = 0;
            int hi = span.Length - 1;

            return BinarySearchFor(in item, span, ref lo, ref hi, out vIndex, byOrder);
        }

        public bool FindLeftBound(ref readonly TValue item, ReadOnlySpan<TValue> span, out int index, bool byOrder = false)
        {
            int lo = 0;
            int hi = span.Length - 1;

            if (!BinarySearchFor(in item, span, ref lo, ref hi, out index, byOrder))
            {
                return false;
            }

            index = FindLeftBound(in item, span, ref lo, ref hi, ref index, byOrder);

            return true;
        }

        public bool FindRightBound(ref readonly TValue item, ReadOnlySpan<TValue> span, out int index, bool byOrder = false)
        {
            int lo = 0;
            int hi = span.Length - 1;

            if (!BinarySearchFor(in item, span, ref lo, ref hi, out index, byOrder))
            {
                return false;
            }

            index = FindRightBound(in item, span, ref lo, ref hi, ref index, byOrder);

            return true;
        }

        public bool FindBoundaries(ref readonly TValue item, ReadOnlySpan<TValue> span, out Boundaries boundaries, bool byOrder = false)
        {
            // Try find any index of the item
            int lo = 0;
            int hi = span.Length - 1;
            if (!BinarySearchFor(in item, span, ref lo, ref hi, out int i, byOrder))
            {
                int insertion = ~i;
                boundaries = new(insertion, insertion);
                return false;
            }

            // Save state for found point
            int savedLo = lo;
            int savedHi = hi;
            int savedI = i;

            int leftBound = FindLeftBound(in item, span, ref lo, ref hi, ref i, byOrder);

            // Restore state of found point
            lo = savedLo;
            hi = savedHi;
            i = savedI;

            // Find right boundary
            int rightBound = FindRightBound(in item, span, ref lo, ref hi, ref i, byOrder);

            boundaries = new(leftBound, rightBound + 1);
            return true;
        }

        private int FindLeftBound(ref readonly TValue item, ReadOnlySpan<TValue> span, ref int lo, ref int hi, ref int i, bool byOrder)
        {
            int index;

            do { index = i; hi = i - 1; }
            while (BinarySearchFor(in item, span, ref lo, ref hi, out i, byOrder));

            return index;
        }

        private int FindRightBound(ref readonly TValue item, ReadOnlySpan<TValue> span, ref int lo, ref int hi, ref int i, bool byOrder)
        {
            int index;

            do { index = i; lo = i + 1; }
            while (BinarySearchFor(in item, span, ref lo, ref hi, out i, byOrder));

            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool BinarySearchFor(ref readonly TValue item, ReadOnlySpan<TValue> span, ref int lo, ref int hi, out int i, bool byOrder)
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

                int compareResult = byOrder ? CompareByOrder(span[i], item) : Compare(span[i], item);

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
