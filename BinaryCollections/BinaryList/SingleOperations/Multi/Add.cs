namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool Add(TValue item) => Add(in item, out _);

        public bool Add(ref readonly TValue item, out int index)
        {
            FindIndex(in item, Values, out index);
            return Insert(in item, ref index);
        }
    }
}
