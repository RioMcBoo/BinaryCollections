using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    [TestClass]
    public partial class CORE_Tests
    {
        [TestMethod]
        public void ShiftArrayLeft_Method()
        {
            int initialCapacity = 18;
            TestTypeComparable[] values = { 1, 2, 3, 4, 5, 6, 10, 8, 9, 7 };
            TestTypeComparable[] expected = { null, null, null, null, null, null, null, null, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10};

            var list = new BinaryList<TestTypeComparable>(initialCapacity);
            foreach (var v in values) list.Add(v);

            Assert.AreEqual(list.Count, values.Length);
            Assert.AreEqual(list.Capacity, expected.Length);
            CollectionAssert.AreEqual(expected, list.ExtractRawArray());
        }

        [TestMethod]
        public void ShiftArrayRight_Method()
        {
            int initialCapacity = 16;
            TestTypeComparable[] values = { 10, 9, 8, 7, 5, 6, 1, 3, 2, 4 };
            TestTypeComparable[] expected = { null, null, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, null, null, null, null };

            var list = new BinaryList<TestTypeComparable>(initialCapacity);
            foreach (var v in values) list.Add(v);

            Assert.AreEqual(list.Count, values.Length);
            Assert.AreEqual(list.Capacity, expected.Length);
            CollectionAssert.AreEqual(expected, list.ExtractRawArray());
        }
    }
}