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

        public (HashSet<TestTypeComparable> left, HashSet<TestTypeComparable> right) BaseHashSibling;
        public (HashSet<TestTypeHashCollisionAble> left, HashSet<TestTypeHashCollisionAble> right) BaseHashCollisionableSibling;
        public (BinaryList<TestTypeComparable> left, BinaryList<TestTypeComparable> right) BaseBinaryListSibling;
        public (BinaryList<TestTypeComparable> left, BinaryList<TestTypeComparable> right) BaseBinaryListCollisionableSibling;
        public (BinaryList<TestTypeComparable> left, BinaryList<TestTypeComparable> right) BaseBinaryListSiblingSingle;
        public (BinaryList<TestTypeComparable> left, BinaryList<TestTypeComparable> right) BaseBinaryListCollisionableSiblingSingle;
        public (BinaryList<TestTypeComparable> left, List<TestTypeComparable> right) BaseBinaryList;
        public (BinaryList<TestTypeComparable> left, List<TestTypeComparable> right) BaseBinaryListCollisionable;
        public (List<TestTypeComparable> left, List<TestTypeComparable> right) BaseList;
        public (List<TestTypeHashCollisionAble> left, List<TestTypeHashCollisionAble> right) BaseListCollisionable;

        [GlobalSetup]
        public void Setup()
        {
            BaseHashSibling = GenerateRandomData(TotalElements);
            BaseHashCollisionableSibling = GenerateRandomDataCollisionable(BaseHashSibling);
            BaseBinaryList = (new BinaryList<TestTypeComparable>(BaseHashSibling.left, TestComparer.Properly), new List<TestTypeComparable>(BaseHashSibling.right));
            BaseBinaryListCollisionable = (new BinaryList<TestTypeComparable>(BaseHashSibling.left, TestComparer.WithCollisions), new List<TestTypeComparable>(BaseHashSibling.right));
            BaseBinaryListSibling = (new BinaryList<TestTypeComparable>(BaseHashSibling.left, TestComparer.Properly), new BinaryList<TestTypeComparable>(BaseHashSibling.right, TestComparer.Properly));
            BaseBinaryListCollisionableSibling = (new BinaryList<TestTypeComparable>(BaseHashSibling.left, TestComparer.WithCollisions), new BinaryList<TestTypeComparable>(BaseHashSibling.right, TestComparer.WithCollisions));
            BaseBinaryListSiblingSingle = (new BinaryList<TestTypeComparable>(BaseHashSibling.left, TestComparer.Properly), new BinaryList<TestTypeComparable>(BaseHashSibling.right, TestComparer.Properly));
            BaseBinaryListCollisionableSiblingSingle = (new BinaryList<TestTypeComparable>(BaseHashSibling.left, TestComparer.WithCollisions), new BinaryList<TestTypeComparable>(BaseHashSibling.right, TestComparer.WithCollisions));
            BaseList = (new List<TestTypeComparable>(BaseHashSibling.left), new List<TestTypeComparable>(BaseHashSibling.right));
            BaseListCollisionable = (new List<TestTypeHashCollisionAble>(BaseHashCollisionableSibling.left), new List<TestTypeHashCollisionAble>(BaseHashCollisionableSibling.right));
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

        public (HashSet<TestTypeComparable> left, HashSet<TestTypeComparable> right) GenerateRandomData(int count)
        {
            Random rnd = new Random(42); // Fixed seed for reproducibility
            var left = new HashSet<TestTypeComparable>();
            var right = new HashSet<TestTypeComparable>();


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

        public (HashSet<TestTypeHashCollisionAble> left, HashSet<TestTypeHashCollisionAble> right) GenerateRandomDataCollisionable((HashSet<TestTypeComparable> left, HashSet<TestTypeComparable> right) input)
        {
            var left = new HashSet<TestTypeHashCollisionAble>();
            var right = new HashSet<TestTypeHashCollisionAble>();

            foreach (var item in input.left)
            {
                left.Add(item.Value);
            }

            foreach (var item in input.right)
            {
                right.Add(item.Value);
            }

            return (left, right);
        }

        [Benchmark]
        public HashSet<TestTypeComparable> HashSet_Sibling()
        {
            BaseHashSibling.left.UnionWith(BaseHashSibling.right);
            return BaseHashSibling.left;
        }

        [Benchmark]
        public HashSet<TestTypeHashCollisionAble> HashSetCollisionable_Sibling()
        {
            BaseHashCollisionableSibling.left.UnionWith(BaseHashCollisionableSibling.right);
            return BaseHashCollisionableSibling.left;
        }

        [Benchmark]
        public BinaryList<TestTypeComparable> BinaryList_vs_List()
        {
            BaseBinaryList.left.UnionWithUnique(BaseBinaryList.right);
            return BaseBinaryList.left;
        }

        [Benchmark]
        public BinaryList<TestTypeComparable> BinaryList_vs_List_Collisionable()
        {
            BaseBinaryListCollisionable.left.UnionWithUnique(BaseBinaryListCollisionable.right);
            return BaseBinaryList.left;
        }

        [Benchmark]
        public BinaryList<TestTypeComparable> BinaryList_Sibling()
        {
            BaseBinaryListSibling.left.UnionWithUnique(BaseBinaryListSibling.right);
            return BaseBinaryListSibling.left;

        }

        [Benchmark]
        public BinaryList<TestTypeComparable> BinaryList_Collisionable_Sibling()
        {
            BaseBinaryListCollisionableSibling.left.UnionWithUnique(BaseBinaryListCollisionableSibling.right);  
            return BaseBinaryListCollisionableSibling.left;
        }

        [Benchmark]
        public BinaryList<TestTypeComparable> BinaryList_Sibling_Single()
        {
            foreach(var item in BaseBinaryListSiblingSingle.right)
            {
                BaseBinaryListSiblingSingle.left.AddUnique(item);
            }

            return BaseBinaryListSiblingSingle.left;
        }

        [Benchmark]
        public BinaryList<TestTypeComparable> BinaryList_Collisionable_Sibling_Single()
        {
            foreach (var item in BaseBinaryListCollisionableSiblingSingle.right)
            {
                BaseBinaryListCollisionableSiblingSingle.left.AddUnique(item);
            }

            return BaseBinaryListCollisionableSiblingSingle.left;
        }

        [Benchmark]
        public List<TestTypeComparable> List()
        {
            return BaseList.left.Union(BaseList.right).ToList();
        }

        [Benchmark]
        public List<TestTypeHashCollisionAble> List_Collisionable()
        {            
            return BaseListCollisionable.left.Union(BaseListCollisionable.right).ToList();
        }
    }
}
