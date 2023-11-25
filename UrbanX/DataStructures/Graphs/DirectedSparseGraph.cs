/***
 * The Directed Sparse Graph Data Structure.
 * 
 * Definition:
 * A sparse graph is a graph G = (V, E) in which |E| = O(|V|).
 * A directed graph is a graph where each edge follow one direction only between any two vertices.
 * 
 * An adjacency-list digraph (directed-graph) representation. 
 * Implements the IGraph<T> interface.
 */

using System;
using System.Collections.Generic;

namespace UrbanX.DataStructures.Graphs
{
    /// <summary>
    /// The Directed Sparse Graph Data Structure.
    /// An adjacency-list digraph (directed-graph) representation. 
    /// A sparse graph is a graph G = (V, E) in which |E| = O(|V|).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DirectedSparseGraph<T> : IDisposable, IGraph<T> where T : IComparable<T>
    {
        /// <summary>
        /// INSTANCE VARIABLES
        /// </summary>
        protected int _edgesCount;
        protected T _firstInsertedNode;
        protected Dictionary<T, LinkedList<T>> _adjacencyList;


        /// <summary>
        /// Constructor of UndirectedSparseGraph. (Using adjacencyList)
        /// </summary>
        public DirectedSparseGraph() : this(10) { }

        /// <summary>
        /// Constructor of UndirectedSparseGraph with initial capacity. (Using adjacencyList)
        /// </summary>
        /// <param name="capacity"></param>
        public DirectedSparseGraph(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("Capacity can't be less than zero.");
            }

            _edgesCount = 0;
            _adjacencyList = new Dictionary<T, LinkedList<T>>(capacity);
        }

        /// <summary>
        /// Helper function. Checks if edge exists in graph.
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <returns></returns>
        protected bool DoesEdgeExist(T vertex1, T vertex2)
        {
            return _adjacencyList[vertex1].Contains(vertex2);
        }

        public bool IsDirected
        {
            get { return true; }
        }

        public bool IsWeighted
        {
            get { return false; }
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
        protected IEnumerable<UnweightedEdge<T>> GetEdges()
        {
            foreach (var vertex in _adjacencyList)
            {
                foreach (var adjacent in vertex.Value)
                {
                    yield return new UnweightedEdge<T>(vertex.Key, adjacent);
                }
            }//end-foreach
        }

        public IEnumerable<IEdge<T>> IncomingEdges(T vertex)
        {
            return this.GetIncomingEdges(vertex);
        }
        protected IEnumerable<UnweightedEdge<T>> GetIncomingEdges(T vertex)
        {
            if (!HasVertex(vertex))
            {
                throw new KeyNotFoundException("Vertex doesn't belong to graph.");
            }

            foreach (var adjacent in _adjacencyList.Keys)
            {
                if (_adjacencyList[adjacent].Contains(vertex))
                {
                    yield return new UnweightedEdge<T>(adjacent, vertex);
                }
            }
        }

        public IEnumerable<IEdge<T>> OutgoingEdges(T vertex)
        {
            return this.GetOutgoingEdges(vertex);
        }
        protected IEnumerable<UnweightedEdge<T>> GetOutgoingEdges(T vertex)
        {
            if (!HasVertex(vertex))
            {
                throw new KeyNotFoundException("Vertex doesn't belong to graph.");
            }
            foreach (var adjacent in _adjacencyList[vertex])
            {
                yield return new UnweightedEdge<T>(vertex, adjacent);
            }
        }

        public bool AddEdge(T source, T destination)
        {
            if (!HasVertex(source) || !HasVertex(destination))
            {
                return false;
            }
            if (DoesEdgeExist(source, destination))
            {
                return false;
            }

            // Add edge from source to destination.

            _adjacencyList[source].AddLast(destination);

            // Increment edges count.
            ++_edgesCount;

            return true;
        }

        public bool RemoveEdge(T source, T destination)
        {
            // Check existence of nodes and non-existence of edge.
            if (!HasVertex(source) || !HasVertex(destination))
            {
                return false;
            }
            if (!DoesEdgeExist(source, destination))
            {
                return false;
            }

            // Remove edge from source to destination.
            _adjacencyList[source].Remove(destination);

            // Decrement the edges count.
            --_edgesCount;

            return true;
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

            _adjacencyList.Add(vertex, new LinkedList<T>());

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
            // Subtract the number of edges for this vertex from the total edges count.
            _edgesCount -= _adjacencyList[vertex].Count;

            // Remove vertex from graph.
            _adjacencyList.Remove(vertex);

            // Remove destination edges to this vertex.
            foreach (var adjacent in _adjacencyList)
            {
                if (adjacent.Value.Contains(vertex))
                {
                    adjacent.Value.Remove(vertex);
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
            return _adjacencyList[vertex];
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
                    adjacents = string.Format("{0}{1},", adjacents, adjacentNode);

                if (adjacents.Length > 0)
                    adjacents = adjacents.TrimEnd(new char[] { ',', ' ' });

                output = string.Format("{0}{1}]", output, adjacents);
            }

            return output;
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

        public void ClearGraph()
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
