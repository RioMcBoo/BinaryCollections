using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class API_Tests
    {
        [TestMethod]
        public void Comparer_Import_Test()
        {
            List<TestTypeComparable> list = new();
            BinaryList<TestTypeComparable> binaryList = new(list, TestComparer.ByHash);

            Random random = new Random(0);
            for (int i = 0; i < 100000; i++)
            {
                TestTypeComparable item = random.Next(0, 3000);
                list.Add(item);
                binaryList.Add(item);
            }

            list.Sort(TestComparer.ByHash);
            CollectionAssert.AreNotEqual(list, binaryList.ToArray());
            list.Sort(binaryList.Comparer);
            CollectionAssert.AreEqual(list, binaryList.ToArray());
        }
    }
}