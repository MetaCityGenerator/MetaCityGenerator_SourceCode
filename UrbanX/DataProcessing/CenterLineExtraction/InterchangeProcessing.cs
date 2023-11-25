using System;
using System.Collections.Generic;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Precision;
using NetTopologySuite.Operation.Union;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Operation.Polygonize;
using Mawan.Algorithms;
using Mawan.DataStructures;


namespace UrbanX.DataProcessing
{
    public class InterchangeProcessing
    {
        private const int PRECISION = 3;
        private readonly List<LineString> results;
        private readonly EdgeWeightedGraph g;
        private readonly Dictionary<(double x, double y), int> verticeDict;
        private readonly Dictionary<LineString, int> roadDict;
        private readonly STRtree<LineString> rtree;
        private readonly bool[] shouldBeDeleted;

        public InterchangeProcessing(IList<LineString> roads, IList<Envelope> boxes)
        {
            // Data cleaning
            roads = GetCleanedRoads(roads);
            roads = ConnectingRoadSegments.Get(roads);
            boxes = GetCleanedEnvelopes(boxes);

            // Data structures
            g = GraphConverters.RoadNetworkToGraph(LineStringsToFeatureCollection(roads), out (double x, double y)[] verticeArr);
            verticeDict = GetVerticeDict(verticeArr);
            roadDict = GetRoadDict(roads);
            rtree = GetRtree(roads);
            shouldBeDeleted = InitializeBoolArray(roads.Count);

            // Start to simplify overpasses
            List<LineString> newLines = new List<LineString>();
            foreach (Envelope box in boxes)
            {
                ICollection<LineString> roadsIntersectBox = GetRoadsIntersectWithBox(box);
                MultiPolygon overpassGeometry = GetOverpassGeometry(roadsIntersectBox);
                HashSet<int> boundaryVertices = GetBoundaryVertices(overpassGeometry);
                DeleteRoads(overpassGeometry, boundaryVertices);
                IEnumerable<int> connectPointsId = GetConnectPoints(boundaryVertices);
                IEnumerable<Coordinate> connectPoints = GetVerticeCoords(verticeArr, connectPointsId);
                Coordinate centroid = GetCentroid(connectPoints);
                foreach (Coordinate c in connectPoints)
                    newLines.Add(new LineString(new Coordinate[] { centroid, c }));
            }

            // Arrange results
            results = new List<LineString>();
            for (int i = 0; i < roads.Count; i++)
                if (!shouldBeDeleted[i])
                    results.Add(roads[i]);
            foreach (LineString ls in newLines)
                results.Add(ls);
        }

        public List<LineString> Results => results;

        public static IList<Envelope> GetCleanedEnvelopes(IList<Envelope> envelopes)
        {
            // Convert envelopes to polygons
            IList<Polygon> polygons = new List<Polygon>();
            foreach (Envelope envelope in envelopes)
                polygons.Add(EnvelopeToPolygon(envelope));

            // Union the overlapped polygons
            Geometry[] allgeoms = ((GeometryCollection)UnaryUnionOp.Union(polygons)).Geometries;
            polygons = new List<Polygon>();
            foreach (Geometry geom in allgeoms)
                polygons.Add((Polygon)geom);

            // Transform polygons into envelopes
            List<Envelope> res = new List<Envelope>();
            foreach (Polygon p in polygons)
                res.Add(p.EnvelopeInternal);
            return res;
        }

        private static IList<LineString> GetCleanedRoads(IList<LineString> roads)
        {
            // Round the coordinates, and remove overlapping LineStrings by HashSet
            // This is a "coarse" method that could remove the overlapping ones very fast but not so accurately
            GeometryPrecisionReducer gpr = new GeometryPrecisionReducer(new PrecisionModel(Math.Pow(10, PRECISION))) { 
                ChangePrecisionModel = true
            };
            HashSet<LineString> set = new HashSet<LineString>();
            foreach (LineString road in roads)
                set.Add((LineString)gpr.Reduce(road));

            // Remove overlapping LineStrings by UnaryUnionOp, which could accurately remove the overlapping ones
            MultiLineString allRoads = (MultiLineString)UnaryUnionOp.Union(roads);
            IList<LineString> res = new List<LineString>();
            foreach (Geometry geom in allRoads.Geometries)
                res.Add((LineString)gpr.Reduce(geom));

            return res;
        }

        private static FeatureCollection LineStringsToFeatureCollection(IList<LineString> lineStrings)
        {
            FeatureCollection res = new FeatureCollection();
            foreach (LineString ls in lineStrings)
                res.Add(new Feature(ls, new AttributesTable()));
            return res;
        }

        private static Dictionary<(double x, double y), int> GetVerticeDict((double x, double y)[] verticeArr)
        {
            Dictionary<(double x, double y), int> res = new Dictionary<(double x, double y), int>();
            for (int i = 0; i < verticeArr.Length; i++)
                res.Add(verticeArr[i], i);
            return res;
        }

        private static Dictionary<LineString, int> GetRoadDict(IList<LineString> roads)
        {
            Dictionary<LineString, int> res = new Dictionary<LineString, int>();
            for (int i = 0; i < roads.Count; i++)
                res.Add(roads[i], i);
            return res;
        }

        private static STRtree<LineString> GetRtree(IList<LineString> roads)
        {
            STRtree<LineString> res = new STRtree<LineString>();
            foreach (LineString lineString in roads)
                res.Insert(lineString.EnvelopeInternal, lineString);
            return res;
        }

        private static bool[] InitializeBoolArray(int count)
        {
            bool[] res = new bool[count];
            for (int i = 0; i < count; i++)
                res[i] = false;
            return res;
        }

        private ICollection<LineString> GetRoadsIntersectWithBox(Envelope box)
        {
            IList<LineString> temp = rtree.Query(box);
            IList<LineString> roadsIntersectBox = new List<LineString>();

            // Ensure that the roads are REALLY intersected with the box
            Polygon boxPolygon = EnvelopeToPolygon(box);
            foreach (LineString road in temp)
                if (boxPolygon.Intersects(road))
                    roadsIntersectBox.Add(road);

            // Only keep the roads that belong to the overpass, remove the "lonely" roads
            roadsIntersectBox = RemoveIsolatedRoads(roadsIntersectBox);

            // Retrieve the roads which are not intersected by the box,
            // but meanwhile, they connect as "bridges" among the roads that belong to the road set
            // among which the roads intersect with the box
            // However, if this "bridge" road is longer than a threshold, it should not be considered,
            // because the probability of it being a part of the overpass will be too low
            HashSet<LineString> roadsIntersectBoxHash = new HashSet<LineString>(roadsIntersectBox);
            HashSet<int> roadsIntersectBoxVerticeHash = GetRoadsVertices(roadsIntersectBox, verticeDict);
            HashSet<LineString> neighRoads = new HashSet<LineString>();
            foreach (int v in roadsIntersectBoxVerticeHash)
                foreach (Edge e in g.Adj(v))
                    if (!roadsIntersectBoxHash.Contains(e.LineString) && (roadsIntersectBoxVerticeHash.Contains(e.Other(v)))
                        && e.LineString.Length < Math.Sqrt(2 * box.Area))
                        neighRoads.Add(e.LineString);

            // Add these roads
            foreach (LineString ls in neighRoads)
                roadsIntersectBox.Add(ls);

            return roadsIntersectBox;
        }

        private static MultiPolygon GetOverpassGeometry(ICollection<LineString> roadsIntersectBox)
        {
            Polygonizer polygonizer = new Polygonizer() { IsCheckingRingsValid = false };
            foreach (LineString ls in roadsIntersectBox) polygonizer.Add(ls);
            Geometry polygonized = polygonizer.GetGeometry();
            Geometry[] polygonized_geometries = ((GeometryCollection)polygonized).Geometries;
            Polygon[] polygons = new Polygon[polygonized_geometries.Length];
            for (int i = 0; i < polygonized_geometries.Length; i++)
                polygons[i] = (Polygon)polygonized_geometries[i];
            MultiPolygon overpassGeometry = new MultiPolygon(polygons);

            return overpassGeometry;
        }

        /// <summary>
        /// Retrieve all the vertices that lie on the boundary of the polygonized overpass.
        /// They are also intersections among the road network.
        /// </summary>
        /// <param name="overpassGeometry"></param>
        /// <returns></returns>
        private HashSet<int> GetBoundaryVertices(MultiPolygon overpassGeometry)
        {
            HashSet<int> boundaryVertices = new HashSet<int>();
            foreach (Geometry geom in overpassGeometry.Geometries)
                foreach (Coordinate c in ((Polygon)geom).ExteriorRing.Coordinates)
                {
                    (double x, double y) this_vertice = PointToTuple(c);
                    if (verticeDict.ContainsKey(this_vertice))
                        boundaryVertices.Add(verticeDict[this_vertice]);
                }

            return boundaryVertices;
        }

        private void DeleteRoads(MultiPolygon overpassGeometry, HashSet<int> boundaryVertices)
        {
            // Delete all the roads that lies on the boundary of the polygonized overpass
            // This code section will also deal with the roads that do not lie on the boundary, but connect as "bridges"
            // among the detached polygons
            foreach (int v in boundaryVertices)
                foreach (Edge e in g.Adj(v))
                    if (!shouldBeDeleted[roadDict[e.LineString]] && boundaryVertices.Contains(e.Other(v)))
                        shouldBeDeleted[roadDict[e.LineString]] = true;

            // Delete dangles that are linked to the boundary vertices
            foreach (int v in boundaryVertices)
                foreach (Edge e in g.Adj(v))
                    if (!shouldBeDeleted[roadDict[e.LineString]] && g.Degree(e.Other(v)) <= 1)
                        shouldBeDeleted[roadDict[e.LineString]] = true;

            // Delete the roads that are covered by the polygonized overpass
            // These roads do not lie on the boundary of the polygonized overpass
            IList<LineString> roads = rtree.Query(overpassGeometry.EnvelopeInternal);
            foreach (LineString road in roads)
                if (!shouldBeDeleted[roadDict[road]] && overpassGeometry.Covers(road))
                    shouldBeDeleted[roadDict[road]] = true;
        }

        /// <summary>
        /// Output the vertices that act as the touching points with the overpass.
        /// They will be linked to their common centroid, which is the final method of simplifying the overpass
        /// </summary>
        /// <param name="overpassGeometry"></param>
        /// <param name="boundaryVertices"></param>
        /// <returns>It will only output the id of the vertices.
        /// If detailed coordinates are needed, retrieve them in the main program by the <c>verticeArr[]</c> array</returns>
        private IEnumerable<int> GetConnectPoints(HashSet<int> boundaryVertices)
        {
            HashSet<int> connectPoints = new HashSet<int>();
            foreach (int v in boundaryVertices)
                foreach (Edge e in g.Adj(v))
                    if (!shouldBeDeleted[roadDict[e.LineString]])
                    {
                        connectPoints.Add(v);
                        break;
                    }

            return connectPoints;
        }

        private static IEnumerable<Coordinate> GetVerticeCoords((double x, double y)[] verticeArr, IEnumerable<int> verticeIds)
        {
            List<Coordinate> res = new List<Coordinate>();
            foreach (int v in verticeIds)
                res.Add(new Coordinate(verticeArr[v].x, verticeArr[v].y));
            return res;
        }

        private static Coordinate GetCentroid(IEnumerable<Coordinate> points)
        {
            double x = 0;
            double y = 0;
            int total = 0;
            foreach (Coordinate c in points)
            {
                x += c.X;
                y += c.Y;
                total++;
            }
            if (total == 0) return null;
            x /= (total * 1.0);
            y /= (total * 1.0);
            Coordinate res = new Coordinate(x, y);
            return res;
        }

        private static IList<LineString> RemoveIsolatedRoads(IList<LineString> roadsIntersectBox)
        {
            // Build the graph
            EdgeWeightedGraph h = GraphConverters.RoadNetworkToGraph(LineStringsToFeatureCollection(roadsIntersectBox),
                out (double x, double y)[] vArr);
            Dictionary<(double x, double y), int> vDict = GetVerticeDict(vArr);

            // Traverse the graph
            DepthFirstSearch dfs = new DepthFirstSearch(h);
            HashSet<int> mainGroup = dfs.GetMainGroupVertices();

            // Only keep the roads that are linked to the vertices belonging to the main group
            IList<LineString> temp = new List<LineString>();
            foreach (LineString road in roadsIntersectBox)
            {
                (double x, double y) start = PointToTuple(road.StartPoint.Coordinate);
                (double x, double y) end = PointToTuple(road.EndPoint.Coordinate);
                if (mainGroup.Contains(vDict[start]) || mainGroup.Contains(vDict[end]))
                    temp.Add(road);
            }

            return temp;
        }

        private static Polygon EnvelopeToPolygon(Envelope envelope)
        {
            double minX = envelope.MinX;
            double maxX = envelope.MaxX;
            double minY = envelope.MinY;
            double maxY = envelope.MaxY;
            Polygon polygon = new Polygon(new LinearRing(new Coordinate[]
            {
                new Coordinate(minX, minY),
                new Coordinate(maxX, minY),
                new Coordinate(maxX, maxY),
                new Coordinate(minX, maxY),
                new Coordinate(minX, minY)
            }));
            return polygon;
        }

        private static HashSet<int> GetRoadsVertices(IEnumerable<LineString> roads, Dictionary<(double x, double y), int> verticeDict)
        {
            HashSet<int> res = new HashSet<int>();
            foreach (LineString road in roads)
            {
                res.Add(verticeDict[PointToTuple(road.StartPoint.Coordinate)]);
                res.Add(verticeDict[PointToTuple(road.EndPoint.Coordinate)]);
            }
            return res;
        }

        private static (double x, double y) PointToTuple(Coordinate p) => (Math.Round(p.X, PRECISION), Math.Round(p.Y, PRECISION));
    }
}
