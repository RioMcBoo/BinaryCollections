# Array-Backed Binary Sorted List for C#
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
A highly customized, performance-oriented sorted collection utilizing continuous memory storage, binary search, and a floating internal array layout to minimize memory allocations and CPU cache misses.

> **Status: Alpha / Work-in-Progress**  
> This is an experimental, non-production-ready release. The API is unstable, and many helper functions are currently in a skeletal, untested state as I focus on core benchmarking and engine validation.

---

## 1. Introduction & Background

This project is not an attempt to reinvent the wheel or claim a revolutionary mathematical breakthrough. Instead, it is a practical, engineering-focused experiment to see how far we can push a sequential, array-backed structure for sorted data operations in modern .NET. I built this project primarily as a portfolio piece to demonstrate structured data manipulation, algorithmic exploration, and micro-optimization. AI tools were utilized strictly for writing unit tests and acting as an interactive technical consultant for specific low-level details.

In computer science, binary search on a sorted dataset remains the fastest lookup strategy second only to $O(1)$ hash tables. Sequential memory (the standard array) is the absolute ideal storage format for modern CPUs due to spatial locality and L1/L2 cache prefetching. 

However, mainstream software engineering historically avoids array-based sorted lists because insertion and deletion require element shifting, which runs in $O(N)$ time. Many assume that the cost of shifting elements cannot possibly compete with dynamic, pointer-heavy memory structures (like linked nodes or trees) or hash-based structures. This project challenges that assumption under specific, real-world workloads.

---

## 2. Key Discoveries & Architectural Features

While this collection is not designed to be the fastest single-operation structure on the market, it provides an incredible architectural advantage: **the ability to perform highly complex multi-step operations on your data without converting it to intermediate collection types**. 

In real-world business logic, developers frequently chain LINQ queries, convert lists to sets, and perform lookups—allocating memory at every step. This collection avoids that entirely:

* **Near-Zero Memory Allocations:** Unlike Red-Black Trees (`SortedSet<T>`) or Hash Tables (`HashSet<T>`), which require node allocations, rebalancing, and metadata overhead, this list operates entirely within a single pre-allocated continuous array. If initialized with an appropriate capacity, GC allocations during operation drop to virtually **zero**.
* **Floating Internal Array (Circular/Shift Optimization):** To mitigate the cost of element shifting, the internal array uses a floating window design. This allows inserts and deletes near the boundaries to shift only a fraction of the data, saving massive amounts of CPU cycles.
* **Effortless Pagination:** Because the data is stored sequentially, pagination is a trivial index operation ($O(1)$), which is extremely inefficient or outright impossible to do cleanly with standard trees or hash tables.
* **Highly Efficient Set Operations:** For operations like Unions, Intersections, and Differences, sorted sequential memory shines—especially as dataset sizes grow. After a threshold of roughly **50,000 elements**, our sequential merging algorithms (using simple two-pointer sweeps without dynamic memory overhead) outperform pointer-heavy collections.
* **Duplicate Support:** Unlike standard sets, this list natively handles duplicate values, which is critical for many engineering scenarios.
* **Uninterfaced Type Support:** The collection can handle structures and types that do not explicitly implement standard comparison interfaces, albeit with certain configuration trade-offs.

*If Microsoft were to apply low-level CLR optimizations to this type of continuous array-shifting layout, this structure could easily become a silver bullet for many high-performance scenario pipelines.*

---

## 3. Current Status & API Warning

Please keep in mind that this is currently in a **conceptual Alpha** stage:
* **Unstable API:** I have not finalized the return signatures of primary mutating functions (e.g., whether to return the modified index, the number of inserted/deleted elements, or boolean flags).
* **Low-Level Control:** The core design philosophy is to expose enough low-level mechanics so that you can wrap this list in your own custom, high-level collections and write domain-specific optimizations.
* **Untested Helpers:** Because the API is in flux, many of these advanced methods are **untested, unverified, and exist only in a skeletal state**. My priority was to build a solid, functional core engine so that I could evaluate its raw potential using *BenchmarkDotNet* before spending hours polishing helper methods.

---

## 4. Future Roadmap & Benchmark Caveats

When reviewing current benchmarks, please keep the following realities in mind:
1. **Benchmark Bias:** Standard synthetic benchmarks use uniformly distributed random data, which heavily favors Hash Tables. Real-world data is rarely uniform and often contains spatial patterns that play perfectly to the strengths of a cached sequential list.
2. **Planned Optimizations:**
   * Complete refactoring and unification of internal Set operations.
   * Implementation of **Galloping Search** (exponential search) to drastically speed up lookups when searching near previously accessed boundaries.

---

## 5. Feedback, Portfolio Use & License

I am completely open to constructive criticism, architectural reviews, and guidance from more experienced .NET engineers. I need to understand: *Is this concept worth developing further, or is it an over-engineered dead end?*

**License & Use:** You are completely free to use, modify, and integrate this code into your projects. My only request is that you **reference this project in public discussions**. I want this repository to serve as a strong portfolio piece that accurately highlights my engineering effort, dedication, and contributions to C# performance optimization.