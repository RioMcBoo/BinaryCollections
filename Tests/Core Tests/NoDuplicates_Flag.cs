using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        public void Flag_NoDuplicates()
        {
            bool MayContainDuplicates(BinaryList<TestTypeComparable> list)
            {
                return list.Flags.HasTheFlag(DuplicateDensity.Possible);
            }

            BinaryList<TestTypeComparable> first;
            TestTypeComparable[] second;

            // Since problems can only arise if this flag is erroneously present,
            // we will only check critical cases (mostly).

            // 1. Add(TValue item)
            first = new(); Assert.IsFalse(MayContainDuplicates(first));
            first.Add(1); Assert.IsFalse(MayContainDuplicates(first));
            first.Add(1); Assert.IsTrue(MayContainDuplicates(first));

            first = new(); Assert.IsFalse(MayContainDuplicates(first));
            first.Add(1); Assert.IsFalse(MayContainDuplicates(first));
            first.Add(2); Assert.IsFalse(MayContainDuplicates(first));
            first.Add(2); Assert.IsTrue(MayContainDuplicates(first));

            first = new(); Assert.IsFalse(MayContainDuplicates(first));
            first.Add(1); Assert.IsFalse(MayContainDuplicates(first));
            first.Add(2); Assert.IsFalse(MayContainDuplicates(first));
            first.Add(3); Assert.IsFalse(MayContainDuplicates(first));
            first.Add(3); Assert.IsTrue(MayContainDuplicates(first));

            // 2. AddUnique(TValue item) - Check that the NoDuplicates flag is set correctly after using AddUnique.
            first = [1]; Assert.IsFalse(MayContainDuplicates(first));
            first.AddUnique(2); Assert.IsFalse(MayContainDuplicates(first));
            first.AddUnique(2); Assert.IsFalse(MayContainDuplicates(first));

            first = [1, 1]; Assert.IsTrue(MayContainDuplicates(first));
            first.AddUnique(2); Assert.IsTrue(MayContainDuplicates(first));
            first.AddUnique(2); Assert.IsTrue(MayContainDuplicates(first));

            // 3. AddRange(IEnumerable<TValue> items)
            first = [1]; second = [1]; Assert.IsFalse(MayContainDuplicates(first));
            first.AddRange(second); Assert.IsTrue(MayContainDuplicates(first));

            first = [1]; second = [2]; Assert.IsFalse(MayContainDuplicates(first));
            first.AddRange(second); Assert.IsTrue(MayContainDuplicates(first));

            first = [1, 1]; second = [2]; Assert.IsTrue(MayContainDuplicates(first));
            first.AddRange(second); Assert.IsTrue(MayContainDuplicates(first));

            // 4. AddRangeUnique(IEnumerable<TValue> items)
            first = [1]; second = [1]; Assert.IsFalse(MayContainDuplicates(first));
            first.AddRangeUnique(second); Assert.IsFalse(MayContainDuplicates(first));

            first = [1]; second = [2]; Assert.IsFalse(MayContainDuplicates(first));
            first.AddRangeUnique(second); Assert.IsFalse(MayContainDuplicates(first));

            first = [1, 1]; second = [2]; Assert.IsTrue(MayContainDuplicates(first));
            first.AddRangeUnique(second); Assert.IsTrue(MayContainDuplicates(first));

            // 5. Replace(TValue oldItem, TValue newItem)
            first = [1, 2, 3]; Assert.IsFalse(MayContainDuplicates(first));
            first.Replace(new TestTypeComparable(2), 4);
            Assert.IsFalse(MayContainDuplicates(first));
            first.Replace(new TestTypeComparable(1), 4);
            Assert.IsTrue(MayContainDuplicates(first));

            first = [1, 2, 3, 3]; Assert.IsTrue(MayContainDuplicates(first));
            first.Replace(new TestTypeComparable(2), 4); Assert.IsTrue(MayContainDuplicates(first));
            first.Replace(new TestTypeComparable(1), 4); Assert.IsTrue(MayContainDuplicates(first));

            // 6. ReplaceUnique(TValue oldItem, TValue newItem)
            first = [1, 2, 3];
            first.ReplaceUnique(new TestTypeComparable(2), 4); Assert.IsFalse(MayContainDuplicates(first));
            first.ReplaceUnique(new TestTypeComparable(1), 4); Assert.IsFalse(MayContainDuplicates(first));

            first = [1, 2, 3, 3];
            first.ReplaceUnique(new TestTypeComparable(2), 4); Assert.IsTrue(MayContainDuplicates(first));
            first.ReplaceUnique(new TestTypeComparable(1), 4); Assert.IsTrue(MayContainDuplicates(first));

            // 7. Remove(TValue item)
            first = [1, 2, 3, 4, 5, 5]; Assert.IsTrue(MayContainDuplicates(first));
            first.Remove(5); Assert.IsTrue(MayContainDuplicates(first));
            first.Remove(5); Assert.IsTrue(MayContainDuplicates(first));
            first.Remove(4); Assert.IsTrue(MayContainDuplicates(first));
            first.Remove(3); Assert.IsTrue(MayContainDuplicates(first));
            first.Remove(2); Assert.IsFalse(MayContainDuplicates(first));
            first.Remove(1); Assert.IsFalse(MayContainDuplicates(first));

            first = [1, 2, 3, 4, 5]; Assert.IsFalse(MayContainDuplicates(first));
            first.Remove(1); Assert.IsFalse(MayContainDuplicates(first));
            first.Remove(2); Assert.IsFalse(MayContainDuplicates(first));
            first.Remove(3); Assert.IsFalse(MayContainDuplicates(first));
            first.Remove(4); Assert.IsFalse(MayContainDuplicates(first));
            first.Remove(5); Assert.IsFalse(MayContainDuplicates(first));
            first.Remove(5); Assert.IsFalse(MayContainDuplicates(first));

            // 8. Clear()
            first = [1, 1]; Assert.IsTrue(MayContainDuplicates(first));
            first.Clear(); Assert.IsFalse(MayContainDuplicates(first));

            // 9. ExtractArray(int newArraySize)
            first = [1, 1]; Assert.IsTrue(MayContainDuplicates(first));
            first.ExtractArray(newArraySize: 1); Assert.IsFalse(MayContainDuplicates(first));

            first = [1, 1]; Assert.IsTrue(MayContainDuplicates(first));
            first.ExtractArray(newArraySize: 0); Assert.IsFalse(MayContainDuplicates(first));

            first = [1, 1]; Assert.IsTrue(MayContainDuplicates(first));
            first.ExtractArray(); Assert.IsTrue(MayContainDuplicates(first));

            // 10. ExceptWith
            first = [1, 2, 3, 4, 5, 5]; second = [3, 4, 5, 5]; Assert.IsTrue(MayContainDuplicates(first));
            first.ExceptWith(second); Assert.IsTrue(MayContainDuplicates(first));

            first = [1, 2, 3, 4, 5, 5]; second = [2, 3, 4, 5, 5]; Assert.IsTrue(MayContainDuplicates(first));
            first.ExceptWith(second); Assert.IsFalse(MayContainDuplicates(first));

            // 11. IntersectWith
            first = [1, 2, 3, 4, 5, 5]; second = [6, 7, 5, 5]; Assert.IsTrue(MayContainDuplicates(first));
            first.IntersectWith(second); Assert.IsTrue(MayContainDuplicates(first));

            first = [1, 2, 3, 4, 5, 5]; second = [6, 7, 5]; Assert.IsTrue(MayContainDuplicates(first));
            first.IntersectWith(second); Assert.IsFalse(MayContainDuplicates(first));

            // 12. SymmetricExceptWith
            first = [1, 2, 3, 4, 5, 5, 7]; second = [1, 2, 3, 4, 5, 5, 6]; Assert.IsTrue(MayContainDuplicates(first));
            first.SymmetricExceptWith(second); Assert.IsTrue(MayContainDuplicates(first));

            first = [1, 2, 3, 4, 5, 5, 5]; second = [1, 2, 3, 4, 5, 5]; Assert.IsTrue(MayContainDuplicates(first));
            first.SymmetricExceptWith(second); Assert.IsFalse(MayContainDuplicates(first));

            // 12. UnionWith
            first = [1]; second = [2]; Assert.IsFalse(MayContainDuplicates(first));
            first.UnionWith(second); Assert.IsTrue(MayContainDuplicates(first));

            first = [1, 1]; second = [2]; Assert.IsTrue(MayContainDuplicates(first));
            first.UnionWith(second); Assert.IsTrue(MayContainDuplicates(first));
        }
    }
}