
using BinaryCollections.BinaryList;
namespace BinaryList.Tests;

public static class RawCollectionAssert
{
    public static void AreEqual(BinaryList<TestTypeBinary> actual, IEnumerable<TestTypeBinary> expected)
    {
        var expectedEnumerator = expected.GetEnumerator();
        var actualEnumerator = actual.GetEnumerator();

        bool expectedHasData = expectedEnumerator.MoveNext();
        bool actualHasData = actualEnumerator.MoveNext();

        while (expectedHasData && actualHasData)
        {
            if (!actualEnumerator.Current.Equals(expectedEnumerator.Current))
                throw new InvalidDataException("Collections are not equal.");

            expectedHasData = expectedEnumerator.MoveNext();
            actualHasData = actualEnumerator.MoveNext();
        }

        if (expectedHasData != actualHasData)
        {
            throw new InvalidDataException("Collections are not equal.");
        }

        var actualRaw = actual.ExtractRawArray();
        for ( int i = 0; i < actualRaw.Length; i++)
        {
            if (i < actual.ZeroIndex || i >= actual.ZeroIndex + actual.Count)
            {
                if (actualRaw[i] != null)
                    throw new InvalidDataException("BinaryList has not properly cleared empty slots.");
            }
        }        
    }

    //public partial class CORE_Tests
    //{
    //    public static Dictionary<T, int> GetElementCounts<T>(IEnumerable<T> collection, IEqualityComparer<T> comparer) where T : notnull
    //    {
    //        var counts = new Dictionary<T, int>(comparer);
    //        foreach (var item in collection)
    //        {
    //            if (counts.TryGetValue(item, out int count))
    //                counts[item] = count + 1;
    //            else
    //                counts[item] = 1;
    //        }
    //        return counts;
    //    }
    //}

    //public static class MultiSetModifierExpected
    //{
    //    private static List<T> ToFlatList<T>(Dictionary<T, int> counts) where T : notnull
    //    {
    //        var result = new List<T>();
    //        foreach (var pair in counts)
    //        {
    //            for (int i = 0; i < pair.Value; i++)
    //                result.Add(pair.Key);
    //        }
    //        return result;
    //    }

    //    public static List<T> UnionWith<T>(IEnumerable<T> source, IEnumerable<T> target, IEqualityComparer<T> comparer) where T : notnull
    //    {
    //        var srcCounts = CORE_Tests.GetElementCounts(source, comparer);
    //        var tgCounts = CORE_Tests.GetElementCounts(target, comparer);
    //        var result = new Dictionary<T, int>(comparer);

    //        // Все ключи из обеих коллекций
    //        var allKeys = srcCounts.Keys.Union(tgCounts.Keys, comparer);

    //        foreach (var key in allKeys)
    //        {
    //            srcCounts.TryGetValue(key, out int srcCount);
    //            tgCounts.TryGetValue(key, out int tgCount);
    //            result[key] = Math.Max(srcCount, tgCount);
    //        }

    //        return ToFlatList(result);
    //    }

    //    public static List<T> IntersectWith<T>(IEnumerable<T> source, IEnumerable<T> target, IEqualityComparer<T> comparer) where T : notnull
    //    {
    //        var srcCounts = CORE_Tests.GetElementCounts(source, comparer);
    //        var tgCounts = CORE_Tests.GetElementCounts(target, comparer);
    //        var result = new Dictionary<T, int>(comparer);

    //        foreach (var key in srcCounts.Keys)
    //        {
    //            if (tgCounts.TryGetValue(key, out int tgCount))
    //            {
    //                result[key] = Math.Min(srcCounts[key], tgCount);
    //            }
    //        }

    //        return ToFlatList(result);
    //    }

    //    public static List<T> ExceptWith<T>(IEnumerable<T> source, IEnumerable<T> target, IEqualityComparer<T> comparer) where T : notnull
    //    {
    //        var srcCounts = CORE_Tests.GetElementCounts(source, comparer);
    //        var tgCounts = CORE_Tests.GetElementCounts(target, comparer);
    //        var result = new Dictionary<T, int>(comparer);

    //        foreach (var pair in srcCounts)
    //        {
    //            tgCounts.TryGetValue(pair.Key, out int tgCount);
    //            int remaining = pair.Value - tgCount;
    //            if (remaining > 0)
    //            {
    //                result[pair.Key] = remaining;
    //            }
    //        }

    //        return ToFlatList(result);
    //    }

    //    public static List<T> SymmetricExceptWith<T>(IEnumerable<T> source, IEnumerable<T> target, IEqualityComparer<T> comparer) where T : notnull
    //    {
    //        var srcCounts = CORE_Tests.GetElementCounts(source, comparer);
    //        var tgCounts = CORE_Tests.GetElementCounts(target, comparer);
    //        var result = new Dictionary<T, int>(comparer);

    //        var allKeys = srcCounts.Keys.Union(tgCounts.Keys, comparer);

    //        foreach (var key in allKeys)
    //        {
    //            srcCounts.TryGetValue(key, out int srcCount);
    //            tgCounts.TryGetValue(key, out int tgCount);

    //            int diff = Math.Abs(srcCount - tgCount);
    //            if (diff > 0)
    //            {
    //                result[key] = diff;
    //            }
    //        }

    //        return ToFlatList(result);
    //    }
    //}

    //public static class UniqueModifierExpected
    //{
    //    public static List<T> UnionWith<T>(IEnumerable<T> source, IEnumerable<T> target, IEqualityComparer<T> comparer) where T : notnull
    //    {
    //        var set = new HashSet<T>(source, comparer);
    //        set.UnionWith(target);
    //        return set.ToList();
    //    }

    //    public static List<T> IntersectWith<T>(IEnumerable<T> source, IEnumerable<T> target, IEqualityComparer<T> comparer) where T : notnull
    //    {
    //        var set = new HashSet<T>(source, comparer);
    //        set.IntersectWith(target);
    //        return set.ToList();
    //    }

    //    public static List<T> ExceptWith<T>(IEnumerable<T> source, IEnumerable<T> target, IEqualityComparer<T> comparer) where T : notnull
    //    {
    //        var set = new HashSet<T>(source, comparer);
    //        set.ExceptWith(target);
    //        return set.ToList();
    //    }

    //    public static List<T> SymmetricExceptWith<T>(IEnumerable<T> source, IEnumerable<T> target, IEqualityComparer<T> comparer) where T : notnull
    //    {
    //        var set = new HashSet<T>(source, comparer);
    //        set.SymmetricExceptWith(target);
    //        return set.ToList();
    //    }
    //}
}