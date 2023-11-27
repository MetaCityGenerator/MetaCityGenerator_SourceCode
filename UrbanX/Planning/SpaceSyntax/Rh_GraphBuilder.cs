using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;

using MetaCity.DataStructures.Graphs;


namespace MetaCity.Planning.SpaceSyntax
{


    [Obsolete("Using Nts_GraphBuilder3D.")]
    /// <summary>
    /// Construct an undirected weighted graph for space syntax based on Rhino geometry.
    /// Road segments are the vertices V in graph, their connections are the edge E in graph.
    /// <para>Two ways for edge weight. one is length , another is the angle between two segments.</para>
    /// </summary>
    public class Rh_GraphBuilder
    {
        // Tolerance for defining the equality of two points.
        private readonly double _tolerance;

        private readonly Curve[] _CurvesList;


        private readonly int[] _segmentVertices;

        private readonly Dictionary<Point3d, Stack<int>> _adjacentSegments;


        public UndirectedWeightedSparseGraph<int> MetricGraph { get; }
        public UndirectedWeightedSparseGraph<int> AngularGraph { get; }

        public Rh_GraphBuilder(Curve[] curves, double tolerance)
        {
            // The minimum tolerance should be 1E-8.
            _tolerance = tolerance < 1E-8 ? 1E-8 : tolerance;

            PointEqualityComparer comparer = new PointEqualityComparer(_tolerance);

            // Curves should be already cleared during preparation stage.
            _CurvesList = curves;

            _segmentVertices = new int[_CurvesList.Length];
            _adjacentSegments = new Dictionary<Point3d, Stack<int>>(comparer);

            MetricGraph = new UndirectedWeightedSparseGraph<int>(_CurvesList.Length);
            AngularGraph = new UndirectedWeightedSparseGraph<int>(_CurvesList.Length);
        }


        

        public void Build()
        {
            // Add all vertices of graph to collection.
            // Using indices to represent the vertices instead of using objects itselves.
            // Objects(curves) can be queried later by using indices.
            Span<Curve> lsSpan = new Span<Curve>(_CurvesList);

            for (int i = 0; i < lsSpan.Length; i++)
            {
                _segmentVertices[i] = i;

                Point3d[] endsPts = {lsSpan[i].PointAtStart, lsSpan[i].PointAtEnd };
                foreach (var pt in endsPts)
                {
                    if (!_adjacentSegments.ContainsKey(pt))
                    {
                        _adjacentSegments.Add(pt, new Stack<int>());
                    }

                    _adjacentSegments[pt].Push(i);
                }
            }

            BuildingGraph(lsSpan);
        }



      

        // For space syntax, we use undirected and weighted sparse graph.
        private void BuildingGraph(Span<Curve> lsSpan)
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
                        var lengthWeight = (lsSpan[v].GetLength() + lsSpan[w].GetLength()) * 0.5;
                        MetricGraph.AddEdge(v, w, lengthWeight);

                        // For angular weight.
                        // Current point is pt, current segment is v.
                        Vector3d v1, v2;
                        if (pt == lsSpan[v].PointAtStart)
                        {
                            v1 = new Vector3d(pt.X - lsSpan[v].PointAtEnd.X, pt.Y - lsSpan[v].PointAtEnd.Y, pt.Z - lsSpan[v].PointAtEnd.Z);
                        }
                        else
                        {
                            v1 = new Vector3d(pt.X - lsSpan[v].PointAtStart.X, pt.Y - lsSpan[v].PointAtStart.Y, pt.Z - lsSpan[v].PointAtStart.Z);
                        }

                        if (pt == lsSpan[w].PointAtStart)
                        {
                            v2 = new Vector3d(lsSpan[w].PointAtEnd.X - pt.X, lsSpan[w].PointAtEnd.Y - pt.Y, lsSpan[w].PointAtEnd.Z - pt.Z);
                        }
                        else
                        {
                            v2 = new Vector3d(lsSpan[w].PointAtStart.X - pt.X, lsSpan[w].PointAtStart.Y - pt.Y, lsSpan[w].PointAtStart.Z - pt.Z);
                        }

                        // In space syntax methodology, angular weight is from 0 to 2.(0~pi)
                        var ang = GetAngleOfTwoVectors(v1, v2);
                        var angularWeight = 2.0 / Math.PI * ang;

                        AngularGraph.AddEdge(v, w, Math.Round(angularWeight, 6));
                    }
                }
            }
        }



        /// <summary>
        /// Helper method to find the closest point by using RTree.
        /// needles can hold multiple test points.
        /// result contains the one of the most cloest point for each test point respectively.
        /// </summary>
        /// <param name="allNodes"></param>
        /// <param name="needles"></param>
        /// <returns></returns>
        private int[] FindCloestPoints(IList<Point3d> allNodes, IList<Point3d> needles)
        {
            var indicesArray = RTree.Point3dClosestPoints(allNodes, needles, double.MaxValue).ToArray();

            int[] result = new int[indicesArray.Length];
            for (int i = 0; i < indicesArray.Length; i++)
            {
                result[i] = indicesArray[i].First();
            }

            return result;
        }


        private double GetAngleOfTwoVectors(Vector3d v, Vector3d u)
        {
            //double cosine = v1 * v2 / (v1.Length * v2.Length);

            //// Convert radiant to degree. Math.Acos return 0~pi 
            ////return Math.Acos(cosine) / Math.PI * 180;
            //return Math.Acos(cosine);

            //v • u =|𝐯||u| cos𝜃
            //var d = v.Dot(u);
            //var l = v.Length() * u.Length();

            v.Unitize();
            u.Unitize();

            // the result is the abosolute value of angle, dispite the direction between to vectors.
            // due to the floating, dot may larger or smaller than 1 or -1.
            var dot = Math.Round(v*u, 9);
            dot = dot > 1 ? 1 : dot;
            dot = dot < -1 ? -1 : dot;

            return Math.Acos(dot);
        }

        // Comparer is a good way to handle tolerance errors. 
        // With a certain tolerance, points are considered as equal.
        private class PointEqualityComparer : EqualityComparer<Point3d>
        {
            private readonly double _epsilon;

            private readonly int _round;

            public PointEqualityComparer(double epsilon)
            {
                _epsilon = epsilon;
                _round = (int)Math.Log10(1 / _epsilon);
            }

            public override bool Equals(Point3d x, Point3d y)
            {
                return x.Equals(y) || x.EpsilonEquals(y, _epsilon);
            }

            public override int GetHashCode(Point3d obj)
            {
                var pt = RoundPoint(obj);
                return pt.GetHashCode();
            }

            private Point3d RoundPoint(Point3d pt)
            {
                return new Point3d(Math.Round(pt.X, _round), Math.Round(pt.Y, _round), Math.Round(pt.Z, _round));
            }
        }
    }
}
