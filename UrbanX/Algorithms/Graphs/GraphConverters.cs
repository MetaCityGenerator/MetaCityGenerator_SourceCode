using System;
using System.Collections.Generic;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using System.Collections.ObjectModel;
using Mawan.DataStructures;


namespace Mawan.Algorithms
{
    public class GraphConverters
    {
        private static readonly string DEFAULT_WEIGHT_ATTR_NAME = "weight";
        private static readonly string TOPOLOGICAL = "topological";
        private static readonly string METRIC = "metric";
        private static readonly string ANGULAR = "angular";
        private static readonly int PRECISION = 3;

        /// <summary>
        /// In the result Digraph, road intersections are the vertices, and the roads are edges
        /// </summary>
        /// <param name="roads"></param>
        /// <param name="verticeArr"></param>
        /// <returns></returns>
        public static EdgeWeightedDigraph RoadNetworkToDigraph(FeatureCollection roads, out (double x, double y)[] verticeArr)
        {
            EdgeWeightedDigraph g = new EdgeWeightedDigraph();
            Dictionary<(double x, double y), int> verticeDict = new Dictionary<(double x, double y), int>();
            List<(double x, double y)> verticeList = new List<(double x, double y)>();

            foreach (Feature f in roads)
            {
                LineString l = (LineString)f.Geometry;
                (double x, double y) v1 = PointToTuple(l.StartPoint);
                (double x, double y) v2 = PointToTuple(l.EndPoint);
                AddVertice(v1, verticeDict, verticeList, g);
                AddVertice(v2, verticeDict, verticeList, g);
                AddEdgeToG(v1, v2, f.Geometry, verticeDict, g);
            }

            verticeArr = verticeList.ToArray();
            return g;
        }

        public static EdgeWeightedGraph RoadNetworkToGraph(FeatureCollection roads, out (double x, double y)[] verticeArr)
        {
            EdgeWeightedGraph g = new EdgeWeightedGraph();
            Dictionary<(double x, double y), int> verticeDict = new Dictionary<(double x, double y), int>();
            List<(double x, double y)> verticeList = new List<(double x, double y)>();
            Dictionary<Edge, int> edgeDict = new Dictionary<Edge, int>();
            List<Edge> edgeList = new List<Edge>();

            foreach (Feature f in roads)
            {
                LineString l = (LineString)f.Geometry;
                (double x, double y) v1 = PointToTuple(l.StartPoint);
                (double x, double y) v2 = PointToTuple(l.EndPoint);
                AddVertice(v1, verticeDict, verticeList, g);
                AddVertice(v2, verticeDict, verticeList, g);
                AddEdgeToG(v1, v2, f.Geometry, verticeDict, edgeDict, edgeList, g);
            }

            verticeArr = verticeList.ToArray();
            return g;
        }

        /// <summary>
        /// In the result Digraph, road intersections are edges, and the roads are vertices
        /// </summary>
        /// <param name="roads"></param>
        /// <returns></returns>
        public static EdgeWeightedDigraph RoadNetworkToSpaceSyntaxDigraph(Collection<IFeature> roads)
        {
            EdgeWeightedGraph g = new EdgeWeightedGraph();
            Dictionary<(double x, double y), int> verticeDict = new Dictionary<(double x, double y), int>( );
            List<(double x, double y)> verticeList = new List<(double x, double y)>();
            Dictionary<Edge, int> edgeDict = new Dictionary<Edge, int>();
            List<Edge> edgeList = new List<Edge>();

            foreach (Feature f in roads)
            {
                LineString l = (LineString)f.Geometry;
                (double x, double y) v1 = PointToTuple(l.StartPoint);
                (double x, double y) v2 = PointToTuple(l.EndPoint);
                AddVertice(v1, verticeDict, verticeList, g);
                AddVertice(v2, verticeDict, verticeList, g);
                AddEdgeToG(v1, v2, f.Geometry, verticeDict, edgeDict, edgeList, g);
            }

            EdgeWeightedDigraph h = new EdgeWeightedDigraph(edgeList.Count);
            for (int i = 0; i < verticeList.Count; i++)
            {
                List<Edge> adjs = g.Adj(i);
                for (int j = 0; j < adjs.Count - 1; j++)
                {
                    int e1 = edgeDict[adjs[j]];
                    for (int k = j + 1; k < adjs.Count; k++)
                    {
                        int e2 = edgeDict[adjs[k]];
                        DirectedEdge this_e = new DirectedEdge(e1, e2);
                        double this_weight = GetAngularWeight(adjs[j], adjs[k], verticeList);
                        this_e.SetWeight(ANGULAR, this_weight);
                        this_weight = GetMetricWeight(adjs[j], adjs[k]);
                        this_e.SetWeight(METRIC, this_weight);
                        this_e.SetWeight(TOPOLOGICAL, 1);
                        h.AddEdge(this_e);
                        h.AddEdge(this_e.GetReversedEdge());
                    }
                }
            }
            return h;
        }

        private static (double x, double y) PointToTuple(Point p) => (Math.Round(p.X, PRECISION), Math.Round(p.Y, PRECISION));

        private static double GetAngularWeight(Edge e1, Edge e2, List<(double x, double y)> verticeList)
        {
            int e1_from_id = e1.Either();
            int e1_to_id = e1.Other(e1_from_id);
            int e2_from_id = e2.Either();
            int e2_to_id = e2.Other(e2_from_id);
            if (e1_from_id == e2_to_id || e2_from_id == e1_to_id)
            {
                int t = e2_from_id;
                e2_from_id = e2_to_id;
                e2_to_id = t;
            }
            (double x, double y) e1_from = verticeList[e1_from_id];
            (double x, double y) e1_to = verticeList[e1_to_id];
            (double x, double y) e2_from = verticeList[e2_from_id];
            (double x, double y) e2_to = verticeList[e2_to_id];

            double v1_x = e1_to.x - e1_from.x;
            double v1_y = e1_to.y - e1_from.y;
            double v2_x = e2_to.x - e2_from.x;
            double v2_y = e2_to.y - e2_from.y;

            double dot = v1_x * v2_x + v1_y * v2_y;
            double l1 = GetLength(v1_x, v1_y);
            double l2 = GetLength(v2_x, v2_y);

            double cosine = dot / l1 / l2;
            if (cosine < -1) cosine = -1;
            if (cosine > 1) cosine = 1;
            double angle = Math.Acos(cosine);
            angle = RoundAngular(angle);
            double angle_weight = 2.0 - 2.0 / Math.PI * angle;
            return angle_weight;
        }

        private static double GetMetricWeight(Edge e1, Edge e2)
        {
            double e1_weight = e1.GetWeight();
            double e2_weight = e2.GetWeight();
            double res = (e1_weight + e2_weight) / 2.0;
            return res;
        }

        private static double GetLength(double x, double y) => Math.Sqrt(x * x + y * y);

        private static double RoundAngular(double angle, int bins = 1024)
        {
            double width = 2.0 * Math.PI / (bins * 1.0);
            int pos = (int)Math.Truncate(angle / width + 0.5);
            double converted = pos * width;
            return converted;
        }

        private static void AddVertice((double x, double y) v, Dictionary<(double x, double y), int> verticeDict, ICollection<(double x, double y)> verticeList, EdgeWeightedDigraph g)
        {
            if (verticeDict.ContainsKey(v)) return;
            verticeDict.Add(v, verticeList.Count);
            verticeList.Add(v);
            g.AddVertice();
        }

        private static void AddVertice((double x, double y) v, Dictionary<(double x, double y), int> verticeDict, ICollection<(double x, double y)> verticeList, EdgeWeightedGraph g)
        {
            if (verticeDict.ContainsKey(v)) return;
            verticeDict.Add(v, verticeList.Count);
            verticeList.Add(v);
            g.AddVertice();
        }

        private static void AddEdgeToG((double x, double y) v1, (double x, double y) v2, Geometry l, Dictionary<(double x, double y), int> verticeDict,
            Dictionary<Edge, int> edgeDict, List<Edge> edgeList, EdgeWeightedGraph g)
        {
            int id1 = verticeDict[v1];
            int id2 = verticeDict[v2];
            LineString ls = (LineString)l;
            Edge e = new Edge(id1, id2, ls);
            e.SetWeight(DEFAULT_WEIGHT_ATTR_NAME, ls.Length);
            g.AddEdge(e);
            edgeDict.Add(e, edgeList.Count);
            edgeList.Add(e);
        }

        private static void AddEdgeToG((double x, double y) v1, (double x, double y) v2, Geometry l, Dictionary<(double x, double y), int> verticeDict, EdgeWeightedDigraph g)
        {
            // v1 is the start point, and v2 is the end point of l
            int id1 = verticeDict[v1];
            int id2 = verticeDict[v2];
            LineString ls = (LineString)l;
            DirectedEdge e = new DirectedEdge(id1, id2, ls);
            e.SetWeight(DEFAULT_WEIGHT_ATTR_NAME, ls.Length);
            g.AddEdge(e);
            g.AddEdge(e.GetReversedEdge());
        }
    }
}
