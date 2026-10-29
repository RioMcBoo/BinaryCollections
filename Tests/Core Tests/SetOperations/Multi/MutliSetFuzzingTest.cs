using BinaryCollections.BinaryList;
using static BinaryList.Tests.RawCollectionAssert;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void MultiSetOperations_HeavyStressTest(bool trim)
        {
            const int IterationCount = 200000;
            var rand = new Random(2026);

            for (int i = 0; i < IterationCount; i++)
            {
                int countSrc = rand.Next(0, 20);
                int countTgt = rand.Next(0, 20);

                var srcData = new TestTypeComparable[countSrc];
                for (int j = 0; j < countSrc; j++)
                    srcData[j] = new TestTypeComparable(rand.Next(0, 50));

                var tgtData = new TestTypeComparable[countTgt];
                for (int j = 0; j < countTgt; j++)
                    tgtData[j] = new TestTypeComparable(rand.Next(0, 50));

                int initialCapacity = rand.Next(0, countSrc * 3);
                BinaryList<TestTypeComparable> optimizedList;
                List<TestTypeComparable> expected;

                List<TestTypeComparable> RunExpectedMulti(IEnumerable<TestTypeComparable> src, IEnumerable<TestTypeComparable> tgt, Func<IEnumerable<TestTypeComparable>, IEnumerable<TestTypeComparable>, List<TestTypeComparable>> func)
                {
                    var local_src = src.ToList();
                    var local_tgt = tgt.ToList();

                    var res = func(local_src, local_tgt);
                    return res;
                }

                void TestFunction(string function)
                {
                    optimizedList = new BinaryList<TestTypeComparable>(srcData);
                    if (trim) optimizedList.Trim();

                    Func<IReadOnlyCollection<TestTypeComparable>, bool> optFunc;
                    Func<IEnumerable<TestTypeComparable>, IEnumerable<TestTypeComparable>, List<TestTypeComparable>> expFunc;

                    switch (function)
                    {
                        case "AddRange":
                            expFunc = MultiSetModifierExpected.AddRange;
                            optFunc = optimizedList.AddRange;
                            break;
                        case "IntersectWith":
                            expFunc = MultiSetModifierExpected.IntersectWith;
                            optFunc = optimizedList.IntersectWith;
                            break;
                        case "ExceptWith":
                            expFunc = MultiSetModifierExpected.ExceptWith;
                            optFunc = optimizedList.ExceptWith;
                            break;
                        case "SymmetricExceptWith":
                            expFunc = MultiSetModifierExpected.SymmetricExceptWith;
                            optFunc = optimizedList.SymmetricExceptWith;
                            break;
                        case "UnionWith":
                            expFunc = MultiSetModifierExpected.UnionWith;
                            optFunc = optimizedList.UnionWith;
                            break;
                        default:
                            throw new InvalidOperationException();
                    }

                    optFunc(tgtData);
                    expected = RunExpectedMulti(srcData, tgtData, expFunc);
                    expected.Sort(optimizedList.Comparer);
                    RawCollectionAssert.AreEqual(optimizedList, expected);
                }

                void TestBoolFunction(string function)
                {
                    optimizedList = new BinaryList<TestTypeComparable>(srcData);
                    if (trim) optimizedList.Trim();

                    var beforeState = optimizedList.ToList();

                    Func<IReadOnlyCollection<TestTypeComparable>, bool> optFunc;
                    Func<IEnumerable<TestTypeComparable>, IEnumerable<TestTypeComparable>, bool> expFunc;

                    switch (function)
                    {
                        case "IsSubsetOf":
                            expFunc = MultiSetModifierExpected.IsSubsetOf;
                            optFunc = optimizedList.IsSubsetOf;
                            break;
                        case "IsProperSubsetOf":
                            expFunc = MultiSetModifierExpected.IsProperSubsetOf;
                            optFunc = optimizedList.IsProperSubsetOf;
                            break;
                        case "IsSupersetOf":
                            expFunc = MultiSetModifierExpected.IsSupersetOf;
                            optFunc = optimizedList.IsSupersetOf;
                            break;
                        case "IsProperSupersetOf":
                            expFunc = MultiSetModifierExpected.IsProperSupersetOf;
                            optFunc = optimizedList.IsProperSupersetOf;
                            break;
                        case "Overlaps":
                            expFunc = MultiSetModifierExpected.Overlaps;
                            optFunc = optimizedList.Overlaps;
                            break;
                        case "SetEquals":
                            expFunc = MultiSetModifierExpected.SetEquals;
                            optFunc = optimizedList.SetEquals;
                            break;
                        default:
                            throw new InvalidOperationException();
                    }

                    bool actualResult = optFunc(tgtData);
                    bool expectedResult = expFunc(srcData, tgtData);

                    Assert.AreEqual(expectedResult, actualResult);
                    RawCollectionAssert.AreEqual(optimizedList, beforeState);
                }

                try
                {
                    TestFunction("AddRange");
                    TestFunction("IntersectWith");
                    TestFunction("ExceptWith");
                    TestFunction("SymmetricExceptWith");
                    TestFunction("UnionWith");

                    TestBoolFunction("IsSubsetOf");
                    TestBoolFunction("IsProperSubsetOf");
                    TestBoolFunction("IsSupersetOf");
                    TestBoolFunction("IsProperSupersetOf");
                    TestBoolFunction("SetEquals");
                    TestBoolFunction("Overlaps");
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }        
    }
}
