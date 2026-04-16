namespace CustomCollections.BinaryList
{
    public partial class BinaryList<TValue> : IReadOnlyList<TValue>, IReadOnlyCollection<TValue>, ICollection<TValue>
    {
        private bool FindIndexExtended(ref TValue item, out int vIndex)
        {
            if (_count == 0) { vIndex = ~_count; return false; }

            int startIndex = 0;
            int endIndex = _count - 1;
            int currentIndex, lo, hi;

            // Try find item in the very low edge
            currentIndex = lo = hi = startIndex++;
            if (FindIndex(ref item, ref lo, ref hi, out vIndex))
                return true;

            if (vIndex == ~currentIndex)
                return false;

            // Try find item in the very high edge
            currentIndex = lo = hi = endIndex--;
            if (FindIndex(ref item, ref lo, ref hi, out vIndex))
                return true;

            if (vIndex == ~(currentIndex + 1))
                return false;

            // Try find item in the remains
            return FindIndex(ref item, ref startIndex, ref endIndex, out vIndex);
        }

        private bool FindIndexExtendedUnique(ref TValue item, out int vIndex)
        {
            if (_count == 0) { vIndex = ~_count; return false; }

            int startIndex = 0;
            int endIndex = _count - 1;
            int currentIndex, lo, hi;

            // Try find item in the very low edge
            currentIndex = lo = hi = startIndex++;
            if (FindIndex(ref item, ref lo, ref hi, out vIndex))
            {
                if (IsIdentical(ref _storage[ToRealIndex(vIndex)], ref item))
                    return true;

                vIndex = ~vIndex;
                return false;
            }

            if (vIndex == ~currentIndex)
                return false;

            // Try find item in the very high edge
            currentIndex = lo = hi = endIndex--;
            if (FindIndex(ref item, ref lo, ref hi, out vIndex))
            {
                if (IsIdentical(ref _storage[ToRealIndex(vIndex)], ref item))
                    return true;

                vIndex = ~vIndex;
                return false;
            }

            if (vIndex == ~(currentIndex + 1))
                return false;

            // Try find item in the remains
            return FindIndexUnique(ref item, ref startIndex, ref endIndex, out vIndex);
        }
    }
}
