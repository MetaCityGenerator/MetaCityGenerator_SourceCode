using NetTopologySuite.Geometries;

using System;
using System.Collections.Generic;
using System.Linq;

using UrbanX.DataStructures.Graphs;
using UrbanX.Planning.Utility;

namespace UrbanX.Planning.FacilityLocation
{
    /// <summary>
    /// Construct an undirected weighted graph for road network.
    /// Nodes and intersections are the vertices V in graph, their connections(segments) are the edge E in graph.
    /// This is the main difference to graph used in space syntax.
    /// <para>There are two types of node : entry_points of each block and intersections for adjacent roads.</para>
    /// </summary>
    public class Nts_CityGraphBuilder
    {

        private readonly LineString[] _lineStrings;

        /// <summary>
        /// Storing all the points from linestrings.
        /// </summary>
        public readonly Dictionary<Point, string> _pointsToVertices;


        private readonly Dictionary<Point, string> _entryPts;


        public UndirectedWeightedSparseGraph<string> CityGraph { get; }



        /// <summary>
        /// Constructor for CityGraphBuilder with input <see cref="LineString"/> array.
        /// </summary>
        /// <param name="curves">Input curves must have been cleared and dissolved.</param>
        public Nts_CityGraphBuilder(MultiLineString curves)
        {
            _lineStrings = GeometryFactory.ToLineStringArray(curves.Geometries);
            _entryPts = new Dictionary<Point, string>(_lineStrings.Length);
            _pointsToVertices = new Dictionary<Point, string>(_lineStrings.Length * 2);

            CityGraph = new UndirectedWeightedSparseGraph<string>(_lineStrings.Length * 2);
        }



        /// <summary>
        /// Building graph with metric weight. Maybe obselet later.
        /// </summary>
        public void Build()
        {
            // Add vertices of graph to collection. Using indices to represent the vertices instead of using objects itselves.
            // Objects(curves) can be queied later by using indices.

            Span<LineString> lsSpan = new Span<LineString>(_lineStrings);

            for (int i = 0; i < lsSpan.Length; i++)
            {
                // Getting end points for each linestring.
                Point[] endsPts = { lsSpan[i].StartPoint, lsSpan[i].EndPoint };
                foreach (var pt in endsPts)
                {
                    // Check exitence for each points.
                    if (!_pointsToVertices.ContainsKey(pt))
                    {
                        _pointsToVertices.Add(pt, $"Node_{ _pointsToVertices.Count}");
                        CityGraph.AddVertex(_pointsToVertices[pt]);
                    }
                }

                // Add edge by quering point index.
                var s = _pointsToVertices[endsPts[0]];
                var e = _pointsToVertices[endsPts[1]];

                CityGraph.AddEdge(s, e, lsSpan[i].Length);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="belongToSite">Indicates each entry connection linestring belongs to which site.</param>
        public void Build(Dictionary<LineString, string> belongToSite)
        {
            // Add vertices of graph to collection. Using indices to represent the vertices instead of using objects itselves.
            // Objects(curves) can be queied later by using indices.

            Span<LineString> lsSpan = new Span<LineString>(_lineStrings);

            // For debug.
            int lsCount = 0;

            // Step1: first we need to add non-entry connections.
            for (int i = 0; i < lsSpan.Length; i++)
            {
                // During dissolving process, linestring may has changed order. 
                if (belongToSite.ContainsKey(lsSpan[i]) || belongToSite.ContainsKey((LineString)lsSpan[i].Reverse()))
                    continue;

                // Getting end points for each linestring.
                Point[] endsPts = { lsSpan[i].StartPoint, lsSpan[i].EndPoint };
                foreach (var pt in endsPts)
                {
                    // Check exitence for each points.
                    if (!_pointsToVertices.ContainsKey(pt))
                    {
                        _pointsToVertices.Add(pt, $"Node_{ _pointsToVertices.Count}");
                        CityGraph.AddVertex(_pointsToVertices[pt]);
                    }
                }

                // Add edge by quering point index.
                var s = _pointsToVertices[endsPts[0]];
                var e = _pointsToVertices[endsPts[1]];

                CityGraph.AddEdge(s, e, lsSpan[i].Length);
                lsCount++;
            }


            // Step2: add entry connections.
            foreach (var connection in belongToSite.Keys)
            {
                Point[] endsPts = { connection.StartPoint, connection.EndPoint };


                foreach (var pt in endsPts)
                {
                    // Check exitence for each points.
                    if (!_pointsToVertices.ContainsKey(pt))
                    {
                        // Found the entry point. The end point should already been added in the first loop.
                        _pointsToVertices.Add(pt, $"Entry{ _entryPts.Count}_{belongToSite[connection]}");
                        _entryPts.Add(pt, $"Entry{ _entryPts.Count}_{belongToSite[connection]}");
                        CityGraph.AddVertex(_pointsToVertices[pt]);
                    }
                }

                var s = _pointsToVertices[endsPts[0]];
                var e = _pointsToVertices[endsPts[1]];

                CityGraph.AddEdge(s, e, connection.Length);
            }

        }


        /// <summary>
        /// Get the all entry_point vertex in city graph based on a given point.
        /// Handling tolerance error by using STRtree.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public string[] GetEntryPointVertices(Point pt)
        {
            // Finding the nearest point by using STRtree.
            var found = _entryPts.Keys.ToArray().KNN(pt, 1)[0];

            var index = _entryPts[(Point)found];

            var siteIndex = int.Parse(index.Split('_').Last());

            // Finding nodes belong to the same site.
            LinkedList<string> result = new LinkedList<string>();

            foreach (var entry in _entryPts)
            {
                var tempIndex = int.Parse(entry.Value.Split('_').Last());
                if (siteIndex == tempIndex)
                {
                    // Belong to same site.
                    result.AddLast(entry.Value);
                }
            }

            return result.ToArray();
        }


        /// <summary>
        /// Found the vertice for a given point from graph.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public string GetPointVertex(Point pt)
        {
            var found = _pointsToVertices.Keys.ToArray().KNN(pt, 1)[0];
            var vertex = _pointsToVertices[(Point)found];

            return vertex;
        }
    }
}
