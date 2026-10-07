using BinaryCollections.BinaryList;
namespace BinaryList.Tests;

public static class RawCollectionAssert
{
    public static void AreEqual<T>(BinaryList<T> actual, IEnumerable<T> expected) where T : TestType
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

    public partial class CORE_Tests
    {
        public static Dictionary<T, int> GetElementCounts<T>(IEnumerable<T> collection) where T : notnull
        {
            var counts = new Dictionary<T, int>();
            foreach (var item in collection)
            {
                if (counts.TryGetValue(item, out int count))
                    counts[item] = count + 1;
                else
                    counts[item] = 1;
            }
            return counts;
        }
    }

    public static class MultiSetModifierExpected
    {
        private static List<T> ToFlatList<T>(Dictionary<T, int> counts) where T : notnull
        {
            var result = new List<T>();
            foreach (var pair in counts)
            {
                for (int i = 0; i < pair.Value; i++)
                    result.Add(pair.Key);
            }
            return result;
        }

        public static List<T> AddRange<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var result = source.ToList();
            result.AddRange(target);

            return result;
        }

        public static List<T> UnionWith<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var srcCounts = CORE_Tests.GetElementCounts(source);
            var tgCounts = CORE_Tests.GetElementCounts(target);
            var result = new Dictionary<T, int>();

            var allKeys = srcCounts.Keys.Union(tgCounts.Keys);

            foreach (var key in allKeys)
            {
                srcCounts.TryGetValue(key, out int srcCount);
                tgCounts.TryGetValue(key, out int tgCount);
                result[key] = Math.Max(srcCount, tgCount);
            }

            return ToFlatList(result);
        }

        public static List<T> IntersectWith<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var srcCounts = CORE_Tests.GetElementCounts(source);
            var tgCounts = CORE_Tests.GetElementCounts(target);
            var result = new Dictionary<T, int>();

            foreach (var key in srcCounts.Keys)
            {
                if (tgCounts.TryGetValue(key, out int tgCount))
                {
                    result[key] = Math.Min(srcCounts[key], tgCount);
                }
            }

            return ToFlatList(result);
        }

        public static List<T> ExceptWith<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var srcCounts = CORE_Tests.GetElementCounts(source);
            var tgCounts = CORE_Tests.GetElementCounts(target);
            var result = new Dictionary<T, int>();

            foreach (var pair in srcCounts)
            {
                tgCounts.TryGetValue(pair.Key, out int tgCount);
                int remaining = pair.Value - tgCount;
                if (remaining > 0)
                {
                    result[pair.Key] = remaining;
                }
            }

            return ToFlatList(result);
        }

        public static List<T> SymmetricExceptWith<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var srcCounts = CORE_Tests.GetElementCounts(source);
            var tgCounts = CORE_Tests.GetElementCounts(target);
            var result = new Dictionary<T, int>();

            var allKeys = srcCounts.Keys.Union(tgCounts.Keys);

            foreach (var key in allKeys)
            {
                srcCounts.TryGetValue(key, out int srcCount);
                tgCounts.TryGetValue(key, out int tgCount);

                int diff = Math.Abs(srcCount - tgCount);
                if (diff > 0)
                {
                    result[key] = diff;
                }
            }

            return ToFlatList(result);
        }

        public static bool IsSubsetOf<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var srcCounts = CORE_Tests.GetElementCounts(source);
            var tgCounts = CORE_Tests.GetElementCounts(target);

            foreach (var pair in srcCounts)
            {
                tgCounts.TryGetValue(pair.Key, out int tgCount);
                if (pair.Value > tgCount)
                    return false;
            }
            return true;
        }

        public static bool IsProperSubsetOf<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var srcCounts = CORE_Tests.GetElementCounts(source);
            var tgCounts = CORE_Tests.GetElementCounts(target);

            int totalSrc = 0;
            foreach (var pair in srcCounts)
            {
                tgCounts.TryGetValue(pair.Key, out int tgCount);
                if (pair.Value > tgCount)
                    return false;

                totalSrc += pair.Value;
            }

            int totalTg = 0;
            foreach (var count in tgCounts.Values)
                totalTg += count;

            return totalSrc < totalTg;
        }

        public static bool IsSupersetOf<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            return IsSubsetOf(target, source);
        }

        public static bool IsProperSupersetOf<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            return IsProperSubsetOf(target, source);
        }

        public static bool Overlaps<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var srcCounts = CORE_Tests.GetElementCounts(source);
            var tgCounts = CORE_Tests.GetElementCounts(target);

            foreach (var key in srcCounts.Keys)
            {
                if (tgCounts.TryGetValue(key, out int tgCount) && tgCount > 0)
                    return true;
            }
            return false;
        }

        public static bool SetEquals<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var srcCounts = CORE_Tests.GetElementCounts(source);
            var tgCounts = CORE_Tests.GetElementCounts(target);

            if (srcCounts.Count != tgCounts.Count)
                return false;

            foreach (var pair in srcCounts)
            {
                if (!tgCounts.TryGetValue(pair.Key, out int tgCount) || pair.Value != tgCount)
                    return false;
            }
            return true;
        }
    }

    public static class UniqueModifierExpected
    {
        public static List<T> AddRange<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var result = source.ToList();

            foreach (var item in target)
            {
                if (!result.Contains(item))
                {
                    result.Add(item);
                }
            }

            return result;
        }

        public static List<T> UnionWith<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var set = new HashSet<T>(source);
            set.UnionWith(target);
            return set.ToList();
        }

        public static List<T> IntersectWith<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var set = new HashSet<T>(source);
            set.IntersectWith(target);
            return set.ToList();
        }

        public static List<T> ExceptWith<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var set = new HashSet<T>(source);
            set.ExceptWith(target);
            return set.ToList();
        }

        public static List<T> SymmetricExceptWith<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var set = new HashSet<T>(source);
            set.SymmetricExceptWith(target);
            return set.ToList();
        }

        public static bool IsSubsetOf<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var set = new HashSet<T>(source);
            return set.IsSubsetOf(target);
        }

        public static bool IsProperSubsetOf<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var set = new HashSet<T>(source);
            return set.IsProperSubsetOf(target);
        }

        public static bool IsSupersetOf<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var set = new HashSet<T>(source);
            return set.IsSupersetOf(target);
        }

        public static bool IsProperSupersetOf<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var set = new HashSet<T>(source);
            return set.IsProperSupersetOf(target);
        }

        public static bool Overlaps<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var set = new HashSet<T>(source);
            return set.Overlaps(target);
        }

        public static bool SetEquals<T>(IEnumerable<T> source, IEnumerable<T> target) where T : notnull
        {
            var set = new HashSet<T>(source);
            return set.SetEquals(target);
        }
    }
}