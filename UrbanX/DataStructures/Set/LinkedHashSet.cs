using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace MetaCity.DataStructures.Set
{
    /// <summary>
    /// Hash table and linked list implementation of the Set interface, with predictable iteration order.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class LinkedHashSet<T> : ISet<T>, ICollection<T>, IEnumerable<T>
    {
        private readonly HashSet<T> _hashSet;

        private readonly LinkedList<T> _link;

        public int Count => _hashSet.Count;

        public bool IsReadOnly => false;

        public T[] OrderedArray => _link.ToArray();

        public T First => _link.First.Value;

        public T Last => _link.Last.Value;


        public LinkedListNode<T> FirstNode => _link.First;

        public LinkedListNode<T> LastNode => _link.Last;


        public LinkedHashSet(IEqualityComparer<T> comparer= default)
        {
            _hashSet = new HashSet<T>(comparer);
            _link = new LinkedList<T>();
        }


        public LinkedHashSet(int capacity, IEqualityComparer<T> comparer = default)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "ArgumentOutOfRange_NeedNonNegNum");

            _hashSet = new HashSet<T>(capacity,comparer);
            _link = new LinkedList<T>();
        }


        public LinkedHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer = default)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            _hashSet = new HashSet<T>(collection,comparer);
            _link = new LinkedList<T>(collection);
        }



        public bool Add(T item)
        {
            try
            {
                if (!_hashSet.Contains(item))
                {
                    _hashSet.Add(item);
                    _link.AddLast(item); // to store order.
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }


        public void Clear()
        {
            _hashSet.Clear();
            _link.Clear();
        }

        public bool Contains(T item)
        {
            return _hashSet.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException("Arg_RankMultiDimNotSupported", nameof(array));
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException("Arg_NonZeroLowerBound", nameof(array));
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, "ArgumentOutOfRange_Index");
            }

            if (array.Length - arrayIndex < this.Count)
            {
                throw new ArgumentException("Argument_InvalidOffLen");
            }

            try
            {
                Array.Copy(_link.ToArray(), 0, array, arrayIndex, Count);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException("Argument_InvalidArrayType", nameof(array));
            }
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (this.Count == 0)
                return;

            if (other == this)
            {
                this.Clear();
                return;
            }

            _hashSet.ExceptWith(other);

            // rebuild link.
            RebuildLink();
        }

        public IEnumerator<T> GetEnumerator() => _link.GetEnumerator();
   

        public void IntersectWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (this.Count == 0)
                return;

            if (other == this)
                return;


            _hashSet.IntersectWith(other);

            // rebuild link.
            RebuildLink();
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (other is ICollection c)
            {
                if (Count == 0)
                    return c.Count > 0;
            }

            return _hashSet.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (Count == 0)
                return false;

            if (other is ICollection c && c.Count == 0)
                return true;

            return _hashSet.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (Count == 0)
            {
                return true;
            }

            return _hashSet.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (other is ICollection c && c.Count == 0)
                return true;

            return _hashSet.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (Count == 0)
                return false;

            if (other is ICollection<T> c && c.Count == 0)
                return false;

            return _hashSet.Overlaps(other);
        }

        public bool Remove(T item)
        {
            try
            {
                if (_hashSet.Contains(item))
                {
                    var flag1 =_hashSet.Remove(item);
                    var flag2 =_link.Remove(item); // to store order.
                    return flag1&&flag2;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return _hashSet.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (Count == 0)
            {
                UnionWith(other);
                return;
            }

            if (other == this)
            {
                Clear();
                return;
            }


            _hashSet.SymmetricExceptWith(other);

            // rebuild link.
            RebuildLink();
        }

        public void UnionWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            _hashSet.UnionWith(other);

            // rebuild link.
            RebuildLink();
        }


        private void RebuildLink()
        {
            // rebuild link.
            var deepCopy = _link.ToArray();
            _link.Clear();

            foreach (var i in deepCopy)
            {
                if (_hashSet.Contains(i)) // if the rest set contains this item, add this item to link while still keep the order.
                    _link.AddLast(i);
            }
        }

        void ICollection<T>.Add(T item) => Add(item);

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }


        public T[] ToArray()
        {
            return this.OrderedArray;
        }
    }
}
