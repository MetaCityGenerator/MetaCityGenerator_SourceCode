using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;

using System;
using System.Collections.Generic;

using MetaCity.DataStructures.Graphs;

namespace MetaCity.Planning.SpaceSyntax
{
    
   
    /// <summary>
    /// Construct an <see cref="UndirectedWeightedSparseGraph{T}"/> for space syntax calculation based on NTS geometry.
    /// Road segments are the vertices V in graph, their connections are the edges E in graph.
    /// <para>Two ways for getting edge weight: one is segment length , another is the angle between two segments.</para>
    /// </summary>
    public class GraphBuilder
    {
        private readonly LineString[] _lineStrings;

        /// <summary>
        /// Using index(int) to represent each lineString. Share the same order with _lineStrings array.
        /// </summary>
        private readonly int[] _segmentVertices;

        private readonly Dictionary<Point, Stack<int>> _adjacentSegments;


        public UndirectedWeightedSparseGraph<int> MetricGraph { get; }
        public UndirectedWeightedSparseGraph<int> AngularGraph { get; }


        public GraphBuilder(MultiLineString curves)
        {
            _lineStrings = GeometryFactory.ToLineStringArray(curves.Geometries);
            _segmentVertices = new int[_lineStrings.Length];
            _adjacentSegments = new Dictionary<Point, Stack<int>>(_lineStrings.Length * 2);

            MetricGraph = new UndirectedWeightedSparseGraph<int>(_lineStrings.Length);
            AngularGraph = new UndirectedWeightedSparseGraph<int>(_lineStrings.Length);
        }


        public void Build()
        {
            // Add all vertices of graph to collection.
            // Using indices to represent the vertices instead of using objects itselves.
            // Objects(curves) can be queried later by using indices.

            Span<LineString> lsSpan = new Span<LineString>(_lineStrings);

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

            BuildingGraph(lsSpan);
        }


        // For space syntax, we use undirected and weighted sparse graph.
        private void BuildingGraph(Span<LineString> lsSpan)
        {
            MetricGraph.AddVertices(_segmentVertices);
            AngularGraph.AddVertices(_segmentVertices);

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
                        // For metric weight.
                        //var lengthWeight = Math.Round((lsSpan[v].Length + lsSpan[w].Length) * 0.5, 6);// using round of length will cause several linestring have the same weight.
                        var lengthWeight = (lsSpan[v].Length + lsSpan[w].Length) * 0.5;
                        MetricGraph.AddEdge(v, w, Math.Round(lengthWeight, 6));


                        // For angular weight.
                        // Current point is pt, current segment is v.
                        Vector2D v1, v2;
                        if (pt == lsSpan[v].StartPoint)
                        {
                            //v1 = new Vector2D(pt.X - lsSpan[v].Coordinates[1].X, pt.Y - lsSpan[v].Coordinates[1].Y); // Using adjacent two points.

                            // Using start point and end point.
                            v1 = new Vector2D(pt.X - lsSpan[v].EndPoint.X, pt.Y - lsSpan[v].EndPoint.Y);
                        }
                        else
                        {
                            //var num = lsSpan[v].NumPoints;
                            //v1 = new Vector2D(pt.X - lsSpan[v].Coordinates[num - 1].X, pt.Y - lsSpan[v].Coordinates[num - 1].Y);

                            // pt is end point.
                            v1 = new Vector2D(pt.X - lsSpan[v].StartPoint.X, pt.Y - lsSpan[v].StartPoint.Y);
                        }

                        if (pt == lsSpan[w].StartPoint)
                        {
                            //v2 = new Vector2D(lsSpan[w].Coordinates[1].X - pt.X, lsSpan[w].Coordinates[1].Y - pt.Y);
                            v2 = new Vector2D(lsSpan[w].EndPoint.X - pt.X, lsSpan[w].EndPoint.Y - pt.Y);
                        }
                        else
                        {
                            //var num = lsSpan[w].NumPoints;
                            //v2 = new Vector2D(lsSpan[w].Coordinates[num - 1].X - pt.X, lsSpan[w].Coordinates[num - 1].Y - pt.Y);
                            v2 = new Vector2D(lsSpan[w].StartPoint.X - pt.X, lsSpan[w].StartPoint.Y - pt.Y);
                        }

                        // In space syntax methodology, angular weight is from 0 to 2.(0~pi)
                        var ang = v1.Angle(v2);
                        var angularWeight = 2.0 / Math.PI * ang;

                        AngularGraph.AddEdge(v, w, Math.Round(angularWeight, 6));
                    }
                }
            }
        }
    }
}
