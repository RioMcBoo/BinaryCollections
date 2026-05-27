namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool FindIndex(ref readonly TValue item, out int vIndex)
        {
            BinarySearchState state = new BinarySearchState { lo = 0, hi = _count - 1, i = default };
            return BinarySearchFor(in item, ref state.lo, ref state.hi, out vIndex);
        }

        public bool FindLeftIndex(ref readonly TValue item, out int index)
        {
            BinarySearchState state = new BinarySearchState { lo = 0, hi = _count - 1, i = default };

            if (!BinarySearchFor(in item, ref state.lo, ref state.hi, out state.i))
            {
                index = state.i;
                return false;
            }

            index = FindLeftBoundIndex(in item, ref state);

            return true;
        }

        public bool FindRightIndex(ref readonly TValue item, out int index)
        {
            BinarySearchState state = new BinarySearchState { lo = 0, hi = _count - 1, i = default };

            if (!BinarySearchFor(in item, ref state.lo, ref state.hi, out state.i))
            {
                index = state.i;
                return false;
            }

            index = FindRightBoundIndex(in item, ref state);

            return true;
        }

        public bool FindIndexExactly(ref readonly TValue item, out int vIndex)
        {
            if (!FindIndex(in item, out vIndex))
                return false;

            // Find identical in right part
            for (int i = vIndex; i < Count; i++)
            {
                var result = Compare(in _storage[ToRealIndex(i)], in item);

                if (result != 0)
                    break;

                if (IsIdentical(ref _storage[ToRealIndex(i)], in item))
                {
                    vIndex = i;
                    return true;
                }
            }

            // Find identical in left part
            for (int i = vIndex - 1; i >= 0; i--)
            {
                var result = Compare(in _storage[ToRealIndex(i)], in item);

                if (result != 0)
                    break;

                if (IsIdentical(ref _storage[ToRealIndex(i)], in item))
                {
                    vIndex = i;
                    return true;
                }
            }

            vIndex = ~vIndex;
            return false;
        }

        public (int left, int right) FindBoundaries(ref readonly TValue item)
        {
            (int left, int right) result = default;

            // Try find any index of the item
            BinarySearchState state = new BinarySearchState { lo = 0, hi = _count - 1, i = default };
            if (!BinarySearchFor(in item, ref state.lo, ref state.hi, out state.i))
                return (state.i, state.i);

            // Save state for found point
            BinarySearchState foundPointState = state;

            result.left = FindLeftBoundIndex(in item, ref state);

            // Restore state of found point
            state = foundPointState;

            // Find right boundary
            result.right = FindRightBoundIndex(in item, ref state);

            return result;
        }

        private int FindLeftBoundIndex(ref readonly TValue item, ref BinarySearchState state)
        {
            int index = 0;

            do { index = state.i; state.hi = state.i - 1; }
            while (BinarySearchFor(in item, ref state.lo, ref state.hi, out state.i));

            return index;
        }

        private int FindRightBoundIndex(ref readonly TValue item, ref BinarySearchState state)
        {
            int index = 0;

            do { index = state.i; state.lo = state.i + 1; }
            while (BinarySearchFor(in item, ref state.lo, ref state.hi, out state.i));

            return index;
        }

        private bool BinarySearchFor(ref readonly TValue item, ref int lo, ref int hi, out int i)
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

                int compareResult = Compare(in _storage[ToRealIndex(i)], in item);

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

        private int Compare(ref readonly TValue current, ref readonly TValue incoming) => _comparer.Compare(current, incoming);
    }
}
