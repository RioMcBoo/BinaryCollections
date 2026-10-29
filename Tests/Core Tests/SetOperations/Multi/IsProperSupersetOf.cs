using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        public void IsProperSupersetOf()
        {
            // Multiset Proper Superset Oracle: Source must contain target,
            // AND source must have strictly more total elements than target.
            bool CheckIsProperSupersetWith(IReadOnlyList<TestTypeComparable> source, IReadOnlyList<TestTypeComparable> target)
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
                return source.Count > target.Count;
            }

            BinaryList<TestTypeComparable> first, second;

            // 1. Standard containment with unique elements (Strictly larger)
            first = new BinaryList<TestTypeComparable> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            second = new BinaryList<TestTypeComparable> { 0, 1, 2, 3, 4, 6, 7, 8, 9 };
            Assert.IsTrue(first.IsProperSupersetOf(second));
            Assert.IsFalse(second.IsProperSupersetOf(first));
            Assert.AreEqual(CheckIsProperSupersetWith(first, second), first.IsProperSupersetOf(second));
            Assert.AreEqual(CheckIsProperSupersetWith(second, first), second.IsProperSupersetOf(first));

            // 2. CRITICAL PROPER CASE: Identity layout match rejection
            // Even though containment holds, it is not STUCTLY larger. Must return false.
            first = new BinaryList<TestTypeComparable> { 1, 2, 3, 3, 4 };
            second = new BinaryList<TestTypeComparable> { 1, 2, 3, 3, 4 };
            Assert.IsFalse(first.IsProperSupersetOf(second));
            Assert.AreEqual(CheckIsProperSupersetWith(first, second), first.IsProperSupersetOf(second));

            // 3. CRITICAL PROPER CASE: Only extra duplicates (No new unique values)
            // For multiset, extra duplicates count as a valid proper increment.
            first = new BinaryList<TestTypeComparable> { 5, 5, 5, 6, 6 };
            second = new BinaryList<TestTypeComparable> { 5, 6 };
            Assert.IsTrue(first.IsProperSupersetOf(second));
            Assert.IsFalse(second.IsProperSupersetOf(first));
            Assert.AreEqual(CheckIsProperSupersetWith(first, second), first.IsProperSupersetOf(second));
            Assert.AreEqual(CheckIsProperSupersetWith(second, first), second.IsProperSupersetOf(first));

            // 4. Case with empty target (Populated list is a proper superset of empty)
            first = new BinaryList<TestTypeComparable> { 0, 1, 3 };
            second = new BinaryList<TestTypeComparable>();
            Assert.IsTrue(first.IsProperSupersetOf(second));
            Assert.IsFalse(second.IsProperSupersetOf(first));
            Assert.AreEqual(CheckIsProperSupersetWith(first, second), first.IsProperSupersetOf(second));
            Assert.AreEqual(CheckIsProperSupersetWith(second, first), second.IsProperSupersetOf(first));

            // 5. DEGENERATE CASE: Both empty (False, cannot be strictly larger)
            first = new BinaryList<TestTypeComparable>();
            second = new BinaryList<TestTypeComparable>();
            Assert.IsFalse(first.IsProperSupersetOf(second));
            Assert.AreEqual(CheckIsProperSupersetWith(first, second), first.IsProperSupersetOf(second));

            // 6. Multiset failure despite larger total count
            // first has more elements overall (6 items vs 5), but lacks the required third '5'.
            first = new BinaryList<TestTypeComparable> { 5, 5, 6, 7, 8, 9 };
            second = new BinaryList<TestTypeComparable> { 5, 5, 5, 6, 7 };
            Assert.IsFalse(first.IsProperSupersetOf(second));
            Assert.IsFalse(second.IsProperSupersetOf(first));
            Assert.AreEqual(CheckIsProperSupersetWith(first, second), first.IsProperSupersetOf(second));
            Assert.AreEqual(CheckIsProperSupersetWith(second, first), second.IsProperSupersetOf(first));

            // 7. Extra unique values interleaved deep inside the source span
            first = new BinaryList<TestTypeComparable> { 10, 15, 20, 25, 30 };
            second = new BinaryList<TestTypeComparable> { 10, 20, 30 };
            Assert.IsTrue(first.IsProperSupersetOf(second));
            Assert.IsFalse(second.IsProperSupersetOf(first));
            Assert.AreEqual(CheckIsProperSupersetWith(first, second), first.IsProperSupersetOf(second));
            Assert.AreEqual(CheckIsProperSupersetWith(second, first), second.IsProperSupersetOf(first));

            // 8. Edge-heavy duplicate layout in source matching tight target boundaries
            first = new BinaryList<TestTypeComparable> { 10, 10, 10, 11, 12, 12, 12 };
            second = new BinaryList<TestTypeComparable> { 10, 11, 12 };
            Assert.IsTrue(first.IsProperSupersetOf(second));
            Assert.IsFalse(second.IsProperSupersetOf(first));
            Assert.AreEqual(CheckIsProperSupersetWith(first, second), first.IsProperSupersetOf(second));
            Assert.AreEqual(CheckIsProperSupersetWith(second, first), second.IsProperSupersetOf(first));
        }
    }
}