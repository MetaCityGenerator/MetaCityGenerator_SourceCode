/***
 * The Directed Sparse Graph Data Structure.
 * 
 * Definition:
 * A sparse graph is a graph G = (V, E) in which |E| = O(|V|).
 * A directed graph is a graph where each edge follow one direction only between any two vertices.
 * A weighted graph is a graph where each edge has a weight (zero weights mean there is no edge).
 * 
 * An adjacency-list weighted digraph representation. Shares a good deal of implemention details 
 * with the Directed Sparse version (DirectedSparseGraph<T>). Edges are instances of WeightedEdge<T> class. 
 * Implements both interfaces: IGraph<T> and IWeightedGraph<T>.
 */
using System;
using System.Collections.Generic;

using UrbanX.Algorithms.Utility;
using UrbanX.DataStructures.Utility;

namespace UrbanX.DataStructures.Graphs
{
    /// <summary>
    /// The Directed Weighted Sparse Graph Data Structure.
    /// An adjacency-list weighted digraph (directed-graph) representation.
    /// A sparse graph is a graph G = (V, E) in which |E| = O(|V|).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DirectedWeightedSparseGraph<T> : IGraph<T>, IWeightedGraph<T> where T : IComparable<T>
    {
        /// <summary>
        /// INSTANCE VARIABLES
        /// </summary>
        private const double _EMPTY_EDGE_VALUE = 0;
        protected int _edgesCount;
        protected T _firstInsertedNode;
        protected Dictionary<T, LinkedList<WeightedEdge<T>>> _adjacencyList;

        /// <summary>
        /// Constructor of UndirectedSparseGraph (Using adjacencyList)
        /// </summary>
        /// <param name="capacity"></param>
        public DirectedWeightedSparseGraph(int capacity = 10)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("Capacity can't be less than zero.");
            }

            _edgesCount = 0;
            _adjacencyList = new Dictionary<T, LinkedList<WeightedEdge<T>>>(capacity);
        }

        /// <summary>
        /// Helper function. Returns edge object from source to destination, if this edge exists; otherwise return null.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        protected virtual WeightedEdge<T> TryGetEdge(T source, T destination)
        {
            WeightedEdge<T> edge = null;

            var sourceToDestinationPredicate = new Predicate<WeightedEdge<T>>(item => item.Source.IsEqualTo(source) && item.Destination.IsEqualTo(destination));

            if (_adjacencyList.ContainsKey(source))
            {
                _adjacencyList[source].TryFindFirst(sourceToDestinationPredicate, out edge);
            }

            // Could return a null object.
            return edge;
        }

        /// <summary>
        /// Helper function. Checks if edge exist in graph.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        protected virtual bool DoesEdgeExist(T source, T destination)
        {
            return TryGetEdge(source, destination) != null;
        }


        public virtual bool IsDirected
        {
            get { return true; }
        }

        public virtual bool IsWeighted
        {
            get { return true; }
        }


        public virtual int VerticesCount
        {
            get { return _adjacencyList.Count; }
        }

        public virtual int EdgesCount
        {
            get { return _edgesCount; }
        }

        public virtual IEnumerable<T> Vertices
        {
            get
            {
                foreach (var vertex in _adjacencyList)
                {
                    yield return vertex.Key;
                }
            }
        }

        public virtual IEnumerable<IEdge<T>> Edges
        {
            get { return this.GetEdges(); }
        }
        protected IEnumerable<WeightedEdge<T>> GetEdges()
        {
            foreach (var vertex in _adjacencyList)
            {
                foreach (var edge in vertex.Value)
                {
                    yield return edge;
                }
            }//end-foreach
        }

        public virtual IEnumerable<IEdge<T>> IncomingEdges(T vertex)
        {
            return this.GetIncomingEdges(vertex);
        }
        protected IEnumerable<WeightedEdge<T>> GetIncomingEdges(T vertex)
        {
            if (!HasVertex(vertex))
            {
                throw new KeyNotFoundException("Vertex doesn't belong to graph.");
            }

            var predicate = new Predicate<WeightedEdge<T>>(edge => edge.Destination.IsEqualTo(vertex));

            foreach (var adjacent in _adjacencyList)
            {
                if (adjacent.Value.TryFindFirst(predicate, out WeightedEdge<T> incomingEdge))
                {
                    yield return incomingEdge;
                }
            }
        }

        public virtual IEnumerable<IEdge<T>> OutgoingEdges(T vertex)
        {
            return this.GetOutgoingEdges(vertex);
        }
        protected IEnumerable<WeightedEdge<T>> GetOutgoingEdges(T vertex)
        {
            if (!HasVertex(vertex))
            {
                throw new KeyNotFoundException("Vertex doesn't belong to graph.");
            }
            foreach (var edge in _adjacencyList[vertex])
            {
                yield return edge;
            }
        }


        /// <summary>
        /// Obsolete. Another AddEdge function is implemented with a weight parameter.
        /// </summary>
        /// <param name="firstVertex"></param>
        /// <param name="secondVertex"></param>
        /// <returns></returns>
        [Obsolete("Use the AddEdge method with the weight parameter.")]
        public virtual bool AddEdge(T firstVertex, T secondVertex)
        {
            throw new NotImplementedException();
        }
        public virtual bool AddEdge(T source, T destination, double weight)
        {
            // Check existence of nodes, the validity of the weight value, and the non-existence of edge.
            if (weight == _EMPTY_EDGE_VALUE)
            {
                return false;
            }
            if (!HasVertex(source) || !HasVertex(destination))
            {
                return false;
            }
            if (DoesEdgeExist(source, destination))
            {
                return false;
            }

            // Add edge from source to destination.
            var edge = new WeightedEdge<T>(source, destination, weight);
            _adjacencyList[source].AddLast(edge);

            // Increment edges count.
            ++_edgesCount;

            return true;
        }

        public virtual bool RemoveEdge(T source, T destination)
        {
            // Check existence of nodes and non-existence of edge.
            if (!HasVertex(source) || !HasVertex(destination))
            {
                return false;
            }

            // Try get edge.
            var edge = TryGetEdge(source, destination);
            if (edge == null)
            {
                return false;
            }

            // Remove edge from source to destination.
            _adjacencyList[source].Remove(edge);

            // Decrement the edges count.
            --_edgesCount;

            return true;
        }

        public virtual bool UpdateEdgeWeight(T source, T destination, double weight)
        {
            // Check existence of vertices and validity of the weight value.
            if (weight == _EMPTY_EDGE_VALUE)
            {
                return false;
            }
            if (!HasVertex(source) || !HasVertex(destination))
            {
                return false;
            }

            foreach (var edge in _adjacencyList[source])
            {
                if (edge.Destination.IsEqualTo(destination))
                {
                    edge.Weight = weight;

                    return true;
                }
            }

            return false;
        }

        public virtual WeightedEdge<T> GetEdge(T source, T destination)
        {
            if (!HasVertex(source) || !HasVertex(destination))
            {
                throw new KeyNotFoundException("Either one of the vertices or both of them don't exist.");
            }

            var edge = TryGetEdge(source, destination);

            // Check the existence of edge.
            if (edge == null)
            {
                throw new Exception("Edge doesn't exist.");
            }

            // Try get edge.
            return edge;
        }

        public virtual double GetEdgeWeight(T source, T destination)
        {
            return GetEdge(source, destination).Weight;
        }

        public virtual bool AddVertex(T vertex)
        {
            if (_adjacencyList.ContainsKey(vertex))
            {
                return false;
            }
            if (_adjacencyList.Count == 0)
            {
                _firstInsertedNode = vertex;
            }

            _adjacencyList.Add(vertex, new LinkedList<WeightedEdge<T>>());

            return true;
        }

        public virtual void AddVertices(IList<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException();
            }
            foreach (var vertex in collection)
            {
                AddVertex(vertex);
            }
        }

        public virtual bool RemoveVertex(T vertex)
        {
            // Check existence of vertex.
            if (!_adjacencyList.ContainsKey(vertex))
            {
                return false;
            }
            // Subtract the number of edges for this vertex from the total edges count.
            _edgesCount -= _adjacencyList[vertex].Count;

            // Remove vertex from graph.
            _adjacencyList.Remove(vertex);

            // Remove destination edges to this vertex.
            foreach (var adjacent in _adjacencyList)
            {
                var edge = TryGetEdge(adjacent.Key, vertex);

                if (edge != null)
                {
                    adjacent.Value.Remove(edge);
                    --_edgesCount;
                }
            }

            return true;
        }

        public virtual bool HasEdge(T source, T destination)
        {
            return HasVertex(source) && HasVertex(destination) && DoesEdgeExist(source, destination);
        }

        public virtual bool HasVertex(T vertex)
        {
            return _adjacencyList.ContainsKey(vertex);
        }

        public virtual LinkedList<T> Neighbours(T vertex)
        {
            if (!HasVertex(vertex))
            {
                return null;
            }

            var neighbors = new LinkedList<T>();
            var adjacents = _adjacencyList[vertex];

            foreach (var adjacent in adjacents)
            {
                neighbors.AddLast(adjacent.Destination);
            }

            return neighbors;
        }

        public Dictionary<T, double> NeighboursMap(T vertex)
        {
            if (!HasVertex(vertex))
            {
                return null;
            }

            var neighbors = _adjacencyList[vertex];
            var map = new Dictionary<T, double>(neighbors.Count);

            foreach (var adjacent in neighbors)
            {
                map.Add(adjacent.Destination, adjacent.Weight);
            }

            return map;
        }

        public virtual int Degree(T vertex)
        {
            if (!HasVertex(vertex))
            {
                throw new KeyNotFoundException();
            }

            return _adjacencyList[vertex].Count;
        }

        public virtual string ToReadable()
        {
            string output = string.Empty;

            foreach (var node in _adjacencyList)
            {
                var adjacents = string.Empty;

                output = string.Format("{0}\r\n{1}: [", output, node.Key);

                foreach (var adjacentNode in node.Value)
                    adjacents = string.Format("{0}{1}({2}), ", adjacents, adjacentNode.Destination, adjacentNode.Weight);

                if (adjacents.Length > 0)
                    adjacents = adjacents.TrimEnd(new char[] { ',', ' ' });

                output = string.Format("{0}{1}]", output, adjacents);
            }

            return output;
        }

        public virtual IEnumerable<T> BreadthFirstWalk()
        {
            return BreadthFirstWalk(_firstInsertedNode);
        }

        public virtual IEnumerable<T> BreadthFirstWalk(T startingVertex)
        {
            // Check for existence of source.
            if (VerticesCount == 0)
            {
                return new List<T>();
            }
            if (!HasVertex(startingVertex))
            {
                throw new Exception("The specified starting vertex doesn't exist.");
            }

            // Queue of temporary path, the count of stack will increase or decrease with each step.
            var queueOfNodes = new Queue<T>();
            // List of visited nodes. 
            var visitedNodes = new HashSet<T>();
            // List of nodes directly or indirectly connected to the starting node.
            // in a breadth first manner.
            var listOfNodes = new List<T>(VerticesCount);

            visitedNodes.Add(startingVertex);
            queueOfNodes.Enqueue(startingVertex);
            listOfNodes.Add(startingVertex);

            while (queueOfNodes.Count != 0)
            {
                var current = queueOfNodes.Dequeue();

                foreach (var adjacent in Neighbours(current))
                {
                    if (!visitedNodes.Contains(adjacent))
                    {
                        listOfNodes.Add(adjacent);
                        visitedNodes.Add(adjacent);
                        queueOfNodes.Enqueue(adjacent);
                    }
                }
            }
            return listOfNodes;
        }

        public virtual IEnumerable<T> DepthFirstWalk()
        {
            return DepthFirstWalk(_firstInsertedNode);
        }

        public virtual IEnumerable<T> DepthFirstWalk(T startingVertex)
        {
            if (VerticesCount == 0)
            {
                return new List<T>();
            }
            if (!HasVertex(startingVertex))
            {
                throw new Exception("The specified starting vertex doesn't exist.");
            }

            // Stack of temporary path, the count of stack will increase or decrease with each step.
            var stackOfNodes = new Stack<T>(VerticesCount);
            // List of visited nodes. 
            var visitedNodes = new HashSet<T>();
            // List of nodes directly or indirectly connected to the starting node.
            // in a depth first manner.
            var listOfNodes = new List<T>(VerticesCount);

            stackOfNodes.Push(startingVertex);

            while (stackOfNodes.Count != 0)
            {
                var current = stackOfNodes.Pop();

                if (!visitedNodes.Contains(current))
                {
                    listOfNodes.Add(current);
                    visitedNodes.Add(current);

                    foreach (var adjacent in Neighbours(current))
                    {
                        if (!visitedNodes.Contains(adjacent))
                        {
                            stackOfNodes.Push(adjacent);
                        }
                    }
                }
            }
            return listOfNodes;
        }

        public virtual void ClearGraph()
        {
            _edgesCount = 0;
            _adjacencyList.Clear();
        }
    }
}
