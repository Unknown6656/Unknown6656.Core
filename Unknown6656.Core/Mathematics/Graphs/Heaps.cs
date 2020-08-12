#nullable enable

using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Unknown6656.Mathematics.Graphs
{
    public sealed class BinaryHeapNode<T>
        : IEquatable<BinaryHeapNode<T>>
        , IComparable<T>
        where T : IComparable<T>
    {
        internal int Index { get; }

        public BinaryHeap<T> Heap { get; }

        public BinaryHeapNode<T> Root => IsRoot ? this : Heap.Root;

        public BinaryHeapNode<T> Parent => IsRoot ? this : new BinaryHeapNode<T>(Heap, (Index - 1) / 2);

        public BinaryHeapNode<T> Sibling
        {
            get
            {
                BinaryHeapNode<T> p = Parent;

                return p.LeftChild == this ? p.RightChild : p.LeftChild;
            }
        }

        public BinaryHeapNode<T> LeftChild => new BinaryHeapNode<T>(Heap, Index * 2);

        public BinaryHeapNode<T> RightChild => new BinaryHeapNode<T>(Heap, Index * 2 + 1);

        public bool IsAllocated => Index < Heap.Size;

        public bool IsRoot => Index == 0;

        public int Depth => IsRoot ? 0 : (int)Math.Log2(Index + 1);

        public T Value
        {
            set => Heap[Index] = value;
            get => Heap[Index];
        }


        internal BinaryHeapNode(BinaryHeap<T> heap, int index)
        {
            Heap = heap;
            Index = index;
        }

        public void Remove() => Heap.Remove(this);

        public int CompareTo([MaybeNull] T other) => Value.CompareTo(other);
        
        public int CompareTo(BinaryHeapNode<T> other) => CompareTo(other.Value);
        
        public bool Equals(BinaryHeapNode<T>? other) => other is { } && Heap == other.Heap && Index == other.Index;

        public override bool Equals(object? obj) => obj is BinaryHeapNode<T> other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Heap, Index);

        public static implicit operator T(BinaryHeapNode<T> node) => node.Value;

        public static bool operator ==(BinaryHeapNode<T>? n1, BinaryHeapNode<T>? n2) => n1?.Equals((object?)n2) ?? n2 is null;

        public static bool operator !=(BinaryHeapNode<T>? n1, BinaryHeapNode<T>? n2) => !(n1 == n2);
    }

    public sealed class BinaryHeap<T>
        where T : IComparable<T>
    {
        private readonly List<T> _array;

        internal T this[int index]
        {
            get => _array[index];
            set => DecreaseKey(index, value, true);
        }

        public int Size => _array.Count;

        public int Depth => (int)Math.Log2(Size);

        public BinaryHeapNode<T> Root { get; }


        public BinaryHeap()
            : this(new List<T>())
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BinaryHeap(IEnumerable<T> elements)
        {
            _array = elements.ToList();
            Root = new BinaryHeapNode<T>(this, 0);

            Heapify(0);
        }

        private void Swap(int x, int y)
        {
            T tmp = _array[x];

            _array[x] = _array[y];
            _array[y] = tmp;
        }

        private void DecreaseKey(int i, T value, bool set)
        {
            if (set)
                _array[i] = value;

            if (_array[i].CompareTo(value) >= 0)
                Heapify(i);

            while (i > 0 && parent(i) is int p && _array[p].CompareTo(_array[i]) > 0)
            {
                Swap(i, p);

                i = p;
            }
        }

        private void Heapify(int i)
        {
            int l = 2 * i;
            int r = l + 1;
            int s = i;

            if (l < _array.Count && _array[l].CompareTo(_array[i]) < 0)
                s = l;
            else if (r < _array.Count && _array[r].CompareTo(_array[i]) < 0)
                s = r;

            if (s != i)
            {
                Swap(s, i);
                Heapify(s);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(T element)
        {
            _array.Add(element);
            DecreaseKey(_array.Count - 1, element, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BinaryHeap<T> Clone() => new BinaryHeap<T>(_array);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BinaryHeapNode<T> GetMin() => Root;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T RemoveMin() => Remove(GetMin());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Remove(BinaryHeapNode<T> node)
        {
            if (node.Heap != this)
                throw new ArgumentException("The given node is invalid as it references not to the current heap instance", nameof(node));

            T value = node.Value;

            _array.RemoveAt(node.Index);

            Heapify(node.Index);

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray() => _array.ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToSortedArray()
        {
            BinaryHeap<T> copy = Clone();
            T[] array = new T[copy.Size];

            for (int i = 0; i < array.Length; ++i)
                array[i] = copy.RemoveMin();

            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] HeapSort(IEnumerable<T> collection) => new BinaryHeap<T>(collection).ToSortedArray();

        private static int parent(int i) => (i - 1) / 2;
    }
}
