using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        public void IsProperSubsetOf_Comparable()
        {
            // Updated oracle to handle the strict "Proper" condition.
            // A proper subset means: source is a subset of target AND target is strictly larger.
            bool CheckIsProperSubsetWith(IReadOnlyList<TestTypeBinary> source, IReadOnlyList<TestTypeBinary> target)
            {
                var targetCounts = target.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
                var sourceCounts = source.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

                // 1. Basic containment verification
                foreach (var pair in sourceCounts)
                {
                    if (!targetCounts.TryGetValue(pair.Key, out int targetCount) || targetCount < pair.Value)
                    {
                        return false;
                    }
                }

                return target.Count > source.Count;
            }

            BinaryList<TestTypeBinary> first, second;

            // 1. Equal lists with unique elements
            first = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 6, 7, 8, 9 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 2. Equal lists with duplicated elements
            first = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));           
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 3. Different lists with duplicated elements (second is wider on the left)
            first = new BinaryList<TestTypeBinary> { 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 4. Equal lists with duplicated elements (second is wider on the right)
            first = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 5, 6 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 5. Different lists with duplicated elements (second is wider on the left - check boundaries)
            first = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 10, 11, 12, 13 };
            Assert.IsFalse(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 6. Equal lists with duplicated elements (second is wider on the right - check boundaries)
            first = new BinaryList<TestTypeBinary> { 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 4, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsFalse(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 7. Case with empty list
            first = new BinaryList<TestTypeBinary>();
            second = new BinaryList<TestTypeBinary> { 0, 1, 3 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 8. DEGENERATE CASE: Both structures are empty
            // Empty cannot be a proper subset of another empty collection (not strictly smaller)
            first = new BinaryList<TestTypeBinary>();
            second = new BinaryList<TestTypeBinary>();
            Assert.IsFalse(first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));

            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            // 9. Multi-block verification with consecutive elements
            first = new BinaryList<TestTypeBinary> { 2, 2, 3, 4 };
            second = new BinaryList<TestTypeBinary> { 2, 2, 2, 3, 4 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 10. Interleaved extra unique values inside the target span
            first = new BinaryList<TestTypeBinary> { 3, 4 };
            second = new BinaryList<TestTypeBinary> { 2, 2, 3, 4 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 11. Double-sided boundary unique expansion
            first = new BinaryList<TestTypeBinary> { 3, 4 };
            second = new BinaryList<TestTypeBinary> { 1, 3, 4, 6 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 12. Alternating duplicate counts and unique interleaving
            first = new BinaryList<TestTypeBinary> { 10, 10, 20, 20, 20, 30, 40, 40 };
            second = new BinaryList<TestTypeBinary> { 10, 10, 10, 20, 20, 20, 25, 30, 40, 40, 40, 50 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // =========================================================================
            // BRAND NEW EDGE CASES TAILORED FOR ISPROPERSUBSETOF
            // =========================================================================

            // 13. CRITICAL PROPER EDGE CASE: Identity Match Rejection
            // A proper subset must be strictly smaller. If the lists match completely, it should return false.
            first = new BinaryList<TestTypeBinary> { 1, 2, 3, 3, 4 };
            second = new BinaryList<TestTypeBinary> { 1, 2, 3, 3, 4 };
            Assert.IsFalse(first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));

            // 14. CRITICAL PROPER EDGE CASE: Only Extra Duplicates (No New Unique Values)
            // Evaluates behavior when containment is valid, total counts are larger, but unique key count is identical.
            // For Set semantics (Unique) -> should return FALSE. For Multiset semantics -> should return TRUE.
            first = new BinaryList<TestTypeBinary> { 5, 6 };
            second = new BinaryList<TestTypeBinary> { 5, 5, 5, 6, 6 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 15. CRITICAL PROPER EDGE CASE: Only Extra Unique Values (Identical Duplicate Counts)
            // Target introduces brand new unique values inside, but shares exact cardinality on existing values.
            // This should return TRUE under both Set and Multiset paradigms.
            first = new BinaryList<TestTypeBinary> { 10, 10, 30 };
            second = new BinaryList<TestTypeBinary> { 10, 10, 20, 30 }; // '20' is purely unique
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 16. BOUNDARY DEGENERATE CASE: Symmetric Boundaries with Missing Center Value
            // Left and right boundary values match completely (skipping boundary optimizations triggers), 
            // but a value inside the source is completely missing from the center of the target.
            first = new BinaryList<TestTypeBinary> { 10, 15, 20 };
            second = new BinaryList<TestTypeBinary> { 10, 12, 18, 20 }; // Containment fails because 15 is missing
            Assert.IsFalse(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 17. MULTISET CRITICAL: Source has MORE duplicates in one block, but target is larger overall
            // Containment must fail because target lacks enough '5's, even though target.Count > source.Count.
            first = new BinaryList<TestTypeBinary> { 5, 5, 5, 6 };
            second = new BinaryList<TestTypeBinary> { 5, 5, 6, 7, 8, 9 }; // Lacks the third '5'
            Assert.IsFalse(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 18. MULTISET BOUNDARY: Target has extra duplicates exactly at the edge boundaries
            first = new BinaryList<TestTypeBinary> { 10, 11, 12 };
            second = new BinaryList<TestTypeBinary> { 10, 10, 10, 11, 12, 12, 12 }; // Edge heavy layout
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 19. MULTISET ADVANCED: Skipping heavy duplicate structures inside the target layout
            first = new BinaryList<TestTypeBinary> { 10, 30 };
            second = new BinaryList<TestTypeBinary> { 10, 20, 20, 20, 20, 20, 30 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));
        }

        [TestMethod]
        public void IsProperSubsetOf_InComparable()
        {
            // Updated oracle to handle the strict "Proper" condition.
            // A proper subset means: source is a subset of target AND target is strictly larger.
            bool CheckIsProperSubsetWith(IReadOnlyList<TestTypeHash> source, IReadOnlyList<TestTypeHash> target)
            {
                var targetCounts = target.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
                var sourceCounts = source.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

                // 1. Basic containment verification
                foreach (var pair in sourceCounts)
                {
                    if (!targetCounts.TryGetValue(pair.Key, out int targetCount) || targetCount < pair.Value)
                    {
                        return false;
                    }
                }

                return target.Count > source.Count;

                //if (isMultiset)
                //{
                //    // For Multisets, proper subset means target has more total elements (extra duplicates or unique)

                //}
                //else
                //{
                //    // For unique Sets, proper subset means target has more UNIQUE keys
                //    return targetCounts.Keys.Count > sourceCounts.Keys.Count;
                //}
            }

            BinaryList<TestTypeHash> first, second;

            // 1. Equal lists with unique elements
            first = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 6, 7, 8, 9 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 2. Equal lists with duplicated elements
            first = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 3. Different lists with duplicated elements (second is wider on the left)
            first = new BinaryList<TestTypeHash> { 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 4. Equal lists with duplicated elements (second is wider on the right)
            first = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 5, 6 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 5. Different lists with duplicated elements (second is wider on the left - check boundaries)
            first = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 3, 4, 5, 6, 7, 10, 11, 12, 13 };
            Assert.IsFalse(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 6. Equal lists with duplicated elements (second is wider on the right - check boundaries)
            first = new BinaryList<TestTypeHash> { 3, 4, 5, 6, 7, 7, 8, 9 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 4, 4, 5, 6, 7, 7, 7, 8, 9 };
            Assert.IsFalse(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 7. Case with empty list
            first = new BinaryList<TestTypeHash>();
            second = new BinaryList<TestTypeHash> { 0, 1, 3 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 8. DEGENERATE CASE: Both structures are empty
            // Empty cannot be a proper subset of another empty collection (not strictly smaller)
            first = new BinaryList<TestTypeHash>();
            second = new BinaryList<TestTypeHash>();
            Assert.IsFalse(first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));

            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            // 9. Multi-block verification with consecutive elements
            first = new BinaryList<TestTypeHash> { 2, 2, 3, 4 };
            second = new BinaryList<TestTypeHash> { 2, 2, 2, 3, 4 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 10. Interleaved extra unique values inside the target span
            first = new BinaryList<TestTypeHash> { 3, 4 };
            second = new BinaryList<TestTypeHash> { 2, 2, 3, 4 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 11. Double-sided boundary unique expansion
            first = new BinaryList<TestTypeHash> { 3, 4 };
            second = new BinaryList<TestTypeHash> { 1, 3, 4, 6 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 12. Alternating duplicate counts and unique interleaving
            first = new BinaryList<TestTypeHash> { 10, 10, 20, 20, 20, 30, 40, 40 };
            second = new BinaryList<TestTypeHash> { 10, 10, 10, 20, 20, 20, 25, 30, 40, 40, 40, 50 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // =========================================================================
            // BRAND NEW EDGE CASES TAILORED FOR ISPROPERSUBSETOF
            // =========================================================================

            // 13. CRITICAL PROPER EDGE CASE: Identity Match Rejection
            // A proper subset must be strictly smaller. If the lists match completely, it should return false.
            first = new BinaryList<TestTypeHash> { 1, 2, 3, 3, 4 };
            second = new BinaryList<TestTypeHash> { 1, 2, 3, 3, 4 };
            Assert.IsFalse(first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));

            // 14. CRITICAL PROPER EDGE CASE: Only Extra Duplicates (No New Unique Values)
            // Evaluates behavior when containment is valid, total counts are larger, but unique key count is identical.
            // For Set semantics (Unique) -> should return FALSE. For Multiset semantics -> should return TRUE.
            first = new BinaryList<TestTypeHash> { 5, 6 };
            second = new BinaryList<TestTypeHash> { 5, 5, 5, 6, 6 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 15. CRITICAL PROPER EDGE CASE: Only Extra Unique Values (Identical Duplicate Counts)
            // Target introduces brand new unique values inside, but shares exact cardinality on existing values.
            // This should return TRUE under both Set and Multiset paradigms.
            first = new BinaryList<TestTypeHash> { 10, 10, 30 };
            second = new BinaryList<TestTypeHash> { 10, 10, 20, 30 }; // '20' is purely unique
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 16. BOUNDARY DEGENERATE CASE: Symmetric Boundaries with Missing Center Value
            // Left and right boundary values match completely (skipping boundary optimizations triggers), 
            // but a value inside the source is completely missing from the center of the target.
            first = new BinaryList<TestTypeHash> { 10, 15, 20 };
            second = new BinaryList<TestTypeHash> { 10, 12, 18, 20 }; // Containment fails because 15 is missing
            Assert.IsFalse(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 17. MULTISET CRITICAL: Source has MORE duplicates in one block, but target is larger overall
            // Containment must fail because target lacks enough '5's, even though target.Count > source.Count.
            first = new BinaryList<TestTypeHash> { 5, 5, 5, 6 };
            second = new BinaryList<TestTypeHash> { 5, 5, 6, 7, 8, 9 }; // Lacks the third '5'
            Assert.IsFalse(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 18. MULTISET BOUNDARY: Target has extra duplicates exactly at the edge boundaries
            first = new BinaryList<TestTypeHash> { 10, 11, 12 };
            second = new BinaryList<TestTypeHash> { 10, 10, 10, 11, 12, 12, 12 }; // Edge heavy layout
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));

            // 19. MULTISET ADVANCED: Skipping heavy duplicate structures inside the target layout
            first = new BinaryList<TestTypeHash> { 10, 30 };
            second = new BinaryList<TestTypeHash> { 10, 20, 20, 20, 20, 20, 30 };
            Assert.IsTrue(first.IsProperSubsetOf(second));
            Assert.IsFalse(second.IsProperSubsetOf(first));
            Assert.AreEqual(CheckIsProperSubsetWith(first, second), first.IsProperSubsetOf(second));
            Assert.AreEqual(CheckIsProperSubsetWith(second, first), second.IsProperSubsetOf(first));
        }
    }
}