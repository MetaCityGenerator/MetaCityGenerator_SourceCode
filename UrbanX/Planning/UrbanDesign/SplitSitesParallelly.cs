using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using UrbanX.Planning.Utility;


namespace UrbanX.Planning.UrbanDesign
{
    public class CourtyardUnit
    {
        private const int PRECISION = 3;

        private readonly Polygon _boundingPolygon;
        private Geometry _intersectedSite;

        private readonly LineSegment _southSegment;
        private readonly LineSegment _westSegment;
        private readonly LineSegment _eastSegment;

        public Polygon BoundingPolygon => _boundingPolygon;

        public Geometry IntersectedSite => _intersectedSite;

        public CourtyardUnit(Coordinate[] boundingCorners)
        {
            _boundingPolygon = new Polygon(new LinearRing(new Coordinate[] { boundingCorners[0], boundingCorners[1],
            boundingCorners[2], boundingCorners[3], boundingCorners[0]}));

            _southSegment = new LineSegment(boundingCorners[0], boundingCorners[1]);
            _westSegment = new LineSegment(boundingCorners[0], boundingCorners[3]);
            _eastSegment = new LineSegment(boundingCorners[1], boundingCorners[2]);
        }

        public IList<Polygon> GetCourtyardUnitWithinSite(Polygon site)
        {
            List<Polygon> res = new List<Polygon>();

            Geometry _intersects = site.ExteriorRing.Intersection(BoundingPolygon);
            List<LineString> _intersectLines = FilterLineString(_intersects);
            if (_intersectLines.Count < 2) return res;
            _intersectedSite = _intersects;

            LineString northMost = GetNorthMostLineString(_intersectLines);
            LineString southMost = GetSouthMostLineString(_intersectLines);
            Coordinate northPoint = GetSouthMostCoordinate(northMost);
            Coordinate southPoint = GetNorthMostCoordinate(southMost);
            if (northPoint.Distance(southPoint) <= 20) return res;
            LineSegment northSegment = GetParallelSegmentViaPoint(northPoint);
            LineSegment southSegment = GetParallelSegmentViaPoint(southPoint);
            Polygon poly = new Polygon(new LinearRing(new Coordinate[] {
                southSegment.P0, southSegment.P1, northSegment.P1, northSegment.P0, southSegment.P0
            }));
            res.Add(poly);

            //List<Polygon> sitesWithinUnit = new List<Polygon>();
            //_intersectedSite = site.Intersection(_boundingPolygon);
            //if (_intersectedSite is Polygon)
            //    sitesWithinUnit.Add((Polygon)_intersectedSite);
            //else if (_intersectedSite is MultiPolygon)
            //    foreach (Geometry g in (MultiPolygon)_intersectedSite)
            //        sitesWithinUnit.Add((Polygon)g);

            //foreach (Polygon p in sitesWithinUnit)
            //{
            //    IList<Polygon> units = GetCourtyardUnitWithinSubsite(p);
            //    foreach (Polygon q in units)
            //        res.Add(q);
            //}

            return res;
        }

        private static List<LineString> FilterLineString(Geometry g)
        {
            List<LineString> res = new List<LineString>();
            for (int i = 0; i<g.NumGeometries; i++)
            {
                Geometry geom = g.GetGeometryN(i);
                if (geom is LineString) res.Add((LineString)geom);
            }

            return res;
        }

        private LineString GetSouthMostLineString(List<LineString> lineStrings)
        {
            LineString southLineString = _southSegment.ToGeometry(new GeometryFactory(new PrecisionModel(Math.Pow(10, PRECISION))));
            LineString lineString = null;
            double minDist = double.PositiveInfinity;
            foreach (LineString ls in lineStrings)
            {
                double dist = ls.Distance(southLineString);
                if (dist < minDist)
                {
                    lineString = ls;
                    minDist = dist;
                }
            }

            return lineString;
        }

        private LineString GetNorthMostLineString(List<LineString> lineStrings)
        {
            LineString southLineString = _southSegment.ToGeometry(new GeometryFactory(new PrecisionModel(Math.Pow(10, PRECISION))));
            LineString lineString = null;
            double maxDist = double.NegativeInfinity;
            foreach (LineString ls in lineStrings)
            {
                double dist = ls.Distance(southLineString);
                if (dist > maxDist)
                {
                    lineString = ls;
                    maxDist = dist;
                }
            }

            return lineString;
        }

        private Coordinate GetNorthMostCoordinate(LineString lineString)
        {
            Coordinate res = null;
            double maxDist = double.NegativeInfinity;
            foreach (Coordinate c in lineString.Coordinates)
            {
                double dist = _southSegment.DistancePerpendicular(c);
                if (dist > maxDist)
                {
                    res = c;
                    maxDist = dist;
                }
            }

            return res;
        }

        private Coordinate GetSouthMostCoordinate(LineString lineString)
        {
            Coordinate res = null;
            double minDist = double.PositiveInfinity;
            foreach (Coordinate c in lineString.Coordinates)
            {
                double dist = _southSegment.DistancePerpendicular(c);
                if (dist < minDist)
                {
                    res = c;
                    minDist = dist;
                }
            }

            return res;
        }

        private class ComparePoint : IComparer<Coordinate>
        {
            private readonly LineSegment _segment;

            public ComparePoint(LineSegment segment)
            {
                _segment = segment;
            }

            public int Compare(Coordinate a, Coordinate b)
            {
                double da = _segment.DistancePerpendicular(a);
                double db = _segment.DistancePerpendicular(b);
                if (da < db) return -1;
                if (da > db) return 1;
                return 0;
            }
        }

        /// <summary>
        /// The core piece of code, which extract the maximum courtyard unit polygon within an informal polygon
        /// </summary>
        /// <param name="polygon">A site polygon which derives from intersecting the site with a rectangle</param>
        /// <returns></returns>
        private IList<Polygon> GetCourtyardUnitWithinSubsite(Polygon polygon)
        {
            List<Coordinate> coordinateList = new List<Coordinate>();
            foreach (Coordinate c in polygon.ExteriorRing.Coordinates)
                coordinateList.Add(c);

            ComparePoint comparePoint = new ComparePoint(_southSegment);
            coordinateList.Sort(comparePoint);

            LineSegment start = null;
            List<Polygon> res = new List<Polygon>();
            for (int i = 0; i<coordinateList.Count; i++)
            {
                Coordinate c = coordinateList[i];

                // Avoid duplicate coordinates
                if (i > 0 && c.Distance(coordinateList[i - 1]) <= Math.Pow(10, -PRECISION)) continue;

                LineSegment segment = GetParallelSegmentViaPoint(c);
                Geometry lineGeom = segment.ToGeometry(new GeometryFactory()).Intersection(polygon);

                double len = 0;
                if (lineGeom is LineString)
                    len = ((LineString)lineGeom).Length;
                if (lineGeom is MultiLineString)
                    len = ((MultiLineString)lineGeom).Length;

                if (lineGeom is LineString || lineGeom is MultiLineString)
                {
                    if (Math.Abs(len - segment.Length) > Math.Pow(10, -PRECISION+1)) continue;
                    if (start == null)
                        start = segment;
                    else
                    {
                        Polygon p = new Polygon(new LinearRing(new Coordinate[] { start.P0, start.P1, segment.P1, segment.P0, start.P0 }));
                        res.Add(p);
                        start = null;
                    }
                }
            }

            return res;
        }

        private LineSegment GetParallelSegmentViaPoint(Coordinate c)
        {
            Coordinate a = _westSegment.Project(c);
            Coordinate b = _eastSegment.Project(c);
            LineSegment res = new LineSegment(a, b);
            return res;
        }

        public static CourtyardUnit[] GetUnits(Coordinate[] corners, double[] widths)
        {
            CourtyardUnit[] res = new CourtyardUnit[widths.Length];
            LineSegment southSegment = new LineSegment(corners[0], corners[1]);
            LineSegment northSegment = new LineSegment(corners[3], corners[2]);
            double startIndex = 0.0;
            double totalLen = southSegment.Length;
            for (int i = 0; i<widths.Length; i++)
            {
                double endIndex = startIndex + widths[i];

                res[i] = new CourtyardUnit(new Coordinate[] {
                    southSegment.PointAlong(startIndex/totalLen),
                    southSegment.PointAlong(endIndex/totalLen),
                    northSegment.PointAlong(endIndex/totalLen),
                    northSegment.PointAlong(startIndex/totalLen)
                });
                startIndex += widths[i];
            }

            return res;
        }
    }

    public class SplitSitesParallelly
    {
        private const int WIDTH = 20;
        private const int MIN_WIDTH = 16;

        public SplitSitesParallelly(Polygon site)
        {
            Coordinate[] corners = site.GetMinimumRoatatedRect(site.GetPolygonRadiant());
            double southSideLength = corners[0].Distance(corners[1]);
            double[] widths = GetWidthsRandomly(southSideLength);
            CourtyardUnit[] courtyardUnits = CourtyardUnit.GetUnits(corners, widths);

            List<Polygon> results = new List<Polygon>();
            for (int i = 1; i<courtyardUnits.Length-1; i++)
            {
                CourtyardUnit unit = courtyardUnits[i];
                IList<Polygon> polygons = unit.GetCourtyardUnitWithinSite(site);
                foreach (Polygon p in polygons)
                    results.Add(p);
            }

            BoundingPolygons = new Polygon[courtyardUnits.Length];
            IntersectedSites = new Geometry[courtyardUnits.Length];
            for (int i = 1; i < courtyardUnits.Length-1; i++)
            {
                BoundingPolygons[i] = courtyardUnits[i].BoundingPolygon;
                IntersectedSites[i] = courtyardUnits[i].IntersectedSite;
            }
            Results = results.ToArray();
        }

        public Polygon[] BoundingPolygons { get; private set; }

        public Geometry[] IntersectedSites { get; private set; }

        public Polygon[] Results { get; private set; }

        private static double[] GetWidthsEvenly(double len)
        {
            double width = WIDTH;
            int num = (int)Math.Truncate(len / width);
            if (len - num * WIDTH >= WIDTH * 0.8)
                num++;
            else
                width = len / num;

            double[] res = new double[num];
            for (int i = 0; i < num; i++)
                if (i < num - 1)
                    res[i] = width;
                else
                    res[i] = len - i * width;

            return res;
        }

        private static double[] GetWidthsRandomly(double len)
        {
            List<double> lenList = new List<double>();
            double remain = len;
            Random random = new Random();

            int thisLen;
            while (remain >= 2 * WIDTH)
            {
                thisLen = random.Next(MIN_WIDTH, 2 * WIDTH - MIN_WIDTH);
                lenList.Add(thisLen);
                remain -= thisLen;
            }

            if (remain >= 2 * MIN_WIDTH)
                thisLen = (int)Math.Truncate(remain / 2);
            else
                thisLen = random.Next(MIN_WIDTH, 2 * WIDTH - MIN_WIDTH);
            lenList.Add(thisLen);
            lenList.Add(remain - thisLen);

            return lenList.ToArray();
        }
    }
}
