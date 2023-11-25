using System;
using System.Collections.Generic;

namespace UrbanX.DataStructures.Trees
{
    public interface IBinarySearchTree<T> where T : IComparable<T>
    {
        #region Properties
        /// <summary>
        /// Interface property for TreeDrawer.
        /// </summary>
        ITreeNode<T> Root { get; }

        /// <summary>
        /// Returns the number of elements in the tree.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Checks if the tree is empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Returns the height of the tree.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Returns true if tree allows inserting duplicates; otherwise returns false.
        /// </summary>
        bool AllowDuplicates { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Inserts an element to the tree.
        /// </summary>
        /// <param name="item"></param>
        void Insert(T item);

        /// <summary>
        /// Inserts an IList of elements to the tree.
        /// </summary>
        /// <param name="collection"></param>
        void Insert(IList<T> collection);

        /// <summary>
        /// Removes the min value from tree.
        /// </summary>
        void RemoveMin();

        /// <summary>
        /// Removes the max value from tree.
        /// </summary>
        void RemoveMax();

        /// <summary>
        /// Remove an element from tree.
        /// </summary>
        /// <param name="item"></param>
        void Remove(T item);

        /// <summary>
        /// Check for the existence of an item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        bool Contains(T item);

        /// <summary>
        /// Finds the minimum element.
        /// </summary>
        /// <returns></returns>
        T FindMin();

        /// <summary>
        /// Finds the maximum element.
        /// </summary>
        /// <returns></returns>
        T FindMax();

        /// <summary>
        /// Finds all the elements in the tree that match the predicate.
        /// </summary>
        /// <param name="searchPredicate"></param>
        /// <returns></returns>
        IEnumerable<T> FindAll(Predicate<T> searchPredicate);

        /// <summary>
        /// Return an array of the tree element.
        /// </summary>
        /// <returns></returns>
        T[] ToArray();

        /// <summary>
        /// Return a list of the tree element.
        /// </summary>
        /// <returns></returns>
        List<T> ToList();

        /// <summary>
        /// Returns an enumerator that visits node in the order choosed in the constructor
        /// <para>InOrder = 0 : left child, parent, right child.</para> 
        /// <para>PreOrder = 1 : parent, left child, right child.</para>
        /// <para>PostOrder = 2 : left child, right child, parent.</para>
        /// </summary>
        /// <returns></returns>
        IEnumerator<T> GetEnumerator();



        /// <summary>
        /// Clear this tree.
        /// </summary>
        void Clear();

        #endregion
    }
}
