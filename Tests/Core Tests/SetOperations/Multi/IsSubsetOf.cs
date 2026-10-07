using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        public void IsSubsetOf_Comparable()
        {
            bool CheckIsSubsetWith(IReadOnlyList<TestTypeBinary> source, IReadOnlyList<TestTypeBinary> target)
            {
                var targetCounts = target.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
                var sourceCounts = source.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

                foreach (var pair in sourceCounts)
                {
                    if (!targetCounts.TryGetValue(pair.Key, out int targetCount) || targetCount < pair.Value)
                    {
                        return false;
                    }
                }
                return true;
            }

            BinaryList<TestTypeBinary> first, second;

            // 1. Equal lists with unique elements
            first = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 6, 7, 8, 9 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.IsFalse(second.IsSubsetOf(first));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(second, first), second.IsSubsetOf(first));

            // 2. Equal lists with duplicated elements
            first = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.IsFalse(second.IsSubsetOf(first));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(second, first), second.IsSubsetOf(first));

            // 3. Different lists with duplicated elements (second is wider on the left)
            first = new BinaryList<TestTypeBinary> { 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.IsFalse(second.IsSubsetOf(first));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(second, first), second.IsSubsetOf(first));

            // 4. Equal lists with duplicated elements (second is wider on the right)
            first = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 5, 6};
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.IsFalse(second.IsSubsetOf(first));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(second, first), second.IsSubsetOf(first));

            // 5. Different lists with duplicated elements (second is wider on the left - check boundaries)
            first = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 10, 11, 12, 13};
            Assert.IsFalse(first.IsSubsetOf(second));
            Assert.IsFalse(second.IsSubsetOf(first));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(second, first), second.IsSubsetOf(first));

            // 6. Equal lists with duplicated elements (second is wider on the right - check boundaries)
            first = new BinaryList<TestTypeBinary> { 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 4, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsFalse(first.IsSubsetOf(second));
            Assert.IsFalse(second.IsSubsetOf(first));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(second, first), second.IsSubsetOf(first));

            // 7. Case with empty list
            first = new BinaryList<TestTypeBinary>();
            second = new BinaryList<TestTypeBinary> { 0, 1, 3 };
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.IsFalse(second.IsSubsetOf(first));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(second, first), second.IsSubsetOf(first));

            // 8. Case with both empty
            first = new BinaryList<TestTypeBinary>();
            second = new BinaryList<TestTypeBinary>();
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));

            // 9. CRITICAL EDGE CASE: Multi-block verification with consecutive elements
            // Checks if ProcessBothComparable correctly stops running forward when a block ends
            // Instead of swallowing remaining values (like 3 and 4) to OutOfRange.
            first = new BinaryList<TestTypeBinary> { 2, 2, 3, 4 };
            second = new BinaryList<TestTypeBinary> { 2, 2, 2, 3, 4 };
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(second, first), second.IsSubsetOf(first));

            // 10. CRITICAL EDGE CASE: Interleaved extra unique values inside the target span
            // Ensures ProcessTarget only advances over the current duplicated unmatched group
            // and does not flush the rest of the target list, breaking subsequent matches.
            first = new BinaryList<TestTypeBinary> { 3, 4 };
            second = new BinaryList<TestTypeBinary> { 2, 2, 3, 4 };
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));

            // 11. CRITICAL EDGE CASE: Double-sided boundary unique expansion
            // Verifies that passing flags via 'ref' accurately retains information discovered 
            // on both sides, ensuring that finding '6' on the right doesn't overwrite finding '1' on the left.
            first = new BinaryList<TestTypeBinary> { 3, 4 };
            second = new BinaryList<TestTypeBinary> { 1, 3, 4, 6 };
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));

            // 12. EXTRA LARGE PAYLOAD: Alternating duplicate counts and unique interleaving
            // Doubles the normal array payload scale to evaluate pointer coordination stability 
            // over multiple changing states of block compression logic.
            first = new BinaryList<TestTypeBinary> { 10, 10, 20, 20, 20, 30, 40, 40 };
            second = new BinaryList<TestTypeBinary> { 10, 10, 10, 20, 20, 20, 25, 30, 40, 40, 40, 50 };
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.IsFalse(second.IsSubsetOf(first));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(second, first), second.IsSubsetOf(first));
        }

        [TestMethod]
        public void IsSubsetOf_InComparable()
        {
            bool CheckIsSubsetWith(IReadOnlyList<TestTypeHash> source, IReadOnlyList<TestTypeHash> target)
            {
                var targetCounts = target.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
                var sourceCounts = source.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

                foreach (var pair in sourceCounts)
                {
                    if (!targetCounts.TryGetValue(pair.Key, out int targetCount) || targetCount < pair.Value)
                    {
                        return false;
                    }
                }
                return true;
            }

            BinaryList<TestTypeHash> first, second;

            // 1. Equal lists with unique elements
            first = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 6, 7, 8, 9 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.IsFalse(second.IsSubsetOf(first));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(second, first), second.IsSubsetOf(first));

            // 2. Equal lists with duplicated elements
            first = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.IsFalse(second.IsSubsetOf(first));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(second, first), second.IsSubsetOf(first));

            // 3. Different lists with duplicated elements (second is wider on the left)
            first = new BinaryList<TestTypeHash> { 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.IsFalse(second.IsSubsetOf(first));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(second, first), second.IsSubsetOf(first));

            // 4. Equal lists with duplicated elements (second is wider on the right)
            first = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 5, 6 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.IsFalse(second.IsSubsetOf(first));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(second, first), second.IsSubsetOf(first));

            // 5. Different lists with duplicated elements (second is wider on the left - check boundaries)
            first = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 10, 11, 12, 13 };
            Assert.IsFalse(first.IsSubsetOf(second));
            Assert.IsFalse(second.IsSubsetOf(first));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(second, first), second.IsSubsetOf(first));

            // 6. Equal lists with duplicated elements (second is wider on the right - check boundaries)
            first = new BinaryList<TestTypeHash> { 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 4, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsFalse(first.IsSubsetOf(second));
            Assert.IsFalse(second.IsSubsetOf(first));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(second, first), second.IsSubsetOf(first));

            // 7. Case with empty list
            first = new BinaryList<TestTypeHash>();
            second = new BinaryList<TestTypeHash> { 0, 1, 3 };
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.IsFalse(second.IsSubsetOf(first));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(second, first), second.IsSubsetOf(first));

            // 8. Case with both empty
            first = new BinaryList<TestTypeHash>();
            second = new BinaryList<TestTypeHash>();
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));

            // 9. CRITICAL EDGE CASE: Multi-block verification with consecutive elements
            // Checks if ProcessBothComparable correctly stops running forward when a block ends
            // Instead of swallowing remaining values (like 3 and 4) to OutOfRange.
            first = new BinaryList<TestTypeHash> { 2, 2, 3, 4 };
            second = new BinaryList<TestTypeHash> { 2, 2, 2, 3, 4 };
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(second, first), second.IsSubsetOf(first));

            // 10. CRITICAL EDGE CASE: Interleaved extra unique values inside the target span
            // Ensures ProcessTarget only advances over the current duplicated unmatched group
            // and does not flush the rest of the target list, breaking subsequent matches.
            first = new BinaryList<TestTypeHash> { 3, 4 };
            second = new BinaryList<TestTypeHash> { 2, 2, 3, 4 };
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));

            // 11. CRITICAL EDGE CASE: Double-sided boundary unique expansion
            // Verifies that passing flags via 'ref' accurately retains information discovered 
            // on both sides, ensuring that finding '6' on the right doesn't overwrite finding '1' on the left.
            first = new BinaryList<TestTypeHash> { 3, 4 };
            second = new BinaryList<TestTypeHash> { 1, 3, 4, 6 };
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));

            // 12. EXTRA LARGE PAYLOAD: Alternating duplicate counts and unique interleaving
            // Doubles the normal array payload scale to evaluate pointer coordination stability 
            // over multiple changing states of block compression logic.
            first = new BinaryList<TestTypeHash> { 10, 10, 20, 20, 20, 30, 40, 40 };
            second = new BinaryList<TestTypeHash> { 10, 10, 10, 20, 20, 20, 25, 30, 40, 40, 40, 50 };
            Assert.IsTrue(first.IsSubsetOf(second));
            Assert.IsFalse(second.IsSubsetOf(first));
            Assert.AreEqual(CheckIsSubsetWith(first, second), first.IsSubsetOf(second));
            Assert.AreEqual(CheckIsSubsetWith(second, first), second.IsSubsetOf(first));
        }
    }
}