namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool Replace(TValue oldItem, TValue newItem)
        {
            return Replace(in oldItem, in newItem, out _);
        }

        public bool Replace(int index, TValue newItem)
        {
            return Replace(index, in newItem, out _);
        }

        public bool Replace(ref readonly TValue oldItem, ref readonly TValue newItem, out int newIndex)
        {
            return ReplaceCore(in oldItem, out newIndex, in newItem, false);
        }

        public bool Replace(int index, ref readonly TValue newItem, out int newIndex)
        {
            return ReplaceCore(index, out newIndex, in newItem, false);
        }

        private bool ReplaceCore(ref readonly TValue oldItem, out int newVIndex, ref readonly TValue newItem, bool unique)
        {
            FindIndexExactly(in oldItem, Values, Comparer, out var oldVIndex);
            return ReplaceCore(oldVIndex, out newVIndex, in newItem, unique);
        }

        private bool ReplaceCore(int oldVIndex, out int newVIndex, ref readonly TValue newItem, bool unique)
        {
            if (FindIndexExactly(in newItem, Values, Comparer, out newVIndex) && unique)
                return false;

            if (!CheckIndex(oldVIndex))
                return false;

            if (IsIdentical(in _storage[ToRealIndex(oldVIndex)], in newItem))
                return false;

            ReplaceAtIndex(oldVIndex, newVIndex, in newItem);
            return true;
        }        
    }
}
