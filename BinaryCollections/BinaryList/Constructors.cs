using ExceptionHelper;

namespace BinaryCollections.BinaryList
{
    public partial class BinaryList<TValue>
    {
        public BinaryList(BinaryList<TValue> source)
        {
            if (source is null)
                Throw.ArgumentNullException(nameof(BinaryList<TValue>), nameof(source));

            _flags = source._flags;
            _comparer = source._comparer;
            _storage = new TValue[source._storage.Length];
            _zeroRIndex = source._zeroRIndex;
            _count = source._count;

            Array.Copy(source._storage, _zeroRIndex, _storage, _zeroRIndex, source.Count);
        }

        public BinaryList(ReadOnlySpan<TValue> initialValues)
        {
            int capacity = initialValues.Length;

            _comparer = BinaryListComparer<TValue>.Default;
            _storage = capacity > 0 ? new TValue[capacity] : EmptyArray;
            _zeroRIndex = CalculateZeroIndex(_storage.Length, 0);

            AddRange(initialValues);
        }

        public BinaryList(IEnumerable<TValue> initialValues)
        {
            if (initialValues is null)
                Throw.ArgumentNullException(nameof(BinaryList<TValue>), nameof(initialValues));

            int capacity = 0;

            if (initialValues is IReadOnlyCollection<TValue> collection)
            {
                capacity = collection.Count;
            }

            _comparer = BinaryListComparer<TValue>.Default;
            _storage = capacity > 0 ? new TValue[capacity] : EmptyArray;
            _zeroRIndex = CalculateZeroIndex(_storage.Length, 0);

            AddRange(initialValues);
        }

        public BinaryList(int capacity = 0)
        {
            _comparer = BinaryListComparer<TValue>.Default;
            _storage = capacity > 0 ? new TValue[capacity] : EmptyArray;
            _zeroRIndex = CalculateZeroIndex(_storage.Length, 0);
        }

        public BinaryList(IEnumerable<TValue> initialValues, ComparisonComparer<TValue> orderComparer)
            : this(initialValues, (IComparer<TValue>)orderComparer) { }

        public BinaryList(IEnumerable<TValue> initialValues, Comparison<TValue> orderComparison)
            : this(initialValues, (ComparisonComparer<TValue>)orderComparison) { }

        public BinaryList(IEnumerable<TValue> initialValues, IComparer<TValue> orderComparer)
            : this(initialValues, new BinaryListComparer<TValue>(orderComparer)) { }

        public BinaryList(IEnumerable<TValue> initialValues, ComparisonComparer<TValue> orderComparer, ComparisonComparer<TValue> identityComparer)
            : this(initialValues, (IComparer<TValue>)orderComparer, (IComparer<TValue>)identityComparer) { }

        public BinaryList(IEnumerable<TValue> initialValues, Comparison<TValue> orderComparison, Comparison<TValue> identityComparison)
            : this(initialValues, (ComparisonComparer<TValue>)orderComparison, (ComparisonComparer<TValue>)identityComparison) { }

        public BinaryList(IEnumerable<TValue> initialValues, IComparer<TValue> orderComparer, IComparer<TValue> identityComparer)
            : this(initialValues, new BinaryListComparer<TValue>(orderComparer, identityComparer)) { }

        public BinaryList(IEnumerable<TValue> initialValues, BinaryListComparer<TValue> comparer)
        {
            if (initialValues is null)
                Throw.ArgumentNullException(nameof(BinaryList<TValue>), nameof(initialValues));

            int capacity = 0;

            if (initialValues is IReadOnlyCollection<TValue> collection)
            {
                capacity = collection.Count;
            }

            if (comparer is null)
                Throw.ArgumentNullException(nameof(BinaryList<TValue>), nameof(comparer));

            _comparer = comparer;
            _storage = capacity > 0 ? new TValue[capacity] : EmptyArray;
            _zeroRIndex = CalculateZeroIndex(_storage.Length, 0);

            AddRange(initialValues);
        }


        public BinaryList(ReadOnlySpan<TValue> initialValues, ComparisonComparer<TValue> orderComparer)
            : this(initialValues, (IComparer<TValue>)orderComparer) { }

        public BinaryList(ReadOnlySpan<TValue> initialValues, Comparison<TValue> orderComparison)
            : this(initialValues, (ComparisonComparer<TValue>)orderComparison) { }

        public BinaryList(ReadOnlySpan<TValue> initialValues, IComparer<TValue> orderComparer)
            : this(initialValues, new BinaryListComparer<TValue>(orderComparer)) { }

        public BinaryList(ReadOnlySpan<TValue> initialValues, ComparisonComparer<TValue> orderComparer, ComparisonComparer<TValue> identityComparer)
            : this(initialValues, (IComparer<TValue>)orderComparer, (IComparer<TValue>)identityComparer) { }

        public BinaryList(ReadOnlySpan<TValue> initialValues, Comparison<TValue> orderComparison, Comparison<TValue> identityComparison)
            : this(initialValues, (ComparisonComparer<TValue>)orderComparison, (ComparisonComparer<TValue>)identityComparison) { }

        public BinaryList(ReadOnlySpan<TValue> initialValues, IComparer<TValue> orderComparer, IComparer<TValue> identityComparer)
            : this(initialValues, new BinaryListComparer<TValue>(orderComparer, identityComparer)) { }


        public BinaryList(ReadOnlySpan<TValue> initialValues, BinaryListComparer<TValue> comparer)
        {
            int capacity = initialValues.Length;

            if (comparer is null)
                Throw.ArgumentNullException(nameof(BinaryList<TValue>), nameof(comparer));

            _comparer = comparer;
            _storage = capacity > 0 ? new TValue[capacity] : EmptyArray;
            _zeroRIndex = CalculateZeroIndex(_storage.Length, 0);

            AddRange(initialValues);
        }


        public BinaryList(int capacity, ComparisonComparer<TValue> orderComparer)
            : this(capacity, (IComparer<TValue>)orderComparer) { }

        public BinaryList(int capacity, Comparison<TValue> orderComparison)
            : this(capacity, (ComparisonComparer<TValue>)orderComparison) { }

        public BinaryList(int capacity, IComparer<TValue> orderComparer)
            : this(capacity, new BinaryListComparer<TValue>(orderComparer)) { }

        public BinaryList(int capacity, ComparisonComparer<TValue> orderComparer, ComparisonComparer<TValue> identityComparer)
            : this(capacity, (IComparer<TValue>)orderComparer, (IComparer<TValue>)identityComparer) { }

        public BinaryList(int capacity, Comparison<TValue> orderComparison, Comparison<TValue> identityComparison)
            : this(capacity, (ComparisonComparer<TValue>)orderComparison, (ComparisonComparer<TValue>)identityComparison) { }

        public BinaryList(int capacity, IComparer<TValue> orderComparer, IComparer<TValue> identityComparer)
            : this(capacity, new BinaryListComparer<TValue>(orderComparer, identityComparer)) { }

        public BinaryList(int capacity, BinaryListComparer<TValue> comparer)
        {
            if (comparer is null)
                Throw.ArgumentNullException(nameof(BinaryList<TValue>), nameof(comparer));

            capacity = capacity > 0 ? (capacity > Array.MaxLength ? Array.MaxLength : capacity) : 0;            

            _comparer = comparer;
            _storage = capacity > 0 ? new TValue[capacity] : EmptyArray;
            _zeroRIndex = CalculateZeroIndex(_storage.Length, 0);
        }


        public BinaryList(Comparison<TValue> orderComparison)
            : this((ComparisonComparer<TValue>)orderComparison) { }

        public BinaryList(ComparisonComparer<TValue> orderComparer)
            : this((IComparer<TValue>)orderComparer) { }

        public BinaryList(IComparer<TValue> orderComparer)
            : this(new BinaryListComparer<TValue>(orderComparer)) { }

        public BinaryList(Comparison<TValue> orderComparison, Comparison<TValue> identityComparison)
            : this((ComparisonComparer<TValue>)orderComparison, (ComparisonComparer<TValue>)identityComparison) { }

        public BinaryList(ComparisonComparer<TValue> orderComparer, ComparisonComparer<TValue> identityComparer)
            : this((IComparer<TValue>)orderComparer, (IComparer<TValue>)identityComparer) { }

        public BinaryList(IComparer<TValue> orderComparer, IComparer<TValue> identityComparer)
            : this(new BinaryListComparer<TValue>(orderComparer, identityComparer)) { }

        public BinaryList(BinaryListComparer<TValue> comparer)
        {
            if (comparer is null)
                Throw.ArgumentNullException(nameof(BinaryList<TValue>), nameof(comparer));

            _comparer = comparer;
            _storage = EmptyArray;
            _zeroRIndex = CalculateZeroIndex(_storage.Length, 0);
        }
    }
}
