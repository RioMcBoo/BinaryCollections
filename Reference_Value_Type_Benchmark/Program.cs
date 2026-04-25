namespace ArrayTest
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Running;
    using BinaryCollections.BinaryList;
    using System.Runtime.CompilerServices;

    public enum PreSortScenario
    {
        Unsorted,
        PartiallySortedAscending,
        PartiallySortedDescending,
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
            }

            var summary = BenchmarkRunner.Run<DynamicSortBenchmarks>();
        }
    }

    public class TestStructure
    {
        public int Field1;
        public int Field2;
    }

    // Each field is compared with a specified direction (true for ascending, false for descending).
    public class CustomComparer : IComparer<TestStructure>
    {
        private readonly List<bool> directions;
        public CustomComparer(List<bool> directions)
        {
            this.directions = directions;
        }

        public int Compare(TestStructure x, TestStructure y)
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

        public List<TestStructure> BaseListCus;
        public List<TestStructure> BaseListDef;

        // Custom comparer for dynamic insertion – uses cascading comparison across all fields.
        // For example: Field1 ascending, Field2 descending.
        public CustomComparer DynamicComparer = new CustomComparer(
            new List<bool> { true, false }
        );

        [GlobalSetup]
        public void Setup()
        {
            var originalCus = GenerateRandomData(TotalElements);
            var originalDef = originalCus.ToList();

            switch (PreSortScenario)
            {
                case PreSortScenario.FullyAscending:
                    BaseListCus = originalCus.OrderBy(x => x, DynamicComparer).ToList();
                    BaseListDef = originalDef.OrderBy(x => RuntimeHelpers.GetHashCode(x)).ToList();
                    break;
                case PreSortScenario.FullyDescending:
                    BaseListCus = originalCus.OrderByDescending(x => x, DynamicComparer).ToList();
                    BaseListDef = originalDef.OrderByDescending(x => RuntimeHelpers.GetHashCode(x)).ToList();
                    break;
                case PreSortScenario.Unsorted:
                default:
                    BaseListCus = originalCus;
                    BaseListDef = originalDef;
                    break;
            }
        }

        public void Validate()
        {
            TotalElements = 100000;
            PreSortScenario = PreSortScenario.Unsorted;
            Setup();

            var defRes = Default_Comparer_Unique();
            var cusRes = Custom_Comparer_Unique();

            var defTmp = defRes.ToList();
            var cusTmp = cusRes.ToList();

            {
                foreach (var item in cusTmp)
                {
                    if (!defRes.Remove(item))
                        throw new Exception("Error: Bad working with collisions");
                }

                if (defRes.Count > 0)
                    throw new Exception("Error: Bad working with collisions");
            }

            {
                foreach (var item in defTmp)
                {
                    if (!cusRes.Remove(item))
                        throw new Exception("Error: Bad working with collisions");
                }

                if (cusRes.Count > 0)
                    throw new Exception("Error: Bad working with collisions");
            }

            Console.WriteLine("Validation successful!");
        }

        // Method to generate the original random data.
        // Uses a HashSet to ensure unique elements, then converts it to a List.
        public List<TestStructure> GenerateRandomData(int count)
        {
            Random rnd = new Random(42); // Fixed seed for reproducibility
            var list = new List<TestStructure>(count);
            int i = 0;
            for (; i < count; i++)
            {
                var item = new TestStructure
                {
                    Field1 = rnd.Next(0, 100),
                    Field2 = rnd.Next(0, 100),
                };

                list.Add(item);
            }

            return list;
        }

        [Benchmark]
        public BinaryList<TestStructure> Default_Comparer()
        {
            var baseline = BaseListDef;
            var binaryList = new BinaryList<TestStructure>();
            foreach (var item in baseline)
            {
                binaryList.Add(item);
            }
            return binaryList;
        }

        [Benchmark]
        public BinaryList<TestStructure> Default_Comparer_Unique()
        {
            var baseline = BaseListDef;
            var binaryList = new BinaryList<TestStructure>();
            foreach (var item in baseline)
            {
                binaryList.AddUnique(item);
            }
            return binaryList;
        }

        [Benchmark]
        public BinaryList<TestStructure> Custom_Comparer()
        {
            var baseline = BaseListCus;
            var binaryList = new BinaryList<TestStructure>(DynamicComparer);
            foreach (var item in baseline)
            {
                binaryList.Add(item);
            }
            return binaryList;
        }

        [Benchmark]
        public BinaryList<TestStructure> Custom_Comparer_Unique()
        {
            var baseline = BaseListCus;
            var binaryList = new BinaryList<TestStructure>(DynamicComparer);
            foreach (var item in baseline)
            {
                binaryList.AddUnique(item);
            }
            return binaryList;
        }
    }
}
