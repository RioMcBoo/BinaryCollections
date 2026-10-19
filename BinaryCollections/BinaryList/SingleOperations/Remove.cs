namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool Remove(TValue item) => Remove(in item, out _);
        
        public bool Remove(ref readonly TValue item, out int index)
        {
            if (_comparer.FindIdenticalItemIndex(in item, Values, out index))
            {
                RemoveAtIndex(index);
                return true;
            }

            return false;
        }
    }
}
