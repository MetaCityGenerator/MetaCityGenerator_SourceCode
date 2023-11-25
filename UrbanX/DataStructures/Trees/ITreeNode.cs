using System;

namespace UrbanX.DataStructures.Trees
{
    public interface ITreeNode<T> : IComparable<ITreeNode<T>> where T : IComparable<T>
    {
        #region Properties

        /// <summary>
        /// Value of this TreeNode.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// Returns number of direct descendents: 0, 1, 2 (none, left or right, or both).
        /// </summary>
        int ChildrenCount { get; }

        /// <summary>
        /// Interface property for TreeDrawer.
        /// </summary>
        ITreeNode<T> Parent { get; }

        /// <summary>
        /// Interface property for TreeDrawer.
        /// </summary>
        ITreeNode<T> LeftChild { get; }

        /// <summary>
        /// Interface property for TreeDrawer.
        /// </summary>
        ITreeNode<T> RightChild { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Checks whether this node has any children.
        /// </summary>
        /// <returns></returns>
        bool HasChildren();

        /// <summary>
        /// Checks whether this node has left child.
        /// </summary>
        /// <returns></returns>
        bool HasLeftChild();


        /// <summary>
        /// Checks whether this node has right child.
        /// </summary>
        /// <returns></returns>
        bool HasRightChild();

        /// <summary>
        /// Check if this node has only one child and whether it is the left child.
        /// </summary>
        /// <returns></returns>
        bool HasOnlyLeftChild();

        /// <summary>
        /// Check if this node has only one child and whether it is the right child.
        /// </summary>
        /// <returns></returns>
        bool HasOnlyRightChild();

        /// <summary>
        /// Checks whether this node is the left child of its' parent.
        /// </summary>
        /// <returns></returns>
        bool IsLeftChild();

        /// <summary>
        /// Checks whether this node is the left child of it's parent.
        /// </summary>
        /// <returns></returns>
        bool IsRightChild();


        /// <summary>
        /// Checks whether this node is a leaf node.
        /// </summary>
        /// <returns></returns>
        bool IsLeafNode();


        #endregion

    }
}
