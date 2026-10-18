namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool AddUnique(TValue item)
        {
            return AddUnique(ref item, out _);
        }

        public bool AddUnique(ref readonly TValue item, out int index)
        {
            if (FindIndexExactly(in item, Values, Comparer, out index))
                return false;

            return InsertAtIndex(in item, ref index);
        }
    }
}
