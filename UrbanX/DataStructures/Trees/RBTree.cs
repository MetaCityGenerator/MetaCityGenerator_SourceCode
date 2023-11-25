using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UrbanX.DataStructures.Utility;

namespace UrbanX.DataStructures.Trees
{
    /// <summary>
    /// The node color type.
    /// </summary>
    public enum RBTreeColors
    {
        Red = 0,
        Black = 1
    }

    /// <summary>
    /// Red-Black Tree data structure.
    /// </summary>
    public class RBTree<T> : IDisposable, IBinarySearchTree<T> where T : IComparable<T>
    {
        private readonly TraversalMode _traversalMode;


        #region Constructor
        /// <summary>
        /// Constructor. Allows duplicates by default.; Inorder traversal by default, 
        /// </summary>
        public RBTree() : this(true, TraversalMode.InOrder) { }

        /// <summary>
        /// Constructor. If allowDuplictes is set to false, no duplicate items will be inserted.
        /// Need to choose the traversal mode.
        /// </summary>
        /// <param name="allowDuplicates"></param>
        /// <param name="traversal"></param>
        public RBTree(bool allowDuplicates, TraversalMode traversal)
        {
            Count = 0;
            AllowDuplicates = allowDuplicates;
            Root = null;
            _traversalMode = traversal;
        }

        #endregion

        #region Properties

        ITreeNode<T> IBinarySearchTree<T>.Root => this.Root;

        public RBTreeNode<T> Root { get; private set; }

        public int Count { get; private set; }

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

        public bool AllowDuplicates { get; private set; }



        #endregion


        #region Private methods for constructing BinarySearchTree
        /// <summary>
        /// Replaces the node's value from it's parent node object with the newValue.
        /// Used in the recurvive Remove method.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="newNode"></param>
        protected void ReplaceNodeInParent(RBTreeNode<T> node, RBTreeNode<T> newNode = null)
        {
            if (node.Parent != null)
            {
                if (node.IsLeftChild())
                {
                    node.Parent.LeftChild = newNode;
                }
                else
                {
                    node.Parent.RightChild = newNode;
                }
            }
            else
            {
                Root = newNode;
            }

            if (newNode != null)
            {
                newNode.Parent = node.Parent;
            }
        }



        /// <summary>
        /// Inserts a new node to the tree.
        /// </summary>
        /// <param name="newNode">New node to be inserted.</param>
        /// <returns></returns>
        protected bool InsertNode(RBTreeNode<T> newNode)
        {
            if (this.Root == null)
            {
                Root = newNode;
                Count++;
                return true;  // Root doesn't have parent.
            }

            if (newNode.Parent == null)
            {
                newNode.Parent = this.Root;
            }

            // Check for value equality and whether inserting duplicates is allowed.
            if (AllowDuplicates == false && newNode.Parent.Value.IsEqualTo(newNode.Value))
            {
                return false;
            }

            // Go left.
            if (newNode.Parent.Value.IsGreaterThan(newNode.Value)) // newNode < parent.
            {
                if (!newNode.Parent.HasLeftChild())
                {
                    newNode.Parent.LeftChild = newNode;

                    Count++;

                    return true;
                }

                // Go below the current left child.
                newNode.Parent = newNode.Parent.LeftChild;
                return InsertNode(newNode);
            }

            // Go right.  newNode>= parent.  if_allowDuplicates is true, the duplicate node will locate at the right side.
            else
            {
                if (!newNode.Parent.HasRightChild())
                {
                    newNode.Parent.RightChild = newNode;
                    Count++;

                    return true;
                }

                newNode.Parent = newNode.Parent.RightChild;
                return InsertNode(newNode);
            }
        }


        /// <summary>
        /// Calculates the tree height from a specified node, recursively.
        /// Time complexity: O(n), where n is the count of nodes.
        /// </summary>
        /// <param name="node">Node</param>
        /// <returns> Height of node's longest subtree </returns>
        protected int GetTreeHeight(RBTreeNode<T> node)
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
        protected RBTreeNode<T> FindNode(RBTreeNode<T> currentNode, T item)
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
        protected RBTreeNode<T> FindMinNode(RBTreeNode<T> node)
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
        protected RBTreeNode<T> FindMaxNode(RBTreeNode<T> node)
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
        protected RBTreeNode<T> FindNextLargerNode(RBTreeNode<T> node)
        {
            if (node == null)
                return node;
            // if node has right child, just look down to find the minNode, which is the smallest larger one.
            if (node.HasRightChild())
                return FindMinNode(node.RightChild);

            // if node doesn't have right child, we need to look up to find the smallest larger one.
            var currentNode = node;
            while (currentNode.Parent != null && currentNode.IsRightChild())
                currentNode = currentNode.Parent;

            return currentNode.Parent;
        }

        /// <summary>
        /// FInds the next smaller node in value compared to the specified node.
        /// Floor: largest key <= a given key.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected RBTreeNode<T> FindNextSmallerNode(RBTreeNode<T> node)
        {
            if (node == null)
                return node;
            // if node has left child, just look down to find the maxNode, which is the smallest larger one.
            if (node.HasLeftChild())
                return FindMaxNode(node.LeftChild);

            var currentNode = node;
            while (currentNode.Parent != null && currentNode.IsLeftChild())
                currentNode = currentNode.Parent;

            return currentNode.Parent;
        }

        /// <summary>
        /// A recursive private method. Used in the public FindAll(predicate) method.
        /// Implements in-order traversal to find all the matching elements in a subtree.
        /// </summary>
        /// <param name="currentNode">Node to start searching from.</param>
        /// <param name="match"></param>
        /// <param name="list">List to add elements to.</param>
        protected void FindAll(RBTreeNode<T> currentNode, Predicate<T> match, ref LinkedList<T> list)
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
        protected void InOrderTraverse(RBTreeNode<T> currentNode, ref LinkedList<T> list)
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

        private bool IsRoot(RBTreeNode<T> node)
        {
            return node == this.Root;
        }

        /// <summary>
        /// Remove the specified node.
        /// </summary>
        /// <param name="nodeToDelete"></param>
        /// <returns></returns>
        protected bool RemoveNode(RBTreeNode<T> nodeToDelete)
        {
            if (nodeToDelete == null)
            {
                return false;
            }

            if (IsRoot(nodeToDelete) && !nodeToDelete.HasChildren())
            {
                Root = null;
            }
            else
            {
                // X is the node we will replace with the nodeToDelete in the tree once we remove it.
                RBTreeNode<T> x;

                if (!nodeToDelete.HasChildren())
                {
                    x = nodeToDelete;
                    Transplant(nodeToDelete, null);
                }
                else if (nodeToDelete.HasOnlyRightChild())
                {
                    x = nodeToDelete.RightChild;
                    Transplant(nodeToDelete, nodeToDelete.RightChild);
                }
                else if (nodeToDelete.HasOnlyLeftChild())
                {
                    x = nodeToDelete.LeftChild;
                    Transplant(nodeToDelete, nodeToDelete.LeftChild);
                }
                else
                {
                    // Y is the node we will replace with the X in the tree once we move it to the nodeToDelete position.
                    var y = FindMinNode(nodeToDelete.RightChild);
                    x = y.RightChild;

                    if (y.Parent == nodeToDelete)
                    {
                        if (x != null)
                        {
                            x.Parent = y;
                        }
                    }
                    else
                    {
                        Transplant(y, y.RightChild);
                        y.RightChild = nodeToDelete.RightChild;
                        y.RightChild.Parent = y;
                    }

                    Transplant(nodeToDelete, y);
                    y.LeftChild = nodeToDelete.LeftChild;
                    y.LeftChild.Parent = y;
                    y.Color = nodeToDelete.Color;

                    if (Root == nodeToDelete)
                    {
                        Root = y;
                        Root.Parent = null;
                    }
                }

                if (nodeToDelete.Color == RBTreeColors.Black)
                {
                    AdjustTreeAfterRemoval(x);
                }
            }

            Count--;

            return true;
        }

        /// <summary>
        ///     Insert one subtree in the place of the other in his parent.
        /// </summary>
        /// <param name="replaced">Subtree of node will be replaced by <param name="replacement">.</param></param>
        /// <param name="replacement">Subtree replaces <param name="replaced">.</param></param>
        private void Transplant(RBTreeNode<T> replaced, RBTreeNode<T> replacement)
        {
            if (replaced.Parent == null)
            {
                this.Root = replacement;
            }
            else if (replaced == replaced.Parent.LeftChild)
            {
                replaced.Parent.LeftChild = replacement;
            }
            else
            {
                replaced.Parent.RightChild = replacement;
            }

            if (replacement != null)
            {
                replacement.Parent = replaced.Parent;
            }
        }

        #endregion


        #region Helper methods for safely checking, getting and setting.
        /*************************************************************************************************/
        /***
         * Safety Checks/Getters/Setters.
         * 
         * The following are helper methods for safely checking, getting and updating possibly-null objects.
         * These helpers make the algorithms of adjusting the tree after insertion and removal more readable.
         */

        protected RBTreeNode<T> SafeGetGrandParent(RBTreeNode<T> node)
        {
            if (node == null || node.Parent == null)
                return null;

            return node.GetGrandParent();
        }

        protected RBTreeNode<T> SafeGetParent(RBTreeNode<T> node)
        {
            if (node == null || node.Parent == null)
                return null;

            return node.Parent;
        }

        protected RBTreeNode<T> SafeGetSibling(RBTreeNode<T> node)
        {
            if (node == null || node.Parent == null)
                return null;

            return node.GetSibling();
        }

        protected RBTreeNode<T> SafeGetLeftChild(RBTreeNode<T> node)
        {
            if (node == null)
                return null;

            return node.LeftChild;
        }

        protected RBTreeNode<T> SafeGetRightChild(RBTreeNode<T> node)
        {
            if (node == null)
                return null;

            return node.RightChild;
        }

        protected RBTreeColors SafeGetColor(RBTreeNode<T> node)
        {
            if (node == null)
                return RBTreeColors.Black;

            return node.Color;
        }

        protected void SafeUpdateColor(RBTreeNode<T> node, RBTreeColors color)
        {
            if (node == null)
                return;

            node.Color = color;
        }

        protected bool SafeCheckIsBlack(RBTreeNode<T> node)
        {
            return node == null || (node != null && node.IsBlack());
        }

        protected bool SafeCheckIsRed(RBTreeNode<T> node)
        {
            return node != null && node.IsRed();
        }

        #endregion



        #region Private methods for Rotations and Ajustments.
        /*************************************************************************************************/
        /***
         * Tree Rotations and Adjustements.
         * 
         * The following are methods for rotating the tree (left/right) and for adjusting the 
         * ... tree after inserting or removing nodes.
         */

        /// <summary>
        /// Rotates a node to the left in the Red-Black Tree.
        /// </summary>
        protected void RotateLeftAt(RBTreeNode<T> currentNode)
        {
            // We check the right child because it's going to be a pivot node for the rotation
            if (currentNode == null || currentNode.HasRightChild() == false)
                return;

            // Pivot on *right* child
            RBTreeNode<T> pivotNode = currentNode.RightChild;

            // Parent of currentNode
            RBTreeNode<T> parent = currentNode.Parent;

            // Check if currentNode is it's parent's left child.
            bool isLeftChild = currentNode.IsLeftChild();

            // Check if currentNode is the Root
            bool isRootNode = (currentNode == this.Root);

            // Perform the rotation
            currentNode.RightChild = pivotNode.LeftChild;
            pivotNode.LeftChild = currentNode;

            // Update parents references
            currentNode.Parent = pivotNode;
            pivotNode.Parent = parent;

            if (currentNode.HasRightChild())
                currentNode.RightChild.Parent = currentNode;

            //Update the entire tree's Root if necessary
            if (isRootNode)
                this.Root = pivotNode;

            // Update the original parent's child node
            if (isLeftChild)
                parent.LeftChild = pivotNode;
            else if (parent != null)
                parent.RightChild = pivotNode;
        }

        /// <summary>
        /// Rotates a node to the right in the Red-Black Tree.
        /// </summary>
        protected void RotateRightAt(RBTreeNode<T> currentNode)
        {
            // We check the right child because it's going to be a pivot node for the rotation
            if (currentNode == null || currentNode.HasLeftChild() == false)
                return;

            // Pivot on *left* child
            var pivotNode = currentNode.LeftChild;

            // Parent of currentNode
            var parent = currentNode.Parent;

            // Check if currentNode is it's parent's left child.
            bool isLeftChild = currentNode.IsLeftChild();

            // Check if currentNode is the Root
            bool isRootNode = (currentNode == this.Root);

            // Perform the rotation
            currentNode.LeftChild = pivotNode.RightChild;
            pivotNode.RightChild = currentNode;

            // Update parents references
            currentNode.Parent = pivotNode;
            pivotNode.Parent = parent;

            if (currentNode.HasLeftChild())
                currentNode.LeftChild.Parent = currentNode;

            // Update the entire tree's Root if necessary
            if (isRootNode)
                this.Root = pivotNode;

            // Update the original parent's child node
            if (isLeftChild)
                parent.LeftChild = pivotNode;
            else if (parent != null)
                parent.RightChild = pivotNode;
        }

        /// <summary>
        /// After insertion tree-adjustement helper.
        /// </summary>
        protected void AdjustTreeAfterInsertion(RBTreeNode<T> currentNode)
        {
            //
            // STEP 1:
            // Color the currentNode as red
            SafeUpdateColor(currentNode, RBTreeColors.Red);

            //
            // STEP 2:
            // Fix the double red-consecutive-nodes problems, if there exists any.
            if (currentNode != null && currentNode != Root && SafeCheckIsRed(SafeGetParent(currentNode)))
            {
                //
                // STEP 2.A:
                // This is the simplest step: Basically recolor, and bubble up to see if more work is needed.
                if (SafeCheckIsRed(SafeGetSibling(currentNode.Parent)))
                {
                    // If it has a sibling and it is black, then then it has a parent
                    currentNode.Parent.Color = RBTreeColors.Black;

                    // Color sibling of parent as black
                    SafeUpdateColor(SafeGetSibling(currentNode.Parent), RBTreeColors.Black);

                    // Color grandparent as red
                    SafeUpdateColor(SafeGetGrandParent(currentNode), RBTreeColors.Red);

                    // Adjust on the grandparent of currentNode
                    AdjustTreeAfterInsertion(SafeGetGrandParent(currentNode));
                }

                //
                // STEP 2.B:
                // Restructure the tree if the parent of currentNode is a left child to the grandparent of currentNode
                // (parent is a left child to its own parent).
                // If currentNode is also a left child, then do a single right rotation; otherwise, a left-right rotation.
                //
                // using the safe methods to check: currentNode.Parent.IsLeftChild == true
                else if (SafeGetParent(currentNode) == SafeGetLeftChild(SafeGetGrandParent(currentNode)))
                {
                    if (currentNode.IsRightChild())
                    {
                        currentNode = SafeGetParent(currentNode);
                        RotateLeftAt(currentNode);
                    }

                    // Color parent as black
                    SafeUpdateColor(SafeGetParent(currentNode), RBTreeColors.Black);

                    // Color grandparent as red
                    SafeUpdateColor(SafeGetGrandParent(currentNode), RBTreeColors.Red);

                    // Right Rotate tree around the currentNode's grand parent
                    RotateRightAt(SafeGetGrandParent(currentNode));
                }

                //
                // STEP 2.C: 
                // Restructure the tree if the parent of currentNode is a right child to the grandparent of currentNode
                // (parent is a right child to its own parent).
                // If currentNode is a right-child in it's parent, then do a single left rotation; otherwise a right-left rotation.
                //
                // using the safe methods to check: currentNode.Parent.IsRightChild == true
                else if (SafeGetParent(currentNode) == SafeGetRightChild(SafeGetGrandParent(currentNode)))
                {
                    if (currentNode.IsLeftChild())
                    {
                        currentNode = SafeGetParent(currentNode);
                        RotateRightAt(currentNode);
                    }

                    // Color parent as black
                    SafeUpdateColor(SafeGetParent(currentNode), RBTreeColors.Black);

                    // Color grandparent as red
                    SafeUpdateColor(SafeGetGrandParent(currentNode), RBTreeColors.Red);

                    // Left Rotate tree around the currentNode's grand parent
                    RotateLeftAt(SafeGetGrandParent(currentNode));
                }
            }

            // STEP 3:
            // Color the root node as black
            SafeUpdateColor(Root, RBTreeColors.Black);
        }

        /// <summary>
        /// After removal tree-adjustement helper.
        /// </summary>
        protected void AdjustTreeAfterRemoval(RBTreeNode<T> currentNode)
        {
            while (currentNode != null && currentNode != Root && currentNode.IsBlack())
            {
                if (currentNode.IsLeftChild())
                {
                    // Get sibling of currentNode
                    // Safe equivalent of currentNode.Sibling or currentNode.Parent.RightChild
                    var sibling = SafeGetRightChild(SafeGetParent(currentNode));

                    // Safely check sibling.IsRed property
                    if (SafeCheckIsRed(sibling))
                    {
                        // Color currentNode.Sibling as black
                        SafeUpdateColor(sibling, RBTreeColors.Black);

                        // Color currentNode.Parent as red
                        SafeUpdateColor(SafeGetParent(currentNode), RBTreeColors.Red);

                        // Left Rotate on currentNode's parent
                        RotateLeftAt(SafeGetParent(currentNode));

                        // Update sibling reference
                        // Might end be being set to null
                        sibling = SafeGetRightChild(SafeGetParent(currentNode));
                    }

                    // Check if the left and right children of the sibling node are black
                    // Use the safe methods to check for: (sibling.LeftChild.IsBlack && sibling.RightChild.IsBlack)
                    if (SafeCheckIsBlack(SafeGetLeftChild(sibling)) && SafeCheckIsBlack(SafeGetRightChild(sibling)))
                    {
                        // Color currentNode.Sibling as red
                        SafeUpdateColor(sibling, RBTreeColors.Red);

                        // Assign currentNode.Parent to currentNode 
                        currentNode = SafeGetParent(currentNode);
                    }
                    else
                    {
                        if (SafeCheckIsBlack(SafeGetRightChild(sibling)))
                        {
                            // Color currentNode.Sibling.LeftChild as black
                            SafeUpdateColor(SafeGetLeftChild(sibling), RBTreeColors.Black);

                            // Color currentNode.Sibling as red
                            SafeUpdateColor(sibling, RBTreeColors.Red);

                            // Right Rotate on sibling
                            RotateRightAt(sibling);

                            // Update sibling reference
                            // Might end be being set to null
                            sibling = SafeGetRightChild(SafeGetParent(currentNode));
                        }

                        // Color the Sibling node as currentNode.Parent.Color
                        SafeUpdateColor(sibling, SafeGetColor(SafeGetParent(currentNode)));

                        // Color currentNode.Parent as black
                        SafeUpdateColor(SafeGetParent(currentNode), RBTreeColors.Black);

                        // Color Sibling.RightChild as black
                        SafeUpdateColor(SafeGetRightChild(sibling), RBTreeColors.Black);

                        // Rotate on currentNode's parent
                        RotateLeftAt(SafeGetParent(currentNode));

                        currentNode = Root;
                    }
                }
                else
                {
                    // Get sibling of currentNode
                    // Safe equivalent of currentNode.Sibling or currentNode.Parent.LeftChild
                    var sibling = SafeGetLeftChild(SafeGetParent(currentNode));

                    if (SafeCheckIsRed(sibling))
                    {
                        // Color currentNode.Sibling as black
                        SafeUpdateColor(sibling, RBTreeColors.Black);

                        // Color currentNode.Parent as red
                        SafeUpdateColor(SafeGetParent(currentNode), RBTreeColors.Red);

                        // Right Rotate tree around the parent of currentNode
                        RotateRightAt(SafeGetParent(currentNode));

                        // Update sibling reference
                        // Might end be being set to null
                        sibling = SafeGetLeftChild(SafeGetParent(currentNode));
                    }

                    // Check if the left and right children of the sibling node are black
                    // Use the safe methods to check for: (sibling.LeftChild.IsBlack && sibling.RightChild.IsBlack)
                    if (SafeCheckIsBlack(SafeGetLeftChild(sibling)) && SafeCheckIsBlack(SafeGetRightChild(sibling)))
                    {
                        SafeUpdateColor(sibling, RBTreeColors.Red);

                        // Assign currentNode.Parent to currentNode 
                        currentNode = SafeGetParent(currentNode);
                    }
                    else
                    {
                        // Check if sibling.LeftChild.IsBlack == true
                        if (SafeCheckIsBlack(SafeGetLeftChild(sibling)))
                        {
                            // Color currentNode.Sibling.RightChild as black
                            SafeUpdateColor(SafeGetRightChild(sibling), RBTreeColors.Black);

                            // Color currentNode.Sibling as red
                            SafeUpdateColor(sibling, RBTreeColors.Red);

                            // Left rotate on sibling
                            RotateLeftAt(sibling);

                            // Update sibling reference
                            // Might end be being set to null
                            sibling = SafeGetLeftChild(SafeGetParent(currentNode));
                        }

                        // Color the Sibling node as currentNode.Parent.Color
                        SafeUpdateColor(sibling, SafeGetColor(SafeGetParent(currentNode)));

                        // Color currentNode.Parent as black
                        SafeUpdateColor(SafeGetParent(currentNode), RBTreeColors.Black);

                        // Color Sibling.RightChild as black
                        SafeUpdateColor(SafeGetLeftChild(sibling), RBTreeColors.Black);

                        // Right rotate on the parent of currentNode
                        RotateRightAt(SafeGetParent(currentNode));

                        currentNode = Root;
                    }
                }
            }

            // Color currentNode as black
            SafeUpdateColor(currentNode, RBTreeColors.Black);
        }


        /*************************************************************************************************/

        #endregion

        public void Insert(T item)
        {
            var newNode = new RBTreeNode<T>(item);

            var success = InsertNode(newNode);

            if (!success && !AllowDuplicates)
                throw new InvalidOperationException("Tree doesn't allow inserting duplicate elements.");

            // Adjust Red-Black Tree rules.
            if (!newNode.IsEqualTo(Root))
                if (newNode.Parent.Color != RBTreeColors.Black)
                    AdjustTreeAfterInsertion(newNode);

            // Always color root as black.
            Root.Color = RBTreeColors.Black;
        }

        public void Insert(IList<T> collection)
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

        public void RemoveMin()
        {
            if (IsEmpty)
                throw new Exception("Tree is empty.");

            var node = FindMinNode(Root);
            RemoveNode(node);
        }

        public void RemoveMax()
        {
            if (IsEmpty)
                throw new Exception("Tree is empty.");

            var node = FindMaxNode(Root);
            RemoveNode(node);
        }

        public void Remove(T item)
        {
            if (IsEmpty)
                throw new Exception("Tree is empty.");

            var node = FindNode(Root, item);

            bool status = RemoveNode(node);

            if (!status)
                throw new Exception("Item was not found.");
        }

        public bool Contains(T item)
        {
            return FindNode(Root, item) != null;
        }

        /// <summary>
        /// Finds the item's node in the tree. Throws an exception if not found.
        /// </summary>
        /// <param name="item">Item to find. </param>
        /// <returns>Node of this item. </returns>
        public RBTreeNode<T> GetNode(T item)
        {
            if (IsEmpty)
                throw new Exception("Tree is empty.");

            var node = FindNode(Root, item);
            if (node == null)
                throw new Exception("Item was not found.");

            return node;
        }


        public T FindMin()
        {
            if (IsEmpty)
                throw new Exception("Tree is empty.");

            return FindMinNode(Root).Value;
        }

        public T FindMax()
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
        public RBTreeNode<T> FindFloor(RBTreeNode<T> node)
        {
            //var node = FindNode(Root, item);
            var nextSmaller = FindNextSmallerNode(node);

            if (nextSmaller == null)
                //throw new Exception("Item was not found.");
                return null;


            return nextSmaller;
        }

        public RBTreeNode<T> FindFloor(T item)
        {
            var node = FindNode(Root, item);
            var nextSmaller = FindNextSmallerNode(node);

            if (nextSmaller == null)
                //throw new Exception("Item was not found.");
                return null;

            return nextSmaller;
        }

        /// <summary>
        /// Finds the next larger element in tree, compared to the specified item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public RBTreeNode<T> FindCeiling(RBTreeNode<T> node)
        {
            //var node = FindNode(Root, item);
            var nextLarger = FindNextLargerNode(node);

            if (nextLarger == null)
                //throw new Exception("Item was not found.");
                return null;

            return nextLarger;
        }

        public RBTreeNode<T> FindCeiling(T item)
        {
            var node = FindNode(Root, item);
            var nextLarger = FindNextLargerNode(node);

            if (nextLarger == null)
                //throw new Exception("Item was not found.");
                return null;

            return nextLarger;
        }



        public IEnumerable<T> FindAll(Predicate<T> searchPredicate)
        {
            var list = new LinkedList<T>();
            FindAll(Root, searchPredicate, ref list);

            return list;
        }


        public T[] ToArray()
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
                    return new RBTreeInOrderEnumerator(this);

                case TraversalMode.PreOrder:
                    return new RBTreePreOrderEnumerator(this);


                case TraversalMode.PostOrder:
                    return new RBTreePostOrderEnumerator(this);
            }

            return null;
        }


        public void Clear()
        {
            Root = null;
            Count = 0;
        }

        public void Dispose()
        {
            Clear();
        }


        #region Internal Class for BRT enumerator
        /// <summary>
        /// Returns an preorder-traversal enumerator for the tree values.
        /// </summary>
        internal class RBTreePreOrderEnumerator : IEnumerator<T>
        {
            private RBTreeNode<T> _current;
            private RBTree<T> _tree;
            private readonly Queue<RBTreeNode<T>> _traverseQueue;


            public RBTreePreOrderEnumerator(RBTree<T> tree)
            {
                this._tree = tree;

                // Build queue.
                _traverseQueue = new Queue<RBTreeNode<T>>(tree.Count);
                VisitNode(this._tree.Root);
            }


            private void VisitNode(RBTreeNode<T> node)
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
        internal class RBTreeInOrderEnumerator : IEnumerator<T>
        {
            private RBTreeNode<T> _current;
            private RBTree<T> _tree;
            internal Queue<RBTreeNode<T>> _traverseQueue;


            public RBTreeInOrderEnumerator(RBTree<T> tree)
            {
                this._tree = tree;

                // Build queue.
                _traverseQueue = new Queue<RBTreeNode<T>>(tree.Count);
                VisitNode(this._tree.Root);
            }

            // This method is actually quite useful.
            private void VisitNode(RBTreeNode<T> node)
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
        internal class RBTreePostOrderEnumerator : IEnumerator<T>
        {
            private RBTreeNode<T> _current;
            private RBTree<T> _tree;
            internal Queue<RBTreeNode<T>> _traverseQueue;


            public RBTreePostOrderEnumerator(RBTree<T> tree)
            {
                this._tree = tree;

                // Build queue.
                _traverseQueue = new Queue<RBTreeNode<T>>(tree.Count);
                VisitNode(this._tree.Root);
            }


            private void VisitNode(RBTreeNode<T> node)
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
