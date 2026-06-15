using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BinaryCollections.BinaryList
{
    public partial class FloatingList<TValue>
    {

        protected int ToVirtualIndex(int realIndex) => realIndex - _zeroRIndex;
        protected int ToRealIndex(int virtualIndex) => virtualIndex + _zeroRIndex;

        protected bool CheckCountOverload(int rangeCount)
        {
            if (rangeCount > Array.MaxLength - _count)
                return false;

            return true;
        }

        protected void ThrowOverloadException()
        {
            throw new InvalidOperationException("Operation would result in an overflow.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CheckVersion(int version)
        {
            if (version != _version)
                ThrowInvalidOperation();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOperation()
        {
            throw new InvalidOperationException("Array was changed");
        }

        [Obsolete("Only for debugging purposes. If the element is a value type, it will be difficult to find boundaries", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public TValue[] ExtractRawArray(int newArraySize = -1)
        {
            _version = unchecked(++_version);
            TValue[] extracted = _storage;

            if (newArraySize >= 0)
            {
                _count = 0;
                _storage = new TValue[DefaultCapacity];
                _zeroRIndex = CalculateZeroIndex(_storage.Length, _count);
            }
            else
                _storage = null!;

            return extracted;
        }

        public TValue GetItem(int index)
        {
            if (CheckIndex(index))
            {
                return _storage[ToRealIndex(index)];
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range");
            }
        }

        [Obsolete("This method is dangerous because it allows you to change the internal state of collection elements, point to deleted or changed array! Use it only if you understand the consequences..", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref TValue GetItemRef(int index)
        {
            if (CheckIndex(index))
            {
                return ref _storage[ToRealIndex(index)];
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range");
            }
        }

        protected static int CalculateZeroIndex(int capacity, int count)
        {
            if (capacity < 0 || count < 0 || capacity < count)
            {
                throw new ArgumentOutOfRangeException(nameof(CalculateZeroIndex), "capacity < 0 || count < 0 || capacity < count");
            }

            return (capacity - count) / 2;
        }

        protected bool CheckIndex(int vIndex, int length = 1)
        {
            if (vIndex < 0 || vIndex + length > _count)
            {
                return false;
            }

            return true;
        }
    }
}
