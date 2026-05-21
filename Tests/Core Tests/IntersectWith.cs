using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        public void IntersectWith_NoCollisions_Comparable()
        {
            BinaryList<TestTypeComparable> PrepareList(TestTypeComparable[] initialList, TestTypeComparable[] toRemove, TestTypeComparable[] toAdd)
            {
                BinaryList<TestTypeComparable> tempList = new(containCompareCollisions: false);
                tempList.AddRange(initialList);
                tempList.AddRange(toRemove);

                BinaryList<TestTypeComparable> list = new(containCompareCollisions: false);
                list.AddRange(tempList);
                list.AddRange(tempList);
                list.Trim();

                foreach (var i in toRemove)
                    list.RemoveRange(i);

                list.IntersectWith(toAdd);
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

            TestTypeComparable[] result1 = [5, 7, 7, 8, 8];
            TestTypeComparable[] result2 = [1, 1, 2, 2, 4];

            TestTypeComparable[] rawResult1 = [null, 5, 7, 7, 8, 8, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null];
            TestTypeComparable[] rawResult2 = [null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 5, 7, 7, 8, 8, null];
            TestTypeComparable[] rawResult3 = [null, 1, 1, 2, 2, 4, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null];
            TestTypeComparable[] rawResult4 = [null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 1, 1, 2, 2, 4, null];

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
        public void IntersectWith_Collisions_Comparable()
        {
            BinaryList<TestTypeComparable> PrepareList(TestTypeComparable[] initialList, TestTypeComparable[] toRemove, TestTypeComparable[] toAdd)
            {
                BinaryList<TestTypeComparable> tempList = new(containCompareCollisions: true);
                tempList.AddRange(initialList);
                tempList.AddRange(toRemove);

                BinaryList<TestTypeComparable> list = new(containCompareCollisions: true);
                list.AddRange(tempList);
                list.AddRange(tempList);
                list.Trim();

                foreach (var i in toRemove)
                    list.RemoveRange(i);

                list.IntersectWith(toAdd);
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

            TestTypeComparable[] result1 = [5, 7, 7, 8, 8];
            TestTypeComparable[] result2 = [1, 1, 2, 2, 4];

            TestTypeComparable[] rawResult1 = [null, 5, 7, 7, 8, 8, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null];
            TestTypeComparable[] rawResult2 = [null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 5, 7, 7, 8, 8, null];
            TestTypeComparable[] rawResult3 = [null, 1, 1, 2, 2, 4, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null];
            TestTypeComparable[] rawResult4 = [null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 1, 1, 2, 2, 4, null];

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
        public void IntersectWith_Collisions_InComparable()
        {
            BinaryList<TestType> PrepareList(TestType[] initialList, TestType[] toRemove, TestType[] toAdd)
            {
                BinaryList<TestType> tempList = new(containCompareCollisions: true);
                tempList.AddRange(initialList);
                tempList.AddRange(toRemove);

                BinaryList<TestType> list = new(containCompareCollisions: true);
                list.AddRange(tempList);
                list.AddRange(tempList);
                list.Trim();

                foreach (var i in toRemove)
                    list.RemoveRange(i);

                list.IntersectWith(toAdd);
                return list;
            }

            TestType[] initialList = { 0, 1, 2, 4, 5, 7, 8, 9 };
            TestType[] toTestMergingFromLeft = { -4, -3, -2, -1 };
            TestType[] toTestMergingFromRight = { 10, 11, 12, 13 };
            TestType[] toTestShiftRightPart = { 8, 8, 7, 7, 7, 6, 5 };
            TestType[] toTestShiftLeftPart = { 1, 1, 2, 2, 2, 3, 4 };

            var list1 = PrepareList(initialList, toTestMergingFromLeft, toTestShiftRightPart);
            var list2 = PrepareList(initialList, toTestMergingFromRight, toTestShiftRightPart);
            var list3 = PrepareList(initialList, toTestMergingFromLeft, toTestShiftLeftPart);
            var list4 = PrepareList(initialList, toTestMergingFromRight, toTestShiftLeftPart);

            TestType[] result1 = [5, 7, 7, 8, 8];
            TestType[] result2 = [1, 1, 2, 2, 4];

            TestType[] rawResult1 = [null, 5, 7, 7, 8, 8, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null];
            TestType[] rawResult2 = [null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 5, 7, 7, 8, 8, null];
            TestType[] rawResult3 = [null, 1, 1, 2, 2, 4, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null];
            TestType[] rawResult4 = [null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 1, 1, 2, 2, 4, null];

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