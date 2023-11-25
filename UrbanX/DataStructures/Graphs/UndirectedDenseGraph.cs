/***
 * The Dense Graph Data Structure.
 * 
 * Definition: A dense graph is a graph G = (V, E) in which |E| = O(|V|^2).
 * 
 * An adjacency-matrix (two dimensional boolean array) graph representation.
 * This class implements the IGraph<T> interface.
 */
using System;
using System.Collections.Generic;

using UrbanX.Algorithms.Utility;


namespace UrbanX.DataStructures.Graphs
{
    public class UndirectedDenseGraph<T> : IGraph<T> where T : IComparable<T>
    {
        /// <summary>
        /// Instance variables
        /// </summary>

        protected int _edgesCount;
        protected int _verticesCapacity;
        protected List<T> _verticesList;
        protected T _firstInsertedNode;
        /// <summary>
        /// For unWeighted graph, Adjacency matrix holds the bool as each entry indicates the connection bewteen two vertices.
        /// While in Weighted graph, matrix holds numbers as weights.
        /// </summary>
        protected bool[,] _adjacencyMatrix;

        /// <summary>
        /// Constructor of UndirectedDenseGraph. (Using adjacencyMatix)
        /// </summary>
        public UndirectedDenseGraph() : this(10) { }

        /// <summary>
        /// Constructor of UndirectedDenseGraph wiht initial capacity. (Using adjacencyMatix)
        /// </summary>
        /// <param name="capacity"></param>
        public UndirectedDenseGraph(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("Capacity can't be less than zero.");
            }

            _edgesCount = 0;
            _verticesCapacity = capacity;
            _verticesList = new List<T>(_verticesCapacity);
            _adjacencyMatrix = new bool[_verticesCapacity, _verticesCapacity];
            _adjacencyMatrix.Populate(row: _verticesCapacity, columns: _verticesCapacity, defaultValue: false);
        }


        /// <summary>
        /// Helper function. Checks if edge exist in graph.
        /// </summary>
        /// <param name="index1"></param>
        /// <param name="index2"></param>
        /// <returns></returns>
        protected virtual bool DoesEdgeExist(int index1, int index2)
        {
            return _adjacencyMatrix[index1, index2] || _adjacencyMatrix[index2, index1];
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
            get { return _verticesList.Count; }
        }

        public virtual int EdgesCount
        {
            get { return _edgesCount; }
        }

        public virtual IEnumerable<T> Vertices
        {
            get
            {
                foreach (var item in _verticesList)
                {
                    yield return item;
                }
            }
        }



        public virtual IEnumerable<IEdge<T>> Edges
        {
            get { return this.GetEdges(); }
        }

        protected IEnumerable<UnweightedEdge<T>> GetEdges()
        {
            var seen = new HashSet<KeyValuePair<T, T>>();

            foreach (var vertex in _verticesList)
            {
                int source = _verticesList.IndexOf(vertex);
                for (int adjacent = 0; adjacent < _verticesList.Count; ++adjacent)
                {
                    // Check existence of vertex
                    if (_verticesList[adjacent] != null && DoesEdgeExist(source, adjacent))
                    {
                        var neighbor = _verticesList[adjacent];

                        var outgoingEdge = new KeyValuePair<T, T>(vertex, neighbor);
                        var incomingEdge = new KeyValuePair<T, T>(neighbor, vertex);

                        if (seen.Contains(incomingEdge) || seen.Contains(outgoingEdge))
                        {
                            continue;
                        }
                        // outgoingEdge and incomingEdge will calculate the the edges twice, 
                        // so we just need to add only outgoinEdge in seen set.
                        seen.Add(outgoingEdge);

                        yield return new UnweightedEdge<T>(outgoingEdge.Key, outgoingEdge.Value);
                    }
                }
            }// end - foreach
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
            int source = _verticesList.IndexOf(vertex);

            for (int adjacent = 0; adjacent < _verticesList.Count; ++adjacent)
            {
                if (_verticesList[adjacent] != null && DoesEdgeExist(source, adjacent))
                {
                    yield return new UnweightedEdge<T>(_verticesList[adjacent], vertex);
                }
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
            int source = _verticesList.IndexOf(vertex);

            for (int adjacent = 0; adjacent < _verticesList.Count; ++adjacent)
            {
                if (_verticesList[adjacent] != null && DoesEdgeExist(source, adjacent))
                {
                    yield return new UnweightedEdge<T>(vertex, _verticesList[adjacent]);
                }
            }
        }



        public virtual bool AddEdge(T firstVertex, T secondVertex)
        {
            int indexOfFirst = _verticesList.IndexOf(firstVertex);
            int indexOfSecond = _verticesList.IndexOf(secondVertex);

            if (indexOfFirst == -1 || indexOfSecond == -1)
            {
                return false;
            }
            if (DoesEdgeExist(indexOfFirst, indexOfSecond))
            {
                return false;
            }

            _adjacencyMatrix[indexOfFirst, indexOfSecond] = true;
            _adjacencyMatrix[indexOfSecond, indexOfFirst] = true;

            // Increment the edges count.
            ++_edgesCount;

            return true;
        }


        public virtual bool RemoveEdge(T firstVertex, T secondVertex)
        {
            int indexOfFirst = _verticesList.IndexOf(firstVertex);
            int indexOfSecond = _verticesList.IndexOf(secondVertex);

            if (indexOfFirst == -1 || indexOfSecond == -1)
            {
                return false;
            }
            if (!DoesEdgeExist(indexOfFirst, indexOfSecond))
            {
                return false;
            }

            _adjacencyMatrix[indexOfFirst, indexOfSecond] = false;
            _adjacencyMatrix[indexOfSecond, indexOfFirst] = false;

            // Decrement the edges count.
            --_edgesCount;

            return true;
        }

        /// <summary>
        /// Adds a list of all the vertices to the graph ( A graph can use this method multiply times).
        /// Can't exceed the initial capacity due to the matrix structure.
        /// </summary>
        /// <param name="collection"></param>
        public virtual void AddVertices(IList<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentException();
            }
            foreach (var item in collection)
            {
                this.AddVertex(item);
            }
        }


        /// <summary>
        /// Adds a new vertex to graph.
        /// Can't exceed the initial capacity due to the matrix structure.
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        public virtual bool AddVertex(T vertex)
        {
            // Return if graph reached its maximum capacity.
            if (_verticesList.Count >= _verticesCapacity)
            {
                return false;
            }

            // Return if vertex already exists.
            if (HasVertex(vertex))
            {
                return false;
            }

            // Initialize first insertd node.
            if (_verticesList.Count == 0)
            {
                _firstInsertedNode = vertex;
            }

            _verticesList.Add(vertex);

            return true;
        }

        public virtual bool RemoveVertex(T vertex)
        {
            // Return if graph is empty.
            if (_verticesList.Count == 0)
            {
                return false;
            }

            // Get index of vertex
            int index = _verticesList.IndexOf(vertex);

            // Return if vertex doesn't exists.
            if (index == -1)
            {
                return false;
            }

            _verticesList.RemoveAt(index);

            // Delete the edges
            for (int i = 0; i < _verticesCapacity; i++)
            {
                if (DoesEdgeExist(index, i))
                {
                    _adjacencyMatrix[index, i] = false;
                    _adjacencyMatrix[i, index] = false;

                    // Decrement the edges count.
                    --_edgesCount;
                }
            }

            return true;
        }

        public virtual bool HasEdge(T firstVertex, T secondVertex)
        {
            int indexOfFirst = _verticesList.IndexOf(firstVertex);
            int indexOfSecond = _verticesList.IndexOf(secondVertex);

            if (indexOfFirst == -1 || indexOfSecond == -1)
            {
                return false;
            }
            else
            {
                return _adjacencyMatrix[indexOfFirst, indexOfSecond] || _adjacencyMatrix[indexOfSecond, indexOfFirst];
            }
        }

        public virtual bool HasVertex(T vertex)
        {
            return _verticesList.Contains(vertex);
        }

        public virtual LinkedList<T> Neighbours(T vertex)
        {
            var neighbours = new LinkedList<T>();
            int source = _verticesList.IndexOf(vertex);

            if (source != -1)
            {
                for (int adjacent = 0; adjacent < _verticesList.Count; adjacent++)
                {
                    if (_verticesList[adjacent] != null && DoesEdgeExist(source, adjacent))
                    {
                        neighbours.AddLast(_verticesList[adjacent]);
                    }
                }
            }

            return neighbours;
        }

        public virtual int Degree(T vertex)
        {
            if (!HasVertex(vertex))
            {
                throw new KeyNotFoundException();
            }
            return Neighbours(vertex).Count;
        }

        public virtual string ToReadable()
        {
            string output = string.Empty;

            for (int i = 0; i < _verticesList.Count; i++)
            {
                if (_verticesList[i] == null)
                {
                    continue;
                }

                var node = _verticesList[i];
                var adjacents = string.Empty;

                output = string.Format("{0}\r\n{1}: [", output, node);

                foreach (var adjacentNode in Neighbours(node))
                {
                    adjacents = string.Format("{0}{1},", adjacents, adjacentNode);
                }

                if (adjacents.Length > 0)
                {
                    adjacents = adjacents.TrimEnd(new char[] { ',', ' ' });
                }

                output = string.Format("{0}{1}]", output, adjacents);
            }

            return output;
        }

        public virtual IEnumerable<T> DepthFirstWalk()
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

        public IEnumerable<T> BreadthFirstWalk()
        {
            return BreadthFirstWalk(_firstInsertedNode);
        }

        public IEnumerable<T> BreadthFirstWalk(T startingVertex)
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

            queueOfNodes.Enqueue(startingVertex);
            visitedNodes.Add(startingVertex);
            listOfNodes.Add(startingVertex);

            while (queueOfNodes.Count != 0)
            {
                var current = queueOfNodes.Dequeue();
                foreach (var adjacent in Neighbours(current))
                {
                    if (!visitedNodes.Contains(adjacent))
                    {
                        queueOfNodes.Enqueue(adjacent);
                        visitedNodes.Add(adjacent);
                        listOfNodes.Add(adjacent);
                    }
                }
            }
            return listOfNodes;
        }

        public void ClearGraph()
        {
            _edgesCount = 0;
            _verticesList.Clear();
            _adjacencyMatrix = new bool[_verticesCapacity, _verticesCapacity];
            _adjacencyMatrix.Populate(row: _verticesCapacity, columns: _verticesCapacity, defaultValue: false);
        }
    }
}
