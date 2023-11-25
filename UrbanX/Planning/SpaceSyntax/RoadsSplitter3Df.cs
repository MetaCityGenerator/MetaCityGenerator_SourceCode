using System;
using System.Collections.Generic;
using System.Linq;

using UrbanX.Algorithms.Geometry3D;
using UrbanX.Algorithms.Trees;
using UrbanX.DataStructures.Geometry3D;
using UrbanX.DataStructures.Heaps;


namespace UrbanX.Planning.SpaceSyntax
{
    public class RoadsSplitter3Df
    {
        private readonly double _tolerance;

        private readonly int _round;

        private readonly LinesIntersection3D _intersection3D;


        /// <summary>
        /// The splitter points for each linestring including the endpoints.
        /// Using those points to reconstruct LineString.
        /// </summary>
        private readonly Dictionary<ULine, Stack<UPoint>> _splitters;

        /// <summary>
        /// All the line segments waiting for splitting.
        /// </summary>
        private readonly ULine[] _segs;

        /// <summary>
        /// Interval MinPriorityQueue for events.
        /// Event class has own Comparer which only consider the X-coordinate of rectange(bounding box of Curve).
        /// </summary>
        private readonly BinaryMinHeap<Event> _minHeap;

        /// <summary>
        /// Interval Argumented RBTree for storing IntervalNode which contains interval, ID and Max limit.
        /// Can operate Insert, Delete and Find all the intersections methods.
        /// </summary>
        private readonly IntervalTree _intervalTree;


        /// <summary>
        /// All the splitted line segments. 
        /// Using Polyline to store line segment for convenience of GraphBuilder.
        /// </summary>
        public UPolyline[] SplittedLineSegments { get; }

        public RoadsSplitter3Df(UPolyline[] polys, double tolerance)
        {
            _tolerance = tolerance;
            _round = (int)Math.Abs(Math.Log10(_tolerance));

            _intersection3D = new LinesIntersection3D(_tolerance);

            _splitters = GenerateLines(in polys); // keys are all the cleaned lineStrings.
            _segs = _splitters.Keys.ToArray();

            _minHeap = new BinaryMinHeap<Event>(_splitters.Count * 2);
            _intervalTree = new IntervalTree();
            Initialize();
            Sweepline();
            SplittedLineSegments = RebuildLineSegments();
        }


        private  Dictionary<ULine, Stack<UPoint>> GenerateLines(in UPolyline[] polys)
        {
            Dictionary<ULine, Stack<UPoint>> dict = new Dictionary<ULine, Stack<UPoint>>(polys.Length*6);

            for (int i = 0; i < polys.Length; i++)
            {
                var segs = polys[i].GetSegments();

                foreach (var seg in segs)
                {
                    if (seg.Length < _tolerance)
                        continue;
                    if (!dict.ContainsKey(seg))
                    {
                        dict.Add(seg, new Stack<UPoint>()); // end points will be added during rebuilding stage, and also reducing the coordinate precision.
                    }
                }
            }
            return dict;
        }




        private void Initialize()
        {
            for (int i = 0; i < _segs.Length; i++)
            {
                var seg = _segs[i];
                var env = seg.GetEnvelope();

                IntervalNode node = new IntervalNode(new UInterval(env.MinY, env.MaxY), i);
              
                Event start = new Event(node, EventType.Start, env.MinX);
                Event end = new Event(node, EventType.End, env.MaxX);

                _minHeap.Add(start);
                _minHeap.Add(end);
            }
        }


        private void Sweepline()
        {
            while (!_minHeap.IsEmpty)
            {
                var current = _minHeap.ExtractMin();
                switch (current.EventType)
                {
                    case EventType.Start:

                        // Insert the first node as root.
                        if (_intervalTree.Count == 0)
                        {
                            _intervalTree.InsertNode(current.IntervalNode);
                            continue;
                        }

                        // Find all the overlaped nodes.
                        var intervals = _intervalTree.SearchOverlaps(current.IntervalNode, _intervalTree.Root);
                        foreach (var interval in intervals)
                        {
                            var u = _segs[current.IntervalNode._id]; // the current Line.
                            var v = _segs[interval._id];             // the 2D overlaped Line.

                            // Computing 3d intersection for two curves.
                            //var flag = currentCurve.Intersection3D(ctemp,_tolerance ,out Point[] intersectsPts);
                            var flag = _intersection3D.Compute(u.From, u.To, v.From, v.To);

                            if (flag)
                            {
                                // found intersection points. PIntersections.Length should equal to QIntersections.Length.
                                foreach (var x in _intersection3D.PIntersections)
                                {
                                    var checkEqual = x.EqualsExact(u.From, _tolerance) | x.EqualsExact(u.To, _tolerance); // end points.
                                    if (checkEqual)
                                        continue;

                                    _splitters[u].Push(x);
                                }

                                foreach (var x in _intersection3D.QIntersections)
                                {
                                    var checkEqual = x.EqualsExact(v.From, _tolerance) | x.EqualsExact(v.To, _tolerance);
                                    if (checkEqual)
                                        continue;

                                    _splitters[v].Push(x);
                                }
                            }
                        }

                        // Insert current node into IntervalTree.
                        _intervalTree.InsertNode(current.IntervalNode);
                        break;

                    case EventType.End:
                        // Remove current node from intervalTree.Equivalent to deactivate the current curve for computing intersection.
                        // For intervalNode, both interval and ID are considered for comparing and generate the Hashcode.
                        _intervalTree.DeleteNode(current.IntervalNode);
                        break;
                }
            }
        }


        private UPolyline[] RebuildLineSegments()
        {
            HashSet<UPolyline> result = new HashSet<UPolyline>(_segs.Length*3);

            var pComparer = new Point3DComparer(_tolerance);
            SortedSet<UPoint> pts = new SortedSet<UPoint>(pComparer);
            

            foreach (var item in _splitters)
            {
                // Rducing all the points, thus we can reduce LineStrings.
                var p0 = item.Key.From.ReducePrecision(_round);
                var p1 = item.Key.To.ReducePrecision(_round);

                pts.Add(p0);
                pts.Add(p1);

                var splitterPts = item.Value;
                while (splitterPts.Count > 0)
                {
                    var p = splitterPts.Pop().ReducePrecision(_round);
                    pts.Add(p);
                }

                var spts = pts.ToArray();
                // Must round all the linestring because the floating number.
                // For graph builder, GeometryComparer3D don't use tolerance, therefore if we haven't round points, may cause error in GraphBuilder.

                for (int i = 0; i < spts.Length - 1; i++)
                {
                    UPolyline l = new UPolyline(new UPoint[] { spts[i], spts[i + 1] });
                    if (l.Length < _tolerance) // delete invalid item.
                        continue;

                    result.Add(l);
                }

                // clear points.
                pts.Clear();
            }
            return result.ToArray();
        }



        public static UPolyline[] SplitRoads(UPolyline[] polys, float tolerance)
        {
            RoadsSplitter3Df splitter = new RoadsSplitter3Df(polys, tolerance);
            return splitter.SplittedLineSegments;
        }
    }
}
