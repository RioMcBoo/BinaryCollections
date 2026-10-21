namespace ArrayTest
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Running;
    using BinaryCollections.BinaryList;
    using System.Collections.Generic;

    public enum PreSortScenario
    {
        Unsorted,
        FullyAscending,
        FullyDescending,
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var tester = new DynamicSortBenchmarks();
            try
            {
                tester.Validate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical error in logic: {ex.Message}");
                return;
            }

            var summary = BenchmarkRunner.Run<DynamicSortBenchmarks>();
        }
    }

    public class TestStructure
    {
        public int Field1 { get; set; }
        public int Field2 { get; set; }
    }

    // Each field is compared with a specified direction (true for ascending, false for descending).
    public class CustomComparer : IComparer<TestStructure>
    {
        private readonly List<bool> directions;
        public CustomComparer(List<bool> directions)
        {
            this.directions = directions;
        }

        public int Compare(TestStructure? x, TestStructure? y)
        {
            int cmp = x.Field1.CompareTo(y.Field1);
            if (cmp != 0) return directions[0] ? cmp : -cmp;

            cmp = x.Field2.CompareTo(y.Field2);
            if (cmp != 0) return directions[1] ? cmp : -cmp;

            return 0;
        }
    }

    [MemoryDiagnoser]
    public class DynamicSortBenchmarks
    {
        // Parameter specifying the pre-sort scenario based on the first field:
        [Params(PreSortScenario.Unsorted, PreSortScenario.FullyAscending, PreSortScenario.FullyDescending)]
        public PreSortScenario PreSortScenario;

        // Parameter specifying the total number of elements.
        [Params(100, 500, 1000, 5000, 10000)]
        public int TotalElements;

        // The base list that will be pre-sorted as per the selected scenario.
        public List<TestStructure> BaseList;

        // Custom comparer for dynamic insertion – uses cascading comparison across all fields.
        // For example: Field1 ascending, Field2 descending.
        public CustomComparer Comparer = new CustomComparer(
            new List<bool> { true, false }
        );

        [GlobalSetup]
        public void Setup()
        {
            var original = GenerateRandomData(TotalElements);

            switch (PreSortScenario)
            {
                case PreSortScenario.FullyAscending:
                    BaseList = original.OrderBy(x => x, Comparer).ToList();
                    break;
                case PreSortScenario.FullyDescending:
                    BaseList = original.OrderByDescending(x => x, Comparer).ToList();
                    break;
                case PreSortScenario.Unsorted:
                default:
                    BaseList = original;
                    break;
            }
        }

        public void Validate()
        {
            TotalElements = 100000;
            PreSortScenario = PreSortScenario.Unsorted;
            Setup();

            var listRes = Fill_List();
            var binRes = Fill_BinaryList();
            var setRes = Fill_SortedSet();

            if (!binRes.SequenceEqual(listRes))
                throw new Exception("Error: BinaryList != List");

            if (!binRes.SequenceEqual(setRes))
                throw new Exception("Error: BinaryList != Set");

            Console.WriteLine("Validation successful!");
        }

        public List<TestStructure> GenerateRandomData(int count)
        {
            Random rnd = new Random(42); // Fixed seed for reproducibility
            var set = new HashSet<TestStructure>();
            var list = new List<TestStructure>();
            int i = 0;
            for (; i < count;)
            {
                var item = new TestStructure
                {
                    Field1 = rnd.Next(),
                    Field2 = rnd.Next(),
                };

                if (set.Add(item))
                    i++;

                list.Add(item);
            }

            return list;
        }

        [Benchmark]
        public List<TestStructure> Fill_List()
        {
            var baseline = BaseList;
            var result = new List<TestStructure>();
            foreach (var item in baseline)
            {
                int index = result.BinarySearch(item, Comparer);
                if (index < 0)
                    index = ~index;
                result.Insert(index, item);
            }
            return result;
        }

        [Benchmark]
        public BinaryList<TestStructure> Fill_BinaryList()
        {
            var baseline = BaseList;
            var binaryList = new BinaryList<TestStructure>(Comparer, comparerCorrespondsToEquals: true);
            foreach (var item in baseline)
            {
                binaryList.AddUnique(item);
            }
            return binaryList;
        }

        [Benchmark]
        public HashSet<TestStructure> Fill_HashSet()
        {
            var baseline = BaseList;
            var sd = new HashSet<TestStructure>();
            foreach (var item in baseline)
            {
                sd.Add(item);
            }
            return sd;
        }

        [Benchmark]
        public SortedSet<TestStructure> Fill_SortedSet()
        {
            var baseline = BaseList;
            var sd = new SortedSet<TestStructure>(Comparer);
            foreach (var item in baseline)
            {
                sd.Add(item);
            }
            return sd;
        }

        [Benchmark]
        public SortedList<TestStructure, int> Fill_SortedList()
        {
            var baseline = BaseList;
            var sl = new SortedList<TestStructure, int>(Comparer);
            foreach (var item in baseline)
            {
                sl.TryAdd(item, 0);
            }
            return sl;
        }
    }
}
