namespace BinaryList.Tests
{
    /// <summary>
    /// A binary list does not clean up freed cells if they are of a value type to improve performance.
    /// Cleaning is necessary to free memory for the garbage collector.
    /// To simplify testing and provide clarity on what happens to memory, a reference type (class) is used.
    /// </summary>
    public class TestType : IEquatable<TestType>
    {
        public int Value { get; init; }
        public int RandomValue { get; init; } = TestTypeRandomizer.GetNextFromPool(); // used to make expectable collisions
        public static implicit operator TestType(int value) => new() { Value = value };

        public bool Equals(TestType? other)
        {
            if (other == null)
                return false;

            return Value == other.Value;
        }

        public override bool Equals(object? other)
        {
            if (other is not TestType otherInstance)
                return false;

            return Value == otherInstance.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    public class TestTypeHashCollisionAble : IEquatable<TestTypeHashCollisionAble>
    {
        public int Value { get; init; }
        public int RandomValue { get; init; } = TestTypeRandomizer.GetNextFromPool(); // used to make expectable collisions
        public static implicit operator TestTypeHashCollisionAble(int value) => new() { Value = value };

        public bool Equals(TestTypeHashCollisionAble? other)
        {
            if (other == null)
                return false;

            return Value == other.Value;
        }

        public override bool Equals(object? other)
        {
            if (other is not TestTypeHashCollisionAble otherInstance)
                return false;

            return Value == otherInstance.Value;
        }

        public override int GetHashCode()
        {
            return RandomValue.GetHashCode(); // used to make expectable collisions
        }
    }

    public class TestTypeComparable : IComparable<TestTypeComparable>, IEquatable<TestTypeComparable>
    {
        public int Value { get; init; }
        public int RandomValue { get; init; } = TestTypeRandomizer.GetNextFromPool(); // used to make expectable collisions

        public int CompareTo(TestTypeComparable? other)
        {
            return TestComparer.Properly.Compare(this, other);
        }

        public bool Equals(TestTypeComparable? other)
        {
            if (TestComparer.Properly.Compare(this, other) == 0)
                return true;

            return false;
        }

        public override bool Equals(object? other)
        {
            if (other is not TestTypeComparable otherInstance)
                return false;

            return Value == otherInstance.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static implicit operator TestTypeComparable(int value) => new() { Value = value };
    }

    public class TestTypeRandomizer
    {
        private static readonly int[] _shuffledPool;
        private static int _index = 0;
        private static Random _random = new(0);
        static TestTypeRandomizer()
        {
            _shuffledPool = Enumerable.Repeat(new[] { -3, -2, -1 }, 100)
                                      .SelectMany(x => x)
                                      .ToArray();
            _random.Shuffle(_shuffledPool);
        }

        public static int GetNextFromPool()
        {
            int idx = Math.Abs(Interlocked.Increment(ref _index) % _shuffledPool.Length);
            return _shuffledPool[idx];
        }
    }

    public class TestComparer : IComparer<TestTypeComparable>
    {
        private readonly bool _withCollisions;

        public static TestComparer Properly = new(false);
        public static TestComparer WithCollisions = new(true);

        private TestComparer (bool withCollisions) { _withCollisions = withCollisions; }
        /// <returns>
        ///     <para>A value that indicates the relative order of the objects being compared. The return value has these meanings:</para>
        ///     <list type="table">
        ///         <item>
        ///             <term>Less than zero</term>
        ///             <description><paramref name="x" /> is smaller than <paramref name="y" />.</description>
        ///         </item>
        ///         <item>
        ///             <term>Zero</term>
        ///             <description><paramref name="x" /> equal to <paramref name="y" />.</description>
        ///         </item>
        ///         <item>
        ///             <term>Greater than zero</term>
        ///             <description><paramref name="x" /> is greater than <paramref name="y" />.</description>
        ///         </item>
        ///     </list>
        /// </returns>
        public int Compare(TestTypeComparable? x, TestTypeComparable? y) 
        {
            return _withCollisions ? CompareWithCollisions(x, y) : CompareProperly(x, y);
        }

        public int CompareProperly(TestTypeComparable? x, TestTypeComparable? y)
        {
            if (x == null || y == null)
                return Comparer<TestTypeComparable>.Default.Compare(x, y);

            return Comparer<int>.Default.Compare(x.Value, y.Value);
        }

        public int CompareWithCollisions(TestTypeComparable? x, TestTypeComparable? y)
        {
            if (x == null || y == null)
                return Comparer<TestTypeComparable>.Default.Compare(x, y);

            return Comparer<int>.Default.Compare(x.RandomValue, y.RandomValue);
        }

        public override bool Equals(object? obj)
        {
            if (obj is TestComparer other)
            {
                return (_withCollisions == other._withCollisions);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_withCollisions);
        }

        public static bool operator ==(TestComparer? left, TestComparer? right)
        {
            if (left is null || right is null)
                return false;

            if (ReferenceEquals(left, right))
                return true;

            return left.Equals(right);
        }

        public static bool operator !=(TestComparer? left, TestComparer? right)
        {
            return !(left == right);
        }
    }
}
