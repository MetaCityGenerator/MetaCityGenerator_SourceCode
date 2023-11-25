using System;
using System.Collections.Generic;
using System.Linq;

using UrbanX.Algorithms.Utility;
using UrbanX.DataStructures.Heaps;
using UrbanX.DataStructures.Utility;

namespace UrbanX.DataStructures.Trees
{
    public class BSPTreeNode : ITreeNode<double>, IComparable<BSPTreeNode>
    {

        /// <summary>
        /// Node key to store the index storing the order of inital list.
        /// </summary>
        public int Key { get; }
        public double Value { get; }

        public double Priority { get; }

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

        public BSPTreeNode Parent { get; internal set; }

        public BSPTreeNode LeftChild { get; internal set; }

        public BSPTreeNode RightChild { get; internal set; }


        ITreeNode<double> ITreeNode<double>.Parent => this.Parent;

        ITreeNode<double> ITreeNode<double>.LeftChild => this.LeftChild;

        ITreeNode<double> ITreeNode<double>.RightChild => this.RightChild;


        public BSPTreeNode() : this(-1, default, default, null, null, null) { }
        public BSPTreeNode(double value) : this(-1, value, 0, null, null, null) { }

        public BSPTreeNode(double value, double priority) : this(-1, value, priority, null, null, null) { }

        /// <summary>
        /// Constructor for leafnodes.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="priority"></param>
        public BSPTreeNode(int key, double value, double priority) : this(key, value, priority, null, null, null) { }
        /// <summary>
        /// Constructor of BSTNode.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="parent"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public BSPTreeNode(int key, double value, double priority, BSPTreeNode parent, BSPTreeNode left, BSPTreeNode right)
        {
            Key = key;
            Value = value;
            Parent = parent;
            LeftChild = left;
            RightChild = right;
            Priority = priority;
        }

        public int CompareTo(ITreeNode<double> other)
        {
            return this.Value.CompareTo(other.Value);
        }

        public bool HasLeftChild()
        {
            return this.LeftChild != null;
        }

        public bool HasOnlyLeftChild()
        {
            return !this.HasRightChild() && this.HasLeftChild();
        }

        public bool HasOnlyRightChild()
        {
            return !this.HasLeftChild() && this.HasRightChild();
        }

        public bool HasRightChild()
        {
            return this.RightChild != null;
        }

        public bool IsLeafNode()
        {
            return this.ChildrenCount == 0;
        }

        public bool IsLeftChild()
        {
            return this.Parent != null && this.Parent.LeftChild == this;
        }

        public bool IsRightChild()
        {
            return this.Parent != null && this.Parent.RightChild == this;
        }

        public override string ToString()
        {
            return $"Node:{Value} ({Priority})";
        }

        public bool HasChildren()
        {
            return this.ChildrenCount > 0;
        }

        /// <summary>
        /// Using value for sorting.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(BSPTreeNode other)
        {
            var c = this.Value.CompareTo(other.Value);
            return c == 0 ? this.Priority.CompareTo(other.Priority) : c;
        }
    }


    /// <summary>
    /// This is the Binary space partition tree.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BSPTree : IBinarySearchTree<double>
    {
        public BSPTreeNode Root { get; private set; }

        ITreeNode<double> IBinarySearchTree<double>.Root => this.Root;

        public int Count { get { return _leafNodesHeap.Count; } }

        public bool IsEmpty { get { return Count == 0; } }

        public bool AllowDuplicates { get { return true; } }

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

        private readonly BinaryMaxHeap<BSPTreeNode> _leafNodesHeap;

        #region Constructors

        public BSPTree()
        {
            Root = null;
            _leafNodesHeap = new BinaryMaxHeap<BSPTreeNode>();
        }

        public BSPTree(BSPTreeNode[] nodes)
        {
            Root = null;
            _leafNodesHeap = new BinaryMaxHeap<BSPTreeNode>(nodes.Length);

            for (int i = 0; i < nodes.Length; i++)
            {
                _leafNodesHeap.Add(nodes[i]);
            }

            BuildTree();
        }

        #endregion



        #region Method for contruct tree.
        private void BuildTree()
        {
            // Deep copy leaf nodes.
            BinaryMaxHeap<BSPTreeNode> layerNodes = new BinaryMaxHeap<BSPTreeNode>(_leafNodesHeap.Count);
            foreach (var node in _leafNodesHeap.ToArray())
            {
                layerNodes.Add(node);
            }

            while (layerNodes.Count > 1)
            {
                GenerateParent(layerNodes);
            }

            if (layerNodes.Count == 1)
            {
                this.Root = layerNodes.ExtractMax();
            }
        }

        private void GenerateParent(BinaryMaxHeap<BSPTreeNode> currentLayer)
        {

            BinaryMaxHeap<BSPTreeNode> leafNodes = new BinaryMaxHeap<BSPTreeNode>(currentLayer.Count);
            foreach (var node in currentLayer.ToArray())
            {
                leafNodes.Add(node);
            }
            currentLayer.Clear();

            if (leafNodes.Count % 2 == 1)
            {
                // count is odd. Need to extract the largest node.
                var biggest = leafNodes.ExtractMax();
                var parentSelf = new BSPTreeNode(biggest.Value, biggest.Priority)
                {
                    // only has left child.
                    LeftChild = biggest,
                };

                biggest.Parent = parentSelf;

                currentLayer.Add(parentSelf);

                AddParents(leafNodes, currentLayer);
            }
            else
            {
                AddParents(leafNodes, currentLayer);
            }
        }

        private void AddParents(BinaryMaxHeap<BSPTreeNode> currentLayer, BinaryMaxHeap<BSPTreeNode> upperLayer)
        {
            // currentlayer should be random.
            var restNodes = currentLayer.ToArray();
            restNodes.Shuffle();
            var nodesPairs = CoulpeItems(restNodes);

            foreach (var pairs in nodesPairs)
            {
                // Left child should have larger priority. Special case: leftChild.priority == rightChild.priority.
                BSPTreeNode leftChild, rightChild;

                if (pairs[0].Priority == pairs[1].Priority)
                {
                    leftChild = pairs[0];
                    rightChild = pairs[1];
                }
                else
                {
                    // Using priority for getting left child.
                    List<double> priorities = new List<double>(2);
                    for (int i = 0; i < pairs.Length; i++)
                    {
                        priorities.Add(pairs[i].Priority);
                    }
                    var maxId = priorities.IndexOf(priorities.Max());
                    var minId = priorities.IndexOf(priorities.Min());

                    // Left child should have higher priority.
                    leftChild = pairs[maxId];
                    rightChild = pairs[minId];
                }

                var parentValue = leftChild.Value + rightChild.Value;
                var parentPriority = leftChild.Priority + rightChild.Priority;
                var parentNode = new BSPTreeNode(parentValue, parentPriority);
                upperLayer.Add(parentNode);

                FindFamilies(parentNode, leftChild, rightChild);
            }
        }

        private void FindFamilies(BSPTreeNode parent, BSPTreeNode left, BSPTreeNode right)
        {
            left.Parent = parent;
            right.Parent = parent;
            parent.LeftChild = left;
            parent.RightChild = right;
        }

        private BSPTreeNode[][] CoulpeItems(BSPTreeNode[] input)
        {
            int size = 2;
            var result = input.Select((x, i) => new { Key = i / size, Value = x }).GroupBy(x => x.Key, x => x.Value, (k, g) => g.ToArray()).ToArray();
            return result;
        }


        /// <summary>
        /// Calculates the tree height from a specified node, recursively.
        /// Time complexity: O(n), where n is the count of nodes.
        /// </summary>
        /// <param name="node">Node</param>
        /// <returns> Height of node's longest subtree </returns>
        protected int GetTreeHeight(BSPTreeNode node)
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


        public void Insert(double item)
        {
            BSPTreeNode node = new BSPTreeNode(item);
            _leafNodesHeap.Add(node);

            BuildTree();
        }

        public void Insert(IList<double> collection)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                _leafNodesHeap.Add(new BSPTreeNode(collection[i]));
            }

            BuildTree();
        }

        public void InsertNode(BSPTreeNode node)
        {
            _leafNodesHeap.Add(node);
            BuildTree();
        }

        public void InsertNode(BSPTreeNode[] nodes)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                _leafNodesHeap.Add(nodes[i]);
            }
            BuildTree();
        }

        public void RemoveMin()
        {
            var temp = _leafNodesHeap.ToMinHeap();
            temp.RemoveMin();
            _leafNodesHeap.Clear();
            for (int i = 0; i < temp.Count; i++)
            {
                _leafNodesHeap.Add(temp.ToArray()[i]);
            }

            BuildTree();
        }

        public void RemoveMax()
        {
            _leafNodesHeap.RemoveMax();
            BuildTree();
        }

        public void Remove(double item)
        {
            throw new NotImplementedException();
        }

        public bool Contains(double item)
        {
            throw new NotImplementedException();
        }

        public double FindMin()
        {
            return _leafNodesHeap.ToMinHeap().ExtractMin().Value;
        }

        public double FindMax()
        {
            return _leafNodesHeap.ExtractMax().Value;
        }

        public IEnumerable<double> FindAll(Predicate<double> searchPredicate)
        {
            throw new NotImplementedException();
        }

        public double[] ToArray()
        {
            throw new NotImplementedException();
        }

        public List<double> ToList()
        {
            throw new NotImplementedException();
        }

        public IEnumerator<double> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            _leafNodesHeap.Clear();
            Root = null;
        }


        public override string ToString()
        {
            return this.DrawTree();
        }
        #endregion
    }
}
