using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    [TestClass]
    public class API_Tests
    {      

        [TestMethod]
        public void Remove()
        {
            int[] initiateList = { 10, 20 };
            int[] removeResult = { 20 };
            var list = new BinaryList<int>(initiateList);
            var version = list.Version;

            bool result = list.Remove(99);

            Assert.IsFalse(result);
            Assert.AreEqual(version, list.Version);
            CollectionAssert.AreEqual(list.ToArray(), initiateList);

            result = list.Remove(10);

            Assert.IsTrue(result);
            Assert.AreNotEqual(version, list.Version);
            CollectionAssert.AreEqual(list.ToArray(), removeResult);
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