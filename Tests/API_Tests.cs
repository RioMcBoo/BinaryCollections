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

        [TestMethod]
        public void Comparer_Import_Test()
        {
            List<TestTypeComparable> list = new();
            BinaryList<TestTypeComparable> binaryList = new(list, TestComparer.WithCollisions);

            Random random = new Random(0);
            for (int i = 0; i < 100000; i++)
            {
                TestTypeComparable item = random.Next(0, 3000);
                list.Add(item);
                binaryList.Add(item);
            }

            list.Sort(binaryList.Comparer);
            CollectionAssert.AreEqual(list, binaryList.ToArray());
        }
    }
}