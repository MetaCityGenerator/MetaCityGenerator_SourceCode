using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MetaCity.DataStructures.Graphs;
using MetaCity.DataStructures.Heaps;
using NetTopologySuite.Utilities;

namespace MetaCity.Algorithms.Graphs
{
    public enum GraphType: sbyte
    {
        Metric,
        Angular,
        Other,   
    }


    public sealed class CalculateCentrality3D
    {
        private readonly GraphType _gt;
        private readonly bool _recordSubGraph = false;
        private readonly bool _hasSubGraph = false;

        /// <summary>
        /// For space syntax, _subGraphs is essential for finding the clusters in angular computation. (inptr will always copy once)
        /// </summary>
        private readonly int[][] _subGraphs; // *** default is null.

        /// <summary>
        /// For space syntax, radius is essential for finding the clusters.
        /// </summary>
        private readonly double _radius; //*** default is postiveinfinity.

        private readonly double[] _radii;

        private SpaceSyntaxGraph _graph;

        /// <summary>
        /// The total betweenness centrality for every vertex in graph. Index represents the vertex.
        /// </summary>
        public double[] Betweenness { get; }


        /// <summary>
        /// THe total distance(depths) for every single vertex in a graph. Index represents the vertex.
        /// </summary>
        public double[] TotalDepths { get; }

        /// <summary>
        /// Node count should be a integer, Index represents the vertex. 
        /// Node count is the the number of nodes both directly and indirectly connected to source (include source itself).
        /// </summary>
        public int[] NodeCounts { get; }

        public int[][] SubGraphs { get; }


        public CalculateCentrality3D(in SpaceSyntaxGraph graph, GraphType gt,in int[][] subGraphs) : this(in graph, gt, double.PositiveInfinity,in subGraphs) { }

        public CalculateCentrality3D(in SpaceSyntaxGraph graph, GraphType gt, in double[] radii, in int[][] subGraphs = null)
        {
                _recordSubGraph = true;
            if (subGraphs != null)
                _hasSubGraph = true;


            _gt = gt;
            _radii= radii; // need to get value for value type.
            _subGraphs = subGraphs; // default is null.

            Betweenness = new double[graph.VerticesCount];
            TotalDepths = new double[graph.VerticesCount];
            NodeCounts = new int[graph.VerticesCount];

            if (_recordSubGraph)
                SubGraphs = new int[graph.VerticesCount][];

            var vertices = graph.Vertices;
            var edges = graph.Edges;

            Computing(graph, vertices, edges, radii);
        }


        public CalculateCentrality3D(in SpaceSyntaxGraph graph,GraphType gt ,in double radius = double.PositiveInfinity,in int[][] subGraphs = null)
        {
            if (radius < double.PositiveInfinity)
                _recordSubGraph = true;
            if (subGraphs != null)
                _hasSubGraph = true;


            _gt = gt;
            _radius = radius; // need to get value for value type.
            _subGraphs = subGraphs; // default is null.

            Betweenness = new double[graph.VerticesCount];
            TotalDepths = new double[graph.VerticesCount];
            NodeCounts = new int[graph.VerticesCount];

            if(_recordSubGraph)
                SubGraphs = new int[graph.VerticesCount][];

            var vertices = graph.Vertices;
            var edges = graph.Edges;

            Computing(graph, vertices, edges);
        }

        public CalculateCentrality3D(in SpaceSyntaxGraph graph,GraphType gt , bool calc, in double radius = double.PositiveInfinity, in int[][] subGraphs = null)
        {
            if (radius < double.PositiveInfinity)
                _recordSubGraph = true;
            if (subGraphs != null)
                _hasSubGraph = true;

            _graph = graph;
            _gt = gt;
            _radius = radius; // need to get value for value type.
            _subGraphs = subGraphs; // default is null.

            if(_recordSubGraph)
                SubGraphs = new int[graph.VerticesCount][];
        }

        public int[] FindShortestPath(int ori, int des)
        {
            var centrality = new CentralitySingleSourceS(_graph, _graph.Vertices, _graph.Edges, ori, des, _radius, _gt);

            int desIndex = des;
            List<int> pathIndices = new List<int>();
            while (ori != desIndex)
            {
                pathIndices.Add(desIndex);
                var temp = centrality.PathIndices[desIndex].First;
                desIndex = temp.Value;
            }
            pathIndices.Add(ori);
            pathIndices.Reverse();

            return pathIndices.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graph">The reference for current graph.</param>
        /// <param name="vertices">All the vertices within radius, may less than graph.VerticesCount.</param>
        /// <param name="edges">All the edges in current graph.</param>
        private void Computing( SpaceSyntaxGraph graph, int[] vertices, SpaceSyntaxEdge[] edges)
        {
            if (graph.VerticesCount >= 30)
            {

                Parallel.ForEach(vertices, source =>
                {
                    var boundedVertices = _hasSubGraph ? _subGraphs[source] : vertices;
                    var centrality = new CentralitySingleSourceS(graph,  boundedVertices, edges, source, _radius, _gt); // can not using in ref out in parallel.
                    var tempScore = centrality.BetweennessScore;

                    if (_recordSubGraph)
                    {
                        // Get sub_graphs
                        SubGraphs[source] = centrality.VerticesWithinRadius;
                    }

                    for (int i = 0; i < boundedVertices.Length; i++) // boundedvertices has the same order and lengh with tempscore.
                    {
  
                        lock (Betweenness)
                        {
                            Betweenness[boundedVertices[i]] += tempScore[i];
                        }
                    }

                    TotalDepths[source] = centrality.TotalDepthScore; // each thread the source is different, therefore we can use array to update value.
                    NodeCounts[source] = centrality.NodeCount;
                });
            }
            else
            {
                foreach (var source in vertices)
                {
                    var boundedVertices = _hasSubGraph ? _subGraphs[source] : vertices;

                    var centrality = new CentralitySingleSourceS(graph,boundedVertices, edges, source, _radius, _gt);
                    var tempScore = centrality.BetweennessScore;

                    if (_recordSubGraph)
                    {
                        // Get sub_graphs
                        SubGraphs[source] = centrality.VerticesWithinRadius;
                    }


                    for (int i = 0; i < boundedVertices.Length; i++) // boundedvertices has the same order and lengh with tempscore.
                    {
                        Betweenness[boundedVertices[i]] += tempScore[i];
                    }

                    TotalDepths[source] = centrality.TotalDepthScore;
                    NodeCounts[source] = centrality.NodeCount;
                }
            }
        }

        private void Computing(SpaceSyntaxGraph graph, int[] vertices, SpaceSyntaxEdge[] edges, double[] radii)
        {
            if (graph.VerticesCount >= 30)
            {

                Parallel.ForEach(vertices, source =>
                {
                    var boundedVertices = _hasSubGraph ? _subGraphs[source] : vertices;
                    var centrality = new CentralitySingleSourceS(graph, boundedVertices, edges, source, radii, _gt); // can not using in ref out in parallel.
                    var tempScore = centrality.BetweennessScore;

                    if (_recordSubGraph)
                    {
                        // Get sub_graphs
                        SubGraphs[source] = centrality.VerticesWithinRadius;
                    }

                    for (int i = 0; i < boundedVertices.Length; i++) // boundedvertices has the same order and lengh with tempscore.
                    {

                        lock (Betweenness)
                        {
                            Betweenness[boundedVertices[i]] += tempScore[i];
                        }
                    }

                    TotalDepths[source] = centrality.TotalDepthScore; // each thread the source is different, therefore we can use array to update value.
                    NodeCounts[source] = centrality.NodeCount;
                });
            }
            else
            {
                foreach (var source in vertices)
                {
                    var boundedVertices = _hasSubGraph ? _subGraphs[source] : vertices;

                    var centrality = new CentralitySingleSourceS(graph, boundedVertices, edges, source, radii, _gt);
                    var tempScore = centrality.BetweennessScore;

                    if (_recordSubGraph)
                    {
                        // Get sub_graphs
                        SubGraphs[source] = centrality.VerticesWithinRadius;
                    }


                    for (int i = 0; i < boundedVertices.Length; i++) // boundedvertices has the same order and lengh with tempscore.
                    {
                        Betweenness[boundedVertices[i]] += tempScore[i];
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
    internal sealed class CentralitySingleSourceS
    {
        private const float _infinity = float.PositiveInfinity;

        /// <summary>
        /// Using this to store and query vertex's id. 
        /// Key is vertex which may be random integer belong total vertices count, 
        /// while value is the index for all the array member in this class.
        /// This field can also be used as a hashset to check if vertex is out of subgraph.
        /// By implementing this dictionary, can save some memories comparing to make all the members as dictionary.
        /// </summary>
        private readonly Dictionary<int, int> VERTEX_TO_INDEX;


        private readonly LinkedList<int>[] _predecessors; // [] is the INDEX ; LinkedList<int> is VERTEX.

        // _distance[v] is the length from s to v.
        // The largest item in _distance represents the furthest node.
        // The sum of all distance except infinity is the total depth.
        private readonly float[] _distance;// [] is the INDEX ;


        private readonly MinPriorityQueue<int, float> _minPriorityQueue; // key is the VERTEX ; priority is the distance.


        // Fields for betweenness calculation.
        private readonly Stack<int> stack; // int in stack is vertex.
        private readonly int[] sigma; // [] is the INDEX ;
        private readonly float[] delta; // [] is the INDEX ;


        /// <summary>
        /// The partial result of betweenness centrality.
        /// </summary>
        public float[] BetweennessScore { get; } // using float to save memory. For final result, still use double.


        /// <summary>
        /// Total depth equals to the sum of all the distances.
        /// </summary>
        public float TotalDepthScore { get; }


        /// <summary>
        /// Node count is the the number of nodes both directly and indirectly connected to source (include source itself).
        /// </summary>
        public int NodeCount { get; }

        public LinkedList<int>[] PathIndices => _predecessors;


        /// <summary>
        /// Storing all the vertices which are within the radius to the source node.
        /// </summary>
        public int[] VerticesWithinRadius { get; }

        public CentralitySingleSourceS(SpaceSyntaxGraph graph,int[] vertices, SpaceSyntaxEdge[] edges, int source, double radius, GraphType gt ) // should be the only constructor. vertices == subid or vrtices == graph.vertices.
        {
            if (source < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(source));
            }

            VERTEX_TO_INDEX = new Dictionary<int, int>(vertices.Length);
            // Instantiate all the containers with vertices count as the initial capacity. 
            // For some fields, minHeap and stack, the maxisum capacity is the vertices count.
            // When part of the subgraphs are disconnected to graph, the vertices count of shortest path tree will be less than the graph.verticescount.
            _predecessors = new LinkedList<int>[vertices.Length];
     

            //key int should be vertex id in VERTEX_TO_INDEX, not the vertex itself.
            _minPriorityQueue = new MinPriorityQueue<int, float>(vertices.Length);
            _distance = new float[vertices.Length];

            BetweennessScore = new float[vertices.Length];

            // stack.Count may less than vertices count.
            stack = new Stack<int>(vertices.Length);
            // sigma and delta are for all the vertices, therefore they must have same length.
            sigma = new int[vertices.Length];
            delta = new float[vertices.Length];


            Initialize(in vertices, in source);
            Dijkstra(in graph, in edges, gt,in radius);

            // Copy stack items to VertexIndicesWithRadius here, because during Accumulation stage, 
            // stack will become empty.
            VerticesWithinRadius = stack.ToArray();


            //Stack and minheap using vertex itself , the reset collection using id.

            Accumulation(in source);

            TotalDepthScore = GetTotalDepth(out int nodeCount);
            NodeCount = nodeCount;
        }

        public CentralitySingleSourceS(SpaceSyntaxGraph graph, int[] vertices, SpaceSyntaxEdge[] edges, int source, double[] radii, GraphType gt) // should be the only constructor. vertices == subid or vrtices == graph.vertices.
        {
            if (source < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(source));
            }

            VERTEX_TO_INDEX = new Dictionary<int, int>(vertices.Length);
            // Instantiate all the containers with vertices count as the initial capacity. 
            // For some fields, minHeap and stack, the maxisum capacity is the vertices count.
            // When part of the subgraphs are disconnected to graph, the vertices count of shortest path tree will be less than the graph.verticescount.
            _predecessors = new LinkedList<int>[vertices.Length];


            //key int should be vertex id in VERTEX_TO_INDEX, not the vertex itself.
            _minPriorityQueue = new MinPriorityQueue<int, float>(vertices.Length);
            _distance = new float[vertices.Length];

            BetweennessScore = new float[vertices.Length];

            // stack.Count may less than vertices count.
            stack = new Stack<int>(vertices.Length);
            // sigma and delta are for all the vertices, therefore they must have same length.
            sigma = new int[vertices.Length];
            delta = new float[vertices.Length];


            Initialize(in vertices, in source);
            DijkstraBasedOnRadius(in graph, in edges, gt, in radii);

            // Copy stack items to VertexIndicesWithRadius here, because during Accumulation stage, 
            // stack will become empty.
            VerticesWithinRadius = stack.ToArray();


            //Stack and minheap using vertex itself , the reset collection using id.

            Accumulation(in source);

            TotalDepthScore = GetTotalDepth(out int nodeCount);
            NodeCount = nodeCount;
        }

        public CentralitySingleSourceS(SpaceSyntaxGraph graph, int[] vertices, SpaceSyntaxEdge[] edges, int source, int des, double radius, GraphType gt) // should be the only constructor. vertices == subid or vrtices == graph.vertices.
        {
            if (source < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(source));
            }

            VERTEX_TO_INDEX = new Dictionary<int, int>(vertices.Length);
            // Instantiate all the containers with vertices count as the initial capacity. 
            // For some fields, minHeap and stack, the maxisum capacity is the vertices count.
            // When part of the subgraphs are disconnected to graph, the vertices count of shortest path tree will be less than the graph.verticescount.
            _predecessors = new LinkedList<int>[vertices.Length];


            //key int should be vertex id in VERTEX_TO_INDEX, not the vertex itself.
            _minPriorityQueue = new MinPriorityQueue<int, float>(vertices.Length);
            _distance = new float[vertices.Length];

            BetweennessScore = new float[vertices.Length];

            // stack.Count may less than vertices count.
            stack = new Stack<int>(vertices.Length);
            // sigma and delta are for all the vertices, therefore they must have same length.
            sigma = new int[vertices.Length];
            delta = new float[vertices.Length];


            Initialize(in vertices, in source);
            DijkstraBasedOnIndex(in graph, in edges, gt, in radius);

            //// Copy stack items to VertexIndicesWithRadius here, because during Accumulation stage, 
            //// stack will become empty.
            //VerticesWithinRadius = stack.ToArray();

            ////Stack and minheap using vertex itself , the reset collection using id.

            //Accumulation(in source);

            //TotalDepthScore = GetTotalDepth(out int nodeCount);
            //NodeCount = nodeCount;
        }


        private void Initialize(in int[] vertices,in int source)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                VERTEX_TO_INDEX.Add(vertices[i], i);

                _distance[i]= _infinity;
                _predecessors[i]= new LinkedList<int>(); // for linked list should add the vertex itself.
                BetweennessScore[i]= 0f;

                sigma[i]= 0;
                delta[i]= 0f;
            }

            _minPriorityQueue.Enqueue(source, 0); // key is the vertex itself.
            var sourceId = VERTEX_TO_INDEX[source];

            _distance[sourceId] = 0;
            sigma[sourceId] = 1;
        }



        /// <summary>
        /// The Dijkstra's algorithm for one single source to all the destinations.
        /// CurrentVertex is v in graph theory, while adjacentVertex is w .
        /// </summary>
        private void Dijkstra(in SpaceSyntaxGraph graph, in SpaceSyntaxEdge[] edges, GraphType gt,in double radius)
        {
            while (!_minPriorityQueue.IsEmpty)
            {
                var cVertex = _minPriorityQueue.DequeueMin();
                var cVertexIndex = VERTEX_TO_INDEX[cVertex]; 

                stack.Push(cVertex);

                var edgesId = graph.GetAdjacentEdgesId(cVertex);

                // Find the precessor of current node.
                var predecessors = _predecessors[cVertexIndex];

                foreach (var eId in edgesId)
                {
                    var adjacentVertex = edges[eId].OtherVertex(cVertex);

                    // Check subindices.
                    if (!VERTEX_TO_INDEX.ContainsKey(adjacentVertex))
                        continue;

                    var adjacentIndex = VERTEX_TO_INDEX[adjacentVertex]; 
                    // adjacent node shouldn't be seen.
                    if (stack.Contains(adjacentVertex)) 
                        continue;

                    if (predecessors.Count > 0)
                    {
                        //Has predecessors.
                        // adjacent node shouldn't is one of the predecessor of current node.
                        if (predecessors.Contains(adjacentVertex)) 
                            continue;

                        // Important: For spacesyntax only, predecessor, current node and adjacent node shouldn't form a cycle.
                        int flag = 0;
                        foreach (var pre in predecessors)
                        {
                            if (graph.EdgeExist(pre, adjacentVertex))
                                flag++;
                        }
                        if (flag > 0)
                            continue;
                    }


                    float weight= float.NaN;
                    switch (gt)
                    {
                        case GraphType.Metric:
                            weight = edges[eId].Weights.X;
                            break;
                        case GraphType.Angular:
                            weight = edges[eId].Weights.Y;
                            break;
                        case GraphType.Other:
                            weight = edges[eId].Weights.Z;
                            break;
                    }
                    //var dist = (float)Math.Round(_distance[cVertexIndex] + weight, 6);
                    var dist = _distance[cVertexIndex] + weight;

                    if (dist <= radius) // Handle radius.
                    {
                        if (dist < _distance[adjacentIndex])
                        {
                            // update distTo and edgeTo
                            _distance[adjacentIndex] = dist;


                            if (_minPriorityQueue.Contains(adjacentVertex))
                            {
                                _minPriorityQueue.UpdatePriority(adjacentVertex, dist);
                            }
                            else
                            {
                                _minPriorityQueue.Enqueue(adjacentVertex, dist);
                            }

                            // update sigma, becasue of finding a new shortest path to adjacent node.
                            sigma[adjacentIndex] = sigma[cVertexIndex];

                            // Find the shorter path, therefore we need to update the predecessors by cleaning the linkedlist.
                            _predecessors[adjacentIndex].Clear();
                            _predecessors[adjacentIndex].AddLast(cVertex);
                        }
                        // Handle equal distance. Meaning there are multiply shortest paths to vertex w.
                        else if (dist == _distance[adjacentIndex])
                        {
                            // dist and _distance[adjacentIndex] can not both equal to _infinity, therefore priorityqueue already has a node(adjacentIndex, dist).
                            sigma[adjacentIndex] += sigma[cVertexIndex];
                            _predecessors[adjacentIndex].AddLast(cVertex);
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

        private void DijkstraBasedOnIndex(in SpaceSyntaxGraph graph, in SpaceSyntaxEdge[] edges, GraphType gt, in double radius)
        {
            while (!_minPriorityQueue.IsEmpty)
            {
                var cVertex = _minPriorityQueue.DequeueMin();
                var cVertexIndex = VERTEX_TO_INDEX[cVertex];

                stack.Push(cVertex);
                var edgesId = graph.GetAdjacentEdgesId(cVertex);
                // Find the precessor of current node.
                var predecessors = _predecessors[cVertexIndex];
                foreach (var eId in edgesId)
                {
                    //var eId = des;
                    var adjacentVertex = edges[eId].OtherVertex(cVertex);

                    // Check subindices.
                    if (!VERTEX_TO_INDEX.ContainsKey(adjacentVertex))
                        continue;

                    var adjacentIndex = VERTEX_TO_INDEX[adjacentVertex];
                    // adjacent node shouldn't be seen.
                    if (stack.Contains(adjacentVertex))
                        continue;

                    if (predecessors.Count > 0)
                    {
                        //Has predecessors.
                        // adjacent node shouldn't is one of the predecessor of current node.
                        if (predecessors.Contains(adjacentVertex))
                            continue;

                        // Important: For spacesyntax only, predecessor, current node and adjacent node shouldn't form a cycle.
                        int flag = 0;
                        foreach (var pre in predecessors)
                        {
                            if (graph.EdgeExist(pre, adjacentVertex))
                                flag++;
                        }
                        if (flag > 0)
                            continue;
                    }


                    float weight = float.NaN;
                    switch (gt)
                    {
                        case GraphType.Metric:
                            weight = edges[eId].Weights.X;
                            break;
                        case GraphType.Angular:
                            weight = edges[eId].Weights.Y;
                            break;
                        case GraphType.Other:
                            weight = edges[eId].Weights.Z;
                            break;
                    }
                    //var dist = (float)Math.Round(_distance[cVertexIndex] + weight, 6);
                    var dist = _distance[cVertexIndex] + weight;

                    if (dist <= radius) // Handle radius.
                    {
                        if (dist < _distance[adjacentIndex])
                        {
                            // update distTo and edgeTo
                            _distance[adjacentIndex] = dist;


                            if (_minPriorityQueue.Contains(adjacentVertex))
                            {
                                _minPriorityQueue.UpdatePriority(adjacentVertex, dist);
                            }
                            else
                            {
                                _minPriorityQueue.Enqueue(adjacentVertex, dist);
                            }

                            // update sigma, becasue of finding a new shortest path to adjacent node.
                            sigma[adjacentIndex] = sigma[cVertexIndex];

                            // Find the shorter path, therefore we need to update the predecessors by cleaning the linkedlist.
                            _predecessors[adjacentIndex].Clear();
                            _predecessors[adjacentIndex].AddLast(cVertex);
                        }
                        // Handle equal distance. Meaning there are multiply shortest paths to vertex w.
                        else if (dist == _distance[adjacentIndex])
                        {
                            // dist and _distance[adjacentIndex] can not both equal to _infinity, therefore priorityqueue already has a node(adjacentIndex, dist).
                            sigma[adjacentIndex] += sigma[cVertexIndex];
                            _predecessors[adjacentIndex].AddLast(cVertex);
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

        /// <summary>
        /// The Dijkstra's algorithm for one single source to all the destinations.
        /// CurrentVertex is v in graph theory, while adjacentVertex is w .
        /// </summary>
        private void DijkstraBasedOnRadius(in SpaceSyntaxGraph graph, in SpaceSyntaxEdge[] edges, GraphType gt, in double[] radiis)
        {
            while (!_minPriorityQueue.IsEmpty)
            {
                var cVertex = _minPriorityQueue.DequeueMin();
                var cVertexIndex = VERTEX_TO_INDEX[cVertex];

                stack.Push(cVertex);

                var edgesId = graph.GetAdjacentEdgesId(cVertex);

                // Find the precessor of current node.
                var predecessors = _predecessors[cVertexIndex];

                foreach (var eId in edgesId)
                {
                    var adjacentVertex = edges[eId].OtherVertex(cVertex);

                    // Check subindices.
                    if (!VERTEX_TO_INDEX.ContainsKey(adjacentVertex))
                        continue;

                    var adjacentIndex = VERTEX_TO_INDEX[adjacentVertex];
                    // adjacent node shouldn't be seen.
                    if (stack.Contains(adjacentVertex))
                        continue;

                    if (predecessors.Count > 0)
                    {
                        //Has predecessors.
                        // adjacent node shouldn't is one of the predecessor of current node.
                        if (predecessors.Contains(adjacentVertex))
                            continue;

                        // Important: For spacesyntax only, predecessor, current node and adjacent node shouldn't form a cycle.
                        int flag = 0;
                        foreach (var pre in predecessors)
                        {
                            if (graph.EdgeExist(pre, adjacentVertex))
                                flag++;
                        }
                        if (flag > 0)
                            continue;
                    }


                    float weight = float.NaN;
                    switch (gt)
                    {
                        case GraphType.Metric:
                            weight = edges[eId].Weights.X;
                            break;
                        case GraphType.Angular:
                            weight = edges[eId].Weights.Y;
                            break;
                        case GraphType.Other:
                            weight = edges[eId].Weights.Z;
                            break;
                    }
                    //var dist = (float)Math.Round(_distance[cVertexIndex] + weight, 6);
                    var dist = _distance[cVertexIndex] + weight;

                    double radius = radiis[eId];
                    if (dist <= radius) // Handle radius.
                    {
                        if (dist < _distance[adjacentIndex])
                        {
                            // update distTo and edgeTo
                            _distance[adjacentIndex] = dist;


                            if (_minPriorityQueue.Contains(adjacentVertex))
                            {
                                _minPriorityQueue.UpdatePriority(adjacentVertex, dist);
                            }
                            else
                            {
                                _minPriorityQueue.Enqueue(adjacentVertex, dist);
                            }

                            // update sigma, becasue of finding a new shortest path to adjacent node.
                            sigma[adjacentIndex] = sigma[cVertexIndex];

                            // Find the shorter path, therefore we need to update the predecessors by cleaning the linkedlist.
                            _predecessors[adjacentIndex].Clear();
                            _predecessors[adjacentIndex].AddLast(cVertex);
                        }
                        // Handle equal distance. Meaning there are multiply shortest paths to vertex w.
                        else if (dist == _distance[adjacentIndex])
                        {
                            // dist and _distance[adjacentIndex] can not both equal to _infinity, therefore priorityqueue already has a node(adjacentIndex, dist).
                            sigma[adjacentIndex] += sigma[cVertexIndex];
                            _predecessors[adjacentIndex].AddLast(cVertex);
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

        private void Accumulation(in int source)
        {
            while (stack.Count != 0)
            {
                // w vertex
                var currentVertex = stack.Pop();
                var currentVertexIndex = VERTEX_TO_INDEX[currentVertex];

                float coeff = (float)((1.0 + delta[currentVertexIndex]) / sigma[currentVertexIndex]);

                // Find the predecessors v of current vertex w.
                var predecessors = _predecessors[currentVertexIndex];
                foreach (var pre in predecessors)
                {
                    var preIndex = VERTEX_TO_INDEX[pre];
                    delta[preIndex] += sigma[preIndex] * coeff;
                }

                if (currentVertexIndex != VERTEX_TO_INDEX[source])
                {
                    BetweennessScore[currentVertexIndex] += delta[currentVertexIndex];
                }
            }
        }



        /// <summary>
        /// Helper method for computing the cumulative total of the shortest distance between all nodes(include source itself) to source.
        /// Node count is the the number of nodes both directly and indirectly connected to source(include source itself).
        /// </summary>
        /// <param name="nodeCount"></param>
        /// <returns></returns>
        private float GetTotalDepth(out int nodeCount)
        {
            float dist = 0f;
            nodeCount = 0;

            foreach (var d in _distance)
            {
                // Infinity means unvisited node. 
                if (d != _infinity)
                {
                    dist += d;
                    nodeCount++;
                }
            }

            return dist;
        }
    }
}
