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

        private bool Replace(int oldVIndex, ref readonly TValue newItem, out int newVIndex)
        {
            return ReplaceCore(oldVIndex, in newItem, false, out newVIndex);
        }

        public bool Replace(ref readonly TValue oldItem, ref readonly TValue newItem, out int newVIndex)
        {
            return ReplaceCore(in oldItem, in newItem, false, out newVIndex);
        }

        private bool ReplaceCore(int oldVIndex, ref readonly TValue newItem, bool unique, out int newVIndex)
        {
            if (_comparer.FindIndex(in newItem, Values, out newVIndex) && unique)
                return false;

            if (!CheckIndex(oldVIndex))
                return false;

            if (_comparer.Compare(_storage[ToRealIndex(oldVIndex)], newItem) == 0)
                return false;

            ReplaceAtIndex(oldVIndex, newVIndex, in newItem);
            return true;
        }

        private bool ReplaceCore(ref readonly TValue oldItem, ref readonly TValue newItem, bool unique, out int newVIndex)
        {
            if (_comparer.FindIndex(in newItem, Values, out newVIndex) && unique)
                return false;

            if (!_comparer.FindIndex(in oldItem, Values, out var oldVIndex))
                return false;

            if (_comparer.Compare(_storage[ToRealIndex(oldVIndex)], newItem) == 0)
                return false;

            ReplaceAtIndex(oldVIndex, newVIndex, in newItem);
            return true;
        }
    }
}
