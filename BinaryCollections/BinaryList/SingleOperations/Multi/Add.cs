namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool Add(TValue item) => Add(in item, out _);

        public bool Add(ref readonly TValue item, out int index)
        {
            FindIndex(in item, Values, Comparer, out index);

            if (_count == Array.MaxLength)
                return false;

            InsertAtIndex(in item, ref index);
            return true;
        }
    }
}