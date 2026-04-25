using BinaryCollections.BinaryList;

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
        BinaryList<ReferenceTestType> PrepareList(ReferenceTestType[] initialList, ReferenceTestType[] toRemove)
        {
            BinaryList<ReferenceTestType> tempList = new();
            tempList.AddRange(initialList);
            tempList.AddRange(toRemove);

            BinaryList<ReferenceTestType> list = new();
            list.AddRange(tempList);
            list.AddRange(tempList);
            list.AddRange(tempList);
            list.Trim();

            foreach (var i in toRemove)
                list.RemoveRange(i);

            return list;
        }

        ReferenceTestType[] initialList = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        ReferenceTestType[] toTestMergingFromLeft = { -3, -2, -1 };
        ReferenceTestType[] toTestMergingFromRight = { 10, 11, 12 };
        ReferenceTestType[] toTestShiftRightPart = { 7, 7, 7 };
        ReferenceTestType[] toTestShiftLeftPart = { 2, 2, 2 };

        var list1 = PrepareList(initialList, toTestMergingFromLeft);
        list1.AddRangeUnique(toTestShiftRightPart);

        var list2 = PrepareList(initialList, toTestMergingFromRight);
        list2.AddRangeUnique(toTestShiftRightPart);

        var list3 = PrepareList(initialList, toTestMergingFromLeft);
        list3.AddRangeUnique(toTestShiftLeftPart);

        var list4 = PrepareList(initialList, toTestMergingFromRight);
        list4.AddRangeUnique(toTestShiftLeftPart);
    }
}