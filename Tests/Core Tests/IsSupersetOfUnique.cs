using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        public void IsSupersetOfUnique_Comparable()
        {
            // Unique Set Superset Oracle: Source is a superset of target ONLY IF
            // every unique key existing in target is also present within source, completely disregarding duplicate layout counts.
            bool CheckIsSupersetWithUnique(IReadOnlyList<TestTypeBinary> source, IReadOnlyList<TestTypeBinary> target)
            {
                var targetKeys = target.GroupBy(x => x).Select(g => g.Key).ToHashSet();
                var sourceKeys = source.GroupBy(x => x).Select(g => g.Key).ToHashSet();

                foreach (var key in targetKeys)
                {
                    if (!sourceKeys.Contains(key))
                    {
                        return false;
                    }
                }
                return true;
            }

            BinaryList<TestTypeBinary> first, second;

            // 1. Standard unique superset
            first = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 6, 7, 8, 9 };
            Assert.IsTrue(first.IsSupersetOfUnique(second));
            Assert.IsFalse(second.IsSupersetOfUnique(first));
            Assert.AreEqual(CheckIsSupersetWithUnique(first, second), first.IsSupersetOfUnique(second));
            Assert.AreEqual(CheckIsSupersetWithUnique(second, first), second.IsSupersetOfUnique(first));

            // 2. Empty lists verification
            first = new BinaryList<TestTypeBinary> { 1, 2 };
            second = new BinaryList<TestTypeBinary>();
            Assert.IsTrue(first.IsSupersetOfUnique(second));
            Assert.IsFalse(second.IsSupersetOfUnique(first));
            Assert.AreEqual(CheckIsSupersetWithUnique(first, second), first.IsSupersetOfUnique(second));
            Assert.AreEqual(CheckIsSupersetWithUnique(second, first), second.IsSupersetOfUnique(first));

            // 3. Both empty (Symmetric true condition)
            first = new BinaryList<TestTypeBinary>();
            second = new BinaryList<TestTypeBinary>();
            Assert.IsTrue(first.IsSupersetOfUnique(second));
            Assert.AreEqual(CheckIsSupersetWithUnique(first, second), first.IsSupersetOfUnique(second));

            // =========================================================================
            // UNIQUE-LOGIC SPECIFIC EDGE CASES
            // =========================================================================

            // 4. CRITICAL UNIQUE CASE: Target contains MORE duplicates than source
            // Must return FALSE for MultiSet logic, but MUST return TRUE for unique layout matching.
            first = new BinaryList<TestTypeBinary> { 10, 20 };
            second = new BinaryList<TestTypeBinary> { 10, 10, 10, 20, 20, 20 };
            Assert.IsTrue(first.IsSupersetOfUnique(second));
            Assert.IsTrue(second.IsSupersetOfUnique(first));
            Assert.AreEqual(CheckIsSupersetWithUnique(first, second), first.IsSupersetOfUnique(second));
            Assert.AreEqual(CheckIsSupersetWithUnique(second, first), second.IsSupersetOfUnique(first));

            // 5. CRITICAL UNIQUE CASE: Alternating compressed patterns
            // Target has more elements in block A, source has more elements in block B.
            first = new BinaryList<TestTypeBinary> { 5, 6, 6, 6, 6 };
            second = new BinaryList<TestTypeBinary> { 5, 5, 5, 5, 6 };
            Assert.IsTrue(first.IsSupersetOfUnique(second));
            Assert.IsTrue(second.IsSupersetOfUnique(first));
            Assert.AreEqual(CheckIsSupersetWithUnique(first, second), first.IsSupersetOfUnique(second));
            Assert.AreEqual(CheckIsSupersetWithUnique(second, first), second.IsSupersetOfUnique(first));

            // 6. CRITICAL UNIQUE CASE: Skipping massive nested target blocks via compression jump
            // Checks if the pointer processing skips right over unaligned multiple identical values.
            first = new BinaryList<TestTypeBinary> { 1, 1, 1, 2, 2, 5, 5, 5 };
            second = new BinaryList<TestTypeBinary> { 1, 2, 5 };
            Assert.IsTrue(first.IsSupersetOfUnique(second));
            Assert.IsTrue(second.IsSupersetOfUnique(first));
            Assert.AreEqual(CheckIsSupersetWithUnique(first, second), first.IsSupersetOfUnique(second));
            Assert.AreEqual(CheckIsSupersetWithUnique(second, first), second.IsSupersetOfUnique(first));

            // =========================================================================
            // ADVANCED SCANNING BOUNDARIES & STRUCTURAL DRIFT
            // =========================================================================

            // 7. Right boundary flushing with remaining redundant layouts
            // Source matches target fully, but source finishes with heavy trailing blocks of the last matching key.
            first = new BinaryList<TestTypeBinary> { 1, 2, 3, 3, 3, 3, 3 };
            second = new BinaryList<TestTypeBinary> { 1, 2, 3 };
            Assert.IsTrue(first.IsSupersetOfUnique(second));
            Assert.IsTrue(second.IsSupersetOfUnique(first));
            Assert.AreEqual(CheckIsSupersetWithUnique(first, second), first.IsSupersetOfUnique(second));
            Assert.AreEqual(CheckIsSupersetWithUnique(second, first), second.IsSupersetOfUnique(first));

            // 8. Missing internal center unique slot
            // Boundaries match completely (e.g. 10 and 40), but an explicit center value required by target is missing from source.
            first = new BinaryList<TestTypeBinary> { 10, 20, 40 };
            second = new BinaryList<TestTypeBinary> { 10, 30, 40 }; // '30' is missing inside source framework
            Assert.IsFalse(first.IsSupersetOfUnique(second));
            Assert.IsFalse(second.IsSupersetOfUnique(first));
            Assert.AreEqual(CheckIsSupersetWithUnique(first, second), first.IsSupersetOfUnique(second));
            Assert.AreEqual(CheckIsSupersetWithUnique(second, first), second.IsSupersetOfUnique(first));

            // 9. Extra target trailing layout blocks
            // Source matches the front frame perfectly but target introduces an unexpected trailing element '60'.
            first = new BinaryList<TestTypeBinary> { 10, 20, 30, 40, 50 };
            second = new BinaryList<TestTypeBinary> { 10, 20, 30, 40, 50, 60 };
            Assert.IsFalse(first.IsSupersetOfUnique(second));
            Assert.IsTrue(second.IsSupersetOfUnique(first));
            Assert.AreEqual(CheckIsSupersetWithUnique(first, second), first.IsSupersetOfUnique(second));
            Assert.AreEqual(CheckIsSupersetWithUnique(second, first), second.IsSupersetOfUnique(first));
        }

        [TestMethod]
        public void IsSupersetOfUnique_InComparable()
        {
            // Unique Set Superset Oracle: Source is a superset of target ONLY IF
            // every unique key existing in target is also present within source, completely disregarding duplicate layout counts.
            bool CheckIsSupersetWithUnique(IReadOnlyList<TestTypeHash> source, IReadOnlyList<TestTypeHash> target)
            {
                var targetKeys = target.GroupBy(x => x).Select(g => g.Key).ToHashSet();
                var sourceKeys = source.GroupBy(x => x).Select(g => g.Key).ToHashSet();

                foreach (var key in targetKeys)
                {
                    if (!sourceKeys.Contains(key))
                    {
                        return false;
                    }
                }
                return true;
            }

            BinaryList<TestTypeHash> first, second;

            // 1. Standard unique superset
            first = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 6, 7, 8, 9 };
            Assert.IsTrue(first.IsSupersetOfUnique(second));
            Assert.IsFalse(second.IsSupersetOfUnique(first));
            Assert.AreEqual(CheckIsSupersetWithUnique(first, second), first.IsSupersetOfUnique(second));
            Assert.AreEqual(CheckIsSupersetWithUnique(second, first), second.IsSupersetOfUnique(first));

            // 2. Empty lists verification
            first = new BinaryList<TestTypeHash> { 1, 2 };
            second = new BinaryList<TestTypeHash>();
            Assert.IsTrue(first.IsSupersetOfUnique(second));
            Assert.IsFalse(second.IsSupersetOfUnique(first));
            Assert.AreEqual(CheckIsSupersetWithUnique(first, second), first.IsSupersetOfUnique(second));
            Assert.AreEqual(CheckIsSupersetWithUnique(second, first), second.IsSupersetOfUnique(first));

            // 3. Both empty (Symmetric true condition)
            first = new BinaryList<TestTypeHash>();
            second = new BinaryList<TestTypeHash>();
            Assert.IsTrue(first.IsSupersetOfUnique(second));
            Assert.AreEqual(CheckIsSupersetWithUnique(first, second), first.IsSupersetOfUnique(second));

            // =========================================================================
            // UNIQUE-LOGIC SPECIFIC EDGE CASES
            // =========================================================================

            // 4. CRITICAL UNIQUE CASE: Target contains MORE duplicates than source
            // Must return FALSE for MultiSet logic, but MUST return TRUE for unique layout matching.
            first = new BinaryList<TestTypeHash> { 10, 20 };
            second = new BinaryList<TestTypeHash> { 10, 10, 10, 20, 20, 20 };
            Assert.IsTrue(first.IsSupersetOfUnique(second));
            Assert.IsTrue(second.IsSupersetOfUnique(first));
            Assert.AreEqual(CheckIsSupersetWithUnique(first, second), first.IsSupersetOfUnique(second));
            Assert.AreEqual(CheckIsSupersetWithUnique(second, first), second.IsSupersetOfUnique(first));

            // 5. CRITICAL UNIQUE CASE: Alternating compressed patterns
            // Target has more elements in block A, source has more elements in block B.
            first = new BinaryList<TestTypeHash> { 5, 6, 6, 6, 6 };
            second = new BinaryList<TestTypeHash> { 5, 5, 5, 5, 6 };
            Assert.IsTrue(first.IsSupersetOfUnique(second));
            Assert.IsTrue(second.IsSupersetOfUnique(first));
            Assert.AreEqual(CheckIsSupersetWithUnique(first, second), first.IsSupersetOfUnique(second));
            Assert.AreEqual(CheckIsSupersetWithUnique(second, first), second.IsSupersetOfUnique(first));

            // 6. CRITICAL UNIQUE CASE: Skipping massive nested target blocks via compression jump
            // Checks if the pointer processing skips right over unaligned multiple identical values.
            first = new BinaryList<TestTypeHash> { 1, 1, 1, 2, 2, 5, 5, 5 };
            second = new BinaryList<TestTypeHash> { 1, 2, 5 };
            Assert.IsTrue(first.IsSupersetOfUnique(second));
            Assert.IsTrue(second.IsSupersetOfUnique(first));
            Assert.AreEqual(CheckIsSupersetWithUnique(first, second), first.IsSupersetOfUnique(second));
            Assert.AreEqual(CheckIsSupersetWithUnique(second, first), second.IsSupersetOfUnique(first));

            // =========================================================================
            // ADVANCED SCANNING BOUNDARIES & STRUCTURAL DRIFT
            // =========================================================================

            // 7. Right boundary flushing with remaining redundant layouts
            // Source matches target fully, but source finishes with heavy trailing blocks of the last matching key.
            first = new BinaryList<TestTypeHash> { 1, 2, 3, 3, 3, 3, 3 };
            second = new BinaryList<TestTypeHash> { 1, 2, 3 };
            Assert.IsTrue(first.IsSupersetOfUnique(second));
            Assert.IsTrue(second.IsSupersetOfUnique(first));
            Assert.AreEqual(CheckIsSupersetWithUnique(first, second), first.IsSupersetOfUnique(second));
            Assert.AreEqual(CheckIsSupersetWithUnique(second, first), second.IsSupersetOfUnique(first));

            // 8. Missing internal center unique slot
            // Boundaries match completely (e.g. 10 and 40), but an explicit center value required by target is missing from source.
            first = new BinaryList<TestTypeHash> { 10, 20, 40 };
            second = new BinaryList<TestTypeHash> { 10, 30, 40 }; // '30' is missing inside source framework
            Assert.IsFalse(first.IsSupersetOfUnique(second));
            Assert.IsFalse(second.IsSupersetOfUnique(first));
            Assert.AreEqual(CheckIsSupersetWithUnique(first, second), first.IsSupersetOfUnique(second));
            Assert.AreEqual(CheckIsSupersetWithUnique(second, first), second.IsSupersetOfUnique(first));

            // 9. Extra target trailing layout blocks
            // Source matches the front frame perfectly but target introduces an unexpected trailing element '60'.
            first = new BinaryList<TestTypeHash> { 10, 20, 30, 40, 50 };
            second = new BinaryList<TestTypeHash> { 10, 20, 30, 40, 50, 60 };
            Assert.IsFalse(first.IsSupersetOfUnique(second));
            Assert.IsTrue(second.IsSupersetOfUnique(first));
            Assert.AreEqual(CheckIsSupersetWithUnique(first, second), first.IsSupersetOfUnique(second));
            Assert.AreEqual(CheckIsSupersetWithUnique(second, first), second.IsSupersetOfUnique(first));
        }
    }
}