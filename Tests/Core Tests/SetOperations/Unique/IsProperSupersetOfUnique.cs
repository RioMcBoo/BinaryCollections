using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        public void IsProperSupersetOfUnique_Comparable()
        {
            // Unique Proper Superset Oracle: Source must contain all unique keys of target,
            // AND source must have strictly MORE unique keys than target.
            bool CheckIsProperSupersetWithUnique(IReadOnlyList<TestTypeBinary> source, IReadOnlyList<TestTypeBinary> target)
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
                return sourceKeys.Count > targetKeys.Count;
            }

            BinaryList<TestTypeBinary> first, second;

            // 1. Standard unique proper superset (first has extra unique key: 5)
            first = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            second = new BinaryList<TestTypeBinary> { 0, 1, 2, 3, 4, 6, 7, 8, 9 };
            Assert.IsTrue(first.IsProperSupersetOfUnique(second));
            Assert.IsFalse(second.IsProperSupersetOfUnique(first));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(first, second), first.IsProperSupersetOfUnique(second));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(second, first), second.IsProperSupersetOfUnique(first));

            // 2. Equal unique skeleton but different duplicate counts
            // For unique logic, these represent the exact same set, so proper superset MUST be false.
            first = new BinaryList<TestTypeBinary> { 10, 10, 10, 20, 20, 20 };
            second = new BinaryList<TestTypeBinary> { 10, 20 };
            Assert.IsFalse(first.IsProperSupersetOfUnique(second));
            Assert.IsFalse(second.IsProperSupersetOfUnique(first));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(first, second), first.IsProperSupersetOfUnique(second));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(second, first), second.IsProperSupersetOfUnique(first));

            // 3. Case with empty target (Populated unique set is a proper unique superset)
            first = new BinaryList<TestTypeBinary> { 1, 2 };
            second = new BinaryList<TestTypeBinary>();
            Assert.IsTrue(first.IsProperSupersetOfUnique(second));
            Assert.IsFalse(second.IsProperSupersetOfUnique(first));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(first, second), first.IsProperSupersetOfUnique(second));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(second, first), second.IsProperSupersetOfUnique(first));

            // 4. DEGENERATE CASE: Both empty (Must return false)
            first = new BinaryList<TestTypeBinary>();
            second = new BinaryList<TestTypeBinary>();
            Assert.IsFalse(first.IsProperSupersetOfUnique(second));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(first, second), first.IsProperSupersetOfUnique(second));

            // 5. CRITICAL UNIQUE CASE: Source has LESS total elements but MORE unique keys
            // source length is 3, target length is 7. But source unique keys are {5, 6, 7} 
            // while target unique keys are {5, 6}. This must return TRUE.
            first = new BinaryList<TestTypeBinary> { 5, 6, 7 };
            second = new BinaryList<TestTypeBinary> { 5, 5, 5, 5, 6, 6, 6 };
            Assert.IsTrue(first.IsProperSupersetOfUnique(second));
            Assert.IsFalse(second.IsProperSupersetOfUnique(first));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(first, second), first.IsProperSupersetOfUnique(second));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(second, first), second.IsProperSupersetOfUnique(first));

            // 6. Target has a center unique value completely missing from source
            // Even though boundaries match, containment fails.
            first = new BinaryList<TestTypeBinary> { 10, 20, 40 };
            second = new BinaryList<TestTypeBinary> { 10, 30, 40 }; // '30' is not in source
            Assert.IsFalse(first.IsProperSupersetOfUnique(second));
            Assert.IsFalse(second.IsProperSupersetOfUnique(first));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(first, second), first.IsProperSupersetOfUnique(second));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(second, first), second.IsProperSupersetOfUnique(first));

            // 7. Source has massive nested duplicate jumps and extra unique values inside
            first = new BinaryList<TestTypeBinary> { 1, 1, 1, 2, 3, 4, 5, 5, 5 };
            second = new BinaryList<TestTypeBinary> { 1, 5 }; // 2, 3, 4 are extra unique elements in source
            Assert.IsTrue(first.IsProperSupersetOfUnique(second));
            Assert.IsFalse(second.IsProperSupersetOfUnique(first));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(first, second), first.IsProperSupersetOfUnique(second));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(second, first), second.IsProperSupersetOfUnique(first));

            // 8. Alternating duplicate layout with genuine new unique keys in source
            first = new BinaryList<TestTypeBinary> { 10, 10, 15, 20, 25, 30, 30 };
            second = new BinaryList<TestTypeBinary> { 10, 10, 10, 20, 20, 30 }; // '15' and '25' are purely unique to source
            Assert.IsTrue(first.IsProperSupersetOfUnique(second));
            Assert.IsFalse(second.IsProperSupersetOfUnique(first));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(first, second), first.IsProperSupersetOfUnique(second));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(second, first), second.IsProperSupersetOfUnique(first));
        }

        [TestMethod]
        public void IsProperSupersetOfUnique_InComparable()
        {
            // Unique Proper Superset Oracle: Source must contain all unique keys of target,
            // AND source must have strictly MORE unique keys than target.
            bool CheckIsProperSupersetWithUnique(IReadOnlyList<TestTypeHash> source, IReadOnlyList<TestTypeHash> target)
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
                return sourceKeys.Count > targetKeys.Count;
            }

            BinaryList<TestTypeHash> first, second;

            // 1. Standard unique proper superset (first has extra unique key: 5)
            first = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            second = new BinaryList<TestTypeHash> { 0, 1, 2, 3, 4, 6, 7, 8, 9 };
            Assert.IsTrue(first.IsProperSupersetOfUnique(second));
            Assert.IsFalse(second.IsProperSupersetOfUnique(first));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(first, second), first.IsProperSupersetOfUnique(second));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(second, first), second.IsProperSupersetOfUnique(first));

            // 2. Equal unique skeleton but different duplicate counts
            // For unique logic, these represent the exact same set, so proper superset MUST be false.
            first = new BinaryList<TestTypeHash> { 10, 10, 10, 20, 20, 20 };
            second = new BinaryList<TestTypeHash> { 10, 20 };
            Assert.IsFalse(first.IsProperSupersetOfUnique(second));
            Assert.IsFalse(second.IsProperSupersetOfUnique(first));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(first, second), first.IsProperSupersetOfUnique(second));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(second, first), second.IsProperSupersetOfUnique(first));

            // 3. Case with empty target (Populated unique set is a proper unique superset)
            first = new BinaryList<TestTypeHash> { 1, 2 };
            second = new BinaryList<TestTypeHash>();
            Assert.IsTrue(first.IsProperSupersetOfUnique(second));
            Assert.IsFalse(second.IsProperSupersetOfUnique(first));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(first, second), first.IsProperSupersetOfUnique(second));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(second, first), second.IsProperSupersetOfUnique(first));

            // 4. DEGENERATE CASE: Both empty (Must return false)
            first = new BinaryList<TestTypeHash>();
            second = new BinaryList<TestTypeHash>();
            Assert.IsFalse(first.IsProperSupersetOfUnique(second));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(first, second), first.IsProperSupersetOfUnique(second));

            // 5. CRITICAL UNIQUE CASE: Source has LESS total elements but MORE unique keys
            // source length is 3, target length is 7. But source unique keys are {5, 6, 7} 
            // while target unique keys are {5, 6}. This must return TRUE.
            first = new BinaryList<TestTypeHash> { 5, 6, 7 };
            second = new BinaryList<TestTypeHash> { 5, 5, 5, 5, 6, 6, 6 };
            Assert.IsTrue(first.IsProperSupersetOfUnique(second));
            Assert.IsFalse(second.IsProperSupersetOfUnique(first));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(first, second), first.IsProperSupersetOfUnique(second));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(second, first), second.IsProperSupersetOfUnique(first));

            // 6. Target has a center unique value completely missing from source
            // Even though boundaries match, containment fails.
            first = new BinaryList<TestTypeHash> { 10, 20, 40 };
            second = new BinaryList<TestTypeHash> { 10, 30, 40 }; // '30' is not in source
            Assert.IsFalse(first.IsProperSupersetOfUnique(second));
            Assert.IsFalse(second.IsProperSupersetOfUnique(first));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(first, second), first.IsProperSupersetOfUnique(second));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(second, first), second.IsProperSupersetOfUnique(first));

            // 7. Source has massive nested duplicate jumps and extra unique values inside
            first = new BinaryList<TestTypeHash> { 1, 1, 1, 2, 3, 4, 5, 5, 5 };
            second = new BinaryList<TestTypeHash> { 1, 5 }; // 2, 3, 4 are extra unique elements in source
            Assert.IsTrue(first.IsProperSupersetOfUnique(second));
            Assert.IsFalse(second.IsProperSupersetOfUnique(first));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(first, second), first.IsProperSupersetOfUnique(second));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(second, first), second.IsProperSupersetOfUnique(first));

            // 8. Alternating duplicate layout with genuine new unique keys in source
            first = new BinaryList<TestTypeHash> { 10, 10, 15, 20, 25, 30, 30 };
            second = new BinaryList<TestTypeHash> { 10, 10, 10, 20, 20, 30 }; // '15' and '25' are purely unique to source
            Assert.IsTrue(first.IsProperSupersetOfUnique(second));
            Assert.IsFalse(second.IsProperSupersetOfUnique(first));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(first, second), first.IsProperSupersetOfUnique(second));
            Assert.AreEqual(CheckIsProperSupersetWithUnique(second, first), second.IsProperSupersetOfUnique(first));
        }
    }
}