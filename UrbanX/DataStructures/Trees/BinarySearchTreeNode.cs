using System;

namespace UrbanX.DataStructures.Trees
{
    /// <summary>
    /// The binary search tree node.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BSTreeNode<T> : ITreeNode<T> where T : IComparable<T>
    {

        #region Constructors

        public BSTreeNode() : this(default, null, null, null) { }
        public BSTreeNode(T value) : this(value, null, null, null) { }
        /// <summary>
        /// Constructor of BSTNode.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="parent"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public BSTreeNode(T value, BSTreeNode<T> parent, BSTreeNode<T> left, BSTreeNode<T> right)
        {
            Value = value;
            _parent = parent;
            LeftChild = left;
            RightChild = right;
        }

        #endregion

        #region Properties

        public virtual T Value { get; set; }

        public BSTreeNode<T> _parent;

        public virtual BSTreeNode<T> LeftChild { get; set; }

        public virtual BSTreeNode<T> RightChild { get; set; }


        public virtual int ChildrenCount
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

        ITreeNode<T> ITreeNode<T>.Parent => this._parent;
        #endregion


        public virtual bool HasChildren() // quite redantant...
        {
            return this.ChildrenCount > 0;
        }


        public virtual bool HasLeftChild()
        {
            return this.LeftChild != null;
        }


        public virtual bool HasRightChild()
        {
            return this.RightChild != null;
        }


        public virtual bool HasOnlyRightChild()
        {
            return !this.HasLeftChild() && this.HasRightChild();
        }


        public virtual bool HasOnlyLeftChild()
        {
            return !this.HasRightChild() && this.HasLeftChild();
        }


        public virtual bool IsLeftChild()
        {
            return this._parent != null && this._parent.LeftChild == this;
        }


        public virtual bool IsRightChild()
        {
            return this._parent != null && this._parent.RightChild == this;
        }


        public virtual bool IsLeafNode() // quite redantant
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

    }
}
