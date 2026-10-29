using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ExceptionHelper;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public bool SymmetricExceptWith(ReadOnlySpan<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Length);
            try
            {
                range.CopyTo(temp);
                Array.Sort(temp, 0, range.Length, Comparer);
                return SymmetricExceptWithSorted(new ReadOnlySpan<TValue>(temp, 0, range.Length));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool SymmetricExceptWith(IReadOnlyCollection<TValue> range)
        {
            TValue[] temp = ArrayPool<TValue>.Shared.Rent(range.Count);
            try
            {
                int i = 0;
                foreach (var item in range)
                {
                    temp[i++] = item;
                }

                Array.Sort(temp, 0, range.Count, Comparer);
                return SymmetricExceptWithSorted(new ReadOnlySpan<TValue>(temp, 0, range.Count));
            }
            finally
            {
                ArrayPool<TValue>.Shared.Return(temp, true);
            }
        }

        public bool SymmetricExceptWith(IEnumerable<TValue> range)
        {
            if (range is BinaryListEnumerator<TValue> binaryListEnumerator)
            {
                return SymmetricExceptWith(binaryListEnumerator);
            }

            if (range is IReadOnlyCollection<TValue> collection)
            {
                return SymmetricExceptWith(collection);
            }

            List<TValue> temp = new List<TValue>();

            foreach (var item in range)
            {
                temp.Add(item);
            }

            var span = CollectionsMarshal.AsSpan(temp);
            span.Sort(Comparer);
            return SymmetricExceptWithSorted(span);
        }

        public bool SymmetricExceptWith(BinaryListEnumerator<TValue> range)
        {
            if (range.Comparer.Equals(Comparer))
            {
                return SymmetricExceptWithSorted(range.GetSpan());
            }

            return SymmetricExceptWith(range.GetSpan());
        }

        public bool SymmetricExceptWith(BinaryList<TValue> range)
        {
            return SymmetricExceptWith(range.GetEnumerator());
        }

        private bool SymmetricExceptWithSorted(ReadOnlySpan<TValue> range)
        {
            if (range.Length <= 0 && _count <= 0)
                return false;

            if (range.Length <= 0)
                return false;

            if (!CheckCountOverload(range.Length))
            {
                if (!SymmetricExceptWithSortedSafeTest(range))
                    Throw.CollectionOverloadException(nameof(SymmetricExceptWithSorted));
            }

            if (LeftBuffer >= RightBuffer && LeftBuffer >= range.Length)
            {
                return SymmetricExceptWithFromLeft(range);
            }

            if (RightBuffer >= LeftBuffer && RightBuffer >= range.Length)
            {
                return SymmetricExceptWithFromRight(range);
            }

            return ResizeAndSymmetricExceptWithSorted(range);
        }

        private bool SymmetricExceptWithSortedSafeTest(ReadOnlySpan<TValue> range)
        {
            throw new NotImplementedException();
        }

        private bool ResizeAndSymmetricExceptWithSorted(ReadOnlySpan<TValue> range)
        {
            PrepareForResize(range.Length, out var destination);

            int newZeroRIndex = CalculateZeroIndex(destination.Length, _count + range.Length);

            if (_storage == destination)
            {
                Array.Copy(destination, _zeroRIndex, destination, newZeroRIndex, _count);
                _zeroRIndex = newZeroRIndex;
                return SymmetricExceptWithFromRight(range);
            }
#if TEST
            if (_count % 2 == 0)
            {
                // Because this case does not arise in practice,
                // yet we must maintain the code for the eventuality that it might suddenly become active.
                SymmetricExceptWithFromRight(range, destination, newZeroRIndex);
            }
            else
            {
                SymmetricExceptWithFromLeft(range, destination, newZeroRIndex);
            }
#else
            SymmetricExceptWithFromLeft(range, destination, newZeroRIndex);
#endif
            _storage = destination;
            return true;
        }

        private bool SymmetricExceptWithFromLeft(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex - range.Length;
            return SymmetricExceptWithFromLeft(range, _storage, newZeroRIndex);
        }

        private bool SymmetricExceptWithFromLeft(ReadOnlySpan<TValue> second, TValue[] destination, int newZeroRIndex)
        {
            _version = unchecked(++_version);

            var first = _storage;

            if (ReferenceEquals(first, destination))
            {
                Debug.Assert(_zeroRIndex - newZeroRIndex == second.Length, "In-place merge requires place for the new range.");
            }

            int incomingElementsSkipped = 0;
            int rangeLength = second.Length;
            int d = newZeroRIndex;
            int zeroIndexShift = 0;

            Debug.Assert(d > -1, "Left buffer is not long enough");

            bool s_InRange(int s) => s < rangeLength;
            bool f_InRange(int f) => f < _zeroRIndex + _count;

            // General merge loop
            int i = 0;
            int c = _zeroRIndex;

            while (s_InRange(i) && f_InRange(c))
            {
                ref readonly TValue secondItem = ref second[i];
                ref readonly TValue firstItem = ref first[c];

                switch (_comparer.Compare(firstItem, secondItem))
                {
                    case > 0:
                        ProcessMergingSecond(ref d, ref i, second);
                        break;
                    case < 0:
                        ProcessMergingFirst(ref d, ref c);
                        break;
                    case 0:
                        ProcessMergingBoth(ref d, ref i, ref c, second);
                        break;
                }
            }

            // Manage remaining items
            if (s_InRange(i))
            {
                do ProcessMergingSecond(ref d, ref i, second);
                while (s_InRange(i));

                incomingElementsSkipped = newZeroRIndex + _count + second.Length - d;

                // Clear remaining free-spots
                if (ItemType<TValue>.ContainsReferences && incomingElementsSkipped > 0 && first == destination)
                {
                    Array.Clear(first, d, incomingElementsSkipped);
                }
            }
            else if (f_InRange(c))
            {
                int remainingPartLength = _zeroRIndex + _count - c;
                int finishedPartLength = d - newZeroRIndex;

                incomingElementsSkipped = _count + second.Length - remainingPartLength - finishedPartLength;

                if (first != destination)
                {
                    // we just have to copy remaining items into new array
                    Array.Copy(first, c, destination, d, remainingPartLength);
                }
                else if (incomingElementsSkipped > 0)
                {
                    if (finishedPartLength >= remainingPartLength)
                    {
                        // Shift remaining items due to missing duplicates
                        Array.Copy(first, c, first, d, remainingPartLength);

                        // Clear formed dirty free-spots from right side
                        if (ItemType<TValue>.ContainsReferences)
                        {
                            Array.Clear(first, newZeroRIndex + finishedPartLength + remainingPartLength, incomingElementsSkipped);
                        }
                    }
                    else
                    {
                        // Shift finished items due to missing duplicates
                        Array.Copy(first, newZeroRIndex, first, newZeroRIndex + incomingElementsSkipped, finishedPartLength);
                        zeroIndexShift += incomingElementsSkipped;

                        // Clear formed dirty free-spots from left side
                        if (ItemType<TValue>.ContainsReferences)
                        {
                            Array.Clear(first, newZeroRIndex, incomingElementsSkipped);
                        }
                    }
                }
            }
            else
            {
                incomingElementsSkipped = newZeroRIndex + _count + second.Length - d;

                // Clear remaining free-spots
                if (ItemType<TValue>.ContainsReferences && incomingElementsSkipped > 0 && first == destination)
                {
                    Array.Clear(first, d, incomingElementsSkipped);
                }
            }

            _zeroRIndex = newZeroRIndex + zeroIndexShift;
            _count += second.Length - incomingElementsSkipped;

			_flags.CheckFor(_count);

			return true;

            void ProcessMergingBoth(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
            {
                f++; s++; // they are equal here, so we can move both indexes
            }

            void ProcessMergingSecond(ref int d, ref int s, ReadOnlySpan<TValue> second)
            {
                destination[d++] = second[s++];
            }

            void ProcessMergingFirst(ref int d, ref int f)
            {
                destination[d++] = first[f++];
            }
        }

        private bool SymmetricExceptWithFromRight(ReadOnlySpan<TValue> range)
        {
            int newZeroRIndex = _zeroRIndex;
            return SymmetricExceptWithFromRight(range, _storage, newZeroRIndex);
        }

        private bool SymmetricExceptWithFromRight(ReadOnlySpan<TValue> second, TValue[] destination, int newZeroRIndex)
        {
            _version = unchecked(++_version);

            var first = _storage;

            if (ReferenceEquals(first, destination))
            {
                Debug.Assert(newZeroRIndex == _zeroRIndex, "In-place merge requires place for the new range.");
            }

            int incomingElementsSkipped = 0;
            int rangeLength = second.Length;
            int d = newZeroRIndex + _count + second.Length - 1;
            int zeroIndexShift = 0;

            Debug.Assert(d < destination.Length, "Right buffer is not long enough");

            bool s_InRange(int s) => s >= 0;
            bool f_InRange(int f) => f >= _zeroRIndex;

            // General merge loop
            int s = second.Length - 1;
            int f = _zeroRIndex + _count - 1;

            while (s_InRange(s) && f_InRange(f))
            {
                ref readonly TValue secondItem = ref second[s];
                ref readonly TValue firstItem = ref first[f];

                switch (_comparer.Compare(firstItem, secondItem))
                {
                    case > 0:
                        ProcessMergingFirst(ref d, ref f);
                        break;
                    case < 0:
                        ProcessMergingSecond(ref d, ref s, second);
                        break;
                    default:
                        ProcessMergingBoth(ref d, ref s, ref f, second);
                        break;
                }
            }

            // Manage remaining items
            if (s_InRange(s))
            {
                do ProcessMergingSecond(ref d, ref s, second);
                while (s_InRange(s));

                incomingElementsSkipped = (d + 1) - newZeroRIndex;
                zeroIndexShift += incomingElementsSkipped;

                // Clear remaining free-spots
                if (ItemType<TValue>.ContainsReferences && incomingElementsSkipped > 0 && first == destination)
                {
                    Array.Clear(first, newZeroRIndex, incomingElementsSkipped);
                }
            }
            else if (f_InRange(f))
            {
                int remainingPartLength = (f + 1) - _zeroRIndex;
                int finishedPartLength = _count + second.Length - (d + 1 - newZeroRIndex);
                
                incomingElementsSkipped = _count + second.Length - remainingPartLength - finishedPartLength;

                if (first != destination)
                {
                    // we just have to copy remaining items into new array
                    Array.Copy(first, _zeroRIndex, destination, newZeroRIndex + incomingElementsSkipped, remainingPartLength);
                    zeroIndexShift += incomingElementsSkipped;
                }
                else if (incomingElementsSkipped > 0)
                {
                    if (finishedPartLength >= remainingPartLength)
                    {
                        // Shift remaining items due to missing duplicates
                        Array.Copy(first, _zeroRIndex, first, newZeroRIndex + incomingElementsSkipped, remainingPartLength);
                        zeroIndexShift += incomingElementsSkipped;

                        // Clear formed dirty free-spots from left side
                        if (ItemType<TValue>.ContainsReferences)
                        {
                            Array.Clear(first, newZeroRIndex, incomingElementsSkipped);
                        }
                    }
                    else
                    {
                        // Shift finished items due to missing duplicates
                        Array.Copy(first, d + 1, first, d + 1 - incomingElementsSkipped, finishedPartLength);

                        // Clear formed dirty free-spots from right side
                        if (ItemType<TValue>.ContainsReferences)
                        {
                            Array.Clear(first, newZeroRIndex + finishedPartLength + remainingPartLength, incomingElementsSkipped);
                        }
                    }
                }
            }
            else
            {
                incomingElementsSkipped = (d + 1) - newZeroRIndex;
                zeroIndexShift += incomingElementsSkipped;

                // Clear remaining free-spots
                if (ItemType<TValue>.ContainsReferences && incomingElementsSkipped > 0 && first == destination)
                {
                    Array.Clear(first, newZeroRIndex, incomingElementsSkipped);
                }
            }

            _zeroRIndex = newZeroRIndex + zeroIndexShift;
            _count += second.Length - incomingElementsSkipped;

			_flags.CheckFor(_count);

			return true;

            void ProcessMergingBoth(ref int d, ref int s, ref int f, ReadOnlySpan<TValue> second)
            {
                f--; s--; // they are equal here, so we can move both indexes
            }
            
            void ProcessMergingSecond(ref int d, ref int s, ReadOnlySpan<TValue> second)
            {
                destination[d--] = second[s--];
            }

            void ProcessMergingFirst(ref int d, ref int f)
            {
                destination[d--] = first[f--];
            }
        }
    }
}
