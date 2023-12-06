using NetTopologySuite.Geometries;

using System;
using System.Collections.Generic;

using MetaCity.DataStructures.Graphs;

namespace MetaCity.Assessment.SpatialAnalysis
{
    /// <summary>
    /// Construct an <see cref="UndirectedWeightedSparseGraph{T}"/> for space syntax calculation.
    /// Road segments are the vertices V in graph, their connections are the edges E in graph.
    /// <para>Two ways for getting edge weight: one is segment length , another is the angle between two segments.</para>
    /// </summary>
    public class VisualGraphBuilder
    {
        private readonly LineString[] _lineStrings;

        private readonly double[] _lineScores;


        /// <summary>
        /// Using index(int) to represent each lineString. Share the same order with _lineStrings array.
        /// </summary>
        private readonly int[] _segmentVertices;


        private readonly Dictionary<Point, Stack<int>> _adjacentSegments;

        /// <summary>
        /// If we want to consider radius, metric graph must be built beforehand.
        /// </summary>
        public UndirectedWeightedSparseGraph<int> MetricGraph { get; }


        public UndirectedWeightedSparseGraph<int> VisualGraph { get; }
   

        public VisualGraphBuilder(MultiLineString curves , double[] lineScores)
        {
            _lineStrings = GeometryFactory.ToLineStringArray(curves.Geometries);
            _lineScores = lineScores;

            _segmentVertices = new int[_lineStrings.Length];
            _adjacentSegments = new Dictionary<Point, Stack<int>>(_lineStrings.Length * 2);

            MetricGraph = new UndirectedWeightedSparseGraph<int>(_lineStrings.Length);
            VisualGraph = new UndirectedWeightedSparseGraph<int>(_lineStrings.Length);
        }


        public void Build()
        {
            // Add all vertices of graph to collection.
            // Using indices to represent the vertices instead of using objects itselves.
            // Objects(curves) can be queried later by using indices.

            Span<LineString> lsSpan = new Span<LineString>(_lineStrings);
            Span<double> scoreSpan = new Span<double>(_lineScores);

            for (int i = 0; i < lsSpan.Length; i++)
            {
                _segmentVertices[i] = i;

                Point[] endsPts = { lsSpan[i].StartPoint, lsSpan[i].EndPoint };
                foreach (var pt in endsPts)
                {
                    if (!_adjacentSegments.ContainsKey(pt))
                    {
                        //Linestring already be snapped during data clean stage.
                        _adjacentSegments.Add(pt, new Stack<int>());
                    }

                    _adjacentSegments[pt].Push(i);
                }
            }

            BuildingGraph(lsSpan,scoreSpan);
        }


        // Using undirected and weighted sparse graph.
        private void BuildingGraph(Span<LineString> lsSpan , Span<double> scoreSpan)
        {
            MetricGraph.AddVertices(_segmentVertices);
            VisualGraph.AddVertices(_segmentVertices);

            // Add weighted edge in graph.
            foreach (var pt in _adjacentSegments.Keys)
            {
                var stack = _adjacentSegments[pt];

                // If stack.count == 1 , Vertex is isolated, there is no need to add edge.
                while (stack.Count > 1)
                {
                    var v = stack.Pop();
                    foreach (var w in stack)
                    {
                        var lengthWeight = (lsSpan[v].Length + lsSpan[w].Length) * 0.5;
                        MetricGraph.AddEdge(v, w, Math.Round(lengthWeight, 6));

                        var visualWeight = (scoreSpan[v] + scoreSpan[w]) * 0.5;
                        VisualGraph.AddEdge(v, w, Math.Round(visualWeight, 6));
                    }
                }
            }
        }
    }
}
