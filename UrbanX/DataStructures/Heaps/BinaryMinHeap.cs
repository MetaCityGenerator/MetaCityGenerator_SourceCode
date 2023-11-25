using System;
using System.Collections.Generic;
using System.Linq;

using UrbanX.Algorithms.Utility;

namespace UrbanX.DataStructures.Heaps
{
    /// <summary>
    /// Minimum Heap Data Structure.
    /// </summary>
    public class BinaryMinHeap<T> : IMinHeap<T> where T : IComparable<T>
    {
        /// <summary>
        /// Instance variables.
        /// Collection: The list of elements. Implemented as array-based list with auto-resizing.
        /// </summary>
        private List<T> _collection;
        private readonly Comparer<T> _heapComparer;

        /// <summary>
        /// Constructor of a BinaryMinHeap without initial capacity.
        /// </summary>
        public BinaryMinHeap() : this(0, null) { }

        /// <summary>
        /// Constructor of a BinaryMinHeap with initial capacity.
        /// </summary>
        /// <param name="capacity"></param>
        public BinaryMinHeap(int capacity) : this(capacity, null) { }

        /// <summary>
        /// Constructor of a BinaryMinHeap with initial capacity and comparer.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="comparer"></param>
        public BinaryMinHeap(int capacity, Comparer<T> comparer)
        {
            _collection = new List<T>(capacity);
            _heapComparer = comparer ?? Comparer<T>.Default;
        }


        /// <summary>
        /// Helper method. Builds a min heap from the inner array-list collection.
        /// </summary>
        private void BuildMinHeap()
        {
            int lastIndex = _collection.Count - 1;
            int lastNodeWithChildren = lastIndex / 2; // Parent node.

            for (int node = lastNodeWithChildren; node >= 0; node--)
            {
                MinHeapify(node, lastIndex);
            }
        }

        /// <summary>
        /// Helper method. Used to restore heap condition after insertion.
        /// </summary>
        /// <param name="nodeIndex"></param>
        private void SiftUp(int nodeIndex)
        {
            int parent = (nodeIndex - 1) / 2;
            while (_heapComparer.Compare(_collection[nodeIndex], _collection[parent]) < 0)
            {
                _collection.Swap(parent, nodeIndex);
                nodeIndex = parent;
                parent = (nodeIndex - 1) / 2;
            }
        }

        /// <summary>
        /// Helper method. Used in Building a min heap.
        /// </summary>
        /// <param name="nodeIndex"></param>
        /// <param name="lastIndex"></param>
        private void MinHeapify(int nodeIndex, int lastIndex)
        {
            // Assume that the subtrees left(node) and right(node) are max-heaps.
            int left = nodeIndex * 2 + 1;
            int right = left + 1;
            int smallest = nodeIndex;

            // If collection[left] < collection[nodeIndex]
            if (left <= lastIndex && _heapComparer.Compare(_collection[left], _collection[nodeIndex]) < 0)
            {
                smallest = left;
            }

            // If collection[right] < collection[largest]
            if (right <= lastIndex && _heapComparer.Compare(_collection[right], _collection[smallest]) < 0)
            {
                smallest = right;
            }

            // Swap and heapity recursively.
            if (smallest != nodeIndex)
            {
                _collection.Swap(nodeIndex, smallest);
                MinHeapify(smallest, lastIndex);
            }
        }

        public int Count
        {
            get { return _collection.Count; }
        }

        public bool IsEmpty
        {
            get { return _collection.Count == 0; }
        }

        /// <summary>
        /// Gets or sets the item at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index > Count || Count == 0)
                {
                    throw new IndexOutOfRangeException();
                }
                return _collection[index];
            }
            set
            {
                if (index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }

                _collection[index] = value;

                if (index != 0 && _heapComparer.Compare(_collection[index], _collection[(index - 1) / 2]) < 0)
                {
                    SiftUp(index);
                }
                else
                {
                    MinHeapify(index, _collection.Count - 1);
                }
            }
        }

        public void Initialize(IList<T> newCollection)
        {
            if (newCollection.Count > 0)
            {
                // Reset and reserve the size of the newCollection.
                _collection = new List<T>(newCollection.Count);

                // Copy the elements from the collection to the inner collection.
                for (int i = 0; i < newCollection.Count; ++i)
                {
                    _collection.Insert(i, newCollection[i]);
                }
                // Build the heap.
                BuildMinHeap();
            }
        }

        public void Add(T heapKey)
        {
            _collection.Add(heapKey);
            if (!IsEmpty)
            {
                SiftUp(_collection.Count - 1);
            }
        }

        public void Clear()
        {
            if (IsEmpty)
            {
                throw new Exception("Heap is empty.");
            }

            _collection.Clear();
        }

        public T Peek()
        {
            if (IsEmpty)
            {
                throw new Exception("Heap is empty.");
            }

            return _collection.First();
        }

        public void RemoveMin()
        {
            if (IsEmpty)
            {
                throw new Exception("Heap is empty.");
            }

            int min = 0;
            int last = _collection.Count - 1;
            _collection.Swap(min, last);

            _collection.RemoveAt(last);
            last--;

            MinHeapify(0, last);
        }

        public T ExtractMin()
        {
            var min = Peek();
            RemoveMin();

            return min;
        }

        /// <summary>
        /// Rebuild the Heap, only need to use this method when changed the value of item.
        /// Add and remove item will automatically rebuild heap.
        /// </summary>
        public void RebuildHeap()
        {
            BuildMinHeap();
        }

        public T[] ToArray()
        {
            return _collection.ToArray();
        }

        public List<T> ToList()
        {
            return _collection.ToList();
        }


        /// <summary>
        /// Union two heaps together, return a new min heap of both heaps' elements.
        /// And then destroys the original two heaps.
        /// </summary>
        /// <param name="firstMinHeap"></param>
        /// <param name="secondMinHeap"></param>
        /// <returns></returns>
        public BinaryMinHeap<T> Union(ref BinaryMinHeap<T> firstMinHeap, ref BinaryMinHeap<T> secondMinHeap)
        {
            if (firstMinHeap == null || secondMinHeap == null)
            {
                throw new ArgumentNullException("Null heaps are not allowed.");
            }

            // Create a new heap with reserved size.
            int size = firstMinHeap.Count + secondMinHeap.Count;
            var unionHeap = new BinaryMinHeap<T>(size, Comparer<T>.Default);

            // Insert elements into the new heap.
            while (!firstMinHeap.IsEmpty)
            {
                unionHeap.Add(firstMinHeap.ExtractMin());
            }
            while (!secondMinHeap.IsEmpty)
            {
                unionHeap.Add(secondMinHeap.ExtractMin());
            }

            // Destroy the two input heaps.
            firstMinHeap = secondMinHeap = null;

            return unionHeap;
        }

        public IMaxHeap<T> ToMaxHeap()
        {
            BinaryMaxHeap<T> newMaxHeap = new BinaryMaxHeap<T>(Count, _heapComparer);
            newMaxHeap.Initialize(_collection.ToArray());

            return newMaxHeap;
        }
    }
}
