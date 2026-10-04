using ExceptionHelper;
using System.Collections;
using System.Runtime.CompilerServices;

namespace BinaryCollections.BinaryList
{
    public struct BinaryListEnumerator<TValue> : IEnumerator<TValue>, IReadOnlyList<TValue>
    {
        public readonly BinaryListFlags Flags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _binaryListInstance.Flags | _sharedFlags;
        }

        public IComparer<TValue> Comparer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _binaryListInstance.Comparer;
        }

        public int Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _version;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }

        private readonly BinaryList<TValue> _binaryListInstance;
        private readonly TValue[] _sharedData;
        private readonly int _version;
        private readonly int _start;
        private readonly int _count;
        private int _current;
        private BinaryListFlags _sharedFlags;

        internal BinaryListEnumerator(BinaryList<TValue> instance, TValue[] sharedData, int index, int count, BinaryListFlags sharedFlags = BinaryListFlags.None)
        {
            _sharedFlags = sharedFlags;
            _binaryListInstance = instance;
            _sharedData = sharedData;
            _version = instance.Version;
            _start = index;
            _current = _start - 1;
            _count = count;
        }

        public TValue Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _sharedData[_current];
        }

        object IEnumerator.Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Current;
        }

        public TValue this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ValidateVersion();

                if (!CheckIndex(index))
                    Throw.IndexOutOfRangeException("BinaryListEnumerator.[] : ", index);

                return _sharedData[_start + index];
            }
        }

        public bool MoveNext()
        {
            ValidateVersion();
            return ++_current < _count;
        }

        public void Dispose() { }

        public void Reset()
        {
            _current = _start - 1;
        }

        public BinaryListEnumerator<TValue> GetEnumerator()
        {
            var copy = this;
            copy.Reset();
            return copy;
        }

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public ReadOnlySpan<TValue> GetSpan()
        {
            ValidateVersion();
            return new ReadOnlySpan<TValue>(_sharedData, _start, Count);
        }

        public TValue[] ToArray()
        {
            var span = GetSpan();
            return span.ToArray();
        }

        public List<TValue> ToList()
        {
            var span = GetSpan();
            return new(span.ToArray());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateVersion()
        {
            if (_version != _binaryListInstance.Version)
                Throw.ArrayWasChanged(nameof(ValidateVersion));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckIndex(int vIndex)
        {
            return (uint)vIndex < (uint)Count;
        }
    }
}
