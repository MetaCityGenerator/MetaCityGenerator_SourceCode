using System;
using System.Collections.Generic;
using System.Linq;


using NetTopologySuite.Geometries;

using UrbanX.Algorithms.Geometry3D;
using UrbanX.Algorithms.Trees;
using UrbanX.DataStructures.Heaps;
using UrbanX.Planning.Utility;

namespace UrbanX.Planning.SpaceSyntax
{
    /// <summary>
    /// Two event types: Start-event and End-event.
    /// </summary>
    public enum EventType:byte
    {
        Start = 0,
        End = 1
    }

    /// <summary>
    /// Event used in Sweepline algorithm for finding the intersections amonge a set of rectangles.
    /// </summary>
    public class Event : IComparable<Event>
    {
        private readonly double _x;

        public IntervalNode IntervalNode { get; }
        public EventType EventType { get; }

        public Event(IntervalNode node, EventType type, double x)
        {
            EventType = type;
            IntervalNode = node;
            _x = x;
        }

        /// <summary>
        /// Comparer used for priority queue.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Event other)
        {
            var c = _x.CompareTo(other._x);

            // Handle vertical line. Start event should always go first.
            return c == 0 ? EventType.CompareTo(other.EventType) : c;
        }
    }


    public class RoadsSplitter3D
    {
        private readonly GeometryFactory _gf;

        private readonly double _tolerance;

        private readonly int _round;

        private readonly SegmentsIntersection3D _intersection3D;

        private readonly GeometryComparer3D _comparer3D;

        private readonly PointComparer3D _ptComparer;

        /// <summary>
        /// The splitter points for each linestring including the endpoints.
        /// Using those points to reconstruct LineString.
        /// </summary>
        private readonly Dictionary<LineString, Stack<Point>> _splitters;

        private readonly LineString[] _segs;

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



        public RoadsSplitter3D(LineString[] lineStrings, GeometryFactory gf)
        {
            // Check precision model.
            if (gf.PrecisionModel.IsFloating)
                PrecisionSetting.ChangePrecision(ref gf);

            _gf = gf;
            _tolerance = 1.0 / _gf.PrecisionModel.Scale;
            _round = (int)Math.Abs(Math.Log10(_tolerance));

            _comparer3D = new GeometryComparer3D(false);
            _ptComparer = new PointComparer3D(_tolerance);
            _intersection3D = new SegmentsIntersection3D(_tolerance, _gf);

            _splitters = GenerateLines(lineStrings); // keys are all the cleaned lineStrings.
            _segs = _splitters.Keys.ToArray();

            _minHeap = new BinaryMinHeap<Event>(_splitters.Count * 2);
            _intervalTree = new IntervalTree();
            Initialize();
            Sweepline();
        }


        private  Dictionary<LineString, Stack<Point>> GenerateLines(LineString[] lineStrings )
        {
            Dictionary<LineString, Stack<Point>> dict = new Dictionary<LineString, Stack<Point>>(lineStrings.Length * 6, _comparer3D);

            //List<int> hash = new List<int>();
            foreach (var l in lineStrings)
            {
                var pts = l.Coordinates;

                for (int i = 0; i < pts.Length - 1; i++)
                {
                    LineString seg = _gf.CreateLineString(new Coordinate[] { pts[i], pts[i + 1] }); // using factory to create geometry is a better way.
                   // hash.Add(_comparer3D.GetHashCode(seg));
                    //var c = seg.Centroid;
                    //var e = seg.EnvelopeInternal;
                    //var v = seg.IsValid; // for vertical LineString, return false. Issue to Nts.
                    if (seg.Length3D() <_tolerance ) // delete invalid item.
                        continue;

                    if (!dict.ContainsKey(seg))
                        dict.Add(seg, new Stack<Point>() ); // Using point comparer to set up Point HashSet.
                                                                                                        // Important: Adding the endpoints into set to fix this line segment.
                }
            }

            return dict;
        }




        private void Initialize()
        {
       
            for (int i = 0; i < _segs.Length; i++)
            {
                var seg = _segs[i];

                IntervalNode node = new IntervalNode(new UInterval(seg.EnvelopeInternal.MinY, seg.EnvelopeInternal.MaxY), i);
              
                Event start = new Event(node, EventType.Start, seg.EnvelopeInternal.MinX);
                Event end = new Event(node, EventType.End, seg.EnvelopeInternal.MaxX);

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
                            var flag = _intersection3D.Compute(u.StartPoint.Coordinate, u.EndPoint.Coordinate, v.StartPoint.Coordinate, v.EndPoint.Coordinate);

                            if (flag)
                            {
                                // found intersection points. PIntersections.Length should equal to QIntersections.Length.
                                foreach (var x in _intersection3D.PIntersections)
                                {
                                    //var test = _splitters.ContainsKey(u);

                                    var checkEqual = _ptComparer.Equals(x, u.StartPoint) | _ptComparer.Equals(x, u.EndPoint);
                                    if (checkEqual)
                                        continue;

                                    _splitters[u].Push(x);
                                }

                                foreach (var x in _intersection3D.QIntersections)
                                {
                                    var checkEqual = _ptComparer.Equals(x, v.StartPoint) | _ptComparer.Equals(x, v.EndPoint);
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


        private LineString[] RebuildLineStrings()
        {
            HashSet<LineString> result = new HashSet<LineString>(_segs.Length*3 ,_comparer3D);
            SortedSet<Point> pts = new SortedSet<Point>(_ptComparer);
            

            foreach (var item in _splitters)
            {
   
                // Rducing all the points, thus we can reduce LineStrings.
                var p0 = item.Key.StartPoint;
                p0.Reduce3D(_round);
                var p1 = item.Key.EndPoint;
                p1.Reduce3D(_round);
                pts.Add(p0);
                pts.Add(p1);

                var sk = item.Value;
                while (sk.Count > 0)
                {
                    var p = sk.Pop();
                    p.Reduce3D(_round);
                    pts.Add(p);
                }

                var spts = pts.ToArray();
                // Must round all the linestring because the floating number.
                // For graph builder, GeometryComparer3D don't use tolerance, therefore if we haven't round points, may cause error in GraphBuilder.

                for (int i = 0; i < spts.Length - 1; i++)
                {
                    LineString l = _gf.CreateLineString(new Coordinate[] { spts[i].Coordinate, spts[i + 1].Coordinate }); // using factory to create geometry is a better way.
                    if (l.Length3D() < _tolerance) // delete invalid item.
                        continue;

                    result.Add(l);
                }

                // clear points.
                pts.Clear();
            }
            
            return result.ToArray();
        }



        public static LineString[] SplitRoads(LineString[] lineStrings , GeometryFactory gf)
        {
            RoadsSplitter3D splitter3D = new RoadsSplitter3D(lineStrings ,gf);
            return splitter3D.RebuildLineStrings();
        }

    }
}
