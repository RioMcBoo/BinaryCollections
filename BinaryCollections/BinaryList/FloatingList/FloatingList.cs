using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using ExceptionHelper;

namespace BinaryCollections.BinaryList
{
    public partial class FloatingList<TValue>
    {
        public BinaryListFlags Flags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _flags;
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _storage.Length;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }

        public int Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _version;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected int LeftBuffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _zeroRIndex;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected int RightBuffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Capacity - _zeroRIndex - _count;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected bool CanMoveLeft
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => LeftBuffer > 0; 
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected bool CanMoveRight
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => RightBuffer > 0;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        protected ReadOnlySpan<TValue> Values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new ReadOnlySpan<TValue>(_storage, _zeroRIndex, _count);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public int ZeroIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _zeroRIndex;
        }

        protected static IEqualityComparer<TValue> _equalityComparer = EqualityComparer<TValue>.Default;
        protected static readonly bool ContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<TValue>();
        protected static double BackwardShiftPenalty = Avx512F.IsSupported ? 1.05 : 1.25;
        protected const int DefaultCapacity = 4;

        public TValue this[int index] => GetItemRef(index);

        public bool RemoveAt(int index)
        {
            if (!CheckIndex(index))
                return false;

            return RemoveAtIndex(index);
        }

        public TValue? Find(Predicate<TValue> match)
        {
            if (match == null)
            {
                Throw.ArgumentNullException(nameof(Find), match);
            }

            // Linear search
            for (int vIndex = 0; vIndex < _count; vIndex++)
            {
                TValue item = _storage[ToRealIndex(vIndex)];
                if (match(item))
                {
                    return item;
                }
            }

            return default;
        }

        public TValue? FindLast(Predicate<TValue> match)
        {
            if (match == null)
            {
                Throw.ArgumentNullException(nameof(FindLast), match);
            }

            for (int vIndex = _count - 1; vIndex >= 0; vIndex--)
            {
                ref readonly TValue item = ref _storage[ToRealIndex(vIndex)];
                if (match(item))
                {
                    return item;
                }
            }

            return default;
        }

        public List<TValue> FindAll(Predicate<TValue> match)
        {
            if (match == null)
            {
                Throw.ArgumentNullException(nameof(FindAll), match);
            }

            List<TValue> list = new List<TValue>();
            for (int vIndex = _count - 1; vIndex >= 0; vIndex--)
            {
                ref readonly TValue item = ref _storage[ToRealIndex(vIndex)];
                if (match(item))
                {
                    list.Add(item);
                }
            }

            return list;
        }

        public bool Trim()
        {
            return Resize(_count);
        }
        
        public bool Resize(int newCapacity)
        {
            if (_count < newCapacity || _storage.Length == newCapacity)
                return false;

            ResizeArray(newCapacity);
            return true;
        }

        public void Clear(bool? clearArray = default)
        {
            _version = unchecked(++_version);
            _count = 0;
            _zeroRIndex = CalculateZeroIndex(_storage.Length, _count);
            SetDefaultFlags();

            if (clearArray.HasValue)
            {
                if  (clearArray.Value)
                    Array.Clear(_storage, 0, _storage.Length);
            }
            else if (ContainsReferences)
            {
                Array.Clear(_storage, 0, _storage.Length);
            }
        }

        public Span<TValue> ExtractArray(int newArraySize = -1)
        {
            _version = unchecked(++_version);
            var extracted = new Span<TValue>(_storage, _zeroRIndex, _count);

            if (newArraySize >= 0)
            {
                _count = 0;
                _storage = new TValue[DefaultCapacity];
                _zeroRIndex = CalculateZeroIndex(_storage.Length, _count);
                SetDefaultFlags();
            }
            else
            {
                _storage = null!;
            }

            return extracted;
        }

        public void Copy(int fromIndex, TValue[] toArray, int toArrayIndex, int length)
        {
            if (_count - fromIndex < length)
            {
                Throw.ArgumentException(nameof(Copy), "Not enough elements in the source array");
            }

            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_storage, ToRealIndex(fromIndex), toArray, toArrayIndex, length);
        }

        public void CopyTo(TValue[] array, int arrayIndex = 0)
        {
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_storage, _zeroRIndex, array, arrayIndex, _count);
        }
    }
}
