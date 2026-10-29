using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    [TestClass]
    public partial class API_Tests
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
    }
}