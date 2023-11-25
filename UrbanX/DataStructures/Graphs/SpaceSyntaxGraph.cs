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

using UrbanX.DataStructures.Set;

namespace UrbanX.DataStructures.Graphs
{
    public sealed class SpaceSyntaxGraph
    {
        private readonly int _vCount;
        private readonly int[] _vetices;
        private readonly LinkedList<int>[] _adj; // int represent the id of edge.
        private readonly HashSet<int> _edgePairs;
        private readonly List<SpaceSyntaxEdge> _edges;

        public bool IsDirected => false;


        public bool IsWeighted => true;


        public int VerticesCount =>_vCount;


        public int EdgesCount => _edges.Count;

        public int[] Vertices => _vetices; // array is a reference type. array[] will also return reference.

        public SpaceSyntaxEdge[] Edges => _edges.ToArray();

        public LinkedList<int>[] AdjacentEdges => _adj;

        /// <summary>
        /// Constructor for space syntax graph.
        /// </summary>
        /// <param name="verticesCount">Vertices count must be positive.</param>
        public SpaceSyntaxGraph(int verticesCount)
        {
            _vCount = verticesCount;
            _vetices = new int[_vCount];
            _adj = new LinkedList<int>[_vCount];

            for (int i = 0; i < _vCount; i++)
            {
                _vetices[i] = i;
                _adj[i] = new LinkedList<int>();
            }

            _edgePairs = new HashSet<int>(_vCount * 3);
            _edges = new List<SpaceSyntaxEdge>(_vCount * 3);
        }



        /// <summary>
        /// Add one SpaceSyntaxEdge. Do not support self-loop edge and parallel edge.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="u"></param>
        /// <param name="weight"></param>
        public void AddEdge(int v, int u, float w1, float w2 = 0, float w3 = 0 , bool checkParallel = false)
        {
            if (v != u) // self loop.
            {
                var edge = new SpaceSyntaxEdge(v, u, w1, w2, w3);

                if (checkParallel)
                {
                    var flag = EdgeExist(edge.V,edge.U); // check parallel.
                    if (!flag)
                    {
                        _adj[v].AddLast(_edges.Count);// storing the edge id.
                        _adj[u].AddLast(_edges.Count);
                    }
                }
                else
                {
                    _adj[v].AddLast(_edges.Count);
                    _adj[u].AddLast(_edges.Count);
                }

                _edgePairs.Add(edge.GetHashCode()); 
                _edges.Add(edge);
            }
        }



        /// <summary>
        /// Helper method to check if the vertex is between 0 and (V-1).
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private bool ValidateVertex(int v)
        {
            if ( v >= this._vCount || v<0)
                return false;
            else
                return true;
        }




        /// <summary>
        /// Helper method to check if edge exist in current graph.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public bool EdgeExist(int v , int u)
        {
            var min = Math.Min(v, u);
            var max = Math.Max(v, u);

            var h = HashCode.Combine(min, max);
            return _edgePairs.Contains(h);
        }




        /// <summary>
        /// Returns the edges incident on vertex.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public SpaceSyntaxEdge[] GetAdjacentEdges(int v)
        {
            if (ValidateVertex(v))
            {
                SpaceSyntaxEdge[] adj = new SpaceSyntaxEdge[_adj[v].Count];
                int i = 0;
                foreach (var p in _adj[v])
                {
                    adj[i] = _edges[p];
                    i++;
                }
                return adj;
            }
            else
                return null;
        }

        public int[] GetAdjacentEdgesId(int v)
        {
            if (ValidateVertex(v))
            {
                return _adj[v].ToArray();
            }
            else
                return null;
        }

        public int Degree(int v)
        {
            if (ValidateVertex(v))
                return _adj[v].Count;
            else
                return -1;// means this vertex is invalid.
        }


        public override string ToString()
        {
            return $"WeightedUnDigraph: G = (V:{VerticesCount},E:{EdgesCount})";
        }

        //TODO: BFS DFS


        /// <summary>
        /// A breadth first search traversal of the graph, starting from a specified vertex in a FIFO manner.
        /// </summary>
        /// <param name="start">The starting vertex for search.</param>
        /// <returns></returns>
        public int[] BFS(int start)
        {
            if (this.VerticesCount == 0)
                return null;
            if(!ValidateVertex(start))
                throw new Exception("The specified starting vertex doesn't exist.");

            // List of nodes directly or indirectly connected to the starting node.
            // in a breadth first manner.
            LinkedHashSet<int> result = new LinkedHashSet<int>();
            // Queue of temporary path, the count of stack will increase or decrease with each step.
            var tempPath = new Queue<int>();

            result.Add(start);
            tempPath.Enqueue(start);

            while(tempPath.Count != 0)
            {
                var v = tempPath.Dequeue();
                foreach (var w in _adj[v])
                {
                    var flag = result.Add(w);
                    if(flag)
                        tempPath.Enqueue(w);
                }
            }
            return result.ToArray();
        }

        public int[] DFS(int start)
        {
            if (this.VerticesCount == 0)
                return null;
            if (!ValidateVertex(start))
                throw new Exception("The specified starting vertex doesn't exist.");

            // List of nodes directly or indirectly connected to the starting node.
            // in a depth first manner.
            LinkedHashSet<int> result = new LinkedHashSet<int>();
            // Stack of temporary path, the count of stack will increase or decrease with each step.
            var tempPath = new Stack<int>();
            tempPath.Push(start);

            while (tempPath.Count != 0)
            {
                var v = tempPath.Pop();
                var flag = result.Add(v);
                if (flag)
                {
                    foreach (var w in _adj[v])
                    {
                        if (!result.Contains(w))
                            tempPath.Push(w);
                    }
                }
            }
            return result.ToArray();
        }
    }
}
