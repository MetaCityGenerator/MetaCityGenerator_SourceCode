using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;

using MetaCity.DataStructures.Graphs;
using MetaCity.Planning.Utility;

namespace MetaCity.Assessment.SpatialAnalysis
{
    /// <summary>
    /// Construct an <see cref="UndirectedWeightedSparseGraph{T}"/> for space syntax calculation.
    /// Road segments are the vertices V in graph, their connections are the edges E in graph.
    /// <para>Two ways for getting edge weight: one is segment length , another is the angle between two segments.</para>
    /// </summary>
    public class VisualGraphBuilderWithAngular
    {
        private readonly LineString[] _lineStrings;

        private readonly double[] _lineScores;

        private readonly double _visualWeight;
        private readonly double _angularWeight;
        private readonly double _lengthWeight;



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
   

        public VisualGraphBuilderWithAngular(MultiLineString curves , double[] lineScores, double[] weights)
        {
            _lineStrings = GeometryFactory.ToLineStringArray(curves.Geometries);
            _lineScores = lineScores;

            _segmentVertices = new int[_lineStrings.Length];
            _adjacentSegments = new Dictionary<Point, Stack<int>>(_lineStrings.Length * 2);

            _visualWeight = weights[0];
            _angularWeight = weights[1];

            var lengthweigh = 1 - _visualWeight - _angularWeight;
            _lengthWeight = lengthweigh < 0? 0 : lengthweigh;

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
                        // get metric weight and metric edge to graph.
                        var lengthWeight = (lsSpan[v].Length + lsSpan[w].Length) * 0.5;
                        MetricGraph.AddEdge(v, w, Math.Round(lengthWeight, 6));


                        // get angular weight, supports 2d.
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
                        var angularWeight = 1.0 / Math.PI * v1.AngleBetween(v2); // getting the angle between to vectors.
                        // important: NTS Vector2D.Angle(v) getting the absolute value to the x axis, while NTS Vector2D.AngleTo(v) may getting the negtive value due to the direction.



                        // 如果不需要把angle 转换成 0～2 的数值的话，上面那个公式可去掉2/pi
                        // 下面的visualweight是直接按照你提供的每个路段的值得到的，如果要加权angular的话，直接在后面搞就好。



                        var visualWeight = (scoreSpan[v] + scoreSpan[w]) * 0.5*_visualWeight+ angularWeight*_angularWeight + lengthWeight*_lengthWeight; // 在后面加别的权重。
                        VisualGraph.AddEdge(v, w, Math.Round(visualWeight, 6));
                    }
                }
            }
        }
    }
}
