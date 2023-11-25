using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Operation.Distance;
using NetTopologySuite.Precision;

using System;
using System.Collections.Generic;

using UrbanX.Planning.Utility;

namespace UrbanX.Planning.UrbanDesign
{
    /// <summary>
    /// Class represent the minimum rectangle of current site. Contains all the method for splitting site.
    /// </summary>
    public sealed class SiteMinimumRectangle
    {
        private readonly Coordinate[] _corners;

        private readonly double[] _scores;


        private int[] _cutEdgesIndices;

        // Storing the indices for the edges that need to compare the scores.
        private int[] _scoreEdgesIndices;


        private bool _reverse = false;

        /// <summary>
        /// If site is horizental means edge[0].Length>edge[1].Length. Therefore cutter rectangle's coordinate is : {0,spt0,spt1,3,0} ;
        /// Othterwise, coordinate's order is : {0,1, spt0, spt1, 0}.
        /// </summary>
        private bool _isSiteHorizental = false;

        /// <summary>
        /// The four line segments for minimum rectangle in CCW order.
        /// </summary>
        public LineSegment[] Edges { get; }


        public SiteMinimumRectangle(Coordinate[] corners, double[] scores)
        {
            Edges = GetEdges(corners);
            _corners = corners;
            _scores = scores;
        }



        /// <summary>
        /// Splitting brep and return the breps in the order that first item has higher priority.
        /// For most cases, there will generate two breps after splitting. However, returning multiple breps is still possible and the exact number of breps' count can not be determined.
        /// In the degenerate cases, this method only handle the situation that the result of splitting returning  more than two breps.
        /// </summary>
        /// <param name="site"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public Polygon[] SplitPolygon(Polygon site, double ratio)
        {
            var gf = PrecisionSetting.GetGeometryFactory(site);
            GeometryPrecisionReducer reducer = new GeometryPrecisionReducer(gf.PrecisionModel);

            PrepareEdges();

            if (_reverse)
            {
                // if reverse is true.
                ratio = 1 - ratio;
            }

            var l0 = Edges[_cutEdgesIndices[0]];
            var l1 = Edges[_cutEdgesIndices[1]];

            Coordinate spt0 = new Coordinate(l0.P0.X * (1 - ratio) + l0.P1.X * ratio, l0.P0.Y * (1 - ratio) + l0.P1.Y * ratio);
            Coordinate spt1 = new Coordinate(l1.P1.X * (1 - ratio) + l1.P0.X * ratio, l1.P1.Y * (1 - ratio) + l1.P0.Y * ratio);

            // This line only use for getting the roads for subsites by snapping end points and buffering them.

            // Create a cutter rectangle.//TODO: add reducer.
            Coordinate[] cutterCoords;

            if (_isSiteHorizental)
            {
                // { 0,spt0,spt1,3,0}
                cutterCoords = new Coordinate[] { _corners[0], spt0, spt1, _corners[3], _corners[0] };
            }
            else
            {
                // {0,1, spt0, spt1, 0}
                cutterCoords = new Coordinate[] { _corners[0], _corners[1], spt0, spt1, _corners[0] };
            }

            var ring = gf.CreateLinearRing(cutterCoords);
            var temp = gf.CreatePolygon(ring);
            Polygon cutter = (Polygon)reducer.Reduce(temp); // cutter must has the same dimension with site. 

            var partA = reducer.Reduce(site.Intersection(cutter)); // partA should has higher score.
            var partB = reducer.Reduce(site.Difference(cutter));

            var extractA = PolygonExtracter.GetPolygons(partA);
            var extractB = PolygonExtracter.GetPolygons(partB);

            Polygon largestA = null, largestB = null;
            double areaA = 0, areaB = 0;

            for (int i = 0; i < extractA.Count; i++)
            {
                if (extractA[i].Area > areaA)
                {
                    largestA = (Polygon)extractA[i];
                    areaA = largestA.Area;
                }
            }

            for (int i = 0; i < extractB.Count; i++)
            {
                if (extractB[i].Area > areaB)
                {
                    largestB = (Polygon)extractB[i];
                    areaB = largestB.Area;
                }
            }


            Polygon[] subPolygons;
            if (_reverse)
            {
                subPolygons = new Polygon[] { largestB, largestA };
            }
            else
            {
                subPolygons = new Polygon[] { largestA, largestB };
            }

            return subPolygons;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="site"></param>
        /// <param name="ratio"></param>
        /// <param name="splitters"></param>
        /// <returns></returns>
        public Polygon[] SplitPolygon(Polygon site, double ratio, out LineString[] splitters)
        {
            var gf = PrecisionSetting.GetGeometryFactory(site);
            GeometryPrecisionReducer reducer = new GeometryPrecisionReducer(gf.PrecisionModel);


            PrepareEdges();

            if (_reverse)
            {
                // if reverse is true.
                ratio = 1 - ratio;
            }

            var l0 = Edges[_cutEdgesIndices[0]];
            var l1 = Edges[_cutEdgesIndices[1]];

            Coordinate spt0 = new Coordinate(l0.P0.X * (1 - ratio) + l0.P1.X * ratio, l0.P0.Y * (1 - ratio) + l0.P1.Y * ratio);
            Coordinate spt1 = new Coordinate(l1.P1.X * (1 - ratio) + l1.P0.X * ratio, l1.P1.Y * (1 - ratio) + l1.P0.Y * ratio);

            // This line only use for getting the roads for subsites by snapping end points and buffering them.
            //var splitLineFromRect = new LineString(new Coordinate[] { spt0, spt1 });

            var splitLineFromRect = gf.CreateLineString(new Coordinate[] { spt0, spt1 });

            var tempGeom = reducer.Reduce(splitLineFromRect);
            splitLineFromRect = (LineString)tempGeom;

            var segments = LineStringExtracter.GetLines(site.Intersection(splitLineFromRect));

            splitters = new LineString[segments.Count];
            for (int i = 0; i < segments.Count; i++)
            {
                splitters[i] = (LineString)segments[i];
            }

            // Create a cutter rectangle.
            Coordinate[] cutterCoords;

            if (_isSiteHorizental)
            {
                // { 0,spt0,spt1,3,0}
                cutterCoords = new Coordinate[] { _corners[0], spt0, spt1, _corners[3], _corners[0] };
            }
            else
            {
                // {0,1, spt0, spt1, 0}
                cutterCoords = new Coordinate[] { _corners[0], _corners[1], spt0, spt1, _corners[0] };
            }

            var ring = gf.CreateLinearRing(cutterCoords);
            var temp = gf.CreatePolygon(ring);
            Polygon cutter = (Polygon)reducer.Reduce(temp); // cutter must has the same dimension with site. 

            var partA = reducer.Reduce(site.Intersection(cutter)); // partA should has higher score.
            var partB = reducer.Reduce(site.Difference(cutter));

            var extractA = PolygonExtracter.GetPolygons(partA);
            var extractB = PolygonExtracter.GetPolygons(partB);

            Polygon largestA = null, largestB = null;
            double areaA = 0, areaB = 0;

            for (int i = 0; i < extractA.Count; i++)
            {
                if (extractA[i].Area > areaA)
                {
                    largestA = (Polygon)extractA[i];
                    areaA = largestA.Area;
                }
            }

            for (int i = 0; i < extractB.Count; i++)
            {
                if (extractB[i].Area > areaB)
                {
                    largestB = (Polygon)extractB[i];
                    areaB = largestB.Area;
                }
            }

            Polygon[] subPolygons;
            if (_reverse)
            {
                subPolygons = new Polygon[] { largestB, largestA };
            }
            else
            {
                subPolygons = new Polygon[] { largestA, largestB };
            }

            return subPolygons;
        }



        /// <summary>
        /// Method for getting the accessibility scores for children polygons. Scores order is in CCW starting from bottom.
        /// </summary>
        /// <returns></returns>
        public double[][] GetChildrenScores()
        {
            // Ratio of child score to parent score.
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



        /// <summary>
        /// Static method for getting the accessibility scores for current site(polygon).
        /// </summary>
        /// <param name="site"></param>
        /// <param name="minRotatedRect"></param>
        /// <param name="rtree"></param>
        /// <param name="roadScores"></param>
        /// <param name="entries"></param>
        /// <returns></returns>
        public static double[] GetScores(Polygon site, Coordinate[] minRotatedRect, STRtree<Geometry> rtree, Dictionary<LineString, double> roadScores, out Point[] entries)
        {
            double[] scores = new double[4];
            entries = new Point[4];

            // Step 1: get the middle points for each edge of the minimum rectangle.
            var midPts = GetEdgesMidPoints(minRotatedRect);

            // Step 2: get the point on site which is closested to each middle point on minimum rectangle.
            var closestPtsOnSite = new Coordinate[midPts.Length];
            for (int i = 0; i < midPts.Length; i++)
            {
                var pt = midPts[i];
                closestPtsOnSite[i] = DistanceOp.NearestPoints(pt, site)[1];
            }

            // Step 3: find the nearest linestring for each cleseted points.
            for (int i = 0; i < closestPtsOnSite.Length; i++)
            {
                var pt = site.Factory.CreatePoint(closestPtsOnSite[i]);
                var key = (LineString)rtree.NearestNeighbour(pt.EnvelopeInternal, pt, new GeometryItemDistance());

                scores[i] = roadScores[key];
                entries[i] = pt;
            }

            return scores;
        }






        /// <summary>
        /// Static method for getting the middle point for each edge of input minimum rectangle.
        /// </summary>
        /// <param name="corners"></param>
        /// <returns></returns>
        public static Point[] GetEdgesMidPoints(Coordinate[] corners)
        {
            Point[] result = new Point[4];

            for (int i = 0; i < 4; i++)
            {
                int w = i; // Current corner.
                int v = i == 3 ? 0 : w + 1; // Next corner.

                result[i] = new Point(new Coordinate(0.5 * (corners[w].X + corners[v].X), 0.5 * (corners[w].Y + corners[v].Y)));
            }

            return result;
        }


        /// <summary>
        /// Static method for validating input site radiant based on a given degree range which should be less than or equal 45 degree. 
        /// </summary>
        /// <param name="radiant"></param>
        /// <param name="degreeRange"></param>
        public static void ValidatingSiteRadiant(ref double radiant, double degreeRange = double.PositiveInfinity)
        {
            degreeRange = Math.Abs(degreeRange) > Math.PI / 4 ? Math.PI / 4 : Math.Abs(degreeRange);

            /*
            An angle, θ, measured in radians, such that -π ≤ θ ≤ π, and tan(θ) = y / x, where (x, y) is a point in the Cartesian plane. Observe the following:
            For (x, y) in quadrant 1, 0 < θ < π/2.
            For (x, y) in quadrant 2, π/2 < θ ≤ π.
            For (x, y) in quadrant 3, -π < θ < -π/2.
            For (x, y) in quadrant 4, -π/2 < θ < 0.
            */

            // Step1: take radiant into quadrant 1 or 2.
            radiant = radiant < 0 ? radiant + Math.PI : radiant;

            // Step2: Transform radiant into first quadrant.
            radiant = radiant > Math.PI * 0.5 ? radiant - Math.PI * 0.5 : radiant;

            // Step3: take radiant into -π/4 ≤ θ ≤ π/4.
            radiant = radiant > Math.PI * 0.25 ? -(Math.PI * 0.5 - radiant) : radiant;

            // Step4: take radiant into input degree range.
            radiant = Math.Abs(radiant) > degreeRange ? radiant / Math.Abs(radiant) * degreeRange : radiant;
        }




        /// <summary>
        /// Getting four edges based on current minimum rectangle's corners.
        /// </summary>
        /// <param name="corners"></param>
        /// <returns></returns>
        private static LineSegment[] GetEdges(Coordinate[] corners)
        {
            LineSegment[] result = new LineSegment[4];

            for (int i = 0; i < 4; i++)
            {
                int w = i; // Current corner.
                int v = i == 3 ? 0 : w + 1; // Next corner.

                result[i] = new LineSegment(corners[w], corners[v]);
            }

            return result;
        }



        /// <summary>
        /// Getting the larger edges for cutting, comparing the score for the rest two edges.
        /// Determine whether should reverse order.
        /// </summary>
        private void PrepareEdges()
        {
            if (Edges[0].Length >= Edges[1].Length)
            {
                _isSiteHorizental = true;

                _cutEdgesIndices = new int[] { 0, 2 };
                _scoreEdgesIndices = new int[] { 3, 1 };
            }
            else
            {
                _cutEdgesIndices = new int[] { 1, 3 };
                _scoreEdgesIndices = new int[] { 0, 2 };
            }

            // Left child has higher score in term of accessibilty. Ratio = left.score / (left.score + right.score)
            if (_scores[_scoreEdgesIndices[0]] < _scores[_scoreEdgesIndices[1]])
            {
                // left score < right score, reverse
                _reverse = true;
            }
        }
    }
}
