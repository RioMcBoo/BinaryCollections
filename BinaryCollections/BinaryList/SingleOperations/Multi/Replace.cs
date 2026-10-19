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
            _comparer.FindIndexExactly(in oldItem, Values, out var oldVIndex);
            return ReplaceCore(oldVIndex, out newVIndex, in newItem, unique);
        }

        private bool ReplaceCore(int oldVIndex, out int newVIndex, ref readonly TValue newItem, bool unique)
        {
            if (_comparer.FindIndexExactly(in newItem, Values, out newVIndex) && unique)
                return false;

            if (!CheckIndex(oldVIndex))
                return false;

            if (ItemType<TValue>.IsIdentical(in _storage[ToRealIndex(oldVIndex)], in newItem))
                return false;

            ReplaceAtIndex(oldVIndex, newVIndex, in newItem);
            return true;
        }        
    }
}
