using System.Runtime.CompilerServices;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool Replace(TValue oldItem, TValue newItem)
        {
            return Replace(ref oldItem, ref newItem, out _);
        }

        public bool Replace(int index, TValue newItem)
        {
            return Replace(index, ref newItem, out _);
        }

        public bool Replace(ref readonly TValue oldItem, ref readonly TValue newItem, out int newIndex)
        {
            return ReplaceCore(in oldItem, out newIndex, in newItem, false);
        }

        public bool Replace(int index, ref readonly TValue newItem, out int newIndex)
        {
            return ReplaceCore(index, out newIndex, in newItem, false);
        }

        private void ReplaceItemsInArray(ref ArraySplitInfo info, out int addItemIndexDelta)
        {
            addItemIndexDelta = 0;
            bool NeedsMoveMiddlePart = info.MiddPart.Length < info.LeftPart.Length + info.RightPart.Length;

            if (NeedsMoveMiddlePart || Capacity == Count)
            {
                ShiftMiddlePart(ref info, out addItemIndexDelta);
            }
            else if ((info.LeftPoint.NewItemtPushes && !CanMoveLeft) || (info.RightPoint.NewItemtPushes && !CanMoveRight))
            {
                ShiftArrayToCenter(ref info);
            }
            else
            {
                ShiftEdges(ref info);
            }
        }

        private bool ReplaceCore(ref readonly TValue oldItem, out int newVIndex, ref readonly TValue newItem, bool unique)
        {
            FindIndexExactly(in oldItem, out var oldVIndex);
            return ReplaceCore(oldVIndex, out newVIndex, in newItem, unique);
        }

        private bool ReplaceCore(int oldVIndex, out int newVIndex, ref readonly TValue newItem, bool unique)
        {
            if (FindIndexExactly(in newItem, out newVIndex) && unique)
                return false;

            if (!CheckIndex(oldVIndex))
                return false;

            _version = unchecked(++_version);

            if (oldVIndex == newVIndex && newVIndex > -1)
            {
                // oldItem and newItem have same index, so we can replace them easily.
                _storage[ToRealIndex(oldVIndex)] = newItem;
                return true;
            }

            // Do replacement
            newVIndex = newVIndex < 0 ? ~newVIndex : newVIndex;
            var info = new ArraySplitInfo(oldVIndex, newVIndex, Count);
            ReplaceItemsInArray(ref info, out int addItemIndexDelta);

            _storage[ToRealIndex(newVIndex + addItemIndexDelta)] = newItem;

            return true;
        }
    }
}
