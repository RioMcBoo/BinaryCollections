namespace ArrayTest
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Running;
    using BinaryCollections.BinaryList;
    using BinaryList.Tests;

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

    // Each field is compared with a specified direction (true for ascending, false for descending).
    public class CustomComparer : IComparer<TestTypeComparable>
    {
        private readonly List<bool> directions;
        public CustomComparer(List<bool> directions)
        {
            this.directions = directions;
        }

        public int Compare(TestTypeComparable x, TestTypeComparable y)
        {
            return x.Value.CompareTo(y.Value);
        }
    }

    [MemoryDiagnoser]
    public class DynamicSortBenchmarks
    {
        // Parameter specifying the pre-sort scenario based on the first field:
        [Params(PreSortScenario.Unsorted)]
        public PreSortScenario PreSortScenario;

        // Parameter specifying the total number of elements.
        [Params(10000)]
        public int TotalElements;

        public List<TestTypeComparable> BaseList;

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
                    BaseList = originalCus.OrderBy(x => x, DynamicComparer).ToList();
                    break;
                case PreSortScenario.FullyDescending:
                    BaseList = originalCus.OrderByDescending(x => x, DynamicComparer).ToList();
                    break;
                case PreSortScenario.Unsorted:
                default:
                    BaseList = originalCus;
                    break;
            }
        }

        public void Validate()
        {
            TotalElements = 100000;
            PreSortScenario = PreSortScenario.Unsorted;
            Setup();

            Default_Comparer();
            Custom_Comparer();

            var defRes = Default_Comparer_Unsure();
            var cusRes = Custom_Comparer_Unsure();

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
        public List<TestTypeComparable> GenerateRandomData(int count)
        {
            Random rnd = new Random(42); // Fixed seed for reproducibility
            var list = new List<TestTypeComparable>(count);
            int i = 0;
            for (; i < count; i++)
            {
                var item = new TestTypeComparable(rnd.Next(0, 100));

                list.Add(item);
            }

            return list;
        }

        [Benchmark]
        public BinaryList<TestTypeComparable> Default_Comparer()
        {
            var baseline = BaseList;
            var binaryList = new BinaryList<TestTypeComparable>(Comparer<TestTypeComparable>.Default, Comparer<TestTypeComparable>.Default);
            foreach (var item in baseline)
            {
                binaryList.Add(item);
            }
            return binaryList;
        }

        [Benchmark]
        public BinaryList<TestTypeComparable> Default_Comparer_Unsure()
        {
            var baseline = BaseList;
            var binaryList = new BinaryList<TestTypeComparable>(Comparer<TestTypeComparable>.Default);
            foreach (var item in baseline)
            {
                binaryList.Add(item);
            }
            return binaryList;
        }

        [Benchmark]
        public BinaryList<TestTypeComparable> Custom_Comparer()
        {
            var baseline = BaseList;
            var binaryList = new BinaryList<TestTypeComparable>(DynamicComparer, DynamicComparer);
            foreach (var item in baseline)
            {
                binaryList.Add(item);
            }
            return binaryList;
        }

        [Benchmark]
        public BinaryList<TestTypeComparable> Custom_Comparer_Unsure()
        {
            var baseline = BaseList;
            var binaryList = new BinaryList<TestTypeComparable>(DynamicComparer);
            foreach (var item in baseline)
            {
                binaryList.Add(item);
            }
            return binaryList;
        }
    }
}
