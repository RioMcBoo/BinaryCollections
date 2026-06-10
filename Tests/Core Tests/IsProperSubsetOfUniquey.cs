using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        public void IsProperSubsetOfUnique_Comparable()
        {
            // Updated oracle to handle the strict "Proper" condition.
            // A proper subset means: source is a subset of target AND target is strictly larger.
            bool CheckIsProperSubsetWithUnique(IReadOnlyList<TestTypeBinary> source, IReadOnlyList<TestTypeBinary> target)
            {
                var targetKeys = target.GroupBy(x => x).Select(g => g.Key).ToHashSet();
                var sourceKeys = source.GroupBy(x => x).Select(g => g.Key).ToHashSet();

                // 1. Unique containment verification
                foreach (var key in sourceKeys)
                {
                    if (!targetKeys.Contains(key))
                    {
                        return false;
                    }
                }

                // 2. Strict proper evaluation based on unique layout size
                return targetKeys.Count > sourceKeys.Count;
            }

            BinaryList<TestTypeBinary> first, second;

            // 1. Equal lists with unique elements
            first = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 6, 7, 8, 9 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Assert.IsTrue(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // 2. Equal unique keys but different duplicate layout
            // For unique logic, these represent the EXACT SAME set. Thus, proper subset MUST be false.
            first = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsFalse(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // 3. Different lists with duplicated elements (second is wider on the left with new unique keys: 0, 1, 2)
            first = new BinaryList<TestTypeBinary> { 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsTrue(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // 4. Different lists with duplicated elements (second is wider on the right with new unique keys: 7, 8, 9)
            first = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 5, 6 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsTrue(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // 5. Containment failure (10, 11, 12, 13 are in second, but first has 8, 9 which target misses)
            first = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 10, 11, 12, 13 };
            Assert.IsFalse(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // 6. Case with empty list (Empty is a proper unique subset of any populated unique set)
            first = new BinaryList<TestTypeBinary>();
            second = new BinaryList<TestTypeBinary> { 0, 1, 3 };
            Assert.IsTrue(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // 7. DEGENERATE CASE: Both structures are empty (Must return false)
            first = new BinaryList<TestTypeBinary>();
            second = new BinaryList<TestTypeBinary>();
            Assert.IsFalse(first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));

            // 8. Unique identity layout match but different total counts
            // Multiset logic would say TRUE here, but Unique logic MUST say FALSE because unique keys match perfectly.
            first = new BinaryList<TestTypeBinary> { 2, 2, 3, 4 };
            second = new BinaryList<TestTypeBinary> { 2, 2, 2, 3, 4 };
            Assert.IsFalse(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // 9. Interleaved extra unique values inside the target span (True proper subset)
            first = new BinaryList<TestTypeBinary> { 3, 4 };
            second = new BinaryList<TestTypeBinary> { 2, 2, 3, 4 };
            Assert.IsTrue(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // 10. Alternating duplicate counts but target introduces new unique key '25' and '50'
            first = new BinaryList<TestTypeBinary> { 10, 10, 20, 20, 20, 30, 40, 40 };
            second = new BinaryList<TestTypeBinary> { 10, 10, 10, 20, 20, 20, 25, 30, 40, 40, 40, 50 };
            Assert.IsTrue(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // =========================================================================
            // CRITICAL UNIQUE-SPECIFIC EDGE CASES
            // =========================================================================

            // 11. CRITICAL UNIQUE CASE: Source has MORE duplicates but LESS unique keys (TRUE)
            // Source unique keys: {5, 6}. Target unique keys: {5, 6, 7}.
            // This verifies that the pointer algorithm completely strips away duplicate counts.
            first = new BinaryList<TestTypeBinary> { 5, 5, 5, 5, 6, 6, 6 };
            second = new BinaryList<TestTypeBinary> { 5, 6, 7 };
            Assert.IsTrue(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // 12. CRITICAL UNIQUE CASE: Massive duplicate layout with identical unique skeleton (FALSE)
            first = new BinaryList<TestTypeBinary> { 10, 20 };
            second = new BinaryList<TestTypeBinary> { 10, 10, 10, 10, 10, 20, 20, 20, 20, 20 };
            Assert.IsFalse(first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));

            // 13. BOUNDARY CRITICAL: Edge duplicates match but target has an extra inner unique key
            first = new BinaryList<TestTypeBinary> { 10, 10, 30, 30 };
            second = new BinaryList<TestTypeBinary> { 10, 20, 30 }; // '20' makes it a proper unique subset
            Assert.IsTrue(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));
        }

        [TestMethod]
        public void IsProperSubsetOfUnique_InComparable()
        {
            // Updated oracle to handle the strict "Proper" condition.
            // A proper subset means: source is a subset of target AND target is strictly larger.
            bool CheckIsProperSubsetWithUnique(IReadOnlyList<TestTypeHash> source, IReadOnlyList<TestTypeHash> target)
            {
                var targetKeys = target.GroupBy(x => x).Select(g => g.Key).ToHashSet();
                var sourceKeys = source.GroupBy(x => x).Select(g => g.Key).ToHashSet();

                // 1. Unique containment verification
                foreach (var key in sourceKeys)
                {
                    if (!targetKeys.Contains(key))
                    {
                        return false;
                    }
                }

                // 2. Strict proper evaluation based on unique layout size
                return targetKeys.Count > sourceKeys.Count;
            }

            BinaryList<TestTypeHash> first, second;

            // 1. Equal lists with unique elements
            first = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 6, 7, 8, 9 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Assert.IsTrue(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // 2. Equal unique keys but different duplicate layout
            // For unique logic, these represent the EXACT SAME set. Thus, proper subset MUST be false.
            first = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsFalse(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // 3. Different lists with duplicated elements (second is wider on the left with new unique keys: 0, 1, 2)
            first = new BinaryList<TestTypeHash> { 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsTrue(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // 4. Different lists with duplicated elements (second is wider on the right with new unique keys: 7, 8, 9)
            first = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 5, 6 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsTrue(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // 5. Containment failure (10, 11, 12, 13 are in second, but first has 8, 9 which target misses)
            first = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 10, 11, 12, 13 };
            Assert.IsFalse(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // 6. Case with empty list (Empty is a proper unique subset of any populated unique set)
            first = new BinaryList<TestTypeHash>();
            second = new BinaryList<TestTypeHash> { 0, 1, 3 };
            Assert.IsTrue(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // 7. DEGENERATE CASE: Both structures are empty (Must return false)
            first = new BinaryList<TestTypeHash>();
            second = new BinaryList<TestTypeHash>();
            Assert.IsFalse(first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));

            // 8. Unique identity layout match but different total counts
            // Multiset logic would say TRUE here, but Unique logic MUST say FALSE because unique keys match perfectly.
            first = new BinaryList<TestTypeHash> { 2, 2, 3, 4 };
            second = new BinaryList<TestTypeHash> { 2, 2, 2, 3, 4 };
            Assert.IsFalse(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // 9. Interleaved extra unique values inside the target span (True proper subset)
            first = new BinaryList<TestTypeHash> { 3, 4 };
            second = new BinaryList<TestTypeHash> { 2, 2, 3, 4 };
            Assert.IsTrue(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // 10. Alternating duplicate counts but target introduces new unique key '25' and '50'
            first = new BinaryList<TestTypeHash> { 10, 10, 20, 20, 20, 30, 40, 40 };
            second = new BinaryList<TestTypeHash> { 10, 10, 10, 20, 20, 20, 25, 30, 40, 40, 40, 50 };
            Assert.IsTrue(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // =========================================================================
            // CRITICAL UNIQUE-SPECIFIC EDGE CASES
            // =========================================================================

            // 11. CRITICAL UNIQUE CASE: Source has MORE duplicates but LESS unique keys (TRUE)
            // Source unique keys: {5, 6}. Target unique keys: {5, 6, 7}.
            // This verifies that the pointer algorithm completely strips away duplicate counts.
            first = new BinaryList<TestTypeHash> { 5, 5, 5, 5, 6, 6, 6 };
            second = new BinaryList<TestTypeHash> { 5, 6, 7 };
            Assert.IsTrue(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));

            // 12. CRITICAL UNIQUE CASE: Massive duplicate layout with identical unique skeleton (FALSE)
            first = new BinaryList<TestTypeHash> { 10, 20 };
            second = new BinaryList<TestTypeHash> { 10, 10, 10, 10, 10, 20, 20, 20, 20, 20 };
            Assert.IsFalse(first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));

            // 13. BOUNDARY CRITICAL: Edge duplicates match but target has an extra inner unique key
            first = new BinaryList<TestTypeHash> { 10, 10, 30, 30 };
            second = new BinaryList<TestTypeHash> { 10, 20, 30 }; // '20' makes it a proper unique subset
            Assert.IsTrue(first.IsProperSubsetOfUnique(second));
            Assert.IsFalse(second.IsProperSubsetOfUnique(first));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(first, second), first.IsProperSubsetOfUnique(second));
            Assert.AreEqual(CheckIsProperSubsetWithUnique(second, first), second.IsProperSubsetOfUnique(first));
        }
    }
}