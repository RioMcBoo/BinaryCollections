using BinaryCollections.BinaryList;
using Newtonsoft.Json.Linq;

namespace BinaryList.Tests
{
    [TestClass]
    public class CORE_Tests
    {
        [TestMethod]
        public void ShiftArrayLeft_Method()
        {
            int initialCapacity = 18;
            TestTypeComparable[] values = { 1, 2, 3, 4, 5, 6, 10, 8, 9, 7 };
            TestTypeComparable[] expected = { null, null, null, null, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, null, null, null, null };

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
            TestTypeComparable[] expected = { null, null, null, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, null, null, null };

            var list = new BinaryList<TestTypeComparable>(initialCapacity);
            foreach (var v in values) list.Add(v);

            Assert.AreEqual(list.Count, values.Length);
            Assert.AreEqual(list.Capacity, expected.Length);
            CollectionAssert.AreEqual(expected, list.ExtractRawArray());
        }

        [TestMethod]
        public void AddRangeUnique_NoCollisions_Comparable()
        {
            BinaryList<TestTypeComparable> PrepareList(TestTypeComparable[] initialList, TestTypeComparable[] toRemove, TestTypeComparable[] toAdd)
            {
                BinaryList<TestTypeComparable> tempList = new(mayContainCollisions: false);
                tempList.AddRange(initialList);
                tempList.AddRange(toRemove);

                BinaryList<TestTypeComparable> list = new(mayContainCollisions: false);
                list.AddRange(tempList);
                list.AddRange(tempList);
                list.Trim();

                foreach (var i in toRemove)
                    list.RemoveRange(i);

                list.AddRangeUnique(toAdd);
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

            TestTypeComparable[] result1 = [0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 7, 8, 8, 9, 9];
            TestTypeComparable[] result2 = [0, 0, 1, 1, 2, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9];

            TestTypeComparable[] rawResult1 = [null, null, 0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 7, 8, 8, 9, 9, null, null, null, null, null];
            TestTypeComparable[] rawResult2 = [0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 7, 8, 8, 9, 9, null, null, null, null, null, null, null];
            TestTypeComparable[] rawResult3 = [null, null, null, null, null, null, null, 0, 0, 1, 1, 2, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9];
            TestTypeComparable[] rawResult4 = [null, null, null, null, null, 0, 0, 1, 1, 2, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9, null, null];

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
        public void AddRangeUnique_Collisions_Comparable()
        {
            BinaryList<TestTypeComparable> PrepareList(TestTypeComparable[] initialList, TestTypeComparable[] toRemove, TestTypeComparable[] toAdd)
            {
                BinaryList<TestTypeComparable> tempList = new(mayContainCollisions: true);
                tempList.AddRange(initialList);
                tempList.AddRange(toRemove);

                BinaryList<TestTypeComparable> list = new(mayContainCollisions: true);
                list.AddRange(tempList);
                list.AddRange(tempList);
                list.Trim();

                foreach (var i in toRemove)
                    list.RemoveRange(i);

                list.AddRangeUnique(toAdd);
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

            TestTypeComparable[] result1 = [0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 7, 8, 8, 9, 9];
            TestTypeComparable[] result2 = [0, 0, 1, 1, 2, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9];

            TestTypeComparable[] rawResult1 = [null, null, 0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 7, 8, 8, 9, 9, null, null, null, null, null];
            TestTypeComparable[] rawResult2 = [0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 7, 8, 8, 9, 9, null, null, null, null, null, null, null];
            TestTypeComparable[] rawResult3 = [null, null, null, null, null, null, null, 0, 0, 1, 1, 2, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9];
            TestTypeComparable[] rawResult4 = [null, null, null, null, null, 0, 0, 1, 1, 2, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9, null, null];

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
        public void AddRangeUnique_Collisions_InComparable()
        {
            BinaryList<TestType> PrepareList(TestType[] initialList, TestType[] toRemove, TestType[] toAdd)
            {
                BinaryList<TestType> tempList = new(mayContainCollisions: true);
                tempList.AddRange(initialList);
                tempList.AddRange(toRemove);

                BinaryList<TestType> list = new(mayContainCollisions: true);
                list.AddRange(tempList);
                list.AddRange(tempList);
                list.Trim();

                foreach (var i in toRemove)
                    list.RemoveRange(i);

                list.AddRangeUnique(toAdd);
                return list;
            }

            TestType[] initialList = { 0, 1, 2, 4, 5, 7, 8, 9 };
            TestType[] toTestMergingFromLeft = { -4, -3, -2, -1 };
            TestType[] toTestMergingFromRight = { 10, 11, 12, 13 };
            TestType[] toTestShiftRightPart = { 8, 8, 7, 7, 7, 6 };
            TestType[] toTestShiftLeftPart = { 1, 1, 2, 2, 2, 3 };

            var list1 = PrepareList(initialList, toTestMergingFromLeft, toTestShiftRightPart);
            var list2 = PrepareList(initialList, toTestMergingFromRight, toTestShiftRightPart);
            var list3 = PrepareList(initialList, toTestMergingFromLeft, toTestShiftLeftPart);
            var list4 = PrepareList(initialList, toTestMergingFromRight, toTestShiftLeftPart);

            TestType[] result1 = [0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 7, 8, 8, 9, 9];
            TestType[] result2 = [0, 0, 1, 1, 2, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9];

            TestType[] rawResult1 = [null, null, 0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 7, 8, 8, 9, 9, null, null, null, null, null];
            TestType[] rawResult2 = [0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 7, 8, 8, 9, 9, null, null, null, null, null, null, null];
            TestType[] rawResult3 = [null, null, null, null, null, null, null, 0, 0, 1, 1, 2, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9];
            TestType[] rawResult4 = [null, null, null, null, null, 0, 0, 1, 1, 2, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9, null, null];

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
        public void UnionWith_NoCollisions_Comparable()
        {
            BinaryList<TestTypeComparable> PrepareList(TestTypeComparable[] initialList, TestTypeComparable[] toRemove, TestTypeComparable[] toAdd)
            {
                BinaryList<TestTypeComparable> tempList = new(mayContainCollisions: false);
                tempList.AddRange(initialList);
                tempList.AddRange(toRemove);

                BinaryList<TestTypeComparable> list = new(mayContainCollisions: false);
                list.AddRange(tempList);
                list.AddRange(tempList);
                list.Trim();

                foreach (var i in toRemove)
                    list.RemoveRange(i);

                list.UnionWith(toAdd);
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

            TestTypeComparable[] result1 = [0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 7, 7, 8, 8, 9, 9];
            TestTypeComparable[] result2 = [0, 0, 1, 1, 2, 2, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9];

            TestTypeComparable[] rawResult1 = [null, null, 0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 7, 7, 8, 8, 9, 9, null, null, null, null];
            TestTypeComparable[] rawResult2 = [0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 7, 7, 8, 8, 9, 9, null, null, null, null, null, null];
            TestTypeComparable[] rawResult3 = [null, null, null, null, null, null, 0, 0, 1, 1, 2, 2, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9];
            TestTypeComparable[] rawResult4 = [null, null, null, null, 0, 0, 1, 1, 2, 2, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9, null, null];

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
        public void UnionWith_Collisions_Comparable()
        {
            BinaryList<TestTypeComparable> PrepareList(TestTypeComparable[] initialList, TestTypeComparable[] toRemove, TestTypeComparable[] toAdd)
            {
                BinaryList<TestTypeComparable> tempList = new(mayContainCollisions: true);
                tempList.AddRange(initialList);
                tempList.AddRange(toRemove);

                BinaryList<TestTypeComparable> list = new(mayContainCollisions: true);
                list.AddRange(tempList);
                list.AddRange(tempList);
                list.Trim();

                foreach (var i in toRemove)
                    list.RemoveRange(i);

                list.UnionWith(toAdd);
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

            TestTypeComparable[] result1 = [0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 7, 7, 8, 8, 9, 9];
            TestTypeComparable[] result2 = [0, 0, 1, 1, 2, 2, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9];

            TestTypeComparable[] rawResult1 = [null, null, 0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 7, 7, 8, 8, 9, 9, null, null, null, null];
            TestTypeComparable[] rawResult2 = [0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 7, 7, 8, 8, 9, 9, null, null, null, null, null, null];
            TestTypeComparable[] rawResult3 = [null, null, null, null, null, null, 0, 0, 1, 1, 2, 2, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9];
            TestTypeComparable[] rawResult4 = [null, null, null, null, 0, 0, 1, 1, 2, 2, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9, null, null];

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
        public void UnionWith_Collisions_InComparable()
        {
            BinaryList<TestType> PrepareList(TestType[] initialList, TestType[] toRemove, TestType[] toAdd)
            {
                BinaryList<TestType> tempList = new(mayContainCollisions: true);
                tempList.AddRange(initialList);
                tempList.AddRange(toRemove);

                BinaryList<TestType> list = new(mayContainCollisions: true);
                list.AddRange(tempList);
                list.AddRange(tempList);
                list.Trim();

                foreach (var i in toRemove)
                    list.RemoveRange(i);

                list.UnionWith(toAdd);
                return list;
            }

            TestType[] initialList = { 0, 1, 2, 4, 5, 7, 8, 9 };
            TestType[] toTestMergingFromLeft = { -4, -3, -2, -1 };
            TestType[] toTestMergingFromRight = { 10, 11, 12, 13 };
            TestType[] toTestShiftRightPart = { 8, 8, 7, 7, 7, 6 };
            TestType[] toTestShiftLeftPart = { 1, 1, 2, 2, 2, 3 };

            var list1 = PrepareList(initialList, toTestMergingFromLeft, toTestShiftRightPart);
            var list2 = PrepareList(initialList, toTestMergingFromRight, toTestShiftRightPart);
            var list3 = PrepareList(initialList, toTestMergingFromLeft, toTestShiftLeftPart);
            var list4 = PrepareList(initialList, toTestMergingFromRight, toTestShiftLeftPart);

            TestType[] result1 = [0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 7, 7, 8, 8, 9, 9];
            TestType[] result2 = [0, 0, 1, 1, 2, 2, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9];

            TestType[] rawResult1 = [null, null, 0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 7, 7, 8, 8, 9, 9, null, null, null, null];
            TestType[] rawResult2 = [0, 0, 1, 1, 2, 2, 4, 4, 5, 5, 6, 7, 7, 7, 8, 8, 9, 9, null, null, null, null, null, null];
            TestType[] rawResult3 = [null, null, null, null, null, null, 0, 0, 1, 1, 2, 2, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9];
            TestType[] rawResult4 = [null, null, null, null, 0, 0, 1, 1, 2, 2, 2, 3, 4, 4, 5, 5, 7, 7, 8, 8, 9, 9, null, null];

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
        public void UnionWith_NoCollisions_Comparable_Unique()
        {
            BinaryList<TestTypeComparable> PrepareList(TestTypeComparable[] initialList, TestTypeComparable[] toRemove, TestTypeComparable[] toAdd)
            {
                BinaryList<TestTypeComparable> tempList = new(mayContainCollisions: false);
                tempList.AddRange(initialList);
                tempList.AddRange(toRemove);

                BinaryList<TestTypeComparable> list = new(mayContainCollisions: false);
                list.AddRange(tempList);
                list.AddRange(tempList);
                list.Trim();

                foreach (var i in toRemove)
                    list.RemoveRange(i);

                list.UnionWithUnique(toAdd);
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

            TestTypeComparable[] result1 = [0, 1, 2, 4, 5, 6, 7, 8, 9];
            TestTypeComparable[] result2 = [0, 1, 2, 3, 4, 5, 7, 8, 9];

            TestTypeComparable[] rawResult1 = [null, null, 0, 1, 2, 4, 5, 6, 7, 8, 9, null, null, null, null, null, null, null, null, null, null, null, null, null];
            TestTypeComparable[] rawResult2 = [null, null, null, null, null, null, null, null, null, null, null, null, null, 0, 1, 2, 4, 5, 6, 7, 8, 9, null, null];
            TestTypeComparable[] rawResult3 = [null, null, 0, 1, 2, 3, 4, 5, 7, 8, 9, null, null, null, null, null, null, null, null, null, null, null, null, null];
            TestTypeComparable[] rawResult4 = [null, null, null, null, null, null, null, null, null, null, null, null, null, 0, 1, 2, 3, 4, 5, 7, 8, 9, null, null];

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
        public void UnionWith_Collisions_Comparable_Unique()
        {
            BinaryList<TestTypeComparable> PrepareList(TestTypeComparable[] initialList, TestTypeComparable[] toRemove, TestTypeComparable[] toAdd)
            {
                BinaryList<TestTypeComparable> tempList = new(mayContainCollisions: true);
                tempList.AddRange(initialList);
                tempList.AddRange(toRemove);

                BinaryList<TestTypeComparable> list = new(mayContainCollisions: true);
                list.AddRange(tempList);
                list.AddRange(tempList);
                list.Trim();

                foreach (var i in toRemove)
                    list.RemoveRange(i);

                list.UnionWithUnique(toAdd);
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

            TestTypeComparable[] result1 = [0, 1, 2, 4, 5, 6, 7, 8, 9];
            TestTypeComparable[] result2 = [0, 1, 2, 3, 4, 5, 7, 8, 9];

            TestTypeComparable[] rawResult1 = [null, null, 0, 1, 2, 4, 5, 6, 7, 8, 9, null, null, null, null, null, null, null, null, null, null, null, null, null];
            TestTypeComparable[] rawResult2 = [null, null, null, null, null, null, null, null, null, null, null, null, null, 0, 1, 2, 4, 5, 6, 7, 8, 9, null, null];
            TestTypeComparable[] rawResult3 = [null, null, 0, 1, 2, 3, 4, 5, 7, 8, 9, null, null, null, null, null, null, null, null, null, null, null, null, null];
            TestTypeComparable[] rawResult4 = [null, null, null, null, null, null, null, null, null, null, null, null, null, 0, 1, 2, 3, 4, 5, 7, 8, 9, null, null];

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
        public void UnionWith_Collisions_InComparable_Unique()
        {
            BinaryList<TestType> PrepareList(TestType[] initialList, TestType[] toRemove, TestType[] toAdd)
            {
                BinaryList<TestType> tempList = new(mayContainCollisions: true);
                tempList.AddRange(initialList);
                tempList.AddRange(toRemove);

                BinaryList<TestType> list = new(mayContainCollisions: true);
                list.AddRange(tempList);
                list.AddRange(tempList);
                list.Trim();

                foreach (var i in toRemove)
                    list.RemoveRange(i);

                list.UnionWithUnique(toAdd);
                return list;
            }

            TestType[] initialList = { 0, 1, 2, 4, 5, 7, 8, 9 };
            TestType[] toTestMergingFromLeft = { -4, -3, -2, -1 };
            TestType[] toTestMergingFromRight = { 10, 11, 12, 13 };
            TestType[] toTestShiftRightPart = { 8, 8, 7, 7, 7, 6 };
            TestType[] toTestShiftLeftPart = { 1, 1, 2, 2, 2, 3 };

            var list1 = PrepareList(initialList, toTestMergingFromLeft, toTestShiftRightPart);
            var list2 = PrepareList(initialList, toTestMergingFromRight, toTestShiftRightPart);
            var list3 = PrepareList(initialList, toTestMergingFromLeft, toTestShiftLeftPart);
            var list4 = PrepareList(initialList, toTestMergingFromRight, toTestShiftLeftPart);

            TestType[] result1 = [0, 1, 2, 4, 5, 6, 7, 8, 9];
            TestType[] result2 = [0, 1, 2, 3, 4, 5, 7, 8, 9];

            TestType[] rawResult1 = [null, null, 0, 1, 2, 4, 5, 6, 7, 8, 9, null, null, null, null, null, null, null, null, null, null, null, null, null];
            TestType[] rawResult2 = [null, null, null, null, null, null, null, null, null, null, null, null, null, 0, 1, 2, 4, 5, 6, 7, 8, 9, null, null];
            TestType[] rawResult3 = [null, null, 0, 1, 2, 3, 4, 5, 7, 8, 9, null, null, null, null, null, null, null, null, null, null, null, null, null];
            TestType[] rawResult4 = [null, null, null, null, null, null, null, null, null, null, null, null, null, 0, 1, 2, 3, 4, 5, 7, 8, 9, null, null];

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