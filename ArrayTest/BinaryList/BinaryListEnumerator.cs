using System.Collections;
using System.ComponentModel;

namespace CustomCollections.BinaryList
{
    [Flags]
    public enum BinaryListEnumeratorFlags
    {
        None =              0x0,
        /// <summary>Elements are considered equal by the collection comparer.</summary>
        UniformRange =    0x1,
    }

    public struct BinaryListEnumerator<TValue> : IEnumerable<TValue>, IEnumerator<TValue>
    {
        public readonly BinaryListEnumeratorFlags Flags { get; init; }
        public IComparer<TValue> Comparer => _binaryListInstance.Comparer;
        private readonly BinaryList<TValue> _binaryListInstance;
        private readonly TValue[] _sharedData;
        private readonly int _version;
        private readonly int _start;
        private readonly int _end;
        private int _current;
        public int Count => _end - _start;

        internal BinaryListEnumerator(BinaryList<TValue> instance, TValue[] sharedData, int index, int count, BinaryListEnumeratorFlags flags = BinaryListEnumeratorFlags.None)
        {
            Flags = flags;
            _binaryListInstance = instance;
            _sharedData = sharedData;
            _version = instance._version;
            _start = index;
            _current = _start - 1;
            _end = _start + count;
        }

        public TValue Current => _sharedData[_current];

        object IEnumerator.Current => Current;

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

        [Obsolete("This method is dangerous because it allows you to change the internal state of collection elements and use without version checking. Use it only if you understand the consequences..", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
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
    }
}
