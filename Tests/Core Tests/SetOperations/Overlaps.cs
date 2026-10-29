using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        public void Overlaps()
        {
            bool CheckOverlapsWith(IReadOnlyList<TestTypeComparable> source, IReadOnlyList<TestTypeComparable> target)
            {
                // Strict mathematical overlaps evaluation: checks if at least one shared element exists
                return source.Any(s => target.Contains(s));
            }

            BinaryList<TestTypeComparable> first, second;

            // 1. Empty both lists 
            first = new BinaryList<TestTypeComparable>();
            second = new BinaryList<TestTypeComparable>();
            Assert.IsFalse(second.Overlaps(first));
            Assert.AreEqual(CheckOverlapsWith(second, first), second.Overlaps(first));

            // 2. Empty lists case
            first = new BinaryList<TestTypeComparable>();
            second = new BinaryList<TestTypeComparable> { 2, 4, 6 };
            Assert.IsFalse(first.Overlaps(second));
            Assert.IsFalse(second.Overlaps(first));
            Assert.AreEqual(CheckOverlapsWith(first, second), first.Overlaps(second));
            Assert.AreEqual(CheckOverlapsWith(second, first), second.Overlaps(first));

            // 3. Perfect match of single-element lists (basic invariant)
            first = new BinaryList<TestTypeComparable> { 5 };
            second = new BinaryList<TestTypeComparable> { 5 };
            Assert.IsTrue(first.Overlaps(second));
            Assert.AreEqual(CheckOverlapsWith(first, second), first.Overlaps(second));

            // 4. No intersections, but value ranges overlap index-wise
            first = new BinaryList<TestTypeComparable> { 1, 3, 5 };
            second = new BinaryList<TestTypeComparable> { 2, 4, 6 };
            Assert.IsFalse(first.Overlaps(second));
            Assert.IsFalse(second.Overlaps(first));
            Assert.AreEqual(CheckOverlapsWith(first, second), first.Overlaps(second));
            Assert.AreEqual(CheckOverlapsWith(second, first), second.Overlaps(first));

            // 5. Match found on duplicated elements deeper inside the sequence block
            first = new BinaryList<TestTypeComparable> { 2, 2, 2, 3 };
            second = new BinaryList<TestTypeComparable> { 1, 2, 2 };
            Assert.IsTrue(first.Overlaps(second));
            Assert.IsTrue(second.Overlaps(first));
            Assert.AreEqual(CheckOverlapsWith(first, second), first.Overlaps(second));
            Assert.AreEqual(CheckOverlapsWith(second, first), second.Overlaps(first));

            // 6. CRITICAL EDGE CASE: Left boundary clipping (FindLeftIndex vulnerability)
            // If SkipTargetTo expects an EXACT match for '10', it will fail to find it in [5, 20, 25] and return false.
            // However, it must evaluate to true because a valid intersection exists further down at '20'.
            first = new BinaryList<TestTypeComparable> { 10, 20, 30 };
            second = new BinaryList<TestTypeComparable> { 5, 20, 25 };
            Assert.IsTrue(first.Overlaps(second));
            Assert.IsTrue(second.Overlaps(first));
            Assert.AreEqual(CheckOverlapsWith(first, second), first.Overlaps(second));
            Assert.AreEqual(CheckOverlapsWith(second, first), second.Overlaps(first));

            // 7. CRITICAL EDGE CASE: Right boundary clipping (FindRightIndex vulnerability)
            // The algorithm calculates the right boundary limit for '20' within the first list. 
            // The second list lacks '20'. The algorithm must not crash or exit early with false; 
            // it must successfully find the intersection at '15'.
            first = new BinaryList<TestTypeComparable> { 5, 15, 25 };
            second = new BinaryList<TestTypeComparable> { 10, 15, 20 };
            Assert.IsTrue(first.Overlaps(second));
            Assert.IsTrue(second.Overlaps(first));
            Assert.AreEqual(CheckOverlapsWith(first, second), first.Overlaps(second));
            Assert.AreEqual(CheckOverlapsWith(second, first), second.Overlaps(first));
        }
    }
}