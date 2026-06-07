using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        public void ExceptWith_Comparable()
        {
            BinaryList<TestTypeBinary> PrepareList(TestTypeBinary[] initialList, TestTypeBinary[] toRemove, TestTypeBinary[] toAdd)
            {
                BinaryList<TestTypeBinary> tempList = new();
                tempList.AddRange(initialList);
                tempList.AddRange(toRemove);

                BinaryList<TestTypeBinary> list = new();
                list.AddRange(tempList);
                list.AddRange(tempList);
                list.Trim();

                foreach (var i in toRemove)
                    list.RemoveRange(i);

                list.ExceptWith(toAdd);
                return list;
            }

            TestTypeBinary[] initialList = { 0, 1, 2, 4, 5, 7, 8, 9 };
            TestTypeBinary[] toTestMergingFromLeft = { -4, -3, -2, -1 };
            TestTypeBinary[] toTestMergingFromRight = { 10, 11, 12, 13 };
            TestTypeBinary[] toTestShiftRightPart = { 8, 8, 7, 7, 7, 6 };
            TestTypeBinary[] toTestShiftLeftPart = { 1, 1, 2, 2, 2, 3 };

            var list1 = PrepareList(initialList, toTestMergingFromLeft, toTestShiftRightPart);
            var list2 = PrepareList(initialList, toTestMergingFromRight, toTestShiftRightPart);
            var list3 = PrepareList(initialList, toTestMergingFromLeft, toTestShiftLeftPart);
            var list4 = PrepareList(initialList, toTestMergingFromRight, toTestShiftLeftPart);

            TestTypeBinary[] result1 = [0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 9, 9];
            TestTypeBinary[] result2 = [0, 0, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9];

            TestTypeBinary[] rawResult1 = [null, null, 0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 9, 9, null, null, null, null, null, null, null, null, null, null];
            TestTypeBinary[] rawResult2 = [0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 9, 9, null, null, null, null, null, null, null, null, null, null, null, null];
            TestTypeBinary[] rawResult3 = [null, null, null, null, null, null, null, null, null, null, null, null, 0, 0, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9];
            TestTypeBinary[] rawResult4 = [null, null, null, null, null, null, null, null, null, null, 0, 0, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9, null, null];

            CollectionAssert.AreEqual(result1, list1.ToArray());
            CollectionAssert.AreEqual(result1, list2.ToArray());
            CollectionAssert.AreEqual(result2, list3.ToArray());
            CollectionAssert.AreEqual(result2, list4.ToArray());

            CollectionAssert.AreEqual(rawResult1, list1.ExtractRawArray());
            CollectionAssert.AreEqual(rawResult2, list2.ExtractRawArray());
            CollectionAssert.AreEqual(rawResult3, list3.ExtractRawArray());
            CollectionAssert.AreEqual(rawResult4, list4.ExtractRawArray());
        }

        [TestMethod]
        public void ExceptWith_InComparable()
        {
            BinaryList<TestTypeHash> PrepareList(TestTypeHash[] initialList, TestTypeHash[] toRemove, TestTypeHash[] toAdd)
            {
                BinaryList<TestTypeHash> tempList = new();
                tempList.AddRange(initialList);
                tempList.AddRange(toRemove);

                BinaryList<TestTypeHash> list = new();
                list.AddRange(tempList);
                list.AddRange(tempList);
                list.Trim();

                foreach (var i in toRemove)
                    list.RemoveRange(i);

                list.ExceptWith(toAdd);
                return list;
            }

            TestTypeHash[] initialList = { 0, 1, 2, 4, 5, 7, 8, 9 };
            TestTypeHash[] toTestMergingFromLeft = { -4, -3, -2, -1 };
            TestTypeHash[] toTestMergingFromRight = { 10, 11, 12, 13 };
            TestTypeHash[] toTestShiftRightPart = { 8, 8, 7, 7, 7, 6 };
            TestTypeHash[] toTestShiftLeftPart = { 1, 1, 2, 2, 2, 3 };

            var list1 = PrepareList(initialList, toTestMergingFromLeft, toTestShiftRightPart);
            var list2 = PrepareList(initialList, toTestMergingFromRight, toTestShiftRightPart);
            var list3 = PrepareList(initialList, toTestMergingFromLeft, toTestShiftLeftPart);
            var list4 = PrepareList(initialList, toTestMergingFromRight, toTestShiftLeftPart);

            TestTypeHash[] result1 = [0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 9, 9];
            TestTypeHash[] result2 = [0, 0, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9];

            TestTypeHash[] rawResult1 = [null, null, 0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 9, 9, null, null, null, null, null, null, null, null, null, null];
            TestTypeHash[] rawResult2 = [0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 9, 9, null, null, null, null, null, null, null, null, null, null, null, null];
            TestTypeHash[] rawResult3 = [null, null, null, null, null, null, null, null, null, null, null, null, 0, 0, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9];
            TestTypeHash[] rawResult4 = [null, null, null, null, null, null, null, null, null, null, 0, 0, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9, null, null];

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