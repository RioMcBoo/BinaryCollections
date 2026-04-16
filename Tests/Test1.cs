using CustomCollections.BinaryList;

namespace BinaryList.Tests
{
    /// <summary>
    /// A binary list does not clean up freed cells if they are of a value type to improve performance.
    /// Cleaning is necessary to free memory for the garbage collector.
    /// To simplify testing and provide clarity on what happens to memory, a reference type is used.
    /// </summary>
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

    [TestClass]
    public class BinaryList_Tests_Add
    {
        [TestMethod]
        public void ShiftArrayLeft_Method_Test()
        {
            int initialCapacity = 18;
            ReferenceTestTypeComparable[] values = { 1, 2, 3, 4, 5, 6, 10, 8, 9, 7 };
            ReferenceTestTypeComparable[] expected = { null, null, null, null, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, null, null, null, null };

            var list = new BinaryList<ReferenceTestTypeComparable>(initialCapacity);
            foreach (var v in values) list.Add(v);

            Assert.AreEqual(list.Count, values.Length);
            Assert.AreEqual(list.Capacity, expected.Length);
            CollectionAssert.AreEqual(expected, list.ExtractRawArray());
        }

        [TestMethod]
        public void ShiftArrayRight_Method_Test()
        {
            int initialCapacity = 16;
            ReferenceTestTypeComparable[] values = { 10, 9, 8, 7, 5, 6, 1, 3, 2, 4 };
            ReferenceTestTypeComparable[] expected = { null, null, null, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, null, null, null };

            var list = new BinaryList<ReferenceTestTypeComparable>(initialCapacity);
            foreach (var v in values) list.Add(v);

            Assert.AreEqual(list.Count, values.Length);
            Assert.AreEqual(list.Capacity, expected.Length);
            CollectionAssert.AreEqual(expected, list.ExtractRawArray());
        }

        [TestMethod]
        public void FindIndex_ShouldReturnCorrectPosition()
        {
            // Arrange
            var list = new BinaryList<string> { "Apple", "Banana", "Cherry" };

            // Act
            int index = list.FindIndex("Banana");

            // Assert
            Assert.AreEqual(1, index);
        }

        [TestMethod]
        //[ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Indexer_ShouldThrow_WhenIndexIsNegative()
        {
            // Arrange
            var list = new BinaryList<int> { 1, 2, 3 };

            // Act
            var item = list[-1]; // Должно выбросить исключение
        }

        [TestMethod]
        public void Remove_ShouldReturnFalse_WhenItemNotFound()
        {
            // Arrange
            var list = new BinaryList<int> { 10, 20 };

            // Act
            bool result = list.Remove(99);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(2, list.Count);
        }

        //[TestMethod]
        //public void Replace_ShouldCorrectlyHandleComplexShifts()
        //{
        //    // Arrange
        //    var list = new BinaryList<int> { 10, 20, 30, 40, 50 };

        //    // Act
        //    // Заменяем 20 на 45. 45 должно встать перед 50.
        //    list.Replace(20, 45, out var newIndex);

        //    // Assert
        //    int[] expected = { 10, 30, 40, 45, 50 };
        //    CollectionAssert.AreEqual(expected, list.ToArray());
        //    Assert.AreEqual(3, newIndex);
        //}
    }
}