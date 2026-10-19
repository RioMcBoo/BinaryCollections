namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool ReplaceUnique(TValue oldItem, TValue newItem)
        {
            return ReplaceUnique(ref oldItem, ref newItem, out _);
        }

        public bool ReplaceUnique(int index, TValue newItem)
        {
            return ReplaceUnique(index, ref newItem, out _);
        }

        public bool ReplaceUnique(ref readonly TValue oldItem, ref readonly TValue newItem, out int newIndex)
        {
            return ReplaceCore(in oldItem, in newItem, true, out newIndex);
        }

        public bool ReplaceUnique(int index, ref readonly TValue newItem, out int newIndex)
        {
            return ReplaceCore(index, in newItem, true, out newIndex);
        }
    }
}
