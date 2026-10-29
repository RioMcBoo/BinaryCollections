using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        public void IntersectWithUnique()
        {
            BinaryList<TestTypeComparable> PrepareList(TestTypeComparable[] initialList, TestTypeComparable[] toRemove, TestTypeComparable[] toAdd)
            {
                BinaryList<TestTypeComparable> tempList = new();
                tempList.AddRange(initialList);
                tempList.AddRange(toRemove);

                BinaryList<TestTypeComparable> list = new();
                list.AddRange(tempList);
                list.AddRange(tempList);
                list.Trim();

                foreach (var i in toRemove)
                    list.RemoveRange(i);

                list.IntersectWithUnique(toAdd);
                return list;
            }

            TestTypeComparable[] initialList = { 0, 1, 2, 4, 5, 7, 8, 9 };
            TestTypeComparable[] toTestMergingFromLeft = { -4, -3, -2, -1 };
            TestTypeComparable[] toTestMergingFromRight = { 10, 11, 12, 13 };
            TestTypeComparable[] toTestShiftRightPart = { 8, 8, 7, 7, 7, 6, 5 };
            TestTypeComparable[] toTestShiftLeftPart = { 1, 1, 2, 2, 2, 3, 4 };

            var list1 = PrepareList(initialList, toTestMergingFromLeft, toTestShiftRightPart);
            var list2 = PrepareList(initialList, toTestMergingFromRight, toTestShiftRightPart);
            var list3 = PrepareList(initialList, toTestMergingFromLeft, toTestShiftLeftPart);
            var list4 = PrepareList(initialList, toTestMergingFromRight, toTestShiftLeftPart);

            TestTypeComparable[] result1 = [5, 7, 8];
            TestTypeComparable[] result2 = [1, 2, 4];

            TestTypeComparable[] rawResult1 = [null, 5, 7, 8, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null];
            TestTypeComparable[] rawResult2 = [null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 5, 7, 8, null];
            TestTypeComparable[] rawResult3 = [null, 1, 2, 4, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null];
            TestTypeComparable[] rawResult4 = [null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 1, 2, 4, null];

            CollectionAssert.AreEqual(result1, list1.ToArray());
            CollectionAssert.AreEqual(result1, list2.ToArray());
            CollectionAssert.AreEqual(result2, list3.ToArray());
            CollectionAssert.AreEqual(result2, list4.ToArray());

            CollectionAssert.AreEqual(rawResult1, list1.ExtractRawArray());
            CollectionAssert.AreEqual(rawResult2, list2.ExtractRawArray());
            CollectionAssert.AreEqual(rawResult3, list3.ExtractRawArray());
            CollectionAssert.AreEqual(rawResult4, list4.ExtractRawArray());
        }
    }
}