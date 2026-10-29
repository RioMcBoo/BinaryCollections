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
            if (_comparer.FindIndex(in item, Values, out index))
                return false;

            if (_count == Array.MaxLength)
                return false;

            InsertAtIndex(in item, ref index);
            return true;
        }
    }
}
