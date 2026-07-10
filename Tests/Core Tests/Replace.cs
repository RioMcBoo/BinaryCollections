using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Replace(bool trim)
        {
            BinaryList<TestTypeBinary> initial;
            TestTypeBinary[] result, rawRes;

            initial = [1];
            result = [2];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(1), 2);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1];
            result = [0];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(1), 0);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 2];
            result = [0, 2];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(1), 0);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 2];
            result = [2, 3];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(1), 3);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 2];
            result = [1, 3];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(2), 3);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 2];
            result = [0, 1];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(2), 0);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [3, 4];
            result = [4, 4];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(3), 4);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [3, 4];
            result = [3, 3];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(4), 3);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [0, 3, 5];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(1), 0);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [2, 3, 5];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(1), 2);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [3, 4, 5];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(1), 4);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [3, 5, 6];
            rawRes = [null, 3, 5, 6];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(1), 6);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [0, 1, 5];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(3), 0);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [1, 2, 5];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(3), 2);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [1, 4, 5];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(3), 4);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [1, 5, 6];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(3), 6);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [0, 1, 3];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(5), 0);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [1, 2, 3];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(5), 2);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [1, 3, 4];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(5), 4);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [1, 3, 6];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(5), 6);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5, 7, 9];
            result = [1, 5, 6, 7, 9];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(3), 6);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5, 7, 9];
            result = [1, 3, 4, 5, 9];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(7), 4);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5, 7, 9];
            result = [1, 3, 4, 7, 9];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(5), 4);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5, 7, 9];
            result = [3, 5, 7, 9, 10];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(1), 10);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5, 7, 9];
            result = [0, 1, 3, 5, 7];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(9), 0);
            RawCollectionAssert.AreEqual(initial, result);
        }

        [TestMethod]
        public void Replace_Fuzzing_HeavyStressTest()
        {
            // Fixed seed so that if it crashes, you can always reproduce the bug.
            var rand = new Random(2026);

            for (int i = 0; i < 2000000; i++)
            {
                // We generate a random size and random numbers (there may be duplicates, as long as they are valid)
                int count = rand.Next(2, 30);
                int initialCapacity = rand.Next(0, count + 2);
                var sourceData = new TestTypeBinary[count];
                for (int j = 0; j < count; j++)
                {
                    sourceData[j] = new TestTypeBinary(rand.Next(0, 50));
                }

                var optimizedList = new BinaryList<TestTypeBinary>(sourceData, capacity: initialCapacity);
                var comparer = optimizedList.Comparer;

                // Sorting for the initial state
                Array.Sort(sourceData, comparer);

                var referenceList = sourceData.ToList();

                // Randomly trim padding in 50% of cases
                if (rand.Next(2) == 0) optimizedList.Trim();

                // Select a random element to replace, which is guaranteed to exist
                var oldItem = referenceList[rand.Next(referenceList.Count)];

                // We generate a new element (arbitrary: smaller, larger, equal)
                var newItem = new TestTypeBinary(rand.Next(-10, 60));

                // We simulate the behavior on the reference
                // We find the index of the first occurrence of the old element
                int idxToRemove = referenceList.IndexOf(oldItem);
                referenceList.RemoveAt(idxToRemove);

                // We find the index where the new element should be inserted to maintain sort order
                int idxToInsert = referenceList.BinarySearch(newItem);
                if (idxToInsert < 0) idxToInsert = ~idxToInsert;
                referenceList.Insert(idxToInsert, newItem);

                // We perform the same action on your binary list
                optimizedList.Replace(oldItem, newItem);

                // The strictest check: we compare both the elements and the padding nullification!
                RawCollectionAssert.AreEqual(optimizedList, referenceList);
            }
        }
    }
}