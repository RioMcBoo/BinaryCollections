using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        public void SetEquals_Comparable()
        {
            // Multiset Equality Oracle: Both structures must have identical elements
            // with the exact same duplicate counts. Equivalent to SequenceEqual for sorted lists.
            bool CheckSetEqualsWith(IReadOnlyList<TestTypeBinary> source, IReadOnlyList<TestTypeBinary> target)
            {
                if (source.Count != target.Count) return false;

                var targetCounts = target.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
                var sourceCounts = source.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

                if (sourceCounts.Count != targetCounts.Count) return false;

                foreach (var pair in sourceCounts)
                {
                    if (!targetCounts.TryGetValue(pair.Key, out int targetCount) || targetCount != pair.Value)
                    {
                        return false;
                    }
                }
                return true;
            }

            BinaryList<TestTypeBinary> first, second;

            // 1. Fully identical structures (Symmetric True)
            first = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 5, 5, 6 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 5, 5, 6 };
            Assert.IsTrue(first.SetEquals(second));
            Assert.AreEqual(CheckSetEqualsWith(first, second), first.SetEquals(second));

            // 2. Count mismatch: Same unique skeleton, but different duplicate counts
            first = new BinaryList<TestTypeBinary> { 1, 2, 2, 3 };
            second = new BinaryList<TestTypeBinary> { 1, 2, 2, 2, 3 }; // Extra '2'
            Assert.IsFalse(first.SetEquals(second));
            Assert.IsFalse(second.SetEquals(first));
            Assert.AreEqual(CheckSetEqualsWith(first, second), first.SetEquals(second));
            Assert.AreEqual(CheckSetEqualsWith(second, first), second.SetEquals(first));

            // 3. Size mismatch: Same count, but different elements
            first = new BinaryList<TestTypeBinary> { 1, 2, 3 };
            second = new BinaryList<TestTypeBinary> { 1, 2, 4 };
            Assert.IsFalse(first.SetEquals(second));
            Assert.IsFalse(second.SetEquals(first));
            Assert.AreEqual(CheckSetEqualsWith(first, second), first.SetEquals(second));
            Assert.AreEqual(CheckSetEqualsWith(second, first), second.SetEquals(first));

            // 4. Both structures are empty (Symmetric True)
            first = new BinaryList<TestTypeBinary>();
            second = new BinaryList<TestTypeBinary>();
            Assert.IsTrue(first.SetEquals(second));
            Assert.AreEqual(CheckSetEqualsWith(first, second), first.SetEquals(second));

            // 5. One empty, one populated
            first = new BinaryList<TestTypeBinary> { 1, 2 };
            second = new BinaryList<TestTypeBinary>();
            Assert.IsFalse(first.SetEquals(second));
            Assert.IsFalse(second.SetEquals(first));
            Assert.AreEqual(CheckSetEqualsWith(first, second), first.SetEquals(second));
            Assert.AreEqual(CheckSetEqualsWith(second, first), second.SetEquals(first));
        }

        [TestMethod]
        public void SetEquals_InComparable()
        {
            // Multiset Equality Oracle: Both structures must have identical elements
            // with the exact same duplicate counts. Equivalent to SequenceEqual for sorted lists.
            bool CheckSetEqualsWith(IReadOnlyList<TestTypeHash> source, IReadOnlyList<TestTypeHash> target)
            {
                if (source.Count != target.Count) return false;

                var targetCounts = target.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
                var sourceCounts = source.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

                if (sourceCounts.Count != targetCounts.Count) return false;

                foreach (var pair in sourceCounts)
                {
                    if (!targetCounts.TryGetValue(pair.Key, out int targetCount) || targetCount != pair.Value)
                    {
                        return false;
                    }
                }
                return true;
            }

            BinaryList<TestTypeHash> first, second;

            // 1. Fully identical structures (Symmetric True)
            first = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 5, 5, 6 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 5, 5, 6 };
            Assert.IsTrue(first.SetEquals(second));
            Assert.AreEqual(CheckSetEqualsWith(first, second), first.SetEquals(second));

            // 2. Count mismatch: Same unique skeleton, but different duplicate counts
            first = new BinaryList<TestTypeHash> { 1, 2, 2, 3 };
            second = new BinaryList<TestTypeHash> { 1, 2, 2, 2, 3 }; // Extra '2'
            Assert.IsFalse(first.SetEquals(second));
            Assert.IsFalse(second.SetEquals(first));
            Assert.AreEqual(CheckSetEqualsWith(first, second), first.SetEquals(second));
            Assert.AreEqual(CheckSetEqualsWith(second, first), second.SetEquals(first));

            // 3. Size mismatch: Same count, but different elements
            first = new BinaryList<TestTypeHash> { 1, 2, 3 };
            second = new BinaryList<TestTypeHash> { 1, 2, 4 };
            Assert.IsFalse(first.SetEquals(second));
            Assert.IsFalse(second.SetEquals(first));
            Assert.AreEqual(CheckSetEqualsWith(first, second), first.SetEquals(second));
            Assert.AreEqual(CheckSetEqualsWith(second, first), second.SetEquals(first));

            // 4. Both structures are empty (Symmetric True)
            first = new BinaryList<TestTypeHash>();
            second = new BinaryList<TestTypeHash>();
            Assert.IsTrue(first.SetEquals(second));
            Assert.AreEqual(CheckSetEqualsWith(first, second), first.SetEquals(second));

            // 5. One empty, one populated
            first = new BinaryList<TestTypeHash> { 1, 2 };
            second = new BinaryList<TestTypeHash>();
            Assert.IsFalse(first.SetEquals(second));
            Assert.IsFalse(second.SetEquals(first));
            Assert.AreEqual(CheckSetEqualsWith(first, second), first.SetEquals(second));
            Assert.AreEqual(CheckSetEqualsWith(second, first), second.SetEquals(first));
        }
    }
}