using System.Buffers;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CustomCollections.BinaryList
{
    public partial class BinaryList<TValue> : IReadOnlyList<TValue>, IReadOnlyCollection<TValue>, ICollection<TValue>
    {
        public int Capacity => _storage.Length;
        public int Count => _count;

        private int LeftBuffer => _zeroRIndex;
        private int RightBuffer => Capacity - _zeroRIndex - _count;
        private bool CanMoveLeft => LeftBuffer > 0;
        private bool CanMoveRight => RightBuffer > 0;

        public bool IsReadOnly => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ToVirtualIndex(int realIndex) => realIndex - _zeroRIndex;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ToRealIndex(int virtualIndex) => virtualIndex + _zeroRIndex;

        private const int DefaultCapacity = 4;
        private TValue[] _storage;
        private int _zeroRIndex;
        private int _count;
        internal int _version { get; private set; }
        internal IComparer<TValue> Comparer { get; init; } = _defaultComparer;

        internal static IComparer<TValue> _defaultComparer;
        internal static IEqualityComparer<TValue> _equalityComparer;

        private static readonly bool ContainsReferences;
        
        static BinaryList()
        {
            bool IsReferenceType = !typeof(TValue).IsValueType;
            bool IsComparable =
            typeof(IComparable<TValue>).IsAssignableFrom(typeof(TValue)) ||
            typeof(IComparable).IsAssignableFrom(typeof(TValue));

            ContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<TValue>();
            _defaultComparer = Comparer<TValue>.Default;

            if (!IsComparable)
            {
                if (IsReferenceType)
                {
                    _defaultComparer = new ReferenceTypeHashCodeComparer();
                }
                else if (!ContainsReferences)
                {
                    _defaultComparer = new ValueTypeHashCodeComparer();
                }
            }

            _equalityComparer = EqualityComparer<TValue>.Default;
            bool comparsionMethodsOverloaded = FindOverloadedComparisonMethods();

            if (!comparsionMethodsOverloaded)
            {
                if (IsReferenceType)
                {
                    _equalityComparer = new ReferenceTypeEqualityComparer();
                }
                else if (!ContainsReferences)
                {
                    _equalityComparer = new ValueTypeEqualityComparer();
                }
            }
        }

        private static bool FindOverloadedComparisonMethods()
        {
            var type = typeof(TValue);

            bool implementsIEquatable = typeof(IEquatable<TValue>).IsAssignableFrom(type);
            if (implementsIEquatable)
                return true;

            var getHashCodeMethod = type.GetMethod("GetHashCode", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            var equalsMethod = type.GetMethod("Equals", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(object) }, null);

            bool isDefaultHash = getHashCodeMethod?.DeclaringType == typeof(object) || getHashCodeMethod?.DeclaringType == typeof(ValueType);
            bool isDefaultEquals = equalsMethod?.DeclaringType == typeof(object) || equalsMethod?.DeclaringType == typeof(ValueType);

            return !isDefaultHash && !isDefaultEquals;
        }

        public BinaryList(IComparer<TValue> comparer)
        {
            _storage = new TValue[DefaultCapacity];
            _zeroRIndex = CalculateZeroIndex(DefaultCapacity, 0);
            Comparer = comparer;
        }

        public BinaryList(int capacity = DefaultCapacity, IComparer<TValue> comparer = null!)
        {
            capacity = capacity > 0 ? capacity : DefaultCapacity;
            _storage = new TValue[capacity];
            _zeroRIndex = CalculateZeroIndex(capacity, 0);
            Comparer = comparer ?? Comparer;
        }

        public BinaryList(IEnumerable<TValue> initialValues, IComparer<TValue> comparer = null!)
        {
            _storage = new TValue[DefaultCapacity];
            _zeroRIndex = CalculateZeroIndex(DefaultCapacity, 0);
            Comparer = comparer ?? Comparer;

            AddRange(initialValues);
        }

        public BinaryList(IReadOnlyCollection<TValue> initialValues, IComparer<TValue> comparer = null!)
        {
            int capacity = initialValues.Count;
            capacity = capacity > 0 ? capacity : DefaultCapacity;
            _storage = new TValue[capacity];
            _zeroRIndex = CalculateZeroIndex(capacity, 0);
            Comparer = comparer ?? Comparer;

            AddRange(initialValues);
        }

        public BinaryList(BinaryListEnumerator<TValue> initialValues, IComparer<TValue> comparer = null!)
        {
            int capacity = initialValues.Count;
            capacity = capacity > 0 ? capacity : DefaultCapacity;
            _storage = new TValue[capacity];
            _zeroRIndex = CalculateZeroIndex(capacity, 0);
            Comparer = comparer ?? Comparer;

            AddRange(initialValues);
        }

        private static int CalculateZeroIndex(int capacity, int count)
        {
            if (capacity < 0 || count < 0 || capacity < count)
            {
                throw new ArgumentOutOfRangeException(nameof(CalculateZeroIndex), "capacity < 0 || count < 0 || capacity < count");
            }

            return (capacity - count) / 2;
        }        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsIdentical(ref TValue currentItem, ref TValue item)
        {   
            return _equalityComparer.Equals(currentItem, item);
        }

        private bool CheckIndex(int vIndex, int length = 1)
        {
            if (vIndex < 0 || vIndex + length > _count)
            {
                return false;
            }

            return true;
        }

        readonly ref struct ListRangeInfo
        {
            readonly public int RealIndex;
            readonly public int Length;
            readonly public TValue[] Source;

            public ListRangeInfo(int realIndex, int length, TValue[] source)
            {
                RealIndex = realIndex;
                Length = length;
                Source = source;
            }
        }

        readonly struct Part
        {
            readonly public int VIndex;
            readonly public int Length;

            public Part(int vIndex, int length)
            {
                VIndex = vIndex;
                Length = length;
            }
        }

        readonly struct Point
        {
            readonly public int VIndex;
            readonly public int ArraySizeDelta;
            public int PrevPartLength(int prevPartIndex) => VIndex - prevPartIndex;
            public int NextPartIndex => VIndex + (NewItemtPushes ? 0 : -ArraySizeDelta);
            public bool NewItemtPushes => ArraySizeDelta > 0;

            public Point(int vIndex, bool addItem)
            {
                VIndex = vIndex;
                ArraySizeDelta = addItem ? 1 : -1;
            }

            public Point(int vIndex, int arraySizeDelta)
            {
                VIndex = vIndex;
                ArraySizeDelta = arraySizeDelta;
            }
        }

        ref struct ArraySplitInfo
        {
            public Part LeftPart { get; private set; }
            public Point LeftPoint { get; private set; }
            public Part MiddPart { get; private set; }
            public Point RightPoint { get; private set; }
            public Part RightPart { get; private set; }
            public int ArraySizeDelta
            {
                get
                {
                    return (LeftPoint.ArraySizeDelta + RightPoint.ArraySizeDelta) switch
                    {
                        0 => 0,
                        _ => LeftPoint.ArraySizeDelta,
                    };
                }
            }

            public ArraySplitInfo(int removeVIndex, int addVIndex, int arrayLength)
            {
                Point leftPoint = new(removeVIndex, false);
                Point rightPoint = new(addVIndex, true);
                Initialize(leftPoint, rightPoint, arrayLength);
            }

            public ArraySplitInfo(int vIndex, bool add, int arrayLength)
            {
                Point leftPoint, rightPoint;
                leftPoint = rightPoint = new(vIndex, add);
                Initialize(leftPoint, rightPoint, arrayLength);
            }

            public ArraySplitInfo(Point point, int arrayLength)
            {
                Point leftPoint, rightPoint;
                leftPoint = rightPoint = point;
                Initialize(leftPoint, rightPoint, arrayLength);
            }

            private void Initialize(Point leftPoint, Point rightPoint, int arrayLength)
            {
                if (leftPoint.VIndex > rightPoint.VIndex)
                    (leftPoint, rightPoint) = (rightPoint, leftPoint);

                int leftPartVIndex = 0;
                int leftPartLength = leftPoint.VIndex;

                int middPartIndex = leftPoint.NextPartIndex;
                int middPartLength = rightPoint.PrevPartLength(middPartIndex);

                int rightPartVIndex = rightPoint.NextPartIndex;
                int rightPartLength = arrayLength - rightPoint.NextPartIndex;

                LeftPart = new(leftPartVIndex, leftPartLength);
                MiddPart = new(middPartIndex, middPartLength);
                RightPart = new(rightPartVIndex, rightPartLength);

                LeftPoint = leftPoint;
                RightPoint = rightPoint;
            }
        }

        /// <summary>
        /// Searches for the specified item and returns the index of the first occurrence within the entire collection.<br/>  
        /// If item is found, the return value is the index of the item in the list.<br/> 
        /// Otherwise, the bitwise complement of the index that this element would occupy if it were added to the list.
        /// </summary>
        public int FindIndex(TValue item)
        {
            FindIndex(ref item, out int vIndex);
            return vIndex;
        }

        public bool FindIndex(ref TValue item, out int vIndex)
        {
            int lo = 0;
            int hi = _count - 1;

            return FindIndex(ref item, ref lo, ref hi, out vIndex);
        }

        public bool FindIndexUnique(ref TValue item, out int vIndex)
        {
            int lo = 0;
            int hi = _count - 1;

            return FindIndexUnique(ref item, ref lo, ref hi, out vIndex);
        }

        public bool FindIndexUnique(ref TValue item, ref int lo, ref int hi, out int vIndex)
        {
            if (!FindIndex(ref item, ref lo, ref hi, out vIndex))
                return false;

            // Find identical in right part
            for (int i = vIndex; i < Count; i++)
            {
                var result = Comparer.Compare(_storage[ToRealIndex(i)], item);

                if (result != 0)
                    break;

                if (IsIdentical(ref _storage[ToRealIndex(i)], ref item))
                {
                    // Identical
                    vIndex = i;
                    return true;
                }
            }

            // Find identical in left part
            for (int i = vIndex - 1; i >= 0; i--)
            {
                var result = Comparer.Compare(_storage[ToRealIndex(i)], item);

                if (result != 0)
                    break;

                if (IsIdentical(ref _storage[ToRealIndex(i)], ref item))
                {
                    // Identical
                    vIndex = i;
                    return true;
                }
            }

            vIndex = ~vIndex;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool FindIndex(ref TValue item, ref int lo, ref int hi, out int i)
        {
            while (lo <= hi)
            {
                //// PERF: `lo` or `hi` will never be negative inside the loop,
                //       so computing median using uints is safe since we know
                //       `length <= int.MaxValue`, and indices are >= 0
                //       and thus cannot overflow an uint.
                //       Saves one subtraction per loop compared to
                //       `int i = lo + (hi - lo) / 2;`
                //        int i = (int)(((uint)hi + (uint)lo) >> 1);
                i = unchecked((hi + lo) >>> 1);

                int compareResult = Comparer.Compare(_storage[ToRealIndex(i)], item);

                if (compareResult == 0)
                {
                    return true;
                }
                else if (compareResult > 0)
                {
                    hi = i - 1;
                }
                else
                {
                    lo = i + 1;
                }
            }

            i = ~lo;
            return false;
        }

        public (int left, int right) FindBoundaries(ref TValue item)
        {
            int lo, hi, i;
            int left, right, vIndex;
            (int left, int right) result = default;

            // Try find any index of the item
            lo = 0; hi = _count - 1;
            if (!FindIndex(ref item, ref lo, ref hi, out i))
                return (~i, ~i);

            left = lo; right = hi; vIndex = i;

            // Find left boundary
            do { result.left = i; hi = i - 1; }
            while (FindIndex(ref item, ref lo, ref hi, out i));

            lo = left; hi = right; i = vIndex;

            // Find right boundary
            do { result.right = i; lo = i + 1; }
            while (FindIndex(ref item, ref lo, ref hi, out i));

            return result;
        }

        public TValue? Find(Predicate<TValue> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            for (int vIndex = 0; vIndex < _count; vIndex++)
            {
                TValue item = _storage[ToRealIndex(vIndex)];
                if (match(item))
                {
                    return item;
                }
            }

            return default;
        }

        public TValue? FindLast(Predicate<TValue> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            for (int vIndex = _count - 1; vIndex >= 0; vIndex--)
            {
                TValue item = _storage[ToRealIndex(vIndex)];
                if (match(item))
                {
                    return item;
                }
            }

            return default;
        }

        public List<TValue> FindAll(Predicate<TValue> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            List<TValue> list = new List<TValue>();
            for (int vIndex = _count - 1; vIndex >= 0; vIndex--)
            {
                TValue item = _storage[ToRealIndex(vIndex)];
                if (match(item))
                {
                    list.Add(item);
                }
            }

            return list;
        }

        private bool Move(TValue[] srcArray, int srcVIndex, TValue[] destArray, int destVIndex, int length)
        {
            if (length > 0)
            {
                Array.Copy(srcArray, ToRealIndex(srcVIndex), destArray, ToRealIndex(destVIndex), length);
                return true;
            }

            return false;
        }

        private void MoveArray(TValue[] srcArray, TValue[] destArray, int delta)
        {
            Move(srcArray, 0, destArray, delta, _count);
        }

        public bool Trim()
        {
            return Resize(_count);
        }
        
        public bool Resize(int newCapacity)
        {
            if (_count < newCapacity || _storage.Length == newCapacity)
                return false;

            ResizeArray(newCapacity);
            return true;
        }

        private void ResizeArray(int newCapacity)
        {
            TValue[] oldStorage = _storage;
            _storage = new TValue[newCapacity];

            int newZeroIndex = CalculateZeroIndex(_storage.Length, _count);
            int delta = newZeroIndex - _zeroRIndex;

            MoveArray(oldStorage, _storage, delta);        

            _zeroRIndex = newZeroIndex;
        }

        private void PrepareForResize(int newItemsCount, out TValue[] destination)
        {
            destination = _storage;

            int required = _count + newItemsCount + (Capacity >> 2);
            int newCapacity = (int)BitOperations.RoundUpToPowerOf2((uint)required);

            if (newCapacity == 0 || newCapacity > Array.MaxLength)
                newCapacity = Array.MaxLength;

            if (newCapacity < DefaultCapacity)
                newCapacity = DefaultCapacity;

            if (newCapacity > _storage.Length)
                destination = new TValue[newCapacity];
        }

        private void ResizeArray(ref ArraySplitInfo info)
        {
            PrepareForResize(info.ArraySizeDelta, out var destination);
            ShiftArrayToCenter(_storage, destination, ref info);
            _storage = destination;
        }

        private void ShiftArrayToCenter(ref ArraySplitInfo info)
        {
            ShiftArrayToCenter(_storage, _storage, ref info);
        }

        private void ShiftArrayToCenter(TValue[] srcArray, TValue[] destArray, ref ArraySplitInfo info)
        {
            int arraySizeDelta = info.ArraySizeDelta;
            int newCount = _count + arraySizeDelta;
            int newZeroIndex = CalculateZeroIndex(destArray.Length, newCount);
            int delta = newZeroIndex - _zeroRIndex;

            ShiftArray(srcArray, destArray, ref info, delta);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ShiftArray(TValue[] srcArray, TValue[] destArray, ref ArraySplitInfo info, int delta)
        {
            int arraySizeDelta = info.ArraySizeDelta;
            int newZeroRIndex = delta + _zeroRIndex;

            if (delta < 0)
                ShiftArrayLeft(srcArray, destArray, ref info, delta);
            else
                ShiftArrayRight(srcArray, destArray, ref info, delta + arraySizeDelta);

            if (ContainsReferences && srcArray == destArray)
            {
                int cleanLength_ShiftLeft = -delta - arraySizeDelta;
                int cleanIndex_ShiftLeft = _count + delta + arraySizeDelta;

                int cleanLength_ShiftRight = delta;
                int cleanIndex_ShiftRight = _zeroRIndex;

                ClearArray(destArray, cleanIndex_ShiftRight, cleanLength_ShiftRight);
                ClearArray(destArray, cleanIndex_ShiftLeft, cleanLength_ShiftLeft);
            }

            _zeroRIndex = newZeroRIndex;
        }

        private void ClearArray(TValue[] array, int vIndex, int length)
        {
            if (length > 0)
            {
                Array.Clear(array, ToRealIndex(vIndex), length);
            }
        }

        private void ShiftArrayLeft(TValue[] srcArray, TValue[] destArray, ref ArraySplitInfo info, int delta)
        {
            Move(srcArray, info.LeftPart.VIndex, destArray, info.LeftPart.VIndex + delta, info.LeftPart.Length);
            delta += info.LeftPoint.ArraySizeDelta;

            if (Move(srcArray, info.MiddPart.VIndex, destArray, info.MiddPart.VIndex + delta, info.MiddPart.Length))
                delta += info.RightPoint.ArraySizeDelta;

            Move(srcArray, info.RightPart.VIndex, destArray, info.RightPart.VIndex + delta, info.RightPart.Length);
        }

        private void ShiftArrayRight(TValue[] srcArray, TValue[] destArray, ref ArraySplitInfo info, int delta)
        {
            Move(srcArray, info.RightPart.VIndex, destArray, info.RightPart.VIndex + delta, info.RightPart.Length);
            delta -= info.RightPoint.ArraySizeDelta;

            if (Move(srcArray, info.MiddPart.VIndex, destArray, info.MiddPart.VIndex + delta, info.MiddPart.Length))
                delta -= info.LeftPoint.ArraySizeDelta;

            Move(srcArray, info.LeftPart.VIndex, destArray, info.LeftPart.VIndex + delta, info.LeftPart.Length);
        }

        private void ShiftMiddlePart(ref ArraySplitInfo info, out int addItemIndexDelta)
        {
            int delta = info.LeftPoint.ArraySizeDelta;
            Move(_storage, info.MiddPart.VIndex, _storage, info.MiddPart.VIndex + delta, info.MiddPart.Length);
            addItemIndexDelta = info.LeftPoint.NewItemtPushes ? -1 : 0;
        }

        private void ShiftEdges(ref ArraySplitInfo info)
        {
            // ShiftLeftPart changes zero_index, so ShiftRightPart should be called before it
            ShiftRightPart(ref info);
            ShiftLeftPart(ref info);
        }

        private void ShiftLeftPart(ref ArraySplitInfo info)
        {
            int delta = -info.LeftPoint.ArraySizeDelta;
            Move(_storage, info.LeftPart.VIndex, _storage, info.LeftPart.VIndex + delta, info.LeftPart.Length);

            if (ContainsReferences && delta > 0)
                _storage[ToRealIndex(0)] = default!;

            _zeroRIndex += delta;
        }

        private void ShiftRightPart(ref ArraySplitInfo info)
        {
            int delta = info.RightPoint.ArraySizeDelta;
            Move(_storage, info.RightPart.VIndex, _storage, info.RightPart.VIndex + delta, info.RightPart.Length);

            if (ContainsReferences && delta < 0)
                _storage[ToRealIndex(_count - 1)] = default!;
        }

        private void ReplaceItemsInArray(ref ArraySplitInfo info, ref TValue newItem, out int addItemIndexDelta)
        {
            addItemIndexDelta = 0;
            bool NeedsMoveMiddlePart = info.MiddPart.Length < info.LeftPart.Length + info.RightPart.Length;

            if (NeedsMoveMiddlePart || Capacity == Count)
            {
                ShiftMiddlePart(ref info, out addItemIndexDelta);
            }
            else if ((info.LeftPoint.NewItemtPushes && !CanMoveLeft) || (info.RightPoint.NewItemtPushes && !CanMoveRight))
            {
                ShiftArrayToCenter(ref info);
            }
            else
            {
                ShiftEdges(ref info);
            }
        }

        public void Add(TValue item) => Add(ref item, out _);
        public bool Add(ref TValue item, out int index)
        {
            FindIndex(ref item, out index);
            return Insert(ref item, ref index);
        }

        public bool AddUnique(TValue item)
        {
            return AddUnique(ref item, out _);
        }

        public bool AddUnique(ref TValue item, out int index)
        {
            if (FindIndexUnique(ref item, out index))
                return false;

            return Insert(ref item, ref index);
        }

        private bool Insert(ref TValue item, ref int vIndex)
        {
            if (_count == Array.MaxLength)
                return false;

            vIndex = vIndex < 0 ? ~vIndex : vIndex;

            if (_count > 0)
            {
                var info = new ArraySplitInfo(vIndex, true, _count);

                if (CanMoveLeft && info.LeftPart.Length <= info.RightPart.Length)
                {
                    ShiftLeftPart(ref info);
                }
                else if (CanMoveRight && info.RightPart.Length <= info.LeftPart.Length)
                {
                    ShiftRightPart(ref info);
                }
                else
                {
                    ResizeArray(ref info);
                }
            }

            _storage[ToRealIndex(vIndex)] = item;
            _count++;
            _version = unchecked(++_version);

            return true;
        }

        public TValue this[int index]
        {
            get
            {
                if (CheckIndex(index))
                {
                    return _storage[ToRealIndex(index)];
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(index), "Index out of range");
                }
            }
        }

        public TValue GetItem(int index)
        {
            if (CheckIndex(index))
            {
                return _storage[ToRealIndex(index)];
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range");
            }
        }

        [Obsolete("This method is dangerous because it allows you to change the internal state of collection elements. Use it only if you understand the consequences..", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref TValue GetItemRef(int index)
        {
            if (CheckIndex(index))
            {
                return ref _storage[ToRealIndex(index)];
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range");
            }
        }

        public bool RemoveAt(int index)
        {
            if (!CheckIndex(index))
                return false;

            var info = new ArraySplitInfo(index, false, _count);

            if (info.LeftPart.Length <= info.RightPart.Length)
            {
                ShiftLeftPart(ref info);
            }
            else if (info.RightPart.Length < info.LeftPart.Length)
            {
                ShiftRightPart(ref info);
            }

            _count--;
            _version = unchecked(++_version);

            return true;
        }

        public BinaryListEnumerator<TValue> GetEnumerator()
        {
            return new BinaryListEnumerator<TValue>(this, _storage, ToRealIndex(0), _count);
        }

        internal BinaryListEnumerator<TValue> GetEnumerator(int index, int count)
        {
            return new BinaryListEnumerator<TValue>(this, _storage, ToRealIndex(index), count);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Clear()
        {
            _version = unchecked(++_version);
            _count = 0;
            _zeroRIndex = CalculateZeroIndex(_storage.Length, _count);

            if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
            {
                Array.Clear(_storage, 0, _storage.Length);
            }
        }

        public bool Contains(TValue item) => Contains(ref item);
        public bool Contains(ref TValue item) => FindIndex(ref item, out _);

        public void CopyTo(TValue[] array, int arrayIndex = 0)
        {
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_storage, _zeroRIndex, array, arrayIndex, _count);
        }

        public void CopyTo(int index, TValue[] array, int arrayIndex, int count)
        {
            if (_count - index < count)
            {
                throw new ArgumentException("Not enough elements in the source array");
            }

            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_storage, ToRealIndex(index), array, arrayIndex, count);
        }

        public Span<TValue> ExtractArray(int newArraySize = -1)
        {
            _version = unchecked(++_version);
            var extracted = new Span<TValue>(_storage, _zeroRIndex, _count);

            if (newArraySize >= 0)
            {
                _count = 0;
                _storage = new TValue[DefaultCapacity];
                _zeroRIndex = CalculateZeroIndex(_storage.Length, _count);
            }
            else
                _storage = null!;

            return extracted;
        }

        public TValue[] ExtractRawArray(int newArraySize = -1)
        {
            _version = unchecked(++_version);
            TValue[] extracted = _storage;

            if (newArraySize >= 0)
            {
                _count = 0;
                _storage = new TValue[DefaultCapacity];
                _zeroRIndex = CalculateZeroIndex(_storage.Length, _count);
            }
            else
                _storage = null!;

            return extracted;
        }        

        public bool Replace(TValue oldItem, TValue newItem)
        {
            return Replace(ref oldItem, ref newItem, out _);
        }

        public bool Replace(int index, TValue newItem)
        {
            return Replace(index, ref newItem, out _);
        }

        public bool Replace(ref TValue oldItem, ref TValue newItem, out int newIndex)
        {
            return ReplaceCore(ref oldItem, out newIndex, ref newItem, false);
        }

        public bool Replace(int index, ref TValue newItem, out int newIndex)
        {
            return ReplaceCore(index, out newIndex, ref newItem, false);
        }

        public bool ReplaceUnique(TValue oldItem, TValue newItem)
        {
            return ReplaceUnique(ref oldItem, ref newItem, out _);
        }

        public bool ReplaceUnique(int index, TValue newItem)
        {
            return ReplaceUnique(index, ref newItem, out _);
        }

        public bool ReplaceUnique(ref TValue oldItem, ref TValue newItem, out int newIndex)
        {
            return ReplaceCore(ref oldItem, out newIndex, ref newItem, true);
        }

        public bool ReplaceUnique(int index, ref TValue newItem, out int newIndex)
        {
            return ReplaceCore(index, out newIndex, ref newItem, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ReplaceCore(ref TValue oldItem, out int newVIndex, ref TValue newItem, bool unique)
        {
            FindIndexUnique(ref oldItem, out var oldVIndex);
            return ReplaceCore(oldVIndex, out newVIndex, ref newItem, unique);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ReplaceCore(int oldVIndex, out int newVIndex, ref TValue newItem, bool unique)
        {
            if (FindIndexUnique(ref newItem, out newVIndex) && unique)
                return false;

            if (!CheckIndex(oldVIndex))
                return false;

            _version = unchecked(++_version);

            if (oldVIndex == newVIndex && newVIndex > -1)
            {
                // oldItem and newItem have same index, so we can replace them easily.
                _storage[ToRealIndex(oldVIndex)] = newItem;
                return true;
            }

            // Do replacement
            newVIndex = newVIndex < 0 ? ~newVIndex : newVIndex;
            var info = new ArraySplitInfo(oldVIndex, newVIndex, Count);                        
            ReplaceItemsInArray(ref info, ref newItem, out int addItemIndexDelta);

            _storage[ToRealIndex(newVIndex + addItemIndexDelta)] = newItem;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CheckVersion(int version)
        {
            if (version != _version)
                ThrowInvalidOperation();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOperation()
        {
            throw new InvalidOperationException("Array was changed");
        }
    }
}
