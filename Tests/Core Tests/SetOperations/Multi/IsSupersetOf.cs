using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        public void IsSupersetOf_Comparable()
        {
            // Multiset Superset Oracle: Source is a superset of target ONLY IF 
            // for every key, source contains GREATER OR EQUAL number of duplicates than target.
            bool CheckIsSupersetWith(IReadOnlyList<TestTypeBinary> source, IReadOnlyList<TestTypeBinary> target)
            {
                var targetCounts = target.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
                var sourceCounts = source.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

                foreach (var pair in targetCounts)
                {
                    if (!sourceCounts.TryGetValue(pair.Key, out int sourceCount) || sourceCount < pair.Value)
                    {
                        return false;
                    }
                }
                return true;
            }

            BinaryList<TestTypeBinary> first, second;

            // 1. Standard containment (first is strictly larger and encompasses second)
            first = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 6, 7, 8, 9 };
            Assert.IsTrue(first.IsSupersetOf(second));
            Assert.IsFalse(second.IsSupersetOf(first));
            Assert.AreEqual(CheckIsSupersetWith(first, second), first.IsSupersetOf(second));
            Assert.AreEqual(CheckIsSupersetWith(second, first), second.IsSupersetOf(first));

            // 2. Identical structural and duplicate layout (Symmetric superset evaluation)
            first = new BinaryList<TestTypeBinary> { 1, 2, 2, 3, 3, 3 };
            second = new BinaryList<TestTypeBinary> { 1, 2, 2, 3, 3, 3 };
            Assert.IsTrue(first.IsSupersetOf(second));
            Assert.AreEqual(CheckIsSupersetWith(first, second), first.IsSupersetOf(second));

            // 3. Multiset Failure: Target contains more duplicates in a specific block
            first = new BinaryList<TestTypeBinary> { 1, 2, 2, 3 };
            second = new BinaryList<TestTypeBinary> { 1, 2, 2, 2, 3 }; // Target has an extra '2'
            Assert.IsFalse(first.IsSupersetOf(second));
            Assert.IsTrue(second.IsSupersetOf(first));
            Assert.AreEqual(CheckIsSupersetWith(first, second), first.IsSupersetOf(second));
            Assert.AreEqual(CheckIsSupersetWith(second, first), second.IsSupersetOf(first));

            // 4. Case with empty target (Any populated list is a superset of an empty collection)
            first = new BinaryList<TestTypeBinary> { 0, 1, 3 };
            second = new BinaryList<TestTypeBinary>();
            Assert.IsTrue(first.IsSupersetOf(second));
            Assert.IsFalse(second.IsSupersetOf(first));
            Assert.AreEqual(CheckIsSupersetWith(first, second), first.IsSupersetOf(second));
            Assert.AreEqual(CheckIsSupersetWith(second, first), second.IsSupersetOf(first));

            // 5. Degenerate case: Both collections are empty
            first = new BinaryList<TestTypeBinary>();
            second = new BinaryList<TestTypeBinary>();
            Assert.IsTrue(first.IsSupersetOf(second));
            Assert.AreEqual(CheckIsSupersetWith(first, second), first.IsSupersetOf(second));

            // =========================================================================
            // CRITICAL MULTISET EDGE CASES
            // =========================================================================

            // 6. CRITICAL EDGE CASE: Interleaved unmatched elements inside the source layout
            // Verifies that when the target demands sequential verification, the source pointer
            // correctly steps over internal unique blocks (like 20) without crashing the stream synchronization.
            first = new BinaryList<TestTypeBinary> { 10, 10, 20, 20, 30 };
            second = new BinaryList<TestTypeBinary> { 10, 30 };
            Assert.IsTrue(first.IsSupersetOf(second));
            Assert.IsFalse(second.IsSupersetOf(first));
            Assert.AreEqual(CheckIsSupersetWith(first, second), first.IsSupersetOf(second));
            Assert.AreEqual(CheckIsSupersetWith(second, first), second.IsSupersetOf(first));

            // 7. CRITICAL EDGE CASE: Edge duplicates expansion
            // Target boundaries are tightly enclosed, but source has massive duplicate blocks on the borders.
            first = new BinaryList<TestTypeBinary> { 5, 5, 5, 6, 7, 7, 7 };
            second = new BinaryList<TestTypeBinary> { 5, 6, 7 };
            Assert.IsTrue(first.IsSupersetOf(second));
            Assert.IsFalse(second.IsSupersetOf(first));
            Assert.AreEqual(CheckIsSupersetWith(first, second), first.IsSupersetOf(second));
            Assert.AreEqual(CheckIsSupersetWith(second, first), second.IsSupersetOf(first));

            // 8. CRITICAL EDGE CASE: Partial matching leading to a target index breach
            // Source contains everything except the very last required element from target's trailing frame.
            first = new BinaryList<TestTypeBinary> { 1, 2, 3, 4 };
            second = new BinaryList<TestTypeBinary> { 2, 3, 4, 5 };
            Assert.IsFalse(first.IsSupersetOf(second));
            Assert.IsFalse(second.IsSupersetOf(first));
            Assert.AreEqual(CheckIsSupersetWith(first, second), first.IsSupersetOf(second));
            Assert.AreEqual(CheckIsSupersetWith(second, first), second.IsSupersetOf(first));
        }

        [TestMethod]
        public void IsSupersetOf_InComparable()
        {
            // Multiset Superset Oracle: Source is a superset of target ONLY IF 
            // for every key, source contains GREATER OR EQUAL number of duplicates than target.
            bool CheckIsSupersetWith(IReadOnlyList<TestTypeHash> source, IReadOnlyList<TestTypeHash> target)
            {
                var targetCounts = target.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
                var sourceCounts = source.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

                foreach (var pair in targetCounts)
                {
                    if (!sourceCounts.TryGetValue(pair.Key, out int sourceCount) || sourceCount < pair.Value)
                    {
                        return false;
                    }
                }
                return true;
            }

            BinaryList<TestTypeHash> first, second;

            // 1. Standard containment (first is strictly larger and encompasses second)
            first = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 6, 7, 8, 9 };
            Assert.IsTrue(first.IsSupersetOf(second));
            Assert.IsFalse(second.IsSupersetOf(first));
            Assert.AreEqual(CheckIsSupersetWith(first, second), first.IsSupersetOf(second));
            Assert.AreEqual(CheckIsSupersetWith(second, first), second.IsSupersetOf(first));

            // 2. Identical structural and duplicate layout (Symmetric superset evaluation)
            first = new BinaryList<TestTypeHash> { 1, 2, 2, 3, 3, 3 };
            second = new BinaryList<TestTypeHash> { 1, 2, 2, 3, 3, 3 };
            Assert.IsTrue(first.IsSupersetOf(second));
            Assert.AreEqual(CheckIsSupersetWith(first, second), first.IsSupersetOf(second));

            // 3. Multiset Failure: Target contains more duplicates in a specific block
            first = new BinaryList<TestTypeHash> { 1, 2, 2, 3 };
            second = new BinaryList<TestTypeHash> { 1, 2, 2, 2, 3 }; // Target has an extra '2'
            Assert.IsFalse(first.IsSupersetOf(second));
            Assert.IsTrue(second.IsSupersetOf(first));
            Assert.AreEqual(CheckIsSupersetWith(first, second), first.IsSupersetOf(second));
            Assert.AreEqual(CheckIsSupersetWith(second, first), second.IsSupersetOf(first));

            // 4. Case with empty target (Any populated list is a superset of an empty collection)
            first = new BinaryList<TestTypeHash> { 0, 1, 3 };
            second = new BinaryList<TestTypeHash>();
            Assert.IsTrue(first.IsSupersetOf(second));
            Assert.IsFalse(second.IsSupersetOf(first));
            Assert.AreEqual(CheckIsSupersetWith(first, second), first.IsSupersetOf(second));
            Assert.AreEqual(CheckIsSupersetWith(second, first), second.IsSupersetOf(first));

            // 5. Degenerate case: Both collections are empty
            first = new BinaryList<TestTypeHash>();
            second = new BinaryList<TestTypeHash>();
            Assert.IsTrue(first.IsSupersetOf(second));
            Assert.AreEqual(CheckIsSupersetWith(first, second), first.IsSupersetOf(second));

            // =========================================================================
            // CRITICAL MULTISET EDGE CASES
            // =========================================================================

            // 6. CRITICAL EDGE CASE: Interleaved unmatched elements inside the source layout
            // Verifies that when the target demands sequential verification, the source pointer
            // correctly steps over internal unique blocks (like 20) without crashing the stream synchronization.
            first = new BinaryList<TestTypeHash> { 10, 10, 20, 20, 30 };
            second = new BinaryList<TestTypeHash> { 10, 30 };
            Assert.IsTrue(first.IsSupersetOf(second));
            Assert.IsFalse(second.IsSupersetOf(first));
            Assert.AreEqual(CheckIsSupersetWith(first, second), first.IsSupersetOf(second));
            Assert.AreEqual(CheckIsSupersetWith(second, first), second.IsSupersetOf(first));

            // 7. CRITICAL EDGE CASE: Edge duplicates expansion
            // Target boundaries are tightly enclosed, but source has massive duplicate blocks on the borders.
            first = new BinaryList<TestTypeHash> { 5, 5, 5, 6, 7, 7, 7 };
            second = new BinaryList<TestTypeHash> { 5, 6, 7 };
            Assert.IsTrue(first.IsSupersetOf(second));
            Assert.IsFalse(second.IsSupersetOf(first));
            Assert.AreEqual(CheckIsSupersetWith(first, second), first.IsSupersetOf(second));
            Assert.AreEqual(CheckIsSupersetWith(second, first), second.IsSupersetOf(first));

            // 8. CRITICAL EDGE CASE: Partial matching leading to a target index breach
            // Source contains everything except the very last required element from target's trailing frame.
            first = new BinaryList<TestTypeHash> { 1, 2, 3, 4 };
            second = new BinaryList<TestTypeHash> { 2, 3, 4, 5 };
            Assert.IsFalse(first.IsSupersetOf(second));
            Assert.IsFalse(second.IsSupersetOf(first));
            Assert.AreEqual(CheckIsSupersetWith(first, second), first.IsSupersetOf(second));
            Assert.AreEqual(CheckIsSupersetWith(second, first), second.IsSupersetOf(first));
        }
    }
}