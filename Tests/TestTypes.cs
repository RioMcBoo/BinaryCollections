namespace BinaryList.Tests
{
    /// <summary>
    /// A binary list does not clean up freed cells if they are of a value type to improve performance.
    /// Cleaning is necessary to free memory for the garbage collector.
    /// To simplify testing and provide clarity on what happens to memory, a reference type (class) is used.
    /// </summary>
    public interface TestType
    {
        public static int DefaultHashFactor = int.MaxValue;
        public int Value { get; init; }
        public int ValueHash { get; init; }
        public int HashCode { get; init; }
    }

    public class TestTypeComparable : TestType, IComparable<TestTypeComparable>, IEquatable<TestTypeComparable>
    {
        public static int HashFactor = TestType.DefaultHashFactor;
        public int Value { get; init; }
        public int ValueHash { get; init; }
        public int HashCode { get; init; }

        public int CompareTo(TestTypeComparable? other)
        {
            if (other is null)
                return 1;

            return Comparer<int>.Default.Compare(this.Value, other.Value);
        }

        public bool Equals(TestTypeComparable? other)
        {
            if (CompareTo(other) == 0)
                return true;

            return false;
        }

        public override bool Equals(object? other)
        {
            return Equals(other as TestTypeComparable);
        }

        public override int GetHashCode()
        {
            return HashCode;
        }

        public TestTypeComparable(int value)
        {
            Value = value;
            ValueHash = value % 3;
            HashCode = value % HashFactor;
        }

        public static implicit operator TestTypeComparable(int value) => new(value);
    }

    public class TestTypeIncomparable : TestType, IEquatable<TestTypeIncomparable>
    {
        public static int HashFactor = TestType.DefaultHashFactor;
        public int Value { get; init; }
        public int ValueHash { get; init; }
        public int HashCode { get; init; }

        public int CompareTo(TestTypeIncomparable? other)
        {
            if (other is null)
                return 1;

            return Comparer<int>.Default.Compare(this.Value, other.Value);
        }

        public bool Equals(TestTypeIncomparable? other)
        {
            if (CompareTo(other) == 0)
                return true;

            return false;
        }

        public override bool Equals(object? other)
        {
            return Equals(other as TestTypeIncomparable);
        }

        public override int GetHashCode()
        {
            return HashCode;
        }

        public TestTypeIncomparable(int value)
        {
            Value = value;
            ValueHash = value % 3;
            HashCode = value % HashFactor;
        }

        public static implicit operator TestTypeIncomparable(int value) => new(value);
    }

    public class TestComparer : IComparer<TestType>
    {
        private readonly bool _byHash;

        public static TestComparer ByValue = new(false);
        public static TestComparer ByHash = new(true);

        private TestComparer(bool byHash) { _byHash = byHash; }
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
        public int Compare(TestType? x, TestType? y)
        {
            return _byHash ? CompareByHashMirror(x, y) : CompareByValue(x, y);
        }

        public int CompareByValue(TestType? x, TestType? y)
        {
            if (x == null || y == null)
                return Comparer<TestType>.Default.Compare(x, y);

            return Comparer<int>.Default.Compare(x.Value, y.Value);
        }

        public int CompareByHashMirror(TestType? x, TestType? y)
        {
            if (x == null || y == null)
                return Comparer<TestType>.Default.Compare(x, y);

            return Comparer<int>.Default.Compare(x.ValueHash, y.ValueHash);
        }

        public override bool Equals(object? obj)
        {
            if (obj is TestComparer other)
            {
                return (_byHash == other._byHash);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_byHash);
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
