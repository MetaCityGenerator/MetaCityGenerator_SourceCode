using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;

using System;
using System.Collections.Generic;

using MetaCity.DataStructures.Set;
using MetaCity.DataStructures.Graphs;
using MetaCity.Planning.Utility;
using MetaCity.Algorithms.Geometry3D;
using System.Buffers;

namespace MetaCity.Planning.SpaceSyntax
{
  
    public sealed class GraphBuilder3DS 
    {
        private static readonly GeometryComparer3D _comparer3D = new GeometryComparer3D(false);

        private LineString[] _lineStrings;

        /// <summary>
        /// Using index(int) to represent each lineString. Share the same order with _lineStrings array.
        /// </summary>
        private readonly int[] _segmentVertices;

        private readonly Dictionary<Point, Stack<int>> _adjacentSegments;

        public SpaceSyntaxGraph Graph { get; }

        public ReadOnlySpan<LineString> Roads => _lineStrings;


        public GraphBuilder3DS(LineString[] curves3D , bool merging = true)
        {
            _lineStrings = curves3D;
            _segmentVertices = new int[_lineStrings.Length];
            _adjacentSegments = new Dictionary<Point, Stack<int>>(_lineStrings.Length * 2, _comparer3D);

            if (merging)
                MergeLines();

            Graph = new SpaceSyntaxGraph(_lineStrings.Length);
        }


        public void Build()
        {
            // Add all vertices of graph to collection.
            // Using indices to represent the vertices instead of using objects itselves.
            // Objects(curves) can be queried later by using indices.
            Point[] endsPts = new Point[2];
            for (int i = 0; i < _lineStrings.Length; i++)
            {
                _segmentVertices[i] = i;

                endsPts[0] = _lineStrings[i].StartPoint;
                endsPts[1] = _lineStrings[i].EndPoint;


                for (int p = 0; p < 2; p++)
                {
                    var pt = endsPts[p];

                    if (!_adjacentSegments.ContainsKey(pt))
                    {
                        //Linestring already be snapped during data clean stage.
                        _adjacentSegments.Add(pt, new Stack<int>());
                    }

                    _adjacentSegments[pt].Push(i); // 0<=i<= lineStirng.Length-1
                }
            }


            BuildingGraph(_lineStrings);
        }


        private void MergeLines()
        {
            // Collection of merged linestrings.
            List<LineString> merged = new List<LineString>(_lineStrings.Length);
            var f = _lineStrings[0].Factory;

            Dictionary<Point, Stack<int>> adj = new Dictionary<Point, Stack<int>>(_lineStrings.Length * 2,_comparer3D); 
            HashSet<int> origIds = new HashSet<int>(_lineStrings.Length);

            for (int i = 0; i < _lineStrings.Length; i++)
            {
                origIds.Add(i);

                Point[] endsPts = { _lineStrings[i].StartPoint, _lineStrings[i].EndPoint };
                foreach (var pt in endsPts)
                {
                    if (!adj.ContainsKey(pt))
                    {
                        //Linestring already be snapped during data clean stage.
                        adj.Add(pt, new Stack<int>());
                    }

                    adj[pt].Push(i);
                }
            }

            // building a graph for all the vertex whose degree equals to two.
            UndirectedSparseGraph<int> graph = new UndirectedSparseGraph<int>();
            HashSet<int> visitedNodes = new HashSet<int>();

            foreach (var indices in adj.Values)
            {
                if(indices.Count == 2)
                {
                    // Pop out those two vertices.
                    var v = indices.Pop();
                    var w = indices.Pop();

                    graph.AddVertex(v);
                    graph.AddVertex(w);
                    graph.AddEdge(v, w);
                }
            }


            // Getting all the linestrings which needs merging.
            foreach (var source in graph.Vertices)
            {
                if(graph.Degree(source) == 1 && !visitedNodes.Contains(source)) // find the unvisited start vertex.
                {
                    var links = graph.DepthFirstWalk(source);

                    LinkedHashSet<Coordinate> pts = new LinkedHashSet<Coordinate>(); // Coordinate is not Geometry, therefore can not use the 3d comparer.
                    foreach (var id in links)
                    {
                        var l = _lineStrings[id];
                        visitedNodes.Add(id);
                        var ps = new List<Coordinate>(l.Coordinates);
                        if (id == source)
                        {
                            // need to determine the start point.
                            // all the node with degree =2 has already been popped to zero. There is no other situation with zero degree.
                            if (adj[l.StartPoint].Count == 0) // means this point is not the end point.
                            {
                                // starting with endpoint.
                                ps.Reverse();
                            }
                        }
                        else
                        {
                            // means this l is in the middle of the whole path.
                            var pre = f.CreatePoint(pts.Last);
                            if (_comparer3D.Equals(l.EndPoint, pre))
                                ps.Reverse();

                            ps.RemoveAt(0); // delete the first item, because this item is already in LinkedHashSet.
                        }

                        for (int i = 0; i < ps.Count; i++)
                        {
                            var pt = ps[i];
                            // already delete the same point from ps collection,
                            // therefore we don't need to use set.contains to find if this coordinate is already existed.
                            // This is important:
                            // 1. i don't want to create a Coordinate Comparer which can handle z value.
                            // 2. a vertical line's end points may be considered as the same point if we using default comparer.
                            // 3. delete the duplicated point first from ps collection can handle those problem.
                            pts.Add(pt); 
                        }
                    }

                    var nl = f.CreateLineString(pts.OrderedArray);
                    merged.Add(nl);
                }
            }

            // Getting the rest linestrings.
            origIds.ExceptWith(visitedNodes);
            foreach (var id in origIds)
            {
                var l = _lineStrings[id];
                merged.Add(l);
            }

            _lineStrings = merged.ToArray();
        }


        // For space syntax, we use undirected and weighted sparse graph.
        private void BuildingGraph(LineString[] lsSpan)
        {
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
                        // For metric weight.
                        //var lengthWeight = Math.Round((lsSpan[v].Length + lsSpan[w].Length) * 0.5, 6);// using round of length will cause several linestring have the same weight.
                        var lengthWeight = (lsSpan[v].Length3D() + lsSpan[w].Length3D()) * 0.5; // computing 3d length.


                        // For angular weight.
                        // Current point is pt, current segment is v.
                        Vector3D v1, v2;
                        if (pt == lsSpan[v].StartPoint)
                        {
                            // Using start point and end point.
                            v1 = new Vector3D(pt.X - lsSpan[v].EndPoint.X, pt.Y - lsSpan[v].EndPoint.Y, pt.Z - lsSpan[v].EndPoint.Z);
                        }
                        else
                        {
                            // pt is end point.
                            v1 = new Vector3D(pt.X - lsSpan[v].StartPoint.X, pt.Y - lsSpan[v].StartPoint.Y, pt.Z - lsSpan[v].StartPoint.Z);
                        }

                        if (pt == lsSpan[w].StartPoint)
                        {
                            v2 = new Vector3D(lsSpan[w].EndPoint.X - pt.X, lsSpan[w].EndPoint.Y - pt.Y, lsSpan[w].EndPoint.Z - pt.Z);
                        }
                        else
                        {
                            v2 = new Vector3D(lsSpan[w].StartPoint.X - pt.X, lsSpan[w].StartPoint.Y - pt.Y, lsSpan[w].StartPoint.Z - pt.Z);
                        }

                        // In space syntax methodology, angular weight is from 0 to 2.(0~pi)
                        var angularWeight = 2.0 / Math.PI * v1.AngleBetween(v2); // 3d angle.

                        Graph.AddEdge(v, w, (float)Math.Round(lengthWeight, 6), (float)Math.Round(angularWeight, 6));
                    }
                }
            }
        }

    }
}
