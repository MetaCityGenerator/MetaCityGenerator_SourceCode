using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;

using UrbanX.DataStructures.Graphs;

namespace UrbanX.Planning.FacilityLocation
{
    /// <summary>
    /// Construct an undirected weighted graph for road network.
    /// Nodes and intersections are the vertices V in graph, their connections are the edge E in graph.
    /// This is the main difference to graph used in space syntax.
    /// <para>Nodes are the centroid in a site block. </para>
    /// </summary>
    public class Rh_CityGraphBuilder
    {
        // Tolerance for defining the equality of two points.
        private readonly double _tolerance;

        private readonly Curve[] _curvesList;

        // _pointsSEt can be cancelled, just using dictionary.
        private readonly HashSet<Point3d> _pointsSet;

        public readonly Dictionary<Point3d, int> _pointsToVertices;

        public UndirectedWeightedSparseGraph<int> NetworkofMatricWeight { get; }


        /// <summary>
        /// Input curves should be splitted by all the intersection points.
        /// </summary>
        /// <param name="curves"></param>
        /// <param name="tolerance"></param>
        public Rh_CityGraphBuilder(Curve[] curves, double tolerance)
        {
            // The minimum tolerance should be 1E-8.
            //_tolerance = tolerance * 1E-3 < 1E-8 ? 1E-8 : tolerance * 1E-3;
            _tolerance = tolerance;

            PointEqualityComparer comparer = new PointEqualityComparer(_tolerance);

            // Curves should be already cleared during preparation stage(All curves should be splited).
            // All the roads.
            _curvesList = curves;

            // All the points.
            _pointsSet = new HashSet<Point3d>(comparer);

            _pointsToVertices = new Dictionary<Point3d, int>(comparer);

            NetworkofMatricWeight = new UndirectedWeightedSparseGraph<int>(_curvesList.Length);

            Initialize();
            BuildingGraph();
        }

        public int GetPointVertice(Point3d pt)
        {
            if (_pointsToVertices.ContainsKey(pt))
            {
                return _pointsToVertices[pt];
            }
            else
            {
                // handle the tolerance error.
                Point3d[] needles = { pt };
                var cloestIndex = FindCloestPoints(_pointsSet.ToArray(), needles)[0];
                return _pointsToVertices[_pointsSet.ToArray()[cloestIndex]];
            }
        }


        private void Initialize()
        {
            // Add vertices of graph to collection. Using indices to represent the vertices instead of using objects itselves.
            // Objects(curves) can be queied later by using indices.
            for (int i = 0; i < _curvesList.Length; i++)
            {
                _pointsSet.Add(_curvesList[i].PointAtStart);
                _pointsSet.Add(_curvesList[i].PointAtEnd);
            }

            for (int i = 0; i < _pointsSet.Count; i++)
            {
                var pt = _pointsSet.ToArray()[i];
                _pointsToVertices.Add(pt, i);
            }
        }


        // For road network, we use undirected and weighted sparse graph.
        private void BuildingGraph()
        {
            NetworkofMatricWeight.AddVertices(_pointsToVertices.Values.ToArray());

            // Add weighted edge in graph.

            for (int i = 0; i < _curvesList.Length; i++)
            {
                var curve = _curvesList[i];
                Point3d startNode, endNode;

                if (_pointsToVertices.ContainsKey(curve.PointAtStart) && _pointsToVertices.ContainsKey(curve.PointAtEnd))
                {
                    startNode = curve.PointAtStart;
                    endNode = curve.PointAtEnd;
                }
                else
                {
                    // handle the tolerance error.
                    Point3d[] needles = { curve.PointAtStart, curve.PointAtEnd };
                    var cloestIndex = FindCloestPoints(_pointsSet.ToArray(), needles);

                    startNode = _pointsSet.ToArray()[cloestIndex[0]];
                    endNode = _pointsSet.ToArray()[cloestIndex[1]];
                }

                NetworkofMatricWeight.AddEdge(_pointsToVertices[startNode], _pointsToVertices[endNode], curve.GetLength());
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
        public static int[] FindCloestPoints(IList<Point3d> allNodes, IList<Point3d> needles)
        {
            var indicesArray = RTree.Point3dClosestPoints(allNodes, needles, double.MaxValue).ToArray();

            int[] result = new int[indicesArray.Length];
            for (int i = 0; i < indicesArray.Length; i++)
            {
                result[i] = indicesArray[i].First();
            }

            return result;
        }



        // Comparer is a good way to handle tolerance errors. 
        // With a certain tolerance, points are considered as equal.
        public class PointEqualityComparer : EqualityComparer<Point3d>
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
