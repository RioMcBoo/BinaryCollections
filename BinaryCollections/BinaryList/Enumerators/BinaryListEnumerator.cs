using System.Collections;

namespace BinaryCollections.BinaryList
{
    public struct BinaryListEnumerator<TValue> : IEnumerator<TValue>, IReadOnlyList<TValue>
    {
        public readonly BinaryListFlags Flags => _binaryListInstance.Flags | _sharedFlags;
        public IComparer<TValue> Comparer => _binaryListInstance.Comparer;
        public int Version => _version;
        public int Count => _end - _start;

        private readonly BinaryList<TValue> _binaryListInstance;
        private readonly TValue[] _sharedData;
        private readonly int _version;
        private readonly int _start;
        private readonly int _end;
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
            _end = _start + count;
        }

        public TValue Current => _sharedData[_current];

        object IEnumerator.Current => Current;

        public TValue this[int index]
        {
            get
            {
                ValidateVersion();

                if (CheckIndex(index))
                {
                    return _sharedData[_start + index];
                }

                throw new IndexOutOfRangeException($"Index {index} is out of range for enumerator with count {Count}.");
            }
        }

        public bool MoveNext()
        {
            ValidateVersion();
            return ++_current < _end;
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

        private void ValidateVersion()
        {
            _binaryListInstance.CheckVersion(_version);
        }

        private bool CheckIndex(int vIndex)
        {
            if (vIndex < 0 || vIndex >= Count)
            {
                return false;
            }

            return true;
        }
    }
}
