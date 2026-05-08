namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool Remove(TValue item) => Remove(in item, out _);
        
        public bool Remove(ref readonly TValue item, out int index)
        {
            if (FindIndexExactly(in item, out index))
            {
                RemoveAtIndex(index);
                return true;
            }

            return false;
        }

        private bool RemoveAtIndex(int index)
        {
            var info = new ArraySplitInfo(index, false, _count);

            if (info.LeftPart.Length <= info.RightPart.Length)
            {
                ShiftLeftPart(ref info);
            }
            else if (info.RightPart.Length < info.LeftPart.Length)
            {
                ShiftRightPart(ref info);
            }

            _count--;
            _version = unchecked(++_version);

            return true;
        }
    }
}
