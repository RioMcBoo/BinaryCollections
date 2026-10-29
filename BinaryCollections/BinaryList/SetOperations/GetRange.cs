using ExceptionHelper;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue>
    {
        public BinaryListEnumerator<TValue> GetRange(TValue item)
        {
            return GetRange(ref item);
        }

        public BinaryListEnumerator<TValue> GetRangeByOrder(TValue item)
        {
            return GetRange(ref item, true);
        }

        public BinaryListEnumerator<TValue> GetRange(ref readonly TValue item, bool byOrder = false)
        {
            if (_count == 0)
                return GetEnumerator();

            if (_flags.HasTheFlag(DuplicateDensity.Full) && _comparer.Compare(_storage[ToRealIndex(0)], item) == 0)
                return GetEnumerator();

            _comparer.FindBoundaries(in item, Values, out var boundaries, byOrder);

            return new(this, _storage, ToRealIndex(boundaries.Left), boundaries.Length, DuplicateDensity.Full);
        }

        public BinaryListEnumerator<TValue> GetRange(int index, int length)
        {
            if (!CheckIndex(index, length))
                Throw.ArgumentException(nameof(GetRange), $"{nameof(index)} or {nameof(length)} is out of range");

            return new(this, _storage, ToRealIndex(index), length);
        }
    }
}
