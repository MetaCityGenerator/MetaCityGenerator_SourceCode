/***
 * The Sparse Graph Data Structure.
 * Definition: A sparse graph is a graph G = (V, E) in which |E| = O(|V|).
 * 
 * An adjacency-list graph representation. Implemented using a Dictionary. The nodes are inserted as keys, 
 * and the neighbors of every node are implemented as a doubly-linked list of nodes. 
 * This class implements the IGraph<T> interface.
 */


using System;
using System.Collections.Generic;

namespace UrbanX.DataStructures.Graphs
{
    public class UndirectedSparseGraph<T> : IGraph<T> where T : IComparable<T>
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
        public UndirectedSparseGraph() : this(10) { }

        /// <summary>
        /// Constructor of UndirectedSparseGraph with initial capacity. (Using adjacencyList)
        /// </summary>
        /// <param name="capacity"></param>
        public UndirectedSparseGraph(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("Capacity can't be less than zero.");
            }

            _edgesCount = 0;
            _adjacencyList = new Dictionary<T, LinkedList<T>>(capacity);
        }

        public virtual bool IsDirected
        {
            get { return false; }
        }

        public virtual bool IsWeighted
        {
            get { return false; }
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
                var list = new List<T>();
                foreach (var vertex in _adjacencyList.Keys)
                {
                    list.Add(vertex);
                }
                return list;
            }
        }

        public virtual IEnumerable<IEdge<T>> Edges
        {
            get { return this.GetEdges(); }
        }
        protected IEnumerable<UnweightedEdge<T>> GetEdges()
        {
            var seen = new HashSet<KeyValuePair<T, T>>();

            foreach (var vertex in _adjacencyList)
            {
                foreach (var adjacent in vertex.Value)
                {
                    var incomingEdge = new KeyValuePair<T, T>(adjacent, vertex.Key);
                    var outgoingEdge = new KeyValuePair<T, T>(vertex.Key, adjacent);

                    if (seen.Contains(incomingEdge) || seen.Contains(outgoingEdge))
                    {
                        continue;
                    }
                    seen.Add(outgoingEdge);

                    yield return new UnweightedEdge<T>(outgoingEdge.Key, outgoingEdge.Value);
                }
            }//end-foreach
        }

        public virtual IEnumerable<IEdge<T>> IncomingEdges(T vertex)
        {
            return this.GetIncomingEdges(vertex);
        }
        protected IEnumerable<UnweightedEdge<T>> GetIncomingEdges(T vertex)
        {
            if (!HasVertex(vertex))
            {
                throw new KeyNotFoundException("Vertex doesn't belong to graph.");
            }
            foreach (var adjacent in _adjacencyList[vertex])
            {
                yield return new UnweightedEdge<T>(adjacent, vertex);
            }
        }

        public virtual IEnumerable<IEdge<T>> OutgoingEdges(T vertex)
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


        public virtual bool AddEdge(T firstVertex, T secondVertex)
        {
            if (!HasVertex(firstVertex) || !HasVertex(secondVertex))
            {
                return false;
            }
            if (HasEdge(firstVertex, secondVertex))
            {
                return false;
            }

            _adjacencyList[firstVertex].AddLast(secondVertex);
            _adjacencyList[secondVertex].AddLast(firstVertex);

            // Increment the edges count
            ++_edgesCount;

            return true;
        }

        public virtual bool RemoveEdge(T firstVertex, T secondVertex)
        {
            if (!HasVertex(firstVertex) || !HasVertex(secondVertex))
            {
                return false;
            }
            if (!HasEdge(firstVertex, secondVertex))
            {
                return false;
            }

            _adjacencyList[firstVertex].Remove(secondVertex);
            _adjacencyList[secondVertex].Remove(firstVertex);

            // Decrement the edges count
            --_edgesCount;

            return true;
        }

        public virtual void AddVertices(IList<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException();
            }
            foreach (var item in collection)
            {
                this.AddVertex(item);
            }
        }

        public virtual bool AddVertex(T vertex)
        {
            // Check existence of vertex.
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

        public virtual bool RemoveVertex(T vertex)
        {
            // Check existence of vertex.
            if (!HasVertex(vertex))
            {
                return false;
            }

            _adjacencyList.Remove(vertex);

            foreach (var adjacent in _adjacencyList)
            {
                if (adjacent.Value.Contains(vertex))
                {
                    adjacent.Value.Remove(vertex);

                    // Decrement the edges count.
                    --_edgesCount;
                }
            }

            return true;
        }

        public virtual bool HasEdge(T firstVertex, T secondVertex)
        {
            // Check existence of vertices
            if (!HasVertex(firstVertex) || !HasVertex(secondVertex))
            {
                return false;
            }
            return _adjacencyList[firstVertex].Contains(secondVertex) || _adjacencyList[secondVertex].Contains(firstVertex);
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
            return _adjacencyList[vertex];
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
                    adjacents = string.Format("{0}{1},", adjacents, adjacentNode);

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

        public void ClearGraph()
        {
            _edgesCount = 0;
            _adjacencyList.Clear();
        }
    }
}
