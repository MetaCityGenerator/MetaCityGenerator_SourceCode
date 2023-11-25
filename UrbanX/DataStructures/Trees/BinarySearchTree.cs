using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UrbanX.DataStructures.Utility;

namespace UrbanX.DataStructures.Trees
{
    /// <summary>
    /// Specifies the mode of travelling through the tree.
    /// </summary>
    public enum TraversalMode
    {
        InOrder = 0,
        PreOrder = 1,
        PostOrder = 2
    }

    /// <summary>
    /// Implements a generic Binary Search Tree data structure.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BinarySearchTree<T> : IBinarySearchTree<T> where T : IComparable<T>
    {

        private readonly TraversalMode _traversalMode;

        #region Constructors

        /// <summary>
        /// Constructor. Allows duplicates by default. 
        /// GetEnumerator in Inoreder traversal mode.
        /// </summary>
        public BinarySearchTree() : this(true, TraversalMode.InOrder) { }


        /// <summary>
        /// Constructor. If allowDuplictes is set to false, no duplicate items will be inserted.
        /// Need to choose the traversal mode.
        /// </summary>
        /// <param name="allowDuplicates"></param>
        /// <param name="traversal"></param>
        public BinarySearchTree(bool allowDuplicates, TraversalMode traversal)
        {
            Count = 0;
            AllowDuplicates = allowDuplicates;
            Root = null;
            _traversalMode = traversal;
        }

        #endregion

        #region Class Properties

        public BSTreeNode<T> Root { get; private set; }

        public int Count { get; private set; }

        public bool AllowDuplicates { get; }

        public bool IsEmpty
        {
            get { return Count == 0; }
        }

        public int Height
        {
            get
            {
                if (IsEmpty)
                    return 0;

                var currentNode = Root;
                return GetTreeHeight(currentNode);
            }
        }

        ITreeNode<T> IBinarySearchTree<T>.Root => this.Root;



        #endregion

        #region Private methods for constructing BinarySearchTree
        /// <summary>
        /// Replaces the node's value from it's parent node object with the newValue.
        /// Used in the recurvive Remove method.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="newNode"></param>
        protected virtual void ReplaceNodeInParent(BSTreeNode<T> node, BSTreeNode<T> newNode = null)
        {
            if (node._parent != null)
            {
                if (node.IsLeftChild())
                {
                    node._parent.LeftChild = newNode;
                }
                else
                {
                    node._parent.RightChild = newNode;
                }
            }
            else
            {
                Root = newNode;
            }

            if (newNode != null)
            {
                newNode._parent = node._parent;
            }
        }

        /// <summary>
        /// Remove the specified node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual bool RemoveNode(BSTreeNode<T> node)
        {
            if (node == null)
                return false;

            if (node.ChildrenCount == 2)
            {
                var successor = FindNextLargerNode(node);
                node.Value = successor.Value;
                return true && RemoveNode(successor);
            }

            if (node.HasOnlyLeftChild()) // Altered
            {
                ReplaceNodeInParent(node, node.LeftChild);
                Count--;
            }
            else if (node.HasOnlyRightChild())
            {
                ReplaceNodeInParent(node, node.RightChild);
                Count--;
            }
            else  // This node has no children.
            {
                ReplaceNodeInParent(node, null);
                Count--;
            }

            return true;
        }

        /// <summary>
        /// Inserts a new node to the tree.
        /// </summary>
        /// <param name="newNode">New node to be inserted.</param>
        /// <returns></returns>
        protected virtual bool InsertNode(BSTreeNode<T> newNode)
        {
            if (this.Root == null)
            {
                Root = newNode;
                Count++;
                return true;  // Root doesn't have parent.
            }

            if (newNode._parent == null)
            {
                newNode._parent = this.Root;
            }

            // Check for value equality and whether inserting duplicates is allowed.
            if (AllowDuplicates == false && newNode._parent.Value.IsEqualTo(newNode.Value))
            {
                return false;
            }

            // Go left.
            if (newNode._parent.Value.IsGreaterThan(newNode.Value)) // newNode < parent.
            {
                if (!newNode._parent.HasLeftChild())
                {
                    newNode._parent.LeftChild = newNode;

                    Count++;

                    return true;
                }

                // Go below the current left child.
                newNode._parent = newNode._parent.LeftChild;
                return InsertNode(newNode);
            }

            // Go right.  newNode>= parent.  if_allowDuplicates is true, the duplicate node will locate at the right side.
            else
            {
                if (!newNode._parent.HasRightChild())
                {
                    newNode._parent.RightChild = newNode;
                    Count++;

                    return true;
                }

                newNode._parent = newNode._parent.RightChild;
                return InsertNode(newNode);
            }
        }


        /// <summary>
        /// Calculates the tree height from a specified node, recursively.
        /// Time complexity: O(n), where n is the count of nodes.
        /// </summary>
        /// <param name="node">Node</param>
        /// <returns> Height of node's longest subtree </returns>
        protected virtual int GetTreeHeight(BSTreeNode<T> node)
        {
            if (node == null)
                return 0;

            if (node.IsLeafNode())
                return 1;

            if (node.ChildrenCount == 2)
                return 1 + Math.Max(GetTreeHeight(node.LeftChild), GetTreeHeight(node.RightChild));

            if (node.HasOnlyLeftChild())
                return 1 + GetTreeHeight(node.LeftChild);

            // HasOnlyRightChild.
            return 1 + GetTreeHeight(node.RightChild);
        }

        /// <summary>
        /// Finds a node inside another node's subtree, given the first node's value.
        /// </summary>
        /// <param name="currentNode">Node to start search from</param>
        /// <param name="item">Search value</param>
        /// <returns>Node if found; otherwise null</returns>
        protected virtual BSTreeNode<T> FindNode(BSTreeNode<T> currentNode, T item)
        {
            if (currentNode == null)
                return currentNode;

            if (item.IsEqualTo(currentNode.Value))
                return currentNode;

            if (currentNode.HasLeftChild() && item.IsLessThan(currentNode.Value))
                return FindNode(currentNode.LeftChild, item);

            if (currentNode.HasRightChild() && item.IsGreaterThan(currentNode.Value))
                return FindNode(currentNode.RightChild, item);

            return null;
        }


        /// <summary>
        /// Returns the min-node in a subtree.
        /// Used in the recursive RemoveNode function.
        /// </summary>
        /// <param name="node">The tree node with subtree(s).</param>
        /// <returns>The minimum-valued tree node.</returns>
        protected virtual BSTreeNode<T> FindMinNode(BSTreeNode<T> node)
        {
            if (node == null)
                return node;

            var currentNode = node;

            while (currentNode.HasLeftChild())
                currentNode = currentNode.LeftChild;

            return currentNode;
        }


        /// <summary>
        /// Returns the max-node in a subtree.
        /// Used in the recusive _remove function.
        /// </summary>
        /// <param name="node">The tree node with subtree(s).</param>
        /// <returns>The maximum-valued tree node.</returns>
        protected virtual BSTreeNode<T> FindMaxNode(BSTreeNode<T> node)
        {
            if (node == null)
                return node;

            var currentNode = node;

            while (currentNode.HasRightChild())
                currentNode = currentNode.RightChild;

            return currentNode;
        }

        /// <summary>
        /// Finds the next larger node in value compared to the specified node.
        /// Ceiling: smallest key >= a given key.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual BSTreeNode<T> FindNextLargerNode(BSTreeNode<T> node)
        {
            if (node == null)
                return node;
            // if node has right child, just look down to find the minNode, which is the smallest larger one.
            if (node.HasRightChild())
                return FindMinNode(node.RightChild);

            // if node doesn't have right child, we need to look up to find the smallest larger one.
            var currentNode = node;
            while (currentNode._parent != null && currentNode.IsRightChild())
                currentNode = currentNode._parent;

            return currentNode._parent;
        }

        /// <summary>
        /// FInds the next smaller node in value compared to the specified node.
        /// Floor: largest key <= a given key.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual BSTreeNode<T> FindNextSmallerNode(BSTreeNode<T> node)
        {
            if (node == null)
                return node;
            // if node has left child, just look down to find the maxNode, which is the smallest larger one.
            if (node.HasLeftChild())
                return FindMaxNode(node.LeftChild);

            var currentNode = node;
            while (currentNode._parent != null && currentNode.IsLeftChild())
                currentNode = currentNode._parent;

            return currentNode._parent;
        }

        /// <summary>
        /// A recursive private method. Used in the public FindAll(predicate) method.
        /// Implements in-order traversal to find all the matching elements in a subtree.
        /// </summary>
        /// <param name="currentNode">Node to start searching from.</param>
        /// <param name="match"></param>
        /// <param name="list">List to add elements to.</param>
        protected virtual void FindAll(BSTreeNode<T> currentNode, Predicate<T> match, ref LinkedList<T> list)
        {
            if (currentNode == null)
                return;

            // Call the left child.
            FindAll(currentNode.LeftChild, match, ref list);

            if (match(currentNode.Value))
            {
                list.AddLast(currentNode.Value);
            }

            // Call the right child.
            FindAll(currentNode.RightChild, match, ref list);
        }


        /// <summary>
        /// In-order traversal of the subtrees of a node. Returns every node it vists.
        /// </summary>
        /// <param name="currentNode">Node to traverse the tree from.</param>
        /// <param name="list">List to add elements to.</param>
        protected virtual void InOrderTraverse(BSTreeNode<T> currentNode, ref LinkedList<T> list)
        {
            if (currentNode == null)
                return;

            // Call the left child.
            InOrderTraverse(currentNode.LeftChild, ref list);

            // Visit current node.
            list.AddLast(currentNode.Value);

            // Call the right child.
            InOrderTraverse(currentNode.RightChild, ref list);
        }


        #endregion


        #region Class methods
        public virtual void Insert(T item)
        {
            var newNode = new BSTreeNode<T>(item); // Create a new node instance.

            // Insert node recursively starting from the root. Check for success status.
            var success = InsertNode(newNode);

            if (!success && !AllowDuplicates)
                throw new InvalidOperationException("Tree doesn't allow inserting duplicate elements.");
        }

        public virtual void Insert(IList<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException();

            if (collection.Count > 0)
            {
                for (int i = 0; i < collection.Count; i++)
                {
                    this.Insert(collection[i]);
                }
            }
        }

        public virtual void RemoveMin()
        {
            if (IsEmpty)
                throw new Exception("Tree is empty.");

            var node = FindMinNode(Root);
            RemoveNode(node);
        }

        public virtual void RemoveMax()
        {
            if (IsEmpty)
                throw new Exception("Tree is empty.");

            var node = FindMaxNode(Root);
            RemoveNode(node);
        }

        public virtual void Remove(T item)
        {
            if (IsEmpty)
                throw new Exception("Tree is empty.");

            var node = FindNode(Root, item);

            bool status = RemoveNode(node);

            if (!status)
                throw new Exception("Item was not found.");
        }

        public virtual bool Contains(T item)
        {
            return FindNode(Root, item) != null;
        }

        /// <summary>
        /// Finds the item's node in the tree. Throws an exception if not found.
        /// </summary>
        /// <param name="item">Item to find. </param>
        /// <returns>Node of this item. </returns>
        public virtual BSTreeNode<T> GetNode(T item)
        {
            if (IsEmpty)
                throw new Exception("Tree is empty.");

            var node = FindNode(Root, item);
            if (node == null)
                throw new Exception("Item was not found.");

            return node;
        }


        public virtual T FindMin()
        {
            if (IsEmpty)
                throw new Exception("Tree is empty.");

            return FindMinNode(Root).Value;
        }

        public virtual T FindMax()
        {
            if (IsEmpty)
                throw new Exception("Tree is empty.");

            return FindMaxNode(Root).Value;
        }

        /// <summary>
        /// Finds the next smaller element in tree, compared to the specified item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public virtual T FindFloor(T item)
        {
            var node = FindNode(Root, item);
            var nextSmaller = FindNextSmallerNode(node);

            if (nextSmaller == null)
                throw new Exception("Item was not found.");

            return nextSmaller.Value;
        }

        /// <summary>
        /// Finds the next larger element in tree, compared to the specified item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public virtual T FindCeiling(T item)
        {
            var node = FindNode(Root, item);
            var nextLarger = FindNextLargerNode(node);

            if (nextLarger == null)
                throw new Exception("Item was not found.");

            return nextLarger.Value;
        }


        public virtual IEnumerable<T> FindAll(Predicate<T> searchPredicate)
        {
            var list = new LinkedList<T>();
            FindAll(Root, searchPredicate, ref list);

            return list;
        }

        public virtual T[] ToArray()
        {
            var list = new LinkedList<T>();
            InOrderTraverse(Root, ref list);

            return list.ToArray();
        }

        public List<T> ToList()
        {
            var list = new LinkedList<T>();
            InOrderTraverse(Root, ref list);

            return list.ToList();
        }


        public IEnumerator<T> GetEnumerator()
        {
            switch (_traversalMode)
            {
                case TraversalMode.InOrder:
                    return new BinarySearchTreeInOrderEnumerator(this);

                case TraversalMode.PreOrder:
                    return new BinarySearchTreePreOrderEnumerator(this);


                case TraversalMode.PostOrder:
                    return new BinarySearchTreePostOrderEnumerator(this);
            }

            return null;

        }


        public void Clear()
        {
            Root = null;
            Count = 0;
        }

        #endregion


        #region Internal Class for BST enumerator
        /// <summary>
        /// Returns an preorder-traversal enumerator for the tree values.
        /// </summary>
        internal class BinarySearchTreePreOrderEnumerator : IEnumerator<T>
        {
            private BSTreeNode<T> _current;
            private BinarySearchTree<T> _tree;
            private readonly Queue<BSTreeNode<T>> _traverseQueue;


            public BinarySearchTreePreOrderEnumerator(BinarySearchTree<T> tree)
            {
                this._tree = tree;

                // Build queue.
                _traverseQueue = new Queue<BSTreeNode<T>>(tree.Count);
                VisitNode(this._tree.Root);
            }


            private void VisitNode(BSTreeNode<T> node)
            {
                if (node == null)
                    return;

                _traverseQueue.Enqueue(node);
                VisitNode(node.LeftChild);
                VisitNode(node.RightChild);
            }

            public T Current
            {
                get { return _current.Value; }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public void Dispose()
            {
                _current = null;
                _tree = null;
            }

            public bool MoveNext()
            {
                if (_traverseQueue.Count > 0)
                    _current = _traverseQueue.Dequeue();
                else
                    _current = null;

                return _current != null;

            }

            public void Reset()
            {
                _current = null;
            }
        }


        /// <summary>
        /// Returns an inorder-traversal enumerator for the tree values.
        /// </summary>
        internal class BinarySearchTreeInOrderEnumerator : IEnumerator<T>
        {
            private BSTreeNode<T> _current;
            private BinarySearchTree<T> _tree;
            internal Queue<BSTreeNode<T>> _traverseQueue;


            public BinarySearchTreeInOrderEnumerator(BinarySearchTree<T> tree)
            {
                this._tree = tree;

                // Build queue.
                _traverseQueue = new Queue<BSTreeNode<T>>(tree.Count);
                VisitNode(this._tree.Root);
            }


            private void VisitNode(BSTreeNode<T> node)
            {
                if (node == null)
                    return;

                VisitNode(node.LeftChild);
                _traverseQueue.Enqueue(node);
                VisitNode(node.RightChild);
            }

            public T Current
            {
                get { return _current.Value; }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public void Dispose()
            {
                _current = null;
                _tree = null;
            }

            public bool MoveNext()
            {
                if (_traverseQueue.Count > 0)
                    _current = _traverseQueue.Dequeue();
                else
                    _current = null;

                return _current != null;

            }

            public void Reset()
            {
                _current = null;
            }
        }

        /// <summary>
        /// Returns a postorder-traversal enumerator for the tree values.
        /// </summary>
        internal class BinarySearchTreePostOrderEnumerator : IEnumerator<T>
        {
            private BSTreeNode<T> _current;
            private BinarySearchTree<T> _tree;
            internal Queue<BSTreeNode<T>> _traverseQueue;


            public BinarySearchTreePostOrderEnumerator(BinarySearchTree<T> tree)
            {
                this._tree = tree;

                // Build queue.
                _traverseQueue = new Queue<BSTreeNode<T>>(tree.Count);
                VisitNode(this._tree.Root);
            }


            private void VisitNode(BSTreeNode<T> node)
            {
                if (node == null)
                    return;

                VisitNode(node.LeftChild);
                VisitNode(node.RightChild);
                _traverseQueue.Enqueue(node);
            }

            public T Current
            {
                get { return _current.Value; }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public void Dispose()
            {
                _current = null;
                _tree = null;
            }

            public bool MoveNext()
            {
                if (_traverseQueue.Count > 0)
                    _current = _traverseQueue.Dequeue();
                else
                    _current = null;

                return _current != null;

            }

            public void Reset()
            {
                _current = null;
            }
        }
        #endregion
    }
}
