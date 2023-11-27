using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MetaCity.DataStructures.Graphs;
using MetaCity.DataStructures.Heaps;

namespace MetaCity.Algorithms.Graphs
{
    /// <summary>
    /// Calculating the centrality in a graph without radius.
    /// </summary>
    /// <typeparam name="TGraph"></typeparam>
    /// <typeparam name="TVertex"></typeparam>
    public sealed class CalculateCentrality<TGraph, TVertex> where TGraph : IGraph<TVertex>, IWeightedGraph<TVertex> where TVertex : IComparable<TVertex>
    {
        private readonly TGraph _graph;
        private readonly TVertex[] _vertices;
        private readonly Dictionary<TVertex, int> _verticesToIndices;

        /// <summary>
        /// For space syntax, _subGraphs is essential for finding the clusters in angular computation.
        /// </summary>
        private readonly int[][] _subGraphs; // *** default is null.

        /// <summary>
        /// For space syntax, radius is essential for finding the clusters.
        /// </summary>
        private readonly double _radius; //*** default is postiveinfinity.

        /// <summary>
        /// The total betweenness centrality for every vertex in graph.
        /// </summary>
        public Dictionary<TVertex, double> Betweenness { get; }


        /// <summary>
        /// THe total distance(depths) for every single vertex in a graph.
        /// </summary>
        public ConcurrentDictionary<TVertex, double> TotalDepths { get; }


        /// <summary>
        /// Node count should be a integer, using double for convient method of constructing concurrentDictionary.
        /// </summary>
        public ConcurrentDictionary<TVertex, double> NodeCounts { get; }

        public int[][] SubGraphs { get; }


        public CalculateCentrality(TGraph graph) : this(graph, double.PositiveInfinity, default) { }

        public CalculateCentrality(TGraph graph, double radius) : this(graph, radius, default) { }

        public CalculateCentrality(TGraph graph, int[][] subGraphs) : this(graph, double.PositiveInfinity, subGraphs) { }


        public CalculateCentrality(TGraph graph, double radius, int[][] subGraphs)
        {

            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (graph.Edges.Any(edge => edge.Weight < 0))
            {
                throw new ArgumentException("Negative edge weight detected.");
            }

            _graph = graph;
            _vertices = graph.Vertices.ToArray();
            _verticesToIndices = new Dictionary<TVertex, int>(graph.VerticesCount);

            _subGraphs = subGraphs;
            _radius = radius;

            Betweenness = new Dictionary<TVertex, double>(graph.VerticesCount);
            SubGraphs = new int[graph.VerticesCount][];

            Initialize();

            TotalDepths = new ConcurrentDictionary<TVertex, double>(Betweenness);
            NodeCounts = new ConcurrentDictionary<TVertex, double>(Betweenness);

            Computing();
        }


        private void Initialize()
        {
            for (int i = 0; i < _graph.VerticesCount; i++)
            {
                _verticesToIndices.Add(_vertices[i], i);
                Betweenness.Add(_vertices[i], 0.0);
            }
        }


        private void Computing()
        {
            if (_graph.VerticesCount >= 30)
            {
                int taskNumber = 30;
                var range = _graph.VerticesCount / taskNumber;
                List<Task> tasks = new List<Task>(taskNumber);

                // Local concurrent collection for parallel computing.
                ConcurrentBag<Dictionary<TVertex, double>> betweenessBag = new ConcurrentBag<Dictionary<TVertex, double>>();

                // Partitioning the vertices collection.
                IEnumerable<TVertex>[] verticesPartition = new IEnumerable<TVertex>[taskNumber];
                for (int i = 0; i < taskNumber; i++)
                {
                    var rangeCount = 0;
                    if (i == taskNumber - 1)
                    {
                        rangeCount = _graph.VerticesCount - i * range;
                    }
                    else
                    {
                        rangeCount = range;
                    }

                    verticesPartition[i] = _vertices.ToList().GetRange(i * range, rangeCount);
                }


                // Using for loop will encounter some errors due to int i will change during the process of each task. 
                // eg. for task1, i should be 1, but waitall task to complete, i has already changed.
                foreach (var tempVertices in verticesPartition)
                {
                    var t = Task.Run(() =>
                    {
                        var betweennessEachTask = new Dictionary<TVertex, double>(Betweenness);
                        foreach (var source in tempVertices)
                        {
                            var subId = _subGraphs?[_verticesToIndices[source]];

                            var centrality = new CentralitySingleSource<TGraph, TVertex>(_graph, source, _verticesToIndices, _radius, subId);
                            var tempScore = centrality.BetweennessScore;

                            if (_radius != double.PositiveInfinity)
                            {
                                // Get sub_graphs
                                SubGraphs[_verticesToIndices[source]] = centrality.VertexInicesWithinRadius;
                            }

                            foreach (var item in tempScore)
                            {
                                betweennessEachTask[item.Key] += item.Value;
                            }

                            TotalDepths.TryUpdate(source, centrality.TotalDepthScore, 0.0);
                            NodeCounts.TryUpdate(source, centrality.NodeCount, 0);
                        }

                        betweenessBag.Add(betweennessEachTask);
                    });

                    tasks.Add(t);
                }

                Task.WaitAll(tasks.ToArray());

                // Accumulating betweeness score from each bags.
                foreach (var between in betweenessBag)
                {
                    foreach (var item in between)
                    {
                        Betweenness[item.Key] += item.Value;
                    }
                }
            }
            else
            {
                foreach (var source in _vertices)
                {
                    var subId = _subGraphs?[_verticesToIndices[source]];


                    var centrality = new CentralitySingleSource<TGraph, TVertex>(_graph, source, _verticesToIndices, _radius, subId);
                    var tempScore = centrality.BetweennessScore;

                    if (_radius != double.PositiveInfinity)
                    {
                        // Get sub_graphs
                        SubGraphs[_verticesToIndices[source]] = centrality.VertexInicesWithinRadius;
                    }


                    foreach (var item in tempScore)
                    {
                        Betweenness[item.Key] += item.Value;
                    }

                    TotalDepths[source] = centrality.TotalDepthScore;
                    NodeCounts[source] = centrality.NodeCount;
                }
            }
        }
    }



    /// <summary>
    /// Internal class for computing the betweenness centrality for a single source.
    /// BetweennessScore is the dictionary with vertex as key and score as value.
    /// Every betweenness score has been normalized.
    /// </summary>
    /// <typeparam name="TGraph"></typeparam>
    /// <typeparam name="TVertex"></typeparam>
    internal class CentralitySingleSource<TGraph, TVertex> where TGraph : IGraph<TVertex>, IWeightedGraph<TVertex> where TVertex : IComparable<TVertex>
    {
        private const double _infinity = double.PositiveInfinity;

        private readonly Dictionary<int, LinkedList<int>> _predecessors;

        // _distance[v] is the length from s to v.
        // The largest item in _distance represents the furthest node.
        // The sum of all distance except infinity is the total depth.
        private readonly Dictionary<int, double> _distance;

        private readonly TVertex[] _vertices;
        private readonly MinPriorityQueue<int, double> _minPriorityQueue;

        /// <summary>s
        /// Subgraph vertices' index.
        /// </summary>
        private readonly int[] _subIndices;


        // Fields for betweenness calculation
        private readonly Stack<int> stack;
        private readonly Dictionary<int, int> sigma;
        private readonly Dictionary<int, double> delta;


        /// <summary>
        /// Readonly dict means this class can't be reassigned, but all the dict method belond to this class can use.
        /// This dic is used for 1(O) TVertex query.
        /// </summary>
        private readonly Dictionary<TVertex, int> _nodesToIndices;
        private readonly TGraph _graph;
        private readonly TVertex _source;


        /// <summary>
        /// The partial result of betweenness centrality.
        /// </summary>
        public Dictionary<TVertex, double> BetweennessScore { get; }


        /// <summary>
        /// Total depth equals to the sum of all the distances.
        /// </summary>
        public double TotalDepthScore { get; }


        /// <summary>
        /// Node count is the the number of nodes both directly and indirectly connected to source (include source itself).
        /// </summary>
        public int NodeCount { get; }



        /// <summary>
        /// Storing all the vertices' index which are within the radius to the source node.
        /// </summary>
        public int[] VertexInicesWithinRadius { get; }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="source"></param>
        public CentralitySingleSource(TGraph graph, TVertex source, Dictionary<TVertex, int> verticesToIndices, double radius, int[] subGraphVerticesIndex)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _graph = graph;
            _source = source;
            _vertices = _graph.Vertices.ToArray();
            _nodesToIndices = verticesToIndices;

            _subIndices = subGraphVerticesIndex;

            // Get collection count for initialization.
            var count = _subIndices == null ? _graph.VerticesCount : _subIndices.Length;
            var indices = _subIndices ?? _nodesToIndices.Values.ToArray();

            // Instantiate all the containers withe vertices count as the initial capacity. 
            // For some fields, minHeap and stack, the maxisum capacity is the vertices count.
            // When part of the subgraphs are disconnected to graph, the vertices count of shortest path tree will be less than the graph.verticescount.
            _predecessors = new Dictionary<int, LinkedList<int>>(count);
            _minPriorityQueue = new MinPriorityQueue<int, double>(count);
            _distance = new Dictionary<int, double>(count);

            BetweennessScore = new Dictionary<TVertex, double>(count);

            // stack.Count may less than vertices count.
            stack = new Stack<int>(count);
            // sigma and delta are for all the vertices, therefore they must have same length.
            sigma = new Dictionary<int, int>(count);
            delta = new Dictionary<int, double>(count);


            Initialize(indices);
            Dijkstra(radius);

            // Copy stack items to VertexIndicesWithRadius here, because during Accumulation stage, 
            // stack will become empty.
            VertexInicesWithinRadius = stack.ToArray();


            Accumulation();

            TotalDepthScore = GetTotalDepth(out int nodeCount);
            NodeCount = nodeCount;
        }



        private void Initialize(int[] verticesIndex)
        {
            for (int i = 0; i < verticesIndex.Length; i++)
            {
                var vertexIndex = verticesIndex[i];
                var vertex = _vertices[verticesIndex[i]];

                _distance.Add(vertexIndex, _infinity);
                _predecessors.Add(vertexIndex, new LinkedList<int>());
                BetweennessScore.Add(vertex, 0.0);

                sigma.Add(vertexIndex, 0);
                delta.Add(vertexIndex, 0d);
            }

            var sourceIndx = _nodesToIndices[_source];

            _distance[sourceIndx] = 0;
            _minPriorityQueue.Enqueue(sourceIndx, 0);
            sigma[sourceIndx] = 1;
        }



        /// <summary>
        /// The Dijkstra's algorithm for one single source to all the destinations.
        /// CurrentVertex is v in graph theory, while adjacentVertex is w .
        /// </summary>
        private void Dijkstra(double radius)
        {
            while (!_minPriorityQueue.IsEmpty)
            {
                var currentVertexIndex = _minPriorityQueue.DequeueMin();

                stack.Push(currentVertexIndex);

                var currentVertex = _vertices[currentVertexIndex];
                var outgoingEdges = _graph.OutgoingEdges(currentVertex);


                // Find the precessor of current node.
                var predecessors = _predecessors[currentVertexIndex];

                foreach (var outgoingEdge in outgoingEdges)
                {
                    var adjacentIndex = _nodesToIndices[outgoingEdge.Destination];

                    // Check subindices.
                    if (_subIndices != null)
                    {
                        if (!_subIndices.Contains(adjacentIndex))
                            continue;
                    }


                    // adjacent node shouldn't be seen.
                    if (stack.Contains(adjacentIndex))
                        continue;


                    if (predecessors.Count > 0)
                    {
                        //Has predecessors.

                        // adjacent node shouldn't is one of the predecessor of current node.
                        if (predecessors.Contains(adjacentIndex))
                            continue;

                        // Important: For spacesyntax only, predecessor, current node and adjacent node shouldn't form a cycle.
                        int flag = 0;
                        foreach (var pre in predecessors)
                        {
                            if (_graph.HasEdge(_vertices[pre], _vertices[adjacentIndex]) || _graph.HasEdge(_vertices[adjacentIndex], _vertices[pre])) // undirected graph.
                                flag++;
                        }
                        if (flag > 0)
                            continue;
                    }


                    var dist = Math.Round(_distance[currentVertexIndex] + outgoingEdge.Weight, 6);

                    if (dist <= radius) // Handle radius.
                    {
                        if (dist < _distance[adjacentIndex])
                        {
                            // update distTo and edgeTo
                            _distance[adjacentIndex] = dist;


                            if (_minPriorityQueue.Contains(adjacentIndex))
                            {
                                _minPriorityQueue.UpdatePriority(adjacentIndex, dist);
                            }
                            else
                            {
                                _minPriorityQueue.Enqueue(adjacentIndex, dist);
                            }

                            // update sigma, becasue of finding a new shortest path to adjacent node.
                            sigma[adjacentIndex] = 0;
                            sigma[adjacentIndex] += sigma[currentVertexIndex];

                            // Find the shorter path, therefore we need to update the predecessors by cleaning the linkedlist.
                            _predecessors[adjacentIndex].Clear();
                            _predecessors[adjacentIndex].AddLast(currentVertexIndex);
                        }
                        // Handle equal distance. Meaning there are multiply shortest paths to vertex w.
                        else if (dist == _distance[adjacentIndex])
                        {
                            // dist and _distance[adjacentIndex] can not both equal to _infinity, therefore priorityqueue already has a node(adjacentIndex, dist).
                            sigma[adjacentIndex] += sigma[currentVertexIndex];
                            _predecessors[adjacentIndex].AddLast(currentVertexIndex);
                        }
                    }
                    else
                    {
                        // adjacent vertex w is out of current raius. "dist is larger than radius"s
                        continue;
                    }
                }
            }
        }


        private void Accumulation()
        {
            while (stack.Count != 0)
            {
                // w vertex
                var currentVertexIndex = stack.Pop();
                var coeff = (1.0 + delta[currentVertexIndex]) / sigma[currentVertexIndex];

                // Find the predecessors v of current vertex w.
                var predecessors = _predecessors[currentVertexIndex];
                foreach (var v in predecessors)
                {
                    delta[v] += sigma[v] * coeff;
                }

                if (currentVertexIndex != _nodesToIndices[_source])
                {
                    BetweennessScore[_vertices[currentVertexIndex]] += delta[currentVertexIndex];
                }
            }
        }



        /// <summary>
        /// Helper method for computing the cumulative total of the shortest distance between all nodes(include source itself) to source.
        /// Node count is the the number of nodes both directly and indirectly connected to source(include source itself).
        /// </summary>
        /// <param name="nodeCount"></param>
        /// <returns></returns>
        private double GetTotalDepth(out int nodeCount)
        {
            double d = 0;
            nodeCount = 0;

            foreach (var node in _distance)
            {
                // Infinity means unvisited node. 
                if (node.Value != _infinity)
                {
                    d += node.Value;
                    nodeCount++;
                }
            }

            return d;
        }
    }
}
