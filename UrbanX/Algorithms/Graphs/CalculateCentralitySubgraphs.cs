using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UrbanX.DataStructures.Graphs;
using UrbanX.DataStructures.Heaps;


namespace UrbanX.Algorithms.Graphs
{
    /// <summary>
    /// Calculating the centrality in a graph.
    /// This class has two properties: Betweenness, Closeness.
    /// </summary>
    /// <typeparam name="TGraph"></typeparam>
    /// <typeparam name="TVertex"></typeparam>
    public class CalculateCentralitySubgraphs<TGraph, TVertex> where TGraph : IGraph<TVertex>, IWeightedGraph<TVertex> where TVertex : IComparable<TVertex>
    {
        private readonly TGraph _graph;
        private readonly TVertex[] _vertices;
        private readonly Dictionary<TVertex, int> _verticesToIndices;


        private readonly int[][] _subGraphs;

        /// <summary>
        /// The total betweenness centrality for every vertex in graph.
        /// </summary>
        public Dictionary<TVertex, double> Betweenness { get; }


        /// <summary>
        /// The total closeness centrality for every vertex in graph.
        /// Using concurrentDicionary for parallel computing.
        /// </summary>
        public ConcurrentDictionary<TVertex, double> Closeness { get; }

        public ConcurrentDictionary<TVertex, double> TotalDepth { get; }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="graph"></param>
        public CalculateCentralitySubgraphs(TGraph graph, int[][] subGraphs, bool normalise = false)
        {

            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (graph.Edges.Any(edge => edge.Weight <= 0))
            {
                throw new ArgumentException("Negative and zero edge weight detected.");
            }

            _graph = graph;
            _vertices = graph.Vertices.ToArray();
            _verticesToIndices = new Dictionary<TVertex, int>(graph.VerticesCount);

            _subGraphs = subGraphs;

            Betweenness = new Dictionary<TVertex, double>(graph.VerticesCount);

            Initialize();

            // Create closenss dictionary from betweeness to copy all the key(vertex)Value(0.0) pairs.
            Closeness = new ConcurrentDictionary<TVertex, double>(Betweenness);
            TotalDepth = new ConcurrentDictionary<TVertex, double>(Betweenness);

            Computing();

            if (normalise)
            {
                NormalizeBetweenness();
                NormalizeCloseness();
            }
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
            // Run in parallel.
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
                            // Greate sub graph.
                            var centrality = new CentralitySingleSourceSubgraph<TGraph, TVertex>(_graph, source, _verticesToIndices, _subGraphs[_verticesToIndices[source]]);
                            var tempScore = centrality.BetweennessScore;


                            foreach (var item in tempScore)
                            {
                                betweennessEachTask[item.Key] += item.Value;
                            }

                            Closeness.TryUpdate(source, centrality.CloesenessScore, 0.0);
                            TotalDepth.TryUpdate(source, centrality.TotalDepthScore, 0.0);
                        }

                        betweenessBag.Add(betweennessEachTask);
                    });

                    tasks.Add(t);
                }

                Task.WaitAll(tasks.ToArray());

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
                    // Create sub graph
                    var centrality = new CentralitySingleSourceSubgraph<TGraph, TVertex>(_graph, source, _verticesToIndices, _subGraphs[_verticesToIndices[source]]);
                    var tempScore = centrality.BetweennessScore;

                    foreach (var item in tempScore)
                    {
                        Betweenness[item.Key] += item.Value;
                    }

                    Closeness[source] = centrality.CloesenessScore;
                    TotalDepth[source] = centrality.TotalDepthScore;
                }
            }
        }


        /// <summary>
        /// Normalizatoing the betweenness score by using (n - 1) * (n - 2).
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="normalize"></param>
        private void NormalizeBetweenness()
        {
            double scale;

            // if use int, will occur overflow error.
            double n = _graph.VerticesCount;

            if (_graph.IsDirected)
            {
                scale = 2.0 / ((n - 1) * (n - 2));
            }
            else
            {
                scale = 1.0 / ((n - 1) * (n - 2));
            }


            foreach (var vertex in _graph.Vertices)
            {
                Betweenness[vertex] *= scale;
            }
        }


        private void NormalizeCloseness()
        {
            int scale = _graph.VerticesCount - 1;

            if (scale <= 1)
                return;

            foreach (var vertex in _graph.Vertices)
            {
                Closeness[vertex] *= scale;
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
    internal class CentralitySingleSourceSubgraph<TGraph, TVertex> where TGraph : IGraph<TVertex>, IWeightedGraph<TVertex> where TVertex : IComparable<TVertex>
    {
        // Two consts as place holder for initializs arrays.
        private const double _infinity = double.PositiveInfinity;
        private readonly Dictionary<int, LinkedList<int>> _predecessors;
        private readonly Dictionary<int, double> _distance;

        /// <summary>
        /// All the vertices in graph.
        /// </summary>
        private readonly TVertex[] _vertices;

        /// <summary>s
        /// Subgraph vertices' index.
        /// </summary>
        private readonly int[] _subIndices;

        private readonly MinPriorityQueue<int, double> _minPriorityQueue;

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
        /// The closenes centrality for current source vertex. For Graph theory, cloesness should be 1 / sumDistance. For space syntax, using total depth to represent closeness which is  sumDistance.
        /// </summary>
        public double CloesenessScore { get; }


        /// <summary>
        /// Total depth equals to the sum of all the distances.
        /// </summary>
        public double TotalDepthScore { get; }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="source"></param>
        public CentralitySingleSourceSubgraph(TGraph graph, TVertex source, Dictionary<TVertex, int> verticesToIndices, int[] subGraphVerticesIndex)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _graph = graph;
            _source = source;
            _subIndices = subGraphVerticesIndex;

            //_vertices = new TVertex[subGraphVerticesIndex.Length];

            _vertices = _graph.Vertices.ToArray();
            _nodesToIndices = verticesToIndices;

            // Instantiate all the containers withe vertices count as the initial capacity. 
            // For some fields, minHeap and stack, the maxisum capacity is the vertices count.
            // When part of the subgraphs are disconnected to graph, the vertices count of shortest path tree will be less than the graph.verticescount.

            _predecessors = new Dictionary<int, LinkedList<int>>(_subIndices.Length);
            _minPriorityQueue = new MinPriorityQueue<int, double>(_subIndices.Length);
            _distance = new Dictionary<int, double>(_subIndices.Length);


            BetweennessScore = new Dictionary<TVertex, double>(_subIndices.Length);

            // stack.Count may less than vertices count.
            stack = new Stack<int>(_subIndices.Length);
            // sigma and delta are for all the vertices, therefore they must have same length.
            sigma = new Dictionary<int, int>(_subIndices.Length);
            delta = new Dictionary<int, double>(_subIndices.Length);

            Initialize(_subIndices);
            Dijkstra();
            Accumulation();

            TotalDepthScore = GetTotalDepth();
            CloesenessScore = GetCloseness();
        }


        private void Initialize(int[] subVerticesIndex)
        {
            for (int i = 0; i < subVerticesIndex.Length; i++)
            {
                var vertexIndex = subVerticesIndex[i];
                var vertex = _vertices[subVerticesIndex[i]];

                _distance.Add(vertexIndex, _infinity);
                _predecessors.Add(vertexIndex, new LinkedList<int>());
                BetweennessScore.Add(vertex, 0.0);

                sigma.Add(vertexIndex, 0);
                delta.Add(vertexIndex, 0d);
            }

            var sourceIndx = _nodesToIndices[_source];

            _distance[sourceIndx] = 0;
            _minPriorityQueue.Enqueue(sourceIndx, 0);
            _predecessors[sourceIndx].AddLast(sourceIndx);

            sigma[sourceIndx] = 1;
        }


        /// <summary>
        /// The Dijkstra's algorithm for one single source to all the destinations.
        /// CurrentVertex is v in graph theory, while adjacentVertex is w .
        /// </summary>
        private void Dijkstra()
        {
            while (!_minPriorityQueue.IsEmpty)
            {
                var currentVertexIndex = _minPriorityQueue.DequeueMin();

                var predecessors = _predecessors[currentVertexIndex];
                foreach (var pre in predecessors)
                {
                    if (pre == currentVertexIndex)
                    {
                        continue;
                    }

                    sigma[currentVertexIndex] += sigma[pre];
                }

                stack.Push(currentVertexIndex);

                var currentVertex = _vertices[currentVertexIndex];
                var outgoingEdges = _graph.OutgoingEdges(currentVertex);

                foreach (var outgoingEdge in outgoingEdges)
                {
                    var adjacentIndex = _nodesToIndices[outgoingEdge.Destination];

                    if (!_subIndices.Contains(adjacentIndex))
                        continue;

                    // The conditional operator ?:, also known as the ternary conditional operator.
                    var dist = _distance[currentVertexIndex] != _infinity ? _distance[currentVertexIndex] + outgoingEdge.Weight : _infinity;

                    if (dist < _distance[adjacentIndex])
                    {
                        // update distTo and edgeTo
                        _distance[adjacentIndex] = dist;
                        // update sigma, becasue of finding a new shortest path to adjacent node.
                        sigma[adjacentIndex] = 0;

                        // Find the shorter path, therefore we need to update the predecessors by cleaning the linkedlist.
                        _predecessors[adjacentIndex].Clear();
                        _predecessors[adjacentIndex].AddLast(currentVertexIndex);

                        if (_minPriorityQueue.Contains(adjacentIndex))
                        {
                            _minPriorityQueue.UpdatePriority(adjacentIndex, dist);
                        }
                        else
                        {
                            _minPriorityQueue.Enqueue(adjacentIndex, dist);
                        }

                    }
                    // Handle equal distance. Meaning there are multiply shortest paths to vertex w.
                    else if (dist == _distance[adjacentIndex])
                    {
                        sigma[adjacentIndex] += sigma[currentVertexIndex];
                        _predecessors[adjacentIndex].AddLast(currentVertexIndex);
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


        private double GetTotalDepth()
        {
            double d = 0;

            foreach (var item in _distance)
            {
                if (item.Value != _infinity)
                    d += item.Value;
            }
            return d;
        }


        private double GetCloseness()
        {
            if (TotalDepthScore == 0 || _graph.VerticesCount <= 1)
                return 0;
            else
            {
                double scale = (_subIndices.Length - 1) * 1.0 / (_graph.VerticesCount - 1);
                return 1 / TotalDepthScore * scale;
            }
        }
    }
}
