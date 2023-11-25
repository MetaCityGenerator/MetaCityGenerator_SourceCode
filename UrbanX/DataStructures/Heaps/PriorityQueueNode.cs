using System;


namespace UrbanX.DataStructures.Heaps
{
    /// <summary>
    /// The Priority-queue node.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TPriority"></typeparam>
    public class PriorityQueueNode<TKey, TPriority> : IComparable<PriorityQueueNode<TKey, TPriority>> where TKey : IComparable<TKey> where TPriority : IComparable<TPriority>
    {

        public TKey Key { get; set; }

        public TPriority Priority { get; set; }

        /// <summary>
        /// Constructor for priorityQueueNode with default parameters.
        /// </summary>
        public PriorityQueueNode() : this(default, default) { }
        /// <summary>
        /// Constructor for priorityQueueNode.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="priority"></param>
        public PriorityQueueNode(TKey key, TPriority priority)
        {
            Key = key;
            Priority = priority;
        }

        public int CompareTo(PriorityQueueNode<TKey, TPriority> other)
        {
            if (other == null)
            {
                return -1;
            }

            return Priority.CompareTo(other.Priority);
        }
    }
}
