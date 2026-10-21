namespace ArrayTest
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Diagnosers;
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
        public int Field1;
        public int Field2;
        public int Field3;
        public int Field4;
        public int Field5;
        public int Field6;
        public int Field7;
        public int Field8;
        public int Field9;
    }

    // Each field is compared with a specified direction (true for ascending, false for descending).
    public class CustomComparer : IComparer<TestStructure>
    {
        public int Compare(TestStructure? x, TestStructure? y)
        {
            int cmp = x.Field1.CompareTo(y.Field1);
            if (cmp != 0) return cmp;

            cmp = x.Field2.CompareTo(y.Field2);
            if (cmp != 0) return cmp;

            cmp = x.Field3.CompareTo(y.Field3);
            if (cmp != 0) return cmp;

            cmp = x.Field4.CompareTo(y.Field4);
            if (cmp != 0) return cmp;

            cmp = x.Field5.CompareTo(y.Field5);
            if (cmp != 0) return cmp;

            cmp = x.Field6.CompareTo(y.Field6);
            if (cmp != 0) return cmp;

            cmp = x.Field7.CompareTo(y.Field7);
            if (cmp != 0) return cmp;

            cmp = x.Field8.CompareTo(y.Field8);
            if (cmp != 0) return cmp;

            cmp = x.Field9.CompareTo(y.Field9);
            if (cmp != 0) return cmp;

            return 0;
        }
    }

    [MemoryDiagnoser]
    public class DynamicSortBenchmarks
    {
        // Parameter specifying the pre-sort scenario based on the first field:
        [Params(PreSortScenario.Unsorted/*, PreSortScenario.FullyAscending, PreSortScenario.FullyDescending*/)]
        public PreSortScenario PreSortScenario;

        // Parameter specifying the total number of elements.
        [Params(100, 500, 1000, 5000, 10000)]
        public int TotalElements;

        // The base list that will be pre-sorted as per the selected scenario.
        public List<TestStructure> BaseList;

        // Custom comparer for dynamic insertion – uses cascading comparison across all fields.
        // For example: Field1 ascending, Field2 descending.
        public CustomComparer Comparer = new CustomComparer();        

        public void Validate()
        {
            TotalElements = 100000;
            PreSortScenario = PreSortScenario.Unsorted;
            Setup();

            var listRes = Fill_List(); listRes.Sort(Comparer);
            var binRes = Fill_BinaryList();

            if (!binRes.SequenceEqual(listRes))
                throw new Exception("Error: BinaryList != List");

            Console.WriteLine("Validation successful!");
        }

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

        public List<TestStructure> GenerateRandomData(int count)
        {
            Random rnd = new Random(42); // Fixed seed for reproducibility
            var list = new List<TestStructure>();
            int i = 0;
            for (; i < count;i++)
            {
                var item = new TestStructure
                {
                    Field1 = rnd.Next(),
                    Field2 = rnd.Next(),
                    Field3 = rnd.Next(),
                    Field4 = rnd.Next(),
                    Field5 = rnd.Next(),
                    Field6 = rnd.Next(),
                    Field7 = rnd.Next(),
                    Field8 = rnd.Next(),
                    Field9 = rnd.Next(),
                };

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
                result.Add(item);
            }

            return result;
        }

        [Benchmark]
        public List<TestStructure> Fill_Sorted_List()
        {
            var baseline = BaseList;
            var result = new List<TestStructure>();
            foreach (var item in baseline)
            {
                int index = result.BinarySearch(item, Comparer);
                result.Insert(index < 0 ? ~index : index, item);
            }
            return result;
        }

        [Benchmark]
        public List<TestStructure> Fill_List_And_Sort()
        {
            var baseline = BaseList;
            var result = new List<TestStructure>();
            foreach (var item in baseline)
            {
                result.Add(item);
            }

            result.Sort(Comparer);

            return result;
        }

        [Benchmark]
        public BinaryList<TestStructure> Fill_BinaryList()
        {
            var baseline = BaseList;
            var binaryList = new BinaryList<TestStructure>(Comparer, comparerCorrespondsToEquals : true);
            foreach (var item in baseline)
            {
                binaryList.Add(item);
            }
            return binaryList;
        }
    }
}
