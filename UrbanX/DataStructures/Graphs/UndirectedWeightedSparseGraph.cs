/***
 * The Undirected Sparse Graph Data Structure.
 * 
 * Definition:
 * A sparse graph is a graph G = (V, E) in which |E| = O(|V|).
 * A weighted graph is a graph where each edge has a weight (zero weights mean there is no edge).
 * 
 * An adjacency-list weighted graph representation. Shares a good deal of implemention details 
 * with the Undirected Sparse version (UndirectedSparseGraph<T>). Edges are instances of WeightedEdge<T> class. 
 * Implements both interfaces: IGraph<T> and IWeightedGraph<T>.
 */

using System;
using System.Collections.Generic;
using System.Linq;

using MetaCity.Algorithms.Utility;
using MetaCity.DataStructures.Utility;

namespace MetaCity.DataStructures.Graphs
{
    public class UndirectedWeightedSparseGraph<T> : IDisposable, IGraph<T>, IWeightedGraph<T> where T : IComparable<T>
    {
        /// <summary>
        /// INSTANCE VARIABLES
        /// </summary>
        private const double _EMPTY_EDGE_VALUE = 0;
        protected int _edgesCount;
        protected T _firstInsertedNode;
        protected Dictionary<T, LinkedList<WeightedEdge<T>>> _adjacencyList;



        /// <summary>
        /// Constructor of UndirectedSparseGraph. (Using adjacencyList)
        /// </summary>
        public UndirectedWeightedSparseGraph() : this(10) { }

        /// <summary>
        /// Constructor of UndirectedSparseGraph with initial capacity. (Using adjacencyList)
        /// </summary>
        /// <param name="capacity"></param>
        public UndirectedWeightedSparseGraph(int capacity)
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
        protected WeightedEdge<T> TryGetEdge(T source, T destination)
        {
            var success = false;
            WeightedEdge<T> edge = null;

            var sourceToDestinationPredicate = new Predicate<WeightedEdge<T>>(item => item.Source.IsEqualTo(source) && item.Destination.IsEqualTo(destination));
            var destinationToSourcePredicate = new Predicate<WeightedEdge<T>>(item => item.Source.IsEqualTo(destination) && item.Destination.IsEqualTo(source));
            if (_adjacencyList.ContainsKey(source))
            {
                success = _adjacencyList[source].TryFindFirst(sourceToDestinationPredicate, out edge);
            }
            if (!success && _adjacencyList.ContainsKey(destination))
            {
                _adjacencyList[destination].TryFindFirst(destinationToSourcePredicate, out edge);
                // Leave with success is false.
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
        protected bool DoesEdgeExist(T source, T destination)
        {
            return TryGetEdge(source, destination) != null;
        }


        public bool IsDirected
        {
            get { return false; }
        }

        public bool IsWeighted
        {
            get { return true; }
        }


        public int VerticesCount
        {
            get { return _adjacencyList.Count; }
        }

        public int EdgesCount
        {
            get { return _edgesCount; }
        }

        public IEnumerable<T> Vertices
        {
            get
            {
                foreach (var vertex in _adjacencyList)
                {
                    yield return vertex.Key;
                }
            }
        }

        public IEnumerable<IEdge<T>> Edges
        {
            get { return this.GetEdges(); }
        }
        protected IEnumerable<WeightedEdge<T>> GetEdges()
        {
            var seen = new HashSet<KeyValuePair<T, T>>();

            foreach (var vertex in _adjacencyList)
            {
                foreach (var edge in vertex.Value)
                {
                    var incomingEdge = new KeyValuePair<T, T>(edge.Destination, edge.Source);
                    var outgoingEdge = new KeyValuePair<T, T>(edge.Source, edge.Destination);

                    if (seen.Contains(incomingEdge) || seen.Contains(outgoingEdge))
                    {
                        continue;
                    }
                    seen.Add(outgoingEdge);

                    yield return edge;
                }
            }//end-foreach
        }

        public IEnumerable<IEdge<T>> IncomingEdges(T vertex)
        {
            return this.GetIncomingEdges(vertex);
        }
        protected IEnumerable<WeightedEdge<T>> GetIncomingEdges(T vertex)
        {
            if (!HasVertex(vertex))
            {
                throw new KeyNotFoundException("Vertex doesn't belong to graph.");
            }
            foreach (var edge in _adjacencyList[vertex])
            {
                yield return new WeightedEdge<T>(edge.Destination, edge.Source, edge.Weight);
            }
        }

        public IEnumerable<IEdge<T>> OutgoingEdges(T vertex)
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
        public bool AddEdge(T firstVertex, T secondVertex)
        {
            throw new NotImplementedException();
        }
        public bool AddEdge(T source, T destination, double weight)
        {
            // Check existence of nodes, the validity of the weight value, and the non-existence of edge.
            //if (weight == _EMPTY_EDGE_VALUE)
            if (weight < _EMPTY_EDGE_VALUE)
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
            var sourceEdge = new WeightedEdge<T>(source, destination, weight);
            var destinationEdge = new WeightedEdge<T>(destination, source, weight);

            _adjacencyList[source].AddLast(sourceEdge);
            _adjacencyList[destination].AddLast(destinationEdge);

            // Increment edges count.
            ++_edgesCount;

            return true;
        }


        /// <summary>
        /// Deep copy for current graph.
        /// </summary>
        /// <returns></returns>
        public UndirectedWeightedSparseGraph<T> DeepCopy()
        {
            UndirectedWeightedSparseGraph<T> copy = new UndirectedWeightedSparseGraph<T>(VerticesCount);
            copy.AddVertices(Vertices.ToList());
            foreach (var edge in Edges)
            {
                copy.AddEdge(edge.Source, edge.Destination, edge.Weight);
            }

            return copy;
        }



        public bool RemoveEdge(T source, T destination)
        {
            // Check existence of nodes and non-existence of edge.
            if (!HasVertex(source) || !HasVertex(destination))
            {
                return false;
            }

            var sourceToDestinationPredicate = new Predicate<WeightedEdge<T>>(item => item.Source.IsEqualTo(source) && item.Destination.IsEqualTo(destination));
            _adjacencyList[source].TryFindFirst(sourceToDestinationPredicate, out WeightedEdge<T> edge1);

            var destinationToSourcePredicate = new Predicate<WeightedEdge<T>>(item => item.Source.IsEqualTo(destination) && item.Destination.IsEqualTo(source));
            _adjacencyList[destination].TryFindFirst(destinationToSourcePredicate, out WeightedEdge<T> edge2);

            // If edge doesn't exist. return false;
            if (edge1 == null && edge2 == null)
            {
                return false;
            }

            // If edge exists in the source neighbours, remove it.
            if (edge1 != null)
            {
                _adjacencyList[source].Remove(edge1);
            }

            // If edge exists in the destination neighbours, remove it.
            if (edge2 != null)
            {
                _adjacencyList[destination].Remove(edge2);
            }

            // Decrement the edges count.
            --_edgesCount;

            return true;
        }

        public bool UpdateEdgeWeight(T source, T destination, double weight)
        {
            // Check existence of vertices and validity of the weight value.
            //if (weight == _EMPTY_EDGE_VALUE)
            if (weight < _EMPTY_EDGE_VALUE)
            {
                return false;
            }
            if (!HasVertex(source) || !HasVertex(destination))
            {
                return false;
            }

            // Status flag of updating an edge.
            var flag = false;

            // Check the source neighbors
            foreach (var edge in _adjacencyList[source])
            {
                if (edge.Destination.IsEqualTo(destination))
                {
                    edge.Weight = weight;
                    flag |= true;
                    break;
                }
            }

            // Check the destination neighbors.
            foreach (var edge in _adjacencyList[destination])
            {
                if (edge.Destination.IsEqualTo(source))
                {
                    edge.Weight = weight;
                    flag |= true;
                    break;
                }
            }

            return flag;
        }


        public WeightedEdge<T> GetEdge(T source, T destination)
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

        public double GetEdgeWeight(T source, T destination)
        {
            return GetEdge(source, destination).Weight;
        }


        public bool AddVertex(T vertex)
        {
            if (HasVertex(vertex))
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

        public void AddVertices(IList<T> collection)
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

        public bool RemoveVertex(T vertex)
        {
            // Check existence of vertex.
            if (!HasVertex(vertex))
            {
                return false;
            }

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

        public bool HasEdge(T source, T destination)
        {
            return HasVertex(source) && HasVertex(destination) && DoesEdgeExist(source, destination);
        }

        public bool HasVertex(T vertex)
        {
            return _adjacencyList.ContainsKey(vertex);
        }

        public LinkedList<T> Neighbours(T vertex)
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

        public int Degree(T vertex)
        {
            if (!HasVertex(vertex))
            {
                throw new KeyNotFoundException();
            }

            return _adjacencyList[vertex].Count;
        }

        public string ToReadable()
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

        public override string ToString()
        {
            return $"WeightedUnDigraph: G = (V:{VerticesCount},E:{EdgesCount})";
        }


        public IEnumerable<T> BreadthFirstWalk()
        {
            return BreadthFirstWalk(_firstInsertedNode);
        }

        public IEnumerable<T> BreadthFirstWalk(T startingVertex)
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

        public IEnumerable<T> DepthFirstWalk()
        {
            return DepthFirstWalk(_firstInsertedNode);
        }

        public IEnumerable<T> DepthFirstWalk(T startingVertex)
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
            var stackOfNodes = new Stack<T>();
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

        public void Dispose()
        {
            ClearGraph();
        }
    }
}
