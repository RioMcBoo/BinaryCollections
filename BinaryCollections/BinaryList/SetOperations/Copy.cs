namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public void Copy(int fromIndex, TValue[] toArray, int toArrayIndex, int length)
        {
            if (_count - fromIndex < length)
            {
                throw new ArgumentException("Not enough elements in the source array");
            }

            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_storage, ToRealIndex(fromIndex), toArray, toArrayIndex, length);
        }

        public void CopyTo(TValue[] array, int arrayIndex = 0)
        {
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_storage, _zeroRIndex, array, arrayIndex, _count);
        }
    }
}
