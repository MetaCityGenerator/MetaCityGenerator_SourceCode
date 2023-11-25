using System;
using System.Collections.Generic;

using UrbanX.DataStructures.Utility;

namespace UrbanX.DataStructures.Heaps
{
    /// <summary>
    /// Implements the priority Queue Data Structure.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TPriority"></typeparam>
    public class MinPriorityQueue<TKey, TPriority> where TKey : IComparable<TKey> where TPriority : IComparable<TPriority>
    {

        /// <summary>
        /// Instance variables.
        /// The priorities value comparer.
        /// </summary>
        private readonly Comparer<PriorityQueueNode<TKey, TPriority>> _priorityComparer;

        /// <summary>
        /// Instance property.
        /// A dictionary of keys and number of copies in the heap.
        /// </summary>
        public Dictionary<TKey, long> QueueKeys { get; }

        /// <summary>
        /// Instance property.
        /// The internal BinaryMinHeap storing PriorityQueueNodes with key, priority.
        /// </summary>
        public BinaryMinHeap<PriorityQueueNode<TKey, TPriority>> QueueHeap { get; }



        /// <summary>
        /// Constructor of MinPriorityQueue.
        /// </summary>
        public MinPriorityQueue() : this(0, null) { }
        /// <summary>
        /// Constructor of MinPriorityQueue with initial capacity.
        /// </summary>
        /// <param name="capacity"></param>
        public MinPriorityQueue(int capacity) : this(capacity, null) { }
        /// <summary>
        /// Constructor of MinPriorityQueue with initial capacity and comparer.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="priorityComparer"></param>
        public MinPriorityQueue(int capacity, Comparer<PriorityQueueNode<TKey, TPriority>> priorityComparer)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("Capacity can't be less than zero.");
            }


            // Make sure the TPriority is elegible for a priority.
            if (!ValidPriorityType())
            {
                throw new NotSupportedException("The priority type is not supported.");
            }

            // Initialize comparer.
            if (priorityComparer == null)
            {
                _priorityComparer = Comparer<PriorityQueueNode<TKey, TPriority>>.Default;
            }
            else
            {
                _priorityComparer = priorityComparer;
            }

            // Initialize MinPriorityQueue.
            QueueKeys = new Dictionary<TKey, long>();
            QueueHeap = new BinaryMinHeap<PriorityQueueNode<TKey, TPriority>>(capacity, _priorityComparer);
        }

        /// <summary>
        /// Validates the Type of TPriority. Returns true if acceptable, false otherwise.
        /// </summary>
        /// <returns></returns>
        private bool ValidPriorityType()
        {
            bool isValid;
            TypeCode typeCode = Type.GetTypeCode(typeof(TPriority));

            switch (typeCode)
            {
                //case TypeCode.DateTime:
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    isValid = true;
                    break;
                default:
                    isValid = false;
                    break;
            }

            return isValid;
        }

        /// <summary>
        /// Returns the count of elements in the queue.
        /// </summary>
        public int Count
        {
            get { return QueueHeap.Count; }
        }

        /// <summary>
        /// Checks if the queue is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return QueueHeap.IsEmpty; }
        }

        /// <summary>
        /// Returns the highest priority element.
        /// </summary>
        /// <returns></returns>
        public TKey PeekAtMinPriority()
        {
            if (QueueHeap.IsEmpty)
            {
                throw new ArgumentOutOfRangeException("Queue is empty.");
            }

            return QueueHeap.Peek().Key;
        }

        /// <summary>
        /// Checks for the existence of a key in the queue.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Contains(TKey key)
        {
            return QueueKeys.ContainsKey(key);
        }

        /// <summary>
        /// Enqueue the specified key, value and priority.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="priority"></param>
        public void Enqueue(TKey key, TPriority priority)
        {
            var newNode = new PriorityQueueNode<TKey, TPriority>(key, priority);
            QueueHeap.Add(newNode);

            if (QueueKeys.ContainsKey(key))
            {
                QueueKeys[key] += 1;
            }
            else
            {
                QueueKeys.Add(key, 1);
            }
        }

        /// <summary>
        /// Dequeue this instance.
        /// </summary>
        /// <returns></returns>
        public TKey DequeueMin()
        {
            if (QueueHeap.IsEmpty)
            {
                throw new ArgumentOutOfRangeException("Queue is empty.");
            }

            var key = QueueHeap.ExtractMin().Key;

            // Decrease the key count.
            QueueKeys[key] -= 1;

            // Remove key if its count is zero
            if (QueueKeys[key] == 0)
            {
                QueueKeys.Remove(key);
            }

            return key;
        }

        /// <summary>
        /// Sets the priority.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="newPriority"></param>
        public void UpdatePriority(TKey key, TPriority newPriority)
        {
            // Handle boundaries errors.
            if (QueueHeap.IsEmpty)
            {
                throw new ArgumentOutOfRangeException("Queue is empty.");
            }

            if (!QueueKeys.ContainsKey(key))
            {
                throw new KeyNotFoundException();
            }

            int i;
            for (i = 0; i < QueueHeap.Count; i++)
            {
                if (QueueHeap[i].Key.IsEqualTo(key))
                {
                    break;
                }
            }
            QueueHeap[i].Priority = newPriority;

            QueueHeap.RebuildHeap();
        }

        /// <summary>
        /// Clear this priority queue.
        /// </summary>
        public void Clear()
        {
            QueueHeap.Clear();
            QueueKeys.Clear();
        }
    }
}
