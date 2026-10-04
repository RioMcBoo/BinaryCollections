using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using ExceptionHelper;

namespace BinaryCollections.BinaryList
{
    public partial class FloatingList<TValue>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected int ToVirtualIndex(int realIndex) => realIndex - _zeroRIndex;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected int ToRealIndex(int virtualIndex) => virtualIndex + _zeroRIndex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool CheckCountOverload(int rangeCount)
        {
            if (rangeCount > Array.MaxLength - _count)
                return false;

            return true;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue GetItem(int index)
        {
            if (!CheckIndex(index))
            {
                Throw.IndexOutOfRangeException(nameof(GetItemRef), index);
            }

            return _storage[ToRealIndex(index)];
        }

        [Obsolete("This method is dangerous because it allows you to change the internal state of collection elements, point to deleted or changed array! Use it only if you understand the consequences..", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetItemRef(int index)
        {
            if (!CheckIndex(index))
            {
                Throw.IndexOutOfRangeException(nameof(GetItemRef), index);
            }

            return ref _storage[ToRealIndex(index)];
        }

        protected static int CalculateZeroIndex(int capacity, int count)
        {
            Debug.Assert(capacity >= 0, "capacity < 0");
            Debug.Assert(count >= 0, "count < 0");
            Debug.Assert(capacity >= count, "capacity > count");

            return (capacity - count) / 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool CheckIndex(int vIndex, int length)
        {
            return (uint)vIndex + (uint)length <= (uint)_count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool CheckIndex(int vIndex)
        {
            return (uint)vIndex <= (uint)_count;
        }        

        protected void SetFlags(BinaryListFlags flags)
        {
            _flags = flags;
        }

        protected void SetDefaultFlags(BinaryListFlags include = BinaryListFlags.None, BinaryListFlags exclude = BinaryListFlags.None)
        {
            _flags = BinaryListFlags.NoDuplicates;
            ModifyFlags(include, exclude);
        }

        protected void ModifyFlags(BinaryListFlags add = BinaryListFlags.None, BinaryListFlags remove = BinaryListFlags.None)
        {
            _flags = (_flags | add) & ~remove;
        }

        protected void AddFlags(BinaryListFlags add)
        {
            _flags = _flags | add;
        }

        protected void RemoveFlags(BinaryListFlags remove)
        {
            _flags = _flags & ~remove;
        }

        protected bool HasFlag(BinaryListFlags flag)
        {
            return (_flags & flag) == flag;
        }

        protected bool HasAnyFlag(BinaryListFlags flag)
        {
            return (_flags & flag) != 0;
        }
    }
}
