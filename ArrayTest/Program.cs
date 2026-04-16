using CustomCollections.BinaryList;

internal class Program
{
    record class ReferenceTestType
    {
        public int Value { get; init; }
        public static implicit operator ReferenceTestType(int value) => new() { Value = value };
    }

    record class ReferenceTestTypeComparable : IComparable<ReferenceTestTypeComparable>
    {
        public int Value { get; init; }

        public int CompareTo(ReferenceTestTypeComparable? other)
        {
            if (other is null)
                return 1;

            return Value.CompareTo(other.Value);
        }

        public static implicit operator ReferenceTestTypeComparable(int value) => new() { Value = value };
    }

    static void Main(string[] args)
    {
        int initialCapacity = 18;
        ReferenceTestTypeComparable[] values = { 1, 2, 3, 4, 5, 6, 10, 8, 9, 7 };
        ReferenceTestTypeComparable[] expected = { 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 0, 0, 0, 0 };

        var list = new BinaryList<ReferenceTestTypeComparable>(initialCapacity);
        foreach (var v in values) list.Add(v);
    }
}