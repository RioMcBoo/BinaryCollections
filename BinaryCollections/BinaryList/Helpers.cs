using System.Runtime.CompilerServices;
using ExceptionHelper;

namespace BinaryCollections.BinaryList
{
    public partial class BinaryList<TValue>
    {
        private readonly ref struct ArrayRange
        {
            readonly public int Index;
            readonly public int Length;
            readonly public TValue[] Source;

            public ArrayRange(int index, int length, TValue[] source)
            {
                Index = index;
                Length = length;
                Source = source;
            }
        }

        private class HeapCounter
        {
            public int SecondCount;
            public int FirstCount;

            public HeapCounter(int secondCount = 0, int firstCount = 0)
            {
                SecondCount = secondCount;
                FirstCount = firstCount;
            }
        }

        private ref struct RefCounter
        {
            public int SecondCount;
            public int FirstCount;

            public RefCounter(int secondCount = 0, int firstCount = 0)
            {
                SecondCount = secondCount;
                FirstCount = firstCount;
            }
        }

        private ref struct Iterator
        {
            public static TValue Default = default!;
            public bool OutOfRange { get; private set; }
            public readonly ReadOnlySpan<TValue> Span;
            public readonly BinaryListFlags Flags;
            private ref readonly TValue _current;
            private int _index;
            private int _lowerLimit;
            private int _upperLimit;

            public Iterator(ReadOnlySpan<TValue> span, BinaryListFlags flags)
            {
                Span = span;
                Flags = flags;
                _index = -1;
                _lowerLimit = 0;
                _upperLimit = span.Length - 1;
                _current = ref Default;
                OutOfRange = true;
            }

            public void operator ++() => Next(1);

            public void operator --() => Prev(1);

            /// <summary>
            /// Advances the iterator forward by the specified number of elements.
            /// </summary>
            /// <param name="delta">The requested number of elements to skip forward.</param>
            /// <returns>
            /// The actual number of elements advanced. This value will be less than <paramref name="delta"/> 
            /// if the iterator hits the upper boundary (<see cref="UpperLimit"/>) of the range.
            /// </returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="delta"/> is negative.</exception>
            public int Next(int delta)
            {
                if (delta > 0) return _setIndex(_index + delta);

                if (delta < 0)
                    Throw.ArgumentOutOfRangeException(nameof(Next), delta, "Delta must be non-negative.");

                return 0;
            }

            /// <summary>
            /// Moves the iterator backward by the specified number of elements.
            /// </summary>
            /// <param name="delta">The requested number of elements to skip backward.</param>
            /// <returns>
            /// The actual number of elements moved backward. This value will be less than <paramref name="delta"/> 
            /// if the iterator hits the lower boundary (<see cref="LowerLimit"/>) of the range.
            /// </returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="delta"/> is negative.</exception>
            public int Prev(int delta)
            {
                if (delta > 0) return -_setIndex(_index - delta);

                if (delta < 0)
                    Throw.ArgumentOutOfRangeException(nameof(Prev), delta, "Delta must be non-negative.");

                return 0;
            }

            public ref readonly TValue this[int index]
            {
                get
                {
                    if (index < _lowerLimit || index > _upperLimit)
                    {
                        Throw.IndexOutOfRangeException("Iterator.[]", index);
                    }

                    return ref Span[index];
                }
            }

            public int Length => Span.Length;
            public int LimitedCount => _upperLimit - _lowerLimit + 1;

            public int LowerLimit
            {
                get => _lowerLimit;
                set
                {
                    if (value < 0 || value > _upperLimit)
                    {
                        Throw.ArgumentOutOfRangeException(nameof(LowerLimit), value, "Value is out of range.");
                    }

                    _lowerLimit = value;
                    _setIndex(_index);
                }
            }

            public int UpperLimit
            {
                get => _upperLimit;
                set
                {
                    if (value >= Span.Length || value < _lowerLimit)
                    {
                        Throw.ArgumentOutOfRangeException(nameof(UpperLimit), value, "Value is out of range.");
                    }

                    _upperLimit = value;
                    _setIndex(_index);
                }
            }

            public ref readonly TValue Current => ref _current;

            public void SetIndex(int index)
            {
                if ((uint)index > (uint)Span.Length)
                    Throw.IndexOutOfRangeException(nameof(SetIndex), index);

                _setIndex(index);
            }

            /// <summary>
            /// Sets the internal index of the iterator to the specified position, enforced by the range boundaries.
            /// </summary>
            /// <param name="index">The target absolute index to set.</param>
            /// <returns>
            /// The actual signed displacement (delta) applied to the index relative to its previous position. 
            /// If the target index exceeds the limits, it returns the exact distance from the previous position 
            /// to the boundary (<see cref="_upperLimit"/> or <see cref="_lowerLimit"/>).
            /// </returns>
            private int _setIndex(int index)
            {
                int oldIndex = _index;

                if (index > _upperLimit)
                {
                    _index = _upperLimit + 1;
                    _current = ref Default;
                    OutOfRange = true;
                    return _upperLimit - oldIndex;
                }

                if (index < _lowerLimit)
                {
                    _index = _lowerLimit - 1;
                    _current = ref Default;
                    OutOfRange = true;
                    return _lowerLimit - oldIndex;
                }

                _index = index;
                _current = ref Span[_index];
                OutOfRange = false;
                return _index - oldIndex;
            }
        }

        private ref struct SourceInfo
        {
            public ReadOnlySpan<TValue> Array;
            public int Index;
            public int Count;
        }

        private ref struct LoopInfo
        {
            public int IndexDirection;
            public SourceInfo Current;
            public SourceInfo Incoming;
            public SourceInfo Destination;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFoundIndexOutOfRange(int index, int count)
        {
            if (index > 0)
                return index >= count;

            if (index < 0)
                return ~index == 0 || ~index == count;

            Throw.ArgumentException(nameof(IsFoundIndexOutOfRange), "index is wrong");
            return false;
        }
    }
}
