using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        public void SymmetricExceptWith()
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

                list.SymmetricExceptWith(toAdd);
                return list;
            }

            TestTypeComparable[] initialList = { 0, 1, 2, 4, 5, 7, 8, 9 };
            TestTypeComparable[] toTestMergingFromLeft = { -4, -3, -2, -1 };
            TestTypeComparable[] toTestMergingFromRight = { 10, 11, 12, 13 };
            TestTypeComparable[] toTestShiftRightPart = { 8, 8, 7, 7, 7, 6 };
            TestTypeComparable[] toTestShiftLeftPart = { 1, 1, 2, 2, 2, 3 };

            var list1 = PrepareList(initialList, toTestMergingFromLeft, toTestShiftRightPart);
            var list2 = PrepareList(initialList, toTestMergingFromRight, toTestShiftRightPart);
            var list3 = PrepareList(initialList, toTestMergingFromLeft, toTestShiftLeftPart);
            var list4 = PrepareList(initialList, toTestMergingFromRight, toTestShiftLeftPart);

            TestTypeComparable[] result1 = [0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 9, 9];
            TestTypeComparable[] result2 = [0, 0, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9];

            TestTypeComparable[] rawResult1 = [null, null, 0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 9, 9, null, null, null, null, null, null, null, null];
            TestTypeComparable[] rawResult2 = [0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 9, 9, null, null, null, null, null, null, null, null, null, null];
            TestTypeComparable[] rawResult3 = [null, null, null, null, null, null, null, null, null, null, 0, 0, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9];
            TestTypeComparable[] rawResult4 = [null, null, null, null, null, null, null, null, 0, 0, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9, null, null];

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