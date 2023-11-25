using System;
using System.Collections.Generic;

namespace UrbanX.DataStructures.Heaps
{
    public interface IMaxHeap<T> where T : IComparable<T>
    {
        /// <summary>
        /// Returns the number of elements in heap.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Checks whether this heap is empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Heapifies the specified new collection. Overides the current heap.
        /// </summary>
        /// <param name="newCollection"></param>
        void Initialize(IList<T> newCollection);

        /// <summary>
        /// Adding a new key to the heap.
        /// </summary>
        /// <param name="heapKey"></param>
        void Add(T heapKey);

        /// <summary>
        /// Find the maxium node of a max heap.
        /// </summary>
        /// <returns> The maximum. </returns>
        T Peek();

        /// <summary>
        /// Removes the node of maximum value from a max heap.
        /// </summary>
        void RemoveMax();

        /// <summary>
        /// Returns the node of maximum value fro a max heap after removing it from the heap.
        /// </summary>
        /// <returns></returns>
        T ExtractMax();

        /// <summary>
        /// Clear this heap.
        /// </summary>
        void Clear();

        /// <summary>
        /// Rebuilds the heap.
        /// </summary>
        void RebuildHeap();

        /// <summary>
        /// Returns an array version of this heap.
        /// </summary>
        /// <returns></returns>
        T[] ToArray();

        /// <summary>
        /// Returns a list version of this heap.
        /// </summary>
        /// <returns></returns>
        List<T> ToList();

        /// <summary>
        /// Returns a new min heap that contains all elements of this heap.
        /// </summary>
        /// <returns></returns>
        IMinHeap<T> ToMinHeap();
    }
}
