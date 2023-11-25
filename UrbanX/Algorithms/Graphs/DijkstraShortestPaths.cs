using System;
using System.Collections.Generic;
using System.Linq;

using UrbanX.DataStructures.Graphs;
using UrbanX.DataStructures.Heaps;


namespace UrbanX.Algorithms.Graphs
{
    /// <summary>
    /// Computes Dijkstra's Shortest-paths for Directed Weighted Graphs from a single-source to all destinations.
    /// </summary>
    /// <typeparam name="TGraph"></typeparam>
    /// <typeparam name="TVertex"></typeparam>
    public class DijkstraShortestPaths<TGraph, TVertex> where TGraph : IGraph<TVertex>, IWeightedGraph<TVertex> where TVertex : IComparable<TVertex>
    {
        // Two consts as place holder for initializs arrays.
        private const double _infinity = double.PositiveInfinity;
        private const int _nonePredecessor = -1;

        private double[] _distances;
        private int[] _predecessors;
        private TVertex[] _vertices;
        private MinPriorityQueue<TVertex, double> _minPriorityQueue;

        /// <summary>
        /// Readonly dict means this class can't be reassigned, but all the dict method belond to this class can use.
        /// This dic is used for 1(O) TVertex query.
        /// </summary>
        private readonly Dictionary<TVertex, int> _nodesToIndices = new Dictionary<TVertex, int>();

        private readonly TGraph _graph;
        private readonly TVertex _source;

        public DijkstraShortestPaths(TGraph graph, TVertex source)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (!graph.HasVertex(source))
            {
                throw new ArgumentException("The source vertex doesn't exist in this graph.");
            }

            if (graph.Edges.Any(edge => edge.Weight < 0))
            {
                throw new ArgumentException("Negative edge weight detected.");
            }

            _graph = graph;
            _source = source;

            Initialize();
            Dijkstra();
        }


        private void Initialize()
        {
            var verticesCount = _graph.VerticesCount;

            // Instantiate all the containers.
            _distances = new double[verticesCount];
            _predecessors = new int[verticesCount];
            _minPriorityQueue = new MinPriorityQueue<TVertex, double>(verticesCount);

            _vertices = _graph.Vertices.ToArray();

            for (int i = 0; i < verticesCount; i++)
            {
                _distances[i] = _infinity;
                _predecessors[i] = _nonePredecessor;
                _nodesToIndices.Add(_vertices[i], i);
            }

            var sourceIndx = _nodesToIndices[_source];

            _distances[sourceIndx] = 0;
            _minPriorityQueue.Enqueue(_source, 0);
        }


        /// <summary>
        /// The Dijkstra's algorithm.
        /// </summary>
        private void Dijkstra()
        {
            while (!_minPriorityQueue.IsEmpty)
            {
                // currentVertex is v in typical graph denoation.
                var currentVertex = _minPriorityQueue.DequeueMin();
                var currentVertexIndex = _nodesToIndices[currentVertex];
                var outgoingEdges = _graph.OutgoingEdges(currentVertex);

                foreach (var outgoingEdge in outgoingEdges)
                {

                    // adjacentVertex is w in typical graph denotation.
                    var adjacentIndex = _nodesToIndices[outgoingEdge.Destination];

                    // The conditional operator ?:, also known as the ternary conditional operator.
                    var delta = _distances[currentVertexIndex] != _infinity ? _distances[currentVertexIndex] + outgoingEdge.Weight : _infinity;

                    if (delta < _distances[adjacentIndex])
                    {
                        // update distTo and edgeTo
                        _distances[adjacentIndex] = delta;
                        _predecessors[adjacentIndex] = currentVertexIndex;

                        if (_minPriorityQueue.Contains(outgoingEdge.Destination))
                        {
                            _minPriorityQueue.UpdatePriority(outgoingEdge.Destination, delta);
                        }
                        else
                        {
                            _minPriorityQueue.Enqueue(outgoingEdge.Destination, delta);
                        }

                    }
                }
            }
        }

        /// <summary>
        /// Determines whether there is a path from the source vertex to this specified vertex.
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        public bool HasPathTo(TVertex destination)
        {
            return DistanceTo(destination) != _infinity;
        }

        /// <summary>
        /// Returns the distance between the source vertex and the specified vertex.
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        public double DistanceTo(TVertex destination)
        {
            if (!_nodesToIndices.ContainsKey(destination))
            {
                throw new ArgumentException("Graph doesn't have the specified vertex.");
            }

            var index = _nodesToIndices[destination];

            return _distances[index];
        }

        /// <summary>
        /// Returns an enumerable collection of nodes that specify the shortest path from the source vertex to the destination vertex.
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        public IEnumerable<TVertex> ShortestPathTo(TVertex destination)
        {
            if (!HasPathTo(destination))
            {
                return null;
            }

            var destIndex = _nodesToIndices[destination];
            var stack = new Stack<TVertex>();

            int index;
            for (index = destIndex; _distances[index] != 0; index = _predecessors[index])
            {
                stack.Push(_vertices[index]);
            }

            // Add the first vertex(source).
            stack.Push(_vertices[index]);

            return stack;
        }

    }
}
