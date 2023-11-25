using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;

namespace UrbanX.Planning.UrbanDesign
{
    /// <summary>
    /// The input curve prefers convex polygon.
    /// </summary>
    public class SiteBoundingRect : IDisposable
    {
        private readonly Curve _site;

        private Line[] _largerEdges;

        private double[] _scores;

        // Storing the indices for the edges that need to compare the scores.
        private int[] _scoreEdgesIndices;

        private bool _reverse;

        /// <summary>
        /// Corner points are in counter clock wise order.
        /// </summary>
        public Line[] Edges { get; private set; }


        /// <summary>
        /// Scores represent the accessibility for each edge of rectange. Inheritate from the betweenness of roads around site.
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="radiant"></param>
        /// <param name="scores"></param>
        public SiteBoundingRect(Curve curve, double radiant, double[] scores)
        {
            _site = curve.DuplicateCurve();
            _scores = scores;
            _reverse = false;

            Edges = new Line[4];
            _largerEdges = new Line[2];
            _scoreEdgesIndices = new int[2];

            Edges = GetEdges(curve, radiant);
            GetLagerEdges();
        }


        /// <summary>
        /// Helper method for getting the bounding box edges for geometry.
        /// </summary>
        /// <param name="geo"></param>
        /// <param name="radiant"></param>
        /// <returns></returns>
        public static Line[] GetEdges(GeometryBase geo, double radiant)
        {
            Line[] result = new Line[4];

            Plane worldxy = Plane.WorldXY;
            Plane origin = worldxy.Clone();
            worldxy.Rotate(radiant, worldxy.ZAxis);
            var transform = Transform.ChangeBasis(origin, worldxy);

            // BoundingBox is in orientation plane coordinate. Need to locate in world coordinate.
            var box = geo.GetBoundingBox(transform);

            // Should be planary rectangle. Locate box in world coordinate.
            Box locateBox = new Box(worldxy, new Interval(box.Min.X, box.Max.X), new Interval(box.Min.Y, box.Max.Y), new Interval(0, 0));

            var pts = locateBox.GetCorners().ToList().GetRange(0, 4);

            for (int i = 0; i < 4; i++)
            {
                if (i == 3)
                {
                    result[i] = new Line(pts[i], pts[0]);
                    break;
                }
                result[i] = new Line(pts[i], pts[i + 1]);
            }

            return result;
        }


        /// <summary>
        /// Getting the larger edges for cutting, comparing the score for the rest two edges.
        /// Determine whether should reverse order.
        /// </summary>
        private void GetLagerEdges()
        {

            if (Edges[0].Length >= Edges[1].Length)
            {
                _largerEdges[0] = Edges[0];
                _largerEdges[1] = Edges[2];

                // Edges for comparing accessibiltiy scores.
                _scoreEdgesIndices[0] = 3;
                _scoreEdgesIndices[1] = 1;
            }
            else
            {
                _largerEdges[0] = Edges[1];
                _largerEdges[1] = Edges[3];

                _scoreEdgesIndices[0] = 0;
                _scoreEdgesIndices[1] = 2;
            }

            // Left child has higher score in term of accessibilty. Ratio = left.score / (left.score + right.score)
            if (_scores[_scoreEdgesIndices[0]] < _scores[_scoreEdgesIndices[1]])
            {
                // left score < right score, reverse
                _reverse = true;
            }
        }


        /// <summary>
        /// Helper method for splitting brep.
        /// </summary>
        /// <param name="ratio"></param>
        /// <returns></returns>
        private Line GetSplitLine(double ratio)
        {
            if (_reverse)
            {
                // if reverse is true.
                ratio = 1 - ratio;
            }

            var l0 = _largerEdges[0];
            var l1 = _largerEdges[1];

            Point3d spt0 = new Point3d(l0.FromX * (1 - ratio) + l0.ToX * ratio, l0.FromY * (1 - ratio) + l0.ToY * ratio, 0);
            Point3d spt1 = new Point3d(l1.ToX * (1 - ratio) + l1.FromX * ratio, l1.ToY * (1 - ratio) + l1.FromY * ratio, 0);

            return new Line(spt0, spt1);
        }

        /// <summary>
        /// To find orientation of ordered triplet.
        /// 1 --> Clockwise
        /// 2 --> Counterclockwise
        /// 3 --> Share same end points
        /// </summary>
        /// <param name="line"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private double Orientation(Line line, Point3d c)
        {

            Vector2d A = new Vector2d(c.X - line.ToX, c.Y - line.ToY);
            Vector2d B = new Vector2d(c.X - line.FromX, c.Y - line.FromY);

            if (A.Length == 0 || B.Length == 0)
                return 3;

            var val = (A.X * B.Y - A.Y * B.X) / (A.Length * B.Length);

            // clock for 1 ; counterclock for 2
            return (val > 0) ? 1 : 2;
        }

        /// <summary>
        /// Splitting brep and return the breps in the order that first item has higher priority.
        /// For most cases, there will generate two breps after splitting. However, returning multiple breps is still possible and the exact number of breps' count can not be determined.
        /// In the degenerate cases, this method only handle the situation that the result of splitting returning  more than two breps.
        /// </summary>
        /// <param name="ratio"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public Curve[] SplitPlanarCurveFace(double ratio, double tolerance)
        {
            // left part brep has higher score which should be correspoinding to left child in BSPTree.

            var line = GetSplitLine(ratio);

            // The first splitted brep is locate at the right side of cutter line.
            List<Curve> cutter = new List<Curve>() { line.ToNurbsCurve() };
            Brep brep = Brep.CreatePlanarBreps(_site, tolerance)[0];


            // Using BrepFace to split.
            var brepFaces = brep.Faces.First().Split(cutter, tolerance).Faces.ToArray();

            Curve[] result = new Curve[2];


            List<Curve> leftSide = new List<Curve>();
            List<Curve> rightSide = new List<Curve>();
            var comparer = new CurveAreaComparer();

            // First, dividing all the breps in two parts by determing which side is the brep locating against the line.
            for (int i = 0; i < brepFaces.Length; i++)
            {
                var temp = brepFaces[i];
                if (temp == null)
                    continue;

                var loop = temp.OuterLoop.To3dCurve();

                if (!loop.IsClosed || !loop.IsValid)
                {

                    var pl = loop.ToNurbsCurve().Points.ControlPolygon();
                    pl.SetAllZ(0);

                    List<Point3d> pts = new List<Point3d>(pl);

                    if (!pts[pts.Count - 1].Equals(pts[0]))
                        pts.Add(pts[0]);

                    loop = new PolylineCurve(pts);
                }

                var o = Orientation(line, AreaMassProperties.Compute(loop).Centroid);

                if (o == 3)
                    continue;
                else if (o == 1)
                    rightSide.Add(loop);
                else
                    leftSide.Add(loop);
            }
            // Sorting both lists based on the area of each brep.
            rightSide.Sort(comparer);
            leftSide.Sort(comparer);

            if (_reverse)
            {
                result[0] = rightSide.Last();
                result[1] = leftSide.Last();
            }
            else
            {
                result[0] = leftSide.Last();
                result[1] = rightSide.Last();
            }
            return result;
        }


        public double[][] GetChildsScores()
        {
            double k = 0.3;

            double[] scorePart0, scorePart1;


            if (_scoreEdgesIndices[0] == 0)
            {
                // CCW-order: [0,1,mid,3]，[mid,1,2,3]
                scorePart0 = new double[] { _scores[0], _scores[1], (_scores[1] + _scores[3]) * k, _scores[3] };
                scorePart1 = new double[] { (_scores[1] + _scores[3]) * k, _scores[1], _scores[2], _scores[3] };
            }
            else
            {
                // CCW-order: [0,mid,2,3]，[0,1,2,mid]
                scorePart0 = new double[] { _scores[0], (_scores[0] + _scores[2]) * k, _scores[2], _scores[3] };
                scorePart1 = new double[] { _scores[0], _scores[1], _scores[2], (_scores[0] + _scores[2]) * k };
            }

            if (_reverse)
            {
                return new double[][] { scorePart1, scorePart0 };
            }
            return new double[][] { scorePart0, scorePart1 };
        }

        public void Dispose()
        {
            _site.Dispose();
            _largerEdges = null;
            _scores = null;
            _scoreEdgesIndices = null;

            Edges = null;
        }

        private class BrepAreaComparer : IComparer<Brep>
        {
            public int Compare(Brep x, Brep y)
            {
                return x.GetArea().CompareTo(y.GetArea());
            }
        }


        private class CurveAreaComparer : IComparer<Curve>
        {
            public int Compare(Curve x, Curve y)
            {
                var xArea = AreaMassProperties.Compute(x).Area;
                var yArea = AreaMassProperties.Compute(y).Area;
                return xArea.CompareTo(yArea);
            }
        }
    }

}
