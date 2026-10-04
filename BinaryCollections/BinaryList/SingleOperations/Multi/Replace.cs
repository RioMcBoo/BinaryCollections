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

        private void ReplaceItemsInArray(ref readonly ArraySplitInfo info)
        {
            // we take middle alignment info to see how edges will be changed
            var alignmentInfo = info.Alignment.Middle;
            bool BetterMoveMiddlePart = info.MiddlePart.Length <= info.LeftPart.Length + info.RightPart.Length;

            // Try to move middle part
            if (BetterMoveMiddlePart)
            {
                // Check boundaries
                if (info.ArraySizeDelta > 0 && RightBuffer - info.ArraySizeDelta < 0)
                    ResizeArray(in info);
                else
                    ShiftMiddlePart(in info);
            }
            else // Try to move edges
            {
                // check boundaries
                if ((LeftBuffer + alignmentInfo.LeftEdgeIndexShift < 0) || (RightBuffer - alignmentInfo.RightEdgeIndexShift < 0))
                    ResizeArray(in info);
                else
                    ShiftEdges(in info);
            }
        }

        private bool ReplaceCore(ref readonly TValue oldItem, out int newVIndex, ref readonly TValue newItem, bool unique)
        {
            FindIndexExactly(in oldItem, Values, Comparer, out var oldVIndex);
            return ReplaceCore(oldVIndex, out newVIndex, in newItem, unique);
        }

        private bool ReplaceCore(int oldVIndex, out int newVIndex, ref readonly TValue newItem, bool unique)
        {
            if (FindIndexExactly(in newItem, Values, Comparer, out newVIndex) && unique)
                return false;

            if (!CheckIndex(oldVIndex))
                return false;

            if (IsIdentical(in _storage[ToRealIndex(oldVIndex)], in newItem))
                return false;

            _version = unchecked(++_version);

            // Do replacement
            bool ItemExists = newVIndex >= 0;
            newVIndex = ItemExists ? newVIndex : ~newVIndex;

            var replaceInfo = new ReplaceInfo(oldVIndex, newVIndex, Count);
            var info = replaceInfo.Data;

            ReplaceItemsInArray(in info);
            _storage[ToRealIndex(newVIndex + info.ReplaceAddIndexShift)] = newItem;

            if (_count < 2)
            {
                ModifyFlags(add: BinaryListFlags.NoDuplicates, remove: BinaryListFlags.UniformRange);
            }
            else if (ItemExists)
            {
                RemoveFlags(BinaryListFlags.NoDuplicates);

                if (_count == 2)
                    AddFlags(BinaryListFlags.UniformRange);
            }
            else
            {
                RemoveFlags(BinaryListFlags.UniformRange);
            }

            return true;
        }
    }
}
