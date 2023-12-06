using System;
using System.Collections.Generic;
using System.Linq;

using MetaCity.DataStructures.Trees;

namespace MetaCity.Algorithms.Trees
{
    /// <summary>
    /// For classs and structs who share the same name in Rhino.geometry, we use U for the prefix.
    /// This is a struct with [0,0] as the default value.
    /// </summary>
    public readonly struct UInterval : IEquatable<UInterval>
    {
        public readonly double _low, _high;

        public UInterval(double li, double hi)
        {
            _low = li;
            _high = hi;
        }


        public bool Equals(UInterval other)
        {
            return this._low == other._low && this._high == other._high;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UInterval))
                return false;

            return this.Equals((UInterval)obj);
        }

        public override int GetHashCode()
        {
            return _low.GetHashCode() ^ _high.GetHashCode();
        }

        public bool IsOverlap(UInterval other)
        {
            return this._low <= other._high && other._low <= this._high;
        }

        public override string ToString()
        {
            return $"[{_low},{_high}]";
        }
    }


    /// <summary>
    /// IntervalNode class. Contains interval, id, and max ;This class is used as the T in RBTree<T>.
    /// Because this is a mutable class, intervalnode can be null, and max can be updated.
    /// </summary>
    public class IntervalNode : IComparable<IntervalNode>, IEquatable<IntervalNode>
    {
        public UInterval _i;

        // Id is used for identify the IntervalNode in case sevaral objects has the same Uinterval.
        public readonly int _id;


        /// <summary>
        /// Max will be used for finding the intersection of intervals.
        /// Max will change due to the changement of the position in Tree.
        /// </summary>
        public double Max { get; private set; }


        /// <summary>
        /// Constuctor of IntervalNode. Id should be unique.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="id"></param>
        public IntervalNode(UInterval i, int id)
        {
            _i = i;
            _id = id;
            Max = 0;
        }

        public void ChangeInterval(UInterval i)
        {
            _i = i;
        }

        public void UpdateMax(double max)
        {
            Max = max;
        }

        public bool IsOverlap(IntervalNode node)
        {
            return this._i.IsOverlap(node._i);
        }

        /// <summary>
        /// Comparer will be used in RBTree to sort the order for all the nodes.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(IntervalNode other)
        {
            var c = this._i._low.CompareTo(other._i._low);
            return c == 0 ? this._id.CompareTo(other._id) : c;
        }

        // Max will be consistently changing during the process of insert and delete.
        // There for, property Max will not be considered for equality comparer.
        public bool Equals(IntervalNode other)
        {
            return this._i.Equals(other._i) && this._id == other._id;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is IntervalNode))
                return false;

            return this.Equals((IntervalNode)obj);
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode() ^ _i.GetHashCode();
        }

        public override string ToString()
        {
            return $"Id:{_id} " + _i.ToString() + " " + $"Max:{Max}";
        }
    }


    /// <summary>
    /// Argumented RBTree for finding the interval intersections.
    /// Can operate insert, delete and search overlap methods.
    /// </summary>
    public class IntervalTree
    {
        /// <summary>
        /// Internal Red-Black tree for storing and sorting the intervalNodes.
        /// </summary>
        public readonly RBTree<IntervalNode> _intervalTree;

        public int Count { get { return _intervalTree.Count; } }

        public RBTreeNode<IntervalNode> Root { get { return _intervalTree.Root; } }

        public IntervalTree()
        {
            // This RBTree doesn't allow duplicates. 
            // For each intervalNode, it has unique id. When two intervalNode share a same interval , ID will determine whether they are equal  
            // and also to locate the position in tree .
            _intervalTree = new RBTree<IntervalNode>(false, TraversalMode.InOrder);
        }


        public void InsertNode(IntervalNode node)
        {
            _intervalTree.Insert(node);

            // Update max.
            UpdateTree(Root);
        }

        public void DeleteNode(IntervalNode node)
        {
            _intervalTree.Remove(node);

            UpdateTree(Root);
        }


        /// <summary>
        /// Method for finding all the intersection intervals in tree.
        /// Insert the tree root as parameter.
        /// </summary>
        /// <param name="targetNode"></param>
        /// <param name="rootNode"></param>
        /// <returns></returns>
        public List<IntervalNode> SearchOverlaps(IntervalNode targetNode, RBTreeNode<IntervalNode> rootNode)
        {

            List<IntervalNode> result = new List<IntervalNode>(Count);

            if (rootNode.Value.IsOverlap(targetNode))
                result.Add(rootNode.Value);

            // Go left.
            if (rootNode.LeftChild != null && rootNode.LeftChild.Value.Max >= targetNode._i._low)
            {
                result.AddRange(SearchOverlaps(targetNode, rootNode.LeftChild));
            }
            // Go right.
            if (rootNode.RightChild != null && rootNode.RightChild.Value.Max >= targetNode._i._low)
            {
                result.AddRange(SearchOverlaps(targetNode, rootNode.RightChild));
            }

            result.TrimExcess();
            return result;
        }


        /// <summary>
        /// Find one overlap in tree.
        /// </summary>
        /// <param name="targetNode"></param>
        /// <returns></returns>
        public IntervalNode FindOneOverlap(IntervalNode targetNode)
        {
            var node = Root;
            while (node != null)
            {
                if (node.Value.IsOverlap(targetNode))
                    return node.Value;
                else if (node.LeftChild == null)
                    node = node.RightChild;
                else if (node.LeftChild.Value.Max < targetNode._i._low)
                    node = node.RightChild;
                else
                    node = node.LeftChild;
            }

            return null;
        }


        private double UpdateTree(RBTreeNode<IntervalNode> node)
        {
            if (node == null)
                return double.NegativeInfinity;

            var temp = FindMaxNumber(node.Value._i._high, UpdateTree(node.LeftChild), UpdateTree(node.RightChild));
            node.Value.UpdateMax(temp);

            return temp;
        }


        private double FindMaxNumber(double a, double b, double c)
        {
            double[] numbers = { a, b, c };
            return numbers.Max();
        }
    }
}
