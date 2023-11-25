using System;

namespace UrbanX.DataStructures.Trees
{
    /// <summary>
    /// Red-Black tree node.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RBTreeNode<T> : ITreeNode<T> where T : IComparable<T>
    {
        /// <summary>
        /// Constructor with default parameters.
        /// </summary>
        public RBTreeNode() : this(default, null, null, null) { }

        /// <summary>
        /// Constructor with T value.
        /// </summary>
        /// <param name="value"></param>
        public RBTreeNode(T value) : this(value, null, null, null) { }

        /// <summary>
        /// Constructor with T value, height, parent and children.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="parent"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public RBTreeNode(T value, RBTreeNode<T> parent, RBTreeNode<T> left, RBTreeNode<T> right)
        {
            Value = value;
            Color = RBTreeColors.Red;
            Parent = parent;
            LeftChild = left;
            RightChild = right;
        }


        public RBTreeColors Color { get; set; }

        public T Value { get; set; }

        public RBTreeNode<T> Parent { get; set; }

        public RBTreeNode<T> LeftChild { get; set; }

        public RBTreeNode<T> RightChild { get; set; }


        public int ChildrenCount
        {
            get
            {
                int count = 0;

                if (this.HasLeftChild())
                    count++;
                if (this.HasRightChild())
                    count++;

                return count;
            }
        }


        ITreeNode<T> ITreeNode<T>.LeftChild => this.LeftChild;

        ITreeNode<T> ITreeNode<T>.RightChild => this.RightChild;

        ITreeNode<T> ITreeNode<T>.Parent => this.Parent;


        #region Class methods

        public bool HasChildren() // quite redantant...
        {
            return this.ChildrenCount > 0;
        }


        public bool HasLeftChild()
        {
            return this.LeftChild != null;
        }


        public bool HasRightChild()
        {
            return this.RightChild != null;
        }


        public bool HasOnlyRightChild()
        {
            return !this.HasLeftChild() && this.HasRightChild();
        }


        public bool HasOnlyLeftChild()
        {
            return !this.HasRightChild() && this.HasLeftChild();
        }


        public bool IsLeftChild()
        {
            return this.Parent != null && this.Parent.LeftChild == this;
        }


        public bool IsRightChild()
        {
            return this.Parent != null && this.Parent.RightChild == this;
        }


        public bool IsLeafNode() // quite redantant
        {
            return this.ChildrenCount == 0;
        }


        public int CompareTo(ITreeNode<T> other)
        {
            if (other == null)
            {
                return -1;
            }

            return this.Value.CompareTo(other.Value);
        }



        /// <summary>
        /// Returns true  if this node is red. Otherwise, false.
        /// </summary>
        /// <returns></returns>
        public bool IsRed()
        {
            return Color == RBTreeColors.Red;
        }

        /// <summary>
        /// Returns true  if this node is black. Otherwise, false.
        /// </summary>
        /// <returns></returns>
        public bool IsBlack()
        {
            return Color == RBTreeColors.Black;
        }

        /// <summary>
        /// Returns the sibling of this node.
        /// </summary>
        /// <returns></returns>
        public RBTreeNode<T> GetSibling()
        {
            return this.Parent == null ? null : (this.IsLeftChild() ? this.Parent.RightChild : this.Parent.LeftChild);
        }

        /// <summary>
        /// Returns the grandParent of this node.
        /// </summary>
        /// <returns></returns>
        public RBTreeNode<T> GetGrandParent()
        {
            return this.Parent?.Parent;
        }
        #endregion

    }

}
