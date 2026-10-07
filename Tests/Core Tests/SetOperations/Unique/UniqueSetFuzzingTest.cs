using BinaryCollections.BinaryList;
using static BinaryList.Tests.RawCollectionAssert;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void UniqueSetOperations_HeavyStressTest_Comparable(bool trim)
        {
            const int IterationCount = 100000;
            var rand = new Random(2026);

            for (int i = 0; i < IterationCount; i++)
            {
                int countSrc = rand.Next(0, 20);
                int countTgt = rand.Next(0, 20);

                var srcData = new TestTypeBinary[countSrc];
                for (int j = 0; j < countSrc; j++)
                    srcData[j] = new TestTypeBinary(rand.Next(0, 50));

                var tgtData = new TestTypeBinary[countTgt];
                for (int j = 0; j < countTgt; j++)
                    tgtData[j] = new TestTypeBinary(rand.Next(0, 50));

                int initialCapacity = rand.Next(0, countSrc * 3);
                BinaryList<TestTypeBinary> optimizedList;
                List<TestTypeBinary> expected;

                List<TestTypeBinary> RunExpectedMulti(IEnumerable<TestTypeBinary> src, IEnumerable<TestTypeBinary> tgt, Func<IEnumerable<TestTypeBinary>, IEnumerable<TestTypeBinary>, List<TestTypeBinary>> func)
                {
                    var local_src = src.ToList();
                    var local_tgt = tgt.ToList();

                    var res = func(local_src, local_tgt);
                    return res;
                }

                void TestFunction(string function)
                {
                    optimizedList = new BinaryList<TestTypeBinary>(srcData, capacity: initialCapacity);
                    if (trim) optimizedList.Trim();

                    Func<IReadOnlyCollection<TestTypeBinary>, bool> optFunc;
                    Func<IEnumerable<TestTypeBinary>, IEnumerable<TestTypeBinary>, List<TestTypeBinary>> expFunc;

                    switch (function)
                    {
                        case "AddRange":
                            expFunc = UniqueModifierExpected.AddRange;
                            optFunc = optimizedList.AddRangeUnique;
                            break;
                        case "IntersectWith":
                            expFunc = UniqueModifierExpected.IntersectWith;
                            optFunc = optimizedList.IntersectWithUnique;
                            break;
                        case "ExceptWith":
                            expFunc = UniqueModifierExpected.ExceptWith;
                            optFunc = optimizedList.ExceptWithUnique;
                            break;
                        case "SymmetricExceptWith":
                            expFunc = UniqueModifierExpected.SymmetricExceptWith;
                            optFunc = optimizedList.SymmetricExceptWithUnique;
                            break;
                        case "UnionWith":
                            expFunc = UniqueModifierExpected.UnionWith;
                            optFunc = optimizedList.UnionWithUnique;
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
                    optimizedList = new BinaryList<TestTypeBinary>(srcData, capacity: initialCapacity);
                    if (trim) optimizedList.Trim();

                    var beforeState = optimizedList.ToList();

                    Func<IReadOnlyCollection<TestTypeBinary>, bool> optFunc;
                    Func<IEnumerable<TestTypeBinary>, IEnumerable<TestTypeBinary>, bool> expFunc;

                    switch (function)
                    {
                        case "IsSubsetOf":
                            expFunc = UniqueModifierExpected.IsSubsetOf;
                            optFunc = optimizedList.IsSubsetOfUnique;
                            break;
                        case "IsProperSubsetOf":
                            expFunc = UniqueModifierExpected.IsProperSubsetOf;
                            optFunc = optimizedList.IsProperSubsetOfUnique;
                            break;
                        case "IsSupersetOf":
                            expFunc = UniqueModifierExpected.IsSupersetOf;
                            optFunc = optimizedList.IsSupersetOfUnique;
                            break;
                        case "IsProperSupersetOf":
                            expFunc = UniqueModifierExpected.IsProperSupersetOf;
                            optFunc = optimizedList.IsProperSupersetOfUnique;
                            break;
                        case "Overlaps":
                            expFunc = UniqueModifierExpected.Overlaps;
                            optFunc = optimizedList.Overlaps;
                            break;
                        case "SetEquals":
                            expFunc = UniqueModifierExpected.SetEquals;
                            optFunc = optimizedList.SetEqualsUnique;
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

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void UniqueSetOperations_HeavyStressTest_InComparable(bool trim)
        {
            const int IterationCount = 100000;
            var rand = new Random(2026);

            for (int i = 0; i < IterationCount; i++)
            {
                int countSrc = rand.Next(0, 20);
                int countTgt = rand.Next(0, 20);

                var srcData = new TestTypeHash[countSrc];
                for (int j = 0; j < countSrc; j++)
                    srcData[j] = new TestTypeHash(rand.Next(0, 50));

                var tgtData = new TestTypeHash[countTgt];
                for (int j = 0; j < countTgt; j++)
                    tgtData[j] = new TestTypeHash(rand.Next(0, 50));

                int initialCapacity = rand.Next(0, countSrc * 3);
                BinaryList<TestTypeHash> optimizedList;
                List<TestTypeHash> expected;

                List<TestTypeHash> RunExpectedMulti(IEnumerable<TestTypeHash> src, IEnumerable<TestTypeHash> tgt, Func<IEnumerable<TestTypeHash>, IEnumerable<TestTypeHash>, List<TestTypeHash>> func)
                {
                    var local_src = src.ToList();
                    var local_tgt = tgt.ToList();

                    var res = func(local_src, local_tgt);
                    return res;
                }

                void TestFunction(string function)
                {
                    optimizedList = new BinaryList<TestTypeHash>(srcData, capacity: initialCapacity);
                    if (trim) optimizedList.Trim();

                    Func<IReadOnlyCollection<TestTypeHash>, bool> optFunc;
                    Func<IEnumerable<TestTypeHash>, IEnumerable<TestTypeHash>, List<TestTypeHash>> expFunc;

                    switch (function)
                    {
                        case "AddRange":
                            expFunc = UniqueModifierExpected.AddRange;
                            optFunc = optimizedList.AddRangeUnique;
                            break;
                        case "IntersectWith":
                            expFunc = UniqueModifierExpected.IntersectWith;
                            optFunc = optimizedList.IntersectWithUnique;
                            break;
                        case "ExceptWith":
                            expFunc = UniqueModifierExpected.ExceptWith;
                            optFunc = optimizedList.ExceptWithUnique;
                            break;
                        case "SymmetricExceptWith":
                            expFunc = UniqueModifierExpected.SymmetricExceptWith;
                            optFunc = optimizedList.SymmetricExceptWithUnique;
                            break;
                        case "UnionWith":
                            expFunc = UniqueModifierExpected.UnionWith;
                            optFunc = optimizedList.UnionWithUnique;
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
                    optimizedList = new BinaryList<TestTypeHash>(srcData, capacity: initialCapacity);
                    if (trim) optimizedList.Trim();

                    var beforeState = optimizedList.ToList();

                    Func<IReadOnlyCollection<TestTypeHash>, bool> optFunc;
                    Func<IEnumerable<TestTypeHash>, IEnumerable<TestTypeHash>, bool> expFunc;

                    switch (function)
                    {
                        case "IsSubsetOf":
                            expFunc = UniqueModifierExpected.IsSubsetOf;
                            optFunc = optimizedList.IsSubsetOfUnique;
                            break;
                        case "IsProperSubsetOf":
                            expFunc = UniqueModifierExpected.IsProperSubsetOf;
                            optFunc = optimizedList.IsProperSubsetOfUnique;
                            break;
                        case "IsSupersetOf":
                            expFunc = UniqueModifierExpected.IsSupersetOf;
                            optFunc = optimizedList.IsSupersetOfUnique;
                            break;
                        case "IsProperSupersetOf":
                            expFunc = UniqueModifierExpected.IsProperSupersetOf;
                            optFunc = optimizedList.IsProperSupersetOfUnique;
                            break;
                        case "Overlaps":
                            expFunc = UniqueModifierExpected.Overlaps;
                            optFunc = optimizedList.Overlaps;
                            break;
                        case "SetEquals":
                            expFunc = UniqueModifierExpected.SetEquals;
                            optFunc = optimizedList.SetEqualsUnique;
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
