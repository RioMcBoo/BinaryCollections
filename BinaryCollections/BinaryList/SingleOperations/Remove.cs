namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool Remove(TValue item) => Remove(in item, out _);
        
        public bool Remove(ref readonly TValue item, out int index)
        {
            if (FindIndexExactly(in item, Values, Comparer, out index))
            {
                RemoveAtIndex(index);
                return true;
            }

            return false;
        }
    }
}
