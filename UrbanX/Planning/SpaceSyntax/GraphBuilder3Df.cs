using System;
using System.Collections.Generic;

using UrbanX.DataStructures.Set;
using UrbanX.DataStructures.Graphs;
using UrbanX.Planning.Utility;
using UrbanX.DataStructures.Geometry3D;
using System.Numerics;

namespace UrbanX.Planning.SpaceSyntax
{
  // using float and vector.
    public sealed class GraphBuilder3Df
    {
        private UPolyline[] _polylines;

        /// <summary>
        /// Using index(int) to represent each lineString. Share the same order with _lineStrings array.
        /// </summary>
        private readonly int[] _segmentVertices;

        private readonly Dictionary<UPoint, Stack<int>> _adjacentSegments;

        public SpaceSyntaxGraph Graph { get; }

        public ReadOnlySpan<UPolyline> Roads => _polylines;


        public GraphBuilder3Df(UPolyline[] segments , bool merging = true)
        {
            _polylines = segments;
            _segmentVertices = new int[_polylines.Length];
            _adjacentSegments = new Dictionary<UPoint, Stack<int>>(_polylines.Length * 2);

            if (merging)
                MergeLines();

            Graph = new SpaceSyntaxGraph(_polylines.Length);
        }

        public void Build()
        {
            // Add all vertices of graph to collection.
            // Using indices to represent the vertices instead of using objects itselves.
            // Objects(curves) can be queried later by using indices.
            UPoint[] endsPts = new UPoint[2];
            for (int i = 0; i < _polylines.Length; i++)
            {
                _segmentVertices[i] = i;

                endsPts[0] = _polylines[i].First;
                endsPts[1] = _polylines[i].Last;


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

            BuildingGraph(_polylines);
        }

        public void Build(double[] weights)
        {
            // Add all vertices of graph to collection.
            // Using indices to represent the vertices instead of using objects itselves.
            // Objects(curves) can be queried later by using indices.
            UPoint[] endsPts = new UPoint[2];
            for (int i = 0; i < _polylines.Length; i++)
            {
                _segmentVertices[i] = i;

                endsPts[0] = _polylines[i].First;
                endsPts[1] = _polylines[i].Last;


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

            BuildingGraph(_polylines, weights);
        }

        private void MergeLines()
        {
            // Collection of merged linestrings.
            List<UPolyline> merged = new List<UPolyline>(_polylines.Length);

            Dictionary<UPoint, Stack<int>> adj = new Dictionary<UPoint, Stack<int>>(_polylines.Length * 2); 
            HashSet<int> origIds = new HashSet<int>(_polylines.Length);

            for (int i = 0; i < _polylines.Length; i++)
            {
                origIds.Add(i);

                UPoint[] endsPts = { _polylines[i].First, _polylines[i].Last };
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

                    LinkedHashSet<UPoint> pts = new LinkedHashSet<UPoint>(); // Coordinate is not Geometry, therefore can not use the 3d comparer.
                    foreach (var id in links)
                    {
                        var l = _polylines[id];
                        visitedNodes.Add(id);
                        var ps = new List<UPoint>(l.Coordinates);
                        if (id == source)
                        {
                            // need to determine the start point.
                            // all the node with degree =2 has already been popped to zero. There is no other situation with zero degree.
                            if (adj[l.First].Count == 0) // means this point is not the end point.
                            {
                                // starting with endpoint.
                                ps.Reverse();
                            }
                        }
                        else
                        {
                            // means this l is in the middle of the whole path.
                            var pre =pts.Last;
                            if (l.Last.Equals(pre))
                                ps.Reverse();

                            ps.RemoveAt(0); // delete the first item, because this item is already in LinkedHashSet.
                        }

                        for (int i = 0; i < ps.Count; i++)
                        {
                            // already delete the same point from ps collection,
                            // therefore we don't need to use set.contains to find if this coordinate is already existed.
                            // This is important:
                            // 1. i don't want to create a Coordinate Comparer which can handle z value.
                            // 2. a vertical line's end points may be considered as the same point if we using default comparer.
                            // 3. delete the duplicated point first from ps collection can handle those problem.
                            pts.Add(ps[i]); 
                        }
                    }

                    UPolyline pl = new UPolyline(pts.OrderedArray);
                    merged.Add(pl);
                }
            }

            // Getting the rest linestrings.
            origIds.ExceptWith(visitedNodes);
            foreach (var id in origIds)
            {
                var l = _polylines[id];
                merged.Add(l);
            }

            _polylines = merged.ToArray();
        }


        // For space syntax, we use undirected and weighted sparse graph.
        private void BuildingGraph(UPolyline[] lsSpan)
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
                        var lengthWeight = (lsSpan[v].Length+ lsSpan[w].Length) * 0.5; // computing 3d length.


                        // For angular weight.
                        // Current point is pt, current segment is v.
                        UVector3 v1, v2;

                        if (pt == lsSpan[v].First)
                        {
                            v1 = (UVector3)pt - (UVector3)lsSpan[v].Last;
                        }
                        else
                        {
                            // pt is end point.
                            v1 = (UVector3)pt - (UVector3)lsSpan[v].First;
                        }

                        if (pt == lsSpan[w].First)
                        {
                            v2 = (UVector3)lsSpan[w].Last - (UVector3)pt;
                        }
                        else
                        {
                            v2 = (UVector3)lsSpan[w].First - (UVector3)pt;
                        }

                        // In space syntax methodology, angular weight is from 0 to 2.(0~pi)
                        var angularWeight = 2.0 / Math.PI * v1.AngleBetween(v2); // 3d angle.

                        Graph.AddEdge(v, w, (float)Math.Round(lengthWeight, 6), (float)Math.Round(angularWeight, 6));
                    }
                }
            }
        }

        // For space syntax, we use undirected and weighted sparse graph.
        private void BuildingGraph(UPolyline[] lsSpan, double[] weights)
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
                        var lengthWeight = (lsSpan[v].Length + lsSpan[w].Length) * 0.5; // computing 3d length.
                        var customWeight = (weights[v] + weights[w]) * 0.5; // computing 3d length.


                        // For angular weight.
                        // Current point is pt, current segment is v.
                        UVector3 v1, v2;

                        if (pt == lsSpan[v].First)
                        {
                            v1 = (UVector3)pt - (UVector3)lsSpan[v].Last;
                        }
                        else
                        {
                            // pt is end point.
                            v1 = (UVector3)pt - (UVector3)lsSpan[v].First;
                        }

                        if (pt == lsSpan[w].First)
                        {
                            v2 = (UVector3)lsSpan[w].Last - (UVector3)pt;
                        }
                        else
                        {
                            v2 = (UVector3)lsSpan[w].First - (UVector3)pt;
                        }

                        // In space syntax methodology, angular weight is from 0 to 2.(0~pi)
                        var angularWeight = 2.0 / Math.PI * v1.AngleBetween(v2); // 3d angle.

                        Graph.AddEdge(v, w, (float)Math.Round(lengthWeight,6), (float)Math.Round(angularWeight, 6), (float)Math.Round(customWeight, 6));
                    }
                }
            }
        }
    }
}
