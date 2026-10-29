using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        public void SetEqualsUnique()
        {
            // Unique Equality Oracle: Both structures must share the exact same set of unique keys,
            // completely ignoring their duplicate counts and total lengths.
            bool CheckSetEqualsWithUnique(IReadOnlyList<TestTypeComparable> source, IReadOnlyList<TestTypeComparable> target)
            {
                var sourceKeys = source.GroupBy(x => x).Select(g => g.Key).ToHashSet();
                var targetKeys = target.GroupBy(x => x).Select(g => g.Key).ToHashSet();

                return sourceKeys.SetEquals(targetKeys);
            }

            BinaryList<TestTypeComparable> first, second;

            // 1. Identical unique elements, identical duplicate layout
            first = new BinaryList<TestTypeComparable> { 10, 20, 30 };
            second = new BinaryList<TestTypeComparable> { 10, 20, 30 };
            Assert.IsTrue(first.SetEqualsUnique(second));
            Assert.AreEqual(CheckSetEqualsWithUnique(first, second), first.SetEqualsUnique(second));

            // 2. CRITICAL UNIQUE CASE: Wildly different lengths and duplicates, but same unique keys (TRUE)
            // Both collapse to the unique set {1, 2, 3}
            first = new BinaryList<TestTypeComparable> { 1, 1, 1, 1, 2, 3, 3 };
            second = new BinaryList<TestTypeComparable> { 1, 2, 2, 2, 2, 3 };
            Assert.IsTrue(first.SetEqualsUnique(second));
            Assert.IsTrue(second.SetEqualsUnique(first));
            Assert.AreEqual(CheckSetEqualsWithUnique(first, second), first.SetEqualsUnique(second));
            Assert.AreEqual(CheckSetEqualsWithUnique(second, first), second.SetEqualsUnique(first));

            // 3. Both empty (True)
            first = new BinaryList<TestTypeComparable>();
            second = new BinaryList<TestTypeComparable>();
            Assert.IsTrue(first.SetEqualsUnique(second));
            Assert.AreEqual(CheckSetEqualsWithUnique(first, second), first.SetEqualsUnique(second));

            // 4. CRITICAL UNIQUE CASE: One extra unique value at the end of target
            // first = {5, 6}, second = {5, 6, 7}. Unique mismatch.
            first = new BinaryList<TestTypeComparable> { 5, 5, 5, 6, 6 };
            second = new BinaryList<TestTypeComparable> { 5, 6, 7 };
            Assert.IsFalse(first.SetEqualsUnique(second));
            Assert.IsFalse(second.SetEqualsUnique(first));
            Assert.AreEqual(CheckSetEqualsWithUnique(first, second), first.SetEqualsUnique(second));
            Assert.AreEqual(CheckSetEqualsWithUnique(second, first), second.SetEqualsUnique(first));

            // 5. CRITICAL UNIQUE CASE: Missing center value
            // Boundaries match (10 and 30), but center element is unaligned.
            first = new BinaryList<TestTypeComparable> { 10, 20, 30 };
            second = new BinaryList<TestTypeComparable> { 10, 10, 30, 30, 30 }; // '20' is missing
            Assert.IsFalse(first.SetEqualsUnique(second));
            Assert.IsFalse(second.SetEqualsUnique(first));
            Assert.AreEqual(CheckSetEqualsWithUnique(first, second), first.SetEqualsUnique(second));
            Assert.AreEqual(CheckSetEqualsWithUnique(second, first), second.SetEqualsUnique(first));

            // 6. Advanced structural drift with massive duplicate blocks
            first = new BinaryList<TestTypeComparable> { 1, 1, 1, 2, 2, 2, 2, 3 };
            second = new BinaryList<TestTypeComparable> { 1, 2, 4, 4, 4, 4 }; // '3' vs '4' mismatch
            Assert.IsFalse(first.SetEqualsUnique(second));
            Assert.IsFalse(second.SetEqualsUnique(first));
            Assert.AreEqual(CheckSetEqualsWithUnique(first, second), first.SetEqualsUnique(second));
            Assert.AreEqual(CheckSetEqualsWithUnique(second, first), second.SetEqualsUnique(first));
        }
    }
}