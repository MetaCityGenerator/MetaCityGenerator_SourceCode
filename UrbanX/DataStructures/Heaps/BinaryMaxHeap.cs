using System;
using System.Collections.Generic;
using System.Linq;

using UrbanX.Algorithms.Utility;

namespace UrbanX.DataStructures.Heaps
{
    /// <summary>
    /// Maximum Heap Data Structure.
    /// </summary>
    public class BinaryMaxHeap<T> : IMaxHeap<T> where T : IComparable<T>
    {
        /// <summary>
        /// Instance variables.
        /// Collection: The list of elements. Implemented as array-based list with auto-resizing.
        /// </summary>
        private List<T> _collection;

        private readonly Comparer<T> _heapComparer;

        /// <summary>
        /// Constructor of a BinaryMaxHeap without initial capacity.
        /// </summary>
        public BinaryMaxHeap() : this(0, null) { }

        /// <summary>
        /// Constructor of a BinaryMaxHeap with initial capacity.
        /// </summary>
        /// <param name="capacity"></param>
        public BinaryMaxHeap(int capacity) : this(capacity, null) { }

        /// <summary>
        /// Constructor of a BinaryMaxHeap with initial capacity and comparer.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="comparer"></param>
        public BinaryMaxHeap(int capacity, Comparer<T> comparer)
        {
            _collection = new List<T>(capacity);
            _heapComparer = comparer ?? Comparer<T>.Default;
        }

        /// <summary>
        /// Helper method. Builds a max heap from the inner array-list collection.
        /// </summary>
        private void BuildMaxHeap()
        {
            int lastIndex = _collection.Count - 1;
            int lastNodeWithChildren = lastIndex / 2; // Parent node.

            for (int node = lastNodeWithChildren; node >= 0; node--)
            {
                MaxHeapify(node, lastIndex);
            }
        }

        /// <summary>
        /// Helper method. Used to restore heap condition after insertion.
        /// </summary>
        /// <param name="nodeIndex"></param>
        private void SiftUp(int nodeIndex)
        {
            int parent = (nodeIndex - 1) / 2;
            while (_heapComparer.Compare(_collection[nodeIndex], _collection[parent]) > 0)
            {
                _collection.Swap(parent, nodeIndex);
                nodeIndex = parent;
                parent = (nodeIndex - 1) / 2;
            }
        }

        /// <summary>
        /// Helper method. Used in Building a max heap.
        /// </summary>
        /// <param name="nodeIndex"></param>
        /// <param name="lastIndex"></param>
        private void MaxHeapify(int nodeIndex, int lastIndex)
        {
            // Assume that the subtrees left(node) and right(node) are max-heaps.
            int left = nodeIndex * 2 + 1;
            int right = left + 1;
            int largest = nodeIndex;

            // If collection[left] > collection[nodeIndex]
            if (left <= lastIndex && _heapComparer.Compare(_collection[left], _collection[nodeIndex]) > 0)
            {
                largest = left;
            }

            // If collection[right] > collection[largest]
            if (right <= lastIndex && _heapComparer.Compare(_collection[right], _collection[largest]) > 0)
            {
                largest = right;
            }

            // Swap and heapity recursively.
            if (largest != nodeIndex)
            {
                _collection.Swap(nodeIndex, largest);
                MaxHeapify(largest, lastIndex);
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
                if (index < 0 || index > this.Count || this.Count == 0)
                {
                    throw new IndexOutOfRangeException();
                }
                return _collection[index];
            }
            set
            {
                if (index < 0 || index >= this.Count)
                {
                    throw new IndexOutOfRangeException();
                }

                _collection[index] = value;

                if (index != 0 && _heapComparer.Compare(_collection[index], _collection[(index - 1) / 2]) > 0)
                {
                    SiftUp(index);
                }
                else
                {
                    MaxHeapify(index, _collection.Count - 1);
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
                BuildMaxHeap();
            }
        }
        public void Add(T heapKey)
        {
            _collection.Add(heapKey);
            if (!this.IsEmpty)
            {
                SiftUp(_collection.Count - 1);
            }
        }

        public void Clear()
        {
            if (this.IsEmpty)
            {
                throw new Exception("Heap is empty.");
            }

            _collection.Clear();
        }

        public T ExtractMax()
        {
            var max = Peek();
            RemoveMax();
            return max;
        }


        public T Peek()
        {
            if (this.IsEmpty)
            {
                throw new Exception("Heap is empty.");
            }

            return _collection.First();
        }

        public void RebuildHeap()
        {
            BuildMaxHeap();
        }

        public void RemoveMax()
        {
            if (this.IsEmpty)
            {
                throw new Exception("Heap is empty.");
            }

            int max = 0;
            int last = _collection.Count - 1;
            _collection.Swap(max, last);

            _collection.RemoveAt(last);
            last--;

            MaxHeapify(0, last);
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
        /// Union two heaps together, return a new max heap of both heaps' elements.
        /// And then destroys the original two heaps.
        /// </summary>
        /// <param name="firstMaxHeap"></param>
        /// <param name="secondMaxHeap"></param>
        /// <returns></returns>
        public BinaryMaxHeap<T> Union(ref BinaryMaxHeap<T> firstMaxHeap, ref BinaryMaxHeap<T> secondMaxHeap)
        {
            if (firstMaxHeap == null || secondMaxHeap == null)
            {
                throw new ArgumentNullException("Null heaps are not allowed.");
            }

            // Create a new heap with reserved size.
            int size = firstMaxHeap.Count + secondMaxHeap.Count;
            var unionHeap = new BinaryMaxHeap<T>(size, Comparer<T>.Default);

            // Insert elements into the new heap.
            while (!firstMaxHeap.IsEmpty)
            {
                unionHeap.Add(firstMaxHeap.ExtractMax());
            }
            while (!secondMaxHeap.IsEmpty)
            {
                unionHeap.Add(secondMaxHeap.ExtractMax());
            }

            // Destroy the two input heaps.
            firstMaxHeap = secondMaxHeap = null;

            return unionHeap;

        }

        public IMinHeap<T> ToMinHeap()
        {
            BinaryMinHeap<T> newMinHeap = new BinaryMinHeap<T>(this.Count, this._heapComparer);
            newMinHeap.Initialize(this._collection.ToArray());

            return newMinHeap;
        }
    }
}
