using BinaryCollections.BinaryList;

namespace BinaryList.Tests
{
    public partial class CORE_Tests
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Replace(bool trim)
        {
            BinaryList<TestTypeBinary> initial;
            TestTypeBinary[] result, rawRes;

            initial = [1];
            result = [2];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(1), 2);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1];
            result = [0];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(1), 0);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 2];
            result = [0, 2];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(1), 0);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 2];
            result = [2, 3];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(1), 3);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 2];
            result = [1, 3];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(2), 3);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 2];
            result = [0, 1];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(2), 0);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [3, 4];
            result = [4, 4];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(3), 4);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [3, 4];
            result = [3, 3];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(4), 3);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [0, 3, 5];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(1), 0);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [2, 3, 5];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(1), 2);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [3, 4, 5];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(1), 4);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [3, 5, 6];
            rawRes = [null, 3, 5, 6];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(1), 6);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [0, 1, 5];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(3), 0);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [1, 2, 5];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(3), 2);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [1, 4, 5];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(3), 4);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [1, 5, 6];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(3), 6);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [0, 1, 3];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(5), 0);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [1, 2, 3];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(5), 2);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [1, 3, 4];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(5), 4);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5];
            result = [1, 3, 6];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(5), 6);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5, 7, 9];
            result = [1, 5, 6, 7, 9];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(3), 6);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5, 7, 9];
            result = [1, 3, 4, 5, 9];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(7), 4);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5, 7, 9];
            result = [1, 3, 4, 7, 9];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(5), 4);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5, 7, 9];
            result = [3, 5, 7, 9, 10];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(1), 10);
            RawCollectionAssert.AreEqual(initial, result);

            initial = [1, 3, 5, 7, 9];
            result = [0, 1, 3, 5, 7];
            if (trim) initial.Trim();
            initial.Replace(new TestTypeBinary(9), 0);
            RawCollectionAssert.AreEqual(initial, result);
        }

        [TestMethod]
        public void Replace_Fuzzing_HeavyStressTest()
        {
            // Фиксированный сид, чтобы если упадет — всегда можно было воспроизвести баг
            var rand = new Random(2026);

            for (int i = 0; i < 2000000; i++)
            {
                // Генерируем случайный размер и случайные числа (могут быть дубликаты, раз они валидны)
                int count = rand.Next(2, 30);
                int initialCapacity = rand.Next(0, count + 2);
                var sourceData = new TestTypeBinary[count];
                for (int j = 0; j < count; j++)
                {
                    sourceData[j] = new TestTypeBinary(rand.Next(0, 50));
                }

                var optimizedList = new BinaryList<TestTypeBinary>(sourceData, capacity: initialCapacity);
                var comparer = optimizedList.Comparer;
                // Сортируем для исходного состояния
                Array.Sort(sourceData, comparer);

                var referenceList = sourceData.ToList();

                // Рандомно триммим паддинги в 50% случаев
                if (rand.Next(2) == 0) optimizedList.Trim();

                // Выбираем случайный элемент для замены, который ТОЧНО есть
                var oldItem = referenceList[rand.Next(referenceList.Count)];

                // Генерируем новый элемент (произвольный: меньше, больше, равный)
                var newItem = new TestTypeBinary(rand.Next(-10, 60));

                // Моделируем поведение на эталоне
                // Находим индекс первого вхождения старого элемента
                int idxToRemove = referenceList.IndexOf(oldItem);
                referenceList.RemoveAt(idxToRemove);

                // Ищем место для вставки нового, чтобы сохранить сортировку
                int idxToInsert = referenceList.BinarySearch(newItem);
                if (idxToInsert < 0) idxToInsert = ~idxToInsert;
                referenceList.Insert(idxToInsert, newItem);

                // Действие на вашем бинарном списке
                optimizedList.Replace(oldItem, newItem);

                // Самая жесткая проверка: сверяем и элементы, и зануление паддингов!
                RawCollectionAssert.AreEqual(optimizedList, referenceList);
            }
        }
    }
}