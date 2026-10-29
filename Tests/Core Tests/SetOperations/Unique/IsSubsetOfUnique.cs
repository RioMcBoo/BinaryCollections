using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        public void IsSubsetOfUnique()
        {            
            bool CheckIsSubsetWithUnique(IReadOnlyList<TestTypeComparable> source, IReadOnlyList<TestTypeComparable> target)
            {
                var targetCounts = target.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
                var sourceCounts = source.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

                foreach (var pair in sourceCounts)
                {
                    if (!targetCounts.ContainsKey(pair.Key))
                    {
                        return false;
                    }
                }
                return true;
            }

            BinaryList<TestTypeComparable> first, second;

            // 1. Equal lists with unique elements
            first = new BinaryList<TestTypeComparable> { 0, 1, 2, 3, 4, 6, 7, 8, 9 };
            second = new BinaryList<TestTypeComparable> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Assert.IsTrue(first.IsSubsetOfUnique(second));
            Assert.IsFalse(second.IsSubsetOfUnique(first));
            Assert.AreEqual(CheckIsSubsetWithUnique(first, second), first.IsSubsetOfUnique(second));
            Assert.AreEqual(CheckIsSubsetWithUnique(second, first), second.IsSubsetOfUnique(first));

            // 2. Empty lists case
            first = new BinaryList<TestTypeComparable>();
            second = new BinaryList<TestTypeComparable> { 0, 1, 3 };
            Assert.IsTrue(first.IsSubsetOfUnique(second));
            Assert.IsFalse(second.IsSubsetOfUnique(first));
            Assert.AreEqual(CheckIsSubsetWithUnique(first, second), first.IsSubsetOfUnique(second));
            Assert.AreEqual(CheckIsSubsetWithUnique(second, first), second.IsSubsetOfUnique(first));

            // 3. Both lists are empty
            first = new BinaryList<TestTypeComparable>();
            second = new BinaryList<TestTypeComparable>();
            Assert.IsTrue(first.IsSubsetOfUnique(second));
            Assert.AreEqual(CheckIsSubsetWithUnique(first, second), first.IsSubsetOfUnique(second));

            // =========================================================================
            // UNIQUE-LOGIC SPECIFIC EDGE CASES
            // =========================================================================

            // 4. CRITICAL UNIQUE CASE: Source contains MORE duplicates than target
            // Should return false for MultiSet logic, but MUST return true for Unique logic
            // since the unique layout {2, 3} perfectly matches on both sides.
            first = new BinaryList<TestTypeComparable> { 2, 2, 2, 3, 3 };
            second = new BinaryList<TestTypeComparable> { 2, 3 };
            Assert.IsTrue(first.IsSubsetOfUnique(second));
            Assert.IsTrue(second.IsSubsetOfUnique(first));
            Assert.AreEqual(CheckIsSubsetWithUnique(first, second), first.IsSubsetOfUnique(second));
            Assert.AreEqual(CheckIsSubsetWithUnique(second, first), second.IsSubsetOfUnique(first));

            // 5. CRITICAL UNIQUE CASE: Flushing target tail (Match without extra unique blocks)
            // Triggers when source is exhausted, but target contains remaining duplicates of the last element.
            // Verifies that the algorithm does not falsely report hasExtraUniqueElements = true.
            first = new BinaryList<TestTypeComparable> { 1, 2 };
            second = new BinaryList<TestTypeComparable> { 1, 2, 2, 2, 2 };
            Assert.IsTrue(first.IsSubsetOfUnique(second));
            Assert.IsTrue(second.IsSubsetOfUnique(first));
            Assert.AreEqual(CheckIsSubsetWithUnique(first, second), first.IsSubsetOfUnique(second));
            Assert.AreEqual(CheckIsSubsetWithUnique(second, first), second.IsSubsetOfUnique(first));

            // 6. CRITICAL UNIQUE CASE: Flushing target tail (Actual extra unique blocks at the end)
            // Ensures that target trailing segment validation correctly detects genuine new blocks (3, 4).
            first = new BinaryList<TestTypeComparable> { 1, 2, 2 };
            second = new BinaryList<TestTypeComparable> { 1, 2, 2, 2, 3, 4 };
            Assert.IsTrue(first.IsSubsetOfUnique(second));
            Assert.IsFalse(second.IsSubsetOfUnique(first));
            Assert.AreEqual(CheckIsSubsetWithUnique(first, second), first.IsSubsetOfUnique(second));
            Assert.AreEqual(CheckIsSubsetWithUnique(second, first), second.IsSubsetOfUnique(first));

            // 7. CRITICAL UNIQUE CASE: Alternating redundant duplicate patterns
            // Source contains more duplicates in the first block, while target contains more in the second.
            first = new BinaryList<TestTypeComparable> { 1, 1, 1, 2 };
            second = new BinaryList<TestTypeComparable> { 1, 2, 2, 2, 2 };
            Assert.IsTrue(first.IsSubsetOfUnique(second));
            Assert.IsTrue(second.IsSubsetOfUnique(first));
            Assert.AreEqual(CheckIsSubsetWithUnique(first, second), first.IsSubsetOfUnique(second));
            Assert.AreEqual(CheckIsSubsetWithUnique(second, first), second.IsSubsetOfUnique(first));

            // =========================================================================
            // BOUNDARY TEST CASES (LowerLimit / UpperLimit Optimization) FOR UNIQUE LOGIC
            // =========================================================================

            // 8. Shifting lower boundary via binary search when edge duplicates are present
            first = new BinaryList<TestTypeComparable> { 5, 5, 6 };
            second = new BinaryList<TestTypeComparable> { 1, 1, 2, 3, 5, 5, 5, 5, 6, 7 };
            Assert.IsTrue(first.IsSubsetOfUnique(second));
            Assert.IsFalse(second.IsSubsetOfUnique(first));
            Assert.AreEqual(CheckIsSubsetWithUnique(first, second), first.IsSubsetOfUnique(second));
            Assert.AreEqual(CheckIsSubsetWithUnique(second, first), second.IsSubsetOfUnique(first));

            // 9. Out of target left boundary (Source element is smaller than target's initial block layout)
            first = new BinaryList<TestTypeComparable> { 1, 5 };
            second = new BinaryList<TestTypeComparable> { 2, 2, 2, 5, 6 };
            Assert.IsFalse(first.IsSubsetOfUnique(second));
            Assert.IsFalse(second.IsSubsetOfUnique(first));
            Assert.AreEqual(CheckIsSubsetWithUnique(first, second), first.IsSubsetOfUnique(second));
            Assert.AreEqual(CheckIsSubsetWithUnique(second, first), second.IsSubsetOfUnique(first));

            // 10. Out of target right boundary (Source element is greater than target's trailing block layout)
            first = new BinaryList<TestTypeComparable> { 2, 7, 7 };
            second = new BinaryList<TestTypeComparable> { 1, 2, 2, 5, 5 };
            Assert.IsFalse(first.IsSubsetOfUnique(second));
            Assert.IsFalse(second.IsSubsetOfUnique(first));
            Assert.AreEqual(CheckIsSubsetWithUnique(first, second), first.IsSubsetOfUnique(second));
            Assert.AreEqual(CheckIsSubsetWithUnique(second, first), second.IsSubsetOfUnique(first));

            // =========================================================================
            // ADVANCED STRUCTURAL LAYOUTS & ITERATOR STEPPING
            // =========================================================================

            // 11. Skipping unmatched inner target duplicate blocks
            // Verifies that ProcessTarget correctly skips the entire layout of 3s and 4s in a single run
            // without preventing the main loop from safely encountering the 5s block.
            first = new BinaryList<TestTypeComparable> { 2, 5 };
            second = new BinaryList<TestTypeComparable> { 2, 2, 3, 3, 3, 4, 4, 5, 5 };
            Assert.IsTrue(first.IsSubsetOfUnique(second));
            Assert.IsFalse(second.IsSubsetOfUnique(first));
            Assert.AreEqual(CheckIsSubsetWithUnique(first, second), first.IsSubsetOfUnique(second));
            Assert.AreEqual(CheckIsSubsetWithUnique(second, first), second.IsSubsetOfUnique(first));

            // 12. EXTRA LARGE PAYLOAD: Massive chaotic cascade of duplicates
            // Evaluates pointer coordination stability across heavy scales of consecutive identical values.
            first = new BinaryList<TestTypeComparable> { 10, 10, 10, 10, 20, 30, 30, 30, 50 };
            second = new BinaryList<TestTypeComparable> { 5, 10, 20, 20, 20, 20, 30, 30, 40, 50, 50, 50, 60 };
            Assert.IsTrue(first.IsSubsetOfUnique(second));
            Assert.IsFalse(second.IsSubsetOfUnique(first));
            Assert.AreEqual(CheckIsSubsetWithUnique(first, second), first.IsSubsetOfUnique(second));
            Assert.AreEqual(CheckIsSubsetWithUnique(second, first), second.IsSubsetOfUnique(first));

            // 13. Source matching ends exactly on the last element of target
            first = new BinaryList<TestTypeComparable> { 1, 3 };
            second = new BinaryList<TestTypeComparable> { 1, 2, 3 };
            Assert.IsTrue(first.IsSubsetOfUnique(second));
            Assert.IsFalse(second.IsSubsetOfUnique(first));
            Assert.AreEqual(CheckIsSubsetWithUnique(first, second), first.IsSubsetOfUnique(second));
            Assert.AreEqual(CheckIsSubsetWithUnique(second, first), second.IsSubsetOfUnique(first));

            // 14. Target is exhausted, but source still has unique items left
            first = new BinaryList<TestTypeComparable> { 1, 2, 3 };
            second = new BinaryList<TestTypeComparable> { 1, 2 };
            Assert.IsFalse(first.IsSubsetOfUnique(second));
            Assert.IsTrue(second.IsSubsetOfUnique(first));
            Assert.AreEqual(CheckIsSubsetWithUnique(first, second), first.IsSubsetOfUnique(second));
            Assert.AreEqual(CheckIsSubsetWithUnique(second, first), second.IsSubsetOfUnique(first));
        }
    }
}