namespace ArrayTest
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Running;
    using CustomCollections.BinaryList;

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

    public struct TestStructure
    {
        public long Field1;
        public long Field2;
        public long Field3;
        public long Field4;
        public long Field5;
        public long Field6;
        public long Field7;
        public long Field8;
        public long Field9;
        public long Field10;
        public long Field11;
        public long Field12;
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
        [Params(PreSortScenario.Unsorted, PreSortScenario.PartiallySortedAscending, PreSortScenario.PartiallySortedDescending, PreSortScenario.FullyAscending, PreSortScenario.FullyDescending)]
        public PreSortScenario PreSortScenario;

        // Parameter specifying the total number of elements.
        [Params(100, 500, 1000, 5000, 10000, 50000, 100000, 500000)]
        public int TotalElements;

        public List<TestStructure> BaseList;

        // Custom comparer for dynamic insertion – uses cascading comparison across all fields.
        // For example: Field1 ascending, Field2 descending.
        public CustomComparer DynamicComparer = new CustomComparer(
            new List<bool> { true, false }
        );

        [GlobalSetup]
        public void Setup()
        {
            var original = GenerateRandomData(TotalElements).ToList();

            switch (PreSortScenario)
            {
                case PreSortScenario.FullyAscending:
                    BaseList = original.OrderBy(x => x, DynamicComparer).ToList();
                    break;
                case PreSortScenario.FullyDescending:
                    BaseList = original.OrderByDescending(x => x, DynamicComparer).ToList();
                    break;
                case PreSortScenario.PartiallySortedAscending:
                    BaseList = original.OrderBy(x => x.Field1).ToList();
                    break;
                case PreSortScenario.PartiallySortedDescending:
                    BaseList = original.OrderByDescending(x => x.Field1).ToList();
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

            var listRes = DynamicFill_List();
            var binRes = DynamicFill_BinaryList();
            var dictRes = DynamicFill_SortedDictionary().Keys.ToList();

            if (!binRes.SequenceEqual(listRes))
                throw new Exception("Ошибка: BinaryList != List");

            if (!binRes.SequenceEqual(dictRes))
                throw new Exception("Ошибка: BinaryList != Dictionary");

            Console.WriteLine("Валидация пройдена успешно!");
        }

        // Method to generate the original random data.
        // Uses a HashSet to ensure unique elements, then converts it to a List.
        public HashSet<TestStructure> GenerateRandomData(int count)
        {
            Random rnd = new Random(42); // Fixed seed for reproducibility
            var set = new HashSet<TestStructure>(count);
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
            }
            return set;
        }

        // Helper method to get the value of the specified field.
        public static long GetFieldValue(TestStructure item, int fieldIndex)
        {
            return fieldIndex switch
            {
                0 => item.Field1,
                1 => item.Field2,
                _ => 0
            };
        }

        //==========================================================================
        // Variant 1: Dynamic insertion using List<T> (with BinarySearch + Insert)
        [Benchmark]
        public List<TestStructure> DynamicFill_List()
        {
            var baseline = BaseList;
            var result = new List<TestStructure>();
            foreach (var item in baseline)
            {
                int index = result.BinarySearch(item, DynamicComparer);
                if (index < 0)
                    index = ~index;
                result.Insert(index, item);
            }
            return result;
        }

        //==========================================================================
        // Variant 2: Dynamic insertion using BinaryList (custom implementation)
        [Benchmark]
        public BinaryList<TestStructure> DynamicFill_BinaryList()
        {
            var baseline = BaseList;
            var binaryList = new BinaryList<TestStructure>(DynamicComparer);
            foreach (var item in baseline)
            {
                binaryList.AddUnique(item);
            }
            return binaryList;
        }

        //==========================================================================
        // Variant 3: Dynamic insertion using SortedDictionary.
        // The key is the element itself; uniqueness is ensured by the comparer.
        [Benchmark]
        public SortedDictionary<TestStructure, int> DynamicFill_SortedDictionary()
        {
            var baseline = BaseList;
            var sd = new SortedDictionary<TestStructure, int>(DynamicComparer);
            foreach (var item in baseline)
            {
                sd.TryAdd(item, 0);
            }
            return sd;
        }

        //==========================================================================
        // Variant 5: Fill a SortedList.
        [Benchmark]
        public SortedList<TestStructure, int> DynamicFill_SortedList()
        {
            var baseline = BaseList;
            var sl = new SortedList<TestStructure, int>(DynamicComparer);
            foreach (var item in baseline)
            {
                sl.TryAdd(item, 0);
            }
            return sl;
        }
    }
}
