using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Mawan.Algorithms;
using Mawan.DataStructures;


namespace UrbanX.DataProcessing
{
    public class ConnectingRoadSegments
    {
        public static IList<LineString> Get(IEnumerable<LineString> roads)
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            FeatureCollection fc = GeometryListToFC(roads);
            EdgeWeightedGraph g = GraphConverters.RoadNetworkToGraph(fc, out (double x, double y)[] verticeArr);
            IEnumerable<int> intersections = GetIntersections(g);
            HashSet<Edge> visited = new HashSet<Edge>();
            List<LineString> res = new List<LineString>();

            foreach (int v in intersections)
                foreach (Edge e in g.Adj(v))
                    if (!visited.Contains(e))
                    {
                        List<Edge> edgeList = GetEdgeList(g, v, e, visited);
                        LineString singleRoad = GetOne(edgeList);
                        res.Add(singleRoad);
                    }

            //sw.Stop();
            //Console.WriteLine("Combine consecutive roads: {0}s", Math.Round((double)sw.ElapsedMilliseconds / 1000, 1));

            return res;
        }

        public static IList<LineString> Get(IEnumerable<LineSegment> roads)
        {
            GeometryFactory gf = new GeometryFactory();
            List<LineString> newList = new List<LineString>();
            foreach (LineSegment ls in roads)
                newList.Add(ls.ToGeometry(gf));
            IList<LineString> res = Get(newList);
            return res;
        }

        private static LineString GetOne(List<Edge> edgeList)
        {
            List<Coordinate> res = GetCoordinatesOfEdge(edgeList[0]);

            // To deal with the case that the endpoint of the first edge does not touch the next edge
            // TODO: BE CAREFUL of the RING degeneracy case, you might have not fixed it yet!
            if (edgeList.Count > 1)
            {
                LineString nextEdgeLineString = edgeList[1].LineString;
                if (res[0].Equals2D(nextEdgeLineString.StartPoint.Coordinate) || res[0].Equals2D(nextEdgeLineString.EndPoint.Coordinate))
                    res.Reverse();
            }

            for (int i = 1; i<edgeList.Count; i++)
            {
                List<Coordinate> thisEdgeCoords = GetCoordinatesOfEdge(edgeList[i]);
                if (!res[res.Count - 1].Equals2D(thisEdgeCoords[0])) thisEdgeCoords.Reverse();
                res.AddRange(thisEdgeCoords);
            }

            LineString lineString = new LineString(res.ToArray());
            return lineString;
        }

        private static List<Coordinate> GetCoordinatesOfEdge(Edge e) => e.LineString.CoordinateSequence.ToCoordinateArray().ToList();

        private static List<Edge> GetEdgeList(EdgeWeightedGraph g, int v, Edge e, HashSet<Edge> visited)
        {
            int currentV = e.Other(v);
            List<Edge> edges = new List<Edge>() { e };
            visited.Add(e);

            while (g.Degree(currentV) == 2)
            {
                Edge nonVisitedEdge = null;
                foreach (Edge edge in g.Adj(currentV))
                    if (!visited.Contains(edge))
                    {
                        nonVisitedEdge = edge;
                        break;
                    }
                edges.Add(nonVisitedEdge);
                currentV = nonVisitedEdge.Other(currentV);
                visited.Add(nonVisitedEdge);
            }

            return edges;
        }

        private static FeatureCollection GeometryListToFC(IEnumerable<Geometry> geometries)
        {
            FeatureCollection res = new FeatureCollection();
            foreach (Geometry g in geometries)
                res.Add(new Feature(g, new AttributesTable()));
            return res;
        }

        private static IEnumerable<int> GetIntersections(EdgeWeightedGraph g)
        {
            List<int> res = new List<int>();
            for (int v = 0; v < g.V; v++)
                if (g.Degree(v) > 2) res.Add(v);
            return res;
        }
    }
}
