namespace ArrayTest
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Running;
    using BinaryCollections.BinaryList;
    using BinaryList.Tests;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [MemoryDiagnoser]
    public class SetOperationsBenchmark
    {
        private const int IdxUnion = 0;
        private const int IdxIntersect = 1;
        private const int IdxExcept = 2;
        private const int IdxSymmExcept = 3;
        private const int TotalOps = 4;

        [Params(500, 1000, 5000, 10000, 50000, 100000)]
        public int N;

        private BinaryList<TestTypeBinary> A;
        private BinaryList<TestTypeBinary> B;

        readonly BinaryList<TestTypeBinary>[] binary_A = new BinaryList<TestTypeBinary>[TotalOps];
        readonly BinaryList<TestTypeBinary>[] binary_B = new BinaryList<TestTypeBinary>[TotalOps];

        readonly HashSet<TestTypeBinary>[] hash_A = new HashSet<TestTypeBinary>[TotalOps];
        readonly HashSet<TestTypeBinary>[] hash_B = new HashSet<TestTypeBinary>[TotalOps];

        readonly List<TestTypeBinary>[] list_A = new List<TestTypeBinary>[TotalOps];
        readonly List<TestTypeBinary>[] list_B = new List<TestTypeBinary>[TotalOps];

        [GlobalSetup]
        public void Setup()
        {
            var rnd = new Random(0);
            A = new BinaryList<TestTypeBinary>();
            B = new BinaryList<TestTypeBinary>();

            int domain = Math.Max(1, N * 3);

            for (int i = 0; i < N; i++)
            {
                A.AddUnique(rnd.Next(0, domain));
                B.AddUnique(rnd.Next(0, domain));
            }

            for (int i = 0; i < TotalOps; i++)
            {
                binary_A[i] = new BinaryList<TestTypeBinary>();
                binary_B[i] = new BinaryList<TestTypeBinary>();

                hash_A[i] = new HashSet<TestTypeBinary>();
                hash_B[i] = new HashSet<TestTypeBinary>();

                list_A[i] = new List<TestTypeBinary>();
                list_B[i] = new List<TestTypeBinary>();
            }
        }

        [IterationSetup]
        public void IterationSetup()
        {
            for (int i = 0; i < TotalOps; i++)
            {
                binary_A[i].Clear(); binary_B[i].Clear();
                hash_A[i].Clear(); hash_B[i].Clear();
                list_A[i].Clear(); list_B[i].Clear();

                foreach (var item in A) hash_A[i].Add(item);
                foreach (var item in B) hash_B[i].Add(item);

                binary_A[i].AddRange(A);
                binary_B[i].AddRange(B);

                list_A[i].AddRange(binary_A[i]);
                list_B[i].AddRange(binary_B[i]);
            }
        }

        [Benchmark]
        [BenchmarkCategory("Validation")]
        public void VerifyResults()
        {
            for (int i = 0; i < TotalOps; i++)
            {
                var opType = i;

                var binaryResult = binary_A[i].OrderBy(x => x).ToList();
                var hashResult = hash_A[i].OrderBy(x => x).ToList();

                CollectionAssert.AreEquivalent(binaryResult, hashResult);
            }
        }

        // --- BINARY LIST BENCHMARKS ---

        [Benchmark]
        [BenchmarkCategory("Union")]
        public void BinaryList_Union() => binary_A[IdxUnion].UnionWithUnique(binary_B[IdxUnion]);

        [Benchmark]
        [BenchmarkCategory("Intersection")]
        public void BinaryList_Intersect() => binary_A[IdxIntersect].IntersectWithUnique(binary_B[IdxIntersect]);

        [Benchmark]
        [BenchmarkCategory("Except")]
        public void BinaryList_Except() => binary_A[IdxExcept].ExceptWithUnique(binary_B[IdxExcept]);

        [Benchmark]
        [BenchmarkCategory("SymmetricExcept")]
        public void BinaryList_SymmetricExcept() => binary_A[IdxSymmExcept].SymmetricExceptWithUnique(binary_B[IdxSymmExcept]);


        // --- HASH SET BENCHMARKS ---

        [Benchmark]
        [BenchmarkCategory("Union")]
        public void HashSet_Union() => hash_A[IdxUnion].UnionWith(hash_B[IdxUnion]);

        [Benchmark]
        [BenchmarkCategory("Intersection")]
        public void HashSet_Intersect() => hash_A[IdxIntersect].IntersectWith(hash_B[IdxIntersect]);

        [Benchmark]
        [BenchmarkCategory("Except")]
        public void HashSet_Except() => hash_A[IdxExcept].ExceptWith(hash_B[IdxExcept]);

        [Benchmark]
        [BenchmarkCategory("SymmetricExcept")]
        public void HashSet_SymmetricExcept() => hash_A[IdxSymmExcept].SymmetricExceptWith(hash_B[IdxSymmExcept]);


        // --- LINQ LIST BENCHMARKS ---

        [Benchmark]
        [BenchmarkCategory("Union")]
        public void ListLinq_Union() => list_A[IdxUnion] = list_A[IdxUnion].Union(list_B[IdxUnion]).ToList();

        [Benchmark]
        [BenchmarkCategory("Intersection")]
        public void ListLinq_Intersect() => list_A[IdxIntersect] = list_A[IdxIntersect].Intersect(list_B[IdxIntersect]).ToList();

        [Benchmark]
        [BenchmarkCategory("Except")]
        public void ListLinq_Except() => list_A[IdxExcept] = list_A[IdxExcept].Except(list_B[IdxExcept]).ToList();

        [Benchmark]
        [BenchmarkCategory("SymmetricExcept")]
        public void ListLinq_SymmetricExcept() =>
            list_A[IdxSymmExcept] = list_A[IdxSymmExcept].Except(list_B[IdxSymmExcept]).Union(list_B[IdxSymmExcept].Except(list_A[IdxSymmExcept])).ToList();

        public static void Main(string[] args) => BenchmarkRunner.Run<SetOperationsBenchmark>();
    }
}