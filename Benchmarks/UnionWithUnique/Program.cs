namespace ArrayTest
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Running;
    using BinaryCollections.BinaryList;
    using BinaryList.Tests;
    using System.Collections.Generic;

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

    [MemoryDiagnoser]
    public class DynamicSortBenchmarks
    {
        [Params(100, 500, 1000, 5000, 10000, 50000, 100000)]
        public int TotalElements;

        public (HashSet<TestTypeBinary> left, HashSet<TestTypeBinary> right) BaseHashSibling;
        public (HashSet<TestTypeHash> left, HashSet<TestTypeHash> right) BaseHashCollisionableSibling;
        public (BinaryList<TestTypeBinary> left, BinaryList<TestTypeBinary> right) BaseBinaryListSibling;
        public (BinaryList<TestTypeHash> left, BinaryList<TestTypeHash> right) BaseBinaryListCollisionableSibling;
        public (BinaryList<TestTypeBinary> left, BinaryList<TestTypeBinary> right) BaseBinaryListSiblingSingle;
        public (BinaryList<TestTypeHash> left, BinaryList<TestTypeHash> right) BaseBinaryListCollisionableSiblingSingle;
        public (BinaryList<TestTypeBinary> left, List<TestTypeBinary> right) BaseBinaryList;
        public (BinaryList<TestTypeHash> left, List<TestTypeHash> right) BaseBinaryListCollisionable;
        public (List<TestTypeBinary> left, List<TestTypeBinary> right) BaseList;
        public (List<TestTypeHash> left, List<TestTypeHash> right) BaseListCollisionable;

        public static int BestHashCollisionRate = int.MaxValue;
        public static int BadHashCollisionRate = 5000;
        public static int WorstHashCollisionRate = 1;

        [GlobalSetup]
        public void Setup()
        {
            BaseHashSibling = GenerateRandomData(TotalElements);
            BaseHashCollisionableSibling = GenerateRandomDataCollisionable(BaseHashSibling);
            BaseBinaryList = (new BinaryList<TestTypeBinary>(BaseHashSibling.left), new List<TestTypeBinary>(BaseHashSibling.right));
            BaseBinaryListCollisionable = (new BinaryList<TestTypeHash>(BaseHashCollisionableSibling.left, TestComparer.ByHash), new List<TestTypeHash>(BaseHashCollisionableSibling.right));
            BaseBinaryListSibling = (new BinaryList<TestTypeBinary>(BaseHashSibling.left), new BinaryList<TestTypeBinary>(BaseHashSibling.right));
            BaseBinaryListCollisionableSibling = (new BinaryList<TestTypeHash>(BaseHashCollisionableSibling.left, TestComparer.ByHash), new BinaryList<TestTypeHash>(BaseHashCollisionableSibling.right, TestComparer.ByHash));
            BaseBinaryListSiblingSingle = (new BinaryList<TestTypeBinary>(BaseHashSibling.left), new BinaryList<TestTypeBinary>(BaseHashSibling.right));
            BaseBinaryListCollisionableSiblingSingle = (new BinaryList<TestTypeHash>(BaseHashCollisionableSibling.left, TestComparer.ByHash), new BinaryList<TestTypeHash>(BaseHashCollisionableSibling.right, TestComparer.ByHash));
            BaseList = (new List<TestTypeBinary>(BaseHashSibling.left), new List<TestTypeBinary>(BaseHashSibling.right));
            BaseListCollisionable = (new List<TestTypeHash>(BaseHashCollisionableSibling.left), new List<TestTypeHash>(BaseHashCollisionableSibling.right));            
        }

        public void Validate()
        {
            TotalElements = 1000000;
            Setup();

            var binRes = BinaryList_Sibling();
            var hashRes = HashSet_Sibling();

            if (hashRes.Count != binRes.Count)
                throw new Exception("Error: BinaryList != HashSet");

            hashRes.ExceptWith(binRes);

            if (hashRes.Count > 0)
                throw new Exception("Error: BinaryList != HashSet");

            Console.WriteLine("Validation successful!");
        }

        public (HashSet<TestTypeBinary> left, HashSet<TestTypeBinary> right) GenerateRandomData(int count)
        {
            Random rnd = new Random(42); // Fixed seed for reproducibility
            var left = new HashSet<TestTypeBinary>();
            var right = new HashSet<TestTypeBinary>();

            while (left.Count <= count || right.Count <= count)
            {
                var item1 = rnd.Next();
                var item2 = rnd.Next();


                if (left.Count <= count)
                {
                    left.Add(item1);
                }
                if (right.Count <= count)
                {
                    right.Add(item2);
                }
            }

            return (left, right);
        }

        public (HashSet<TestTypeHash> left, HashSet<TestTypeHash> right) GenerateRandomDataCollisionable((HashSet<TestTypeBinary> left, HashSet<TestTypeBinary> right) input)
        {
            var left = new HashSet<TestTypeHash>();
            var right = new HashSet<TestTypeHash>();

            TestTypeHash.HashFactor = BadHashCollisionRate;

            foreach (var item in input.left)
            {
                left.Add(item.Value);
            }

            foreach (var item in input.right)
            {
                right.Add(item.Value);
            }

            TestTypeHash.HashFactor = BestHashCollisionRate;

            return (left, right);
        }

        [Benchmark]
        public HashSet<TestTypeBinary> HashSet_Sibling()
        {
            BaseHashSibling.left.UnionWith(BaseHashSibling.right);
            return BaseHashSibling.left;
        }

        [Benchmark]
        public HashSet<TestTypeHash> HashSetCollisionable_Sibling()
        {
            BaseHashCollisionableSibling.left.UnionWith(BaseHashCollisionableSibling.right);
            return BaseHashCollisionableSibling.left;
        }

        [Benchmark]
        public BinaryList<TestTypeBinary> BinaryList_vs_List()
        {
            BaseBinaryList.left.UnionWithUnique(BaseBinaryList.right);
            return BaseBinaryList.left;
        }

        [Benchmark]
        public BinaryList<TestTypeBinary> BinaryList_vs_List_Collisionable()
        {
            BaseBinaryListCollisionable.left.UnionWithUnique(BaseBinaryListCollisionable.right);
            return BaseBinaryList.left;
        }

        [Benchmark]
        public BinaryList<TestTypeBinary> BinaryList_Sibling()
        {
            BaseBinaryListSibling.left.UnionWithUnique(BaseBinaryListSibling.right);
            return BaseBinaryListSibling.left;

        }

        [Benchmark]
        public BinaryList<TestTypeHash> BinaryList_Collisionable_Sibling()
        {
            BaseBinaryListCollisionableSibling.left.UnionWithUnique(BaseBinaryListCollisionableSibling.right);  
            return BaseBinaryListCollisionableSibling.left;
        }

        //[Benchmark]
        public BinaryList<TestTypeBinary> BinaryList_Sibling_Single()
        {
            foreach(var item in BaseBinaryListSiblingSingle.right)
            {
                BaseBinaryListSiblingSingle.left.AddUnique(item);
            }

            return BaseBinaryListSiblingSingle.left;
        }

        //[Benchmark]
        public BinaryList<TestTypeHash> BinaryList_Collisionable_Sibling_Single()
        {
            foreach (var item in BaseBinaryListCollisionableSiblingSingle.right)
            {
                BaseBinaryListCollisionableSiblingSingle.left.AddUnique(item);
            }

            return BaseBinaryListCollisionableSiblingSingle.left;
        }

        [Benchmark]
        public List<TestTypeBinary> List()
        {
            return BaseList.left.Union(BaseList.right).ToList();
        }

        [Benchmark]
        public List<TestTypeHash> List_Collisionable()
        {            
            return BaseListCollisionable.left.Union(BaseListCollisionable.right).ToList();
        }
    }
}
