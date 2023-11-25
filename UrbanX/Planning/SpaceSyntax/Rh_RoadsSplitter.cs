using Rhino.Geometry;
using Rhino.Geometry.Intersect;

using System;
using System.Collections.Generic;
using System.Linq;

using UrbanX.Algorithms.Trees;
using UrbanX.DataStructures.Heaps;

namespace UrbanX.Planning.SpaceSyntax
{
    /// <summary>
    /// Handling all the degenerate cases: overlaps, shapely identical, intersection and invalid curve.
    /// Using sweepline algorithm for finding the rectangle intersections.
    /// <para>Internal data strcutre: IntervalTree --> RBTree ; PriorityQueue --> BinaryMinheap . </para>
    /// </summary>
    public class Rh_RoadsSplitter
    {
        // tolerance for compute curves intersection.
        private readonly double _tolerance;

        /// <summary>
        /// Internal collection for storing curve and the parameters for splitting.
        /// For each curve, the parameters will be sorted.
        /// </summary>
        private readonly Dictionary<Curve, SortedSet<double>> _splitParameters;

        /// <summary>
        /// Internal collection for storing curves.
        /// Using Hashset to cull duplicated curves.
        /// </summary>
        private readonly Curve[] _curves;

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
        /// The collection of cleaned and splitted curves.
        /// Handed all the degenerate cases: overlaps, shapely identical, intersection and invalid curves.
        /// </summary>
        public HashSet<Curve> Curves { get; }

        public Rh_RoadsSplitter(Curve[] curves, double tolerance)
        {
            // The minimum tolerance should be 1E-8.
            _tolerance = tolerance< 1E-8 ? 1E-8 : tolerance;

            // This costimized comparer only considers the shape of curve, not the direction of curve.
            // Which means if two curves are shapely identical, meanwhile have different directions, they will be determined as equal.
            // Only hashset<curve> use this comparer.
            var curveComparer = new CurveEqualityComparer(_tolerance);

            // In case there are several identical curves in input collection.
            _curves = new HashSet<Curve>(curves, curveComparer).ToArray();

            // Instantiating all the fields.
            // For dictionary and array, GetHashCode method is from the original Curve class which derived from object class.
            _splitParameters = new Dictionary<Curve, SortedSet<double>>(_curves.Length);
            _minHeap = new BinaryMinHeap<Event>(_curves.Length * 2);
            _intervalTree = new IntervalTree();

            // The collection of splitted curves.
            Curves = new HashSet<Curve>(curveComparer);

            Initialize();
            Sweepline();
            SplitCurves();
        }

        private void Initialize()
        {
            for (int i = 0; i < _curves.Length; i++)
            {
                var c = _curves[i];
                if (!c.IsValid || c.GetLength() < _tolerance)
                    continue;

                if (c.PointAtStart.CompareTo(c.PointAtEnd) > 0)
                    c.Reverse();

                _splitParameters.Add(c, new SortedSet<double>());

                // Create interval node.
                var rect = c.GetBoundingBox(Plane.WorldXY);
                IntervalNode node = new IntervalNode(new UInterval(rect.Min.Y, rect.Max.Y), i);

                Event start = new Event(node, EventType.Start, rect.Min.X);
                Event end = new Event(node, EventType.End, rect.Max.X);

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
                            var currentCurve = _curves[current.IntervalNode._id];
                            var ctemp = _curves[interval._id];

                            // Computing intersection for two curves.
                            // IMPORTANT: if curve is 1.18E+10 far away than  point[0,0,0], error will occur.
                            // Please make sure move all curves neart to origin.
                            var sectEvents = Intersection.CurveCurve(currentCurve, ctemp, _tolerance, _tolerance);

                            // If sectEvents is not empty, add all the parameters to each curves respectively.
                            foreach (var sectEvent in sectEvents)
                            {
                                if (sectEvent.IsPoint)
                                {
                                    _splitParameters[currentCurve].Add(sectEvent.ParameterA);
                                    _splitParameters[ctemp].Add(sectEvent.ParameterB);
                                }
                                else
                                {
                                    // Curves as overlaped.
                                    _splitParameters[currentCurve].Add(sectEvent.OverlapA.Min);
                                    _splitParameters[currentCurve].Add(sectEvent.OverlapA.Max);

                                    _splitParameters[ctemp].Add(sectEvent.OverlapB.Min);
                                    _splitParameters[ctemp].Add(sectEvent.OverlapB.Max);
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


        /// <summary>
        /// Working on the splitParameters dictionary. 
        /// </summary>
        private void SplitCurves()
        {
            foreach (var item in _splitParameters)
            {
                var curve = item.Key;
                var parameters = item.Value;

                if (parameters.Count == 0)
                {
                    // No need for splitting, then just add current curve to Curves.
                    // No need for checking validation, because  the initialize method has already done the work .
                    Curves.Add(curve);
                    continue;
                }

                // Splitting current curve and add result to Curves memberwisely.
                var result = curve.Split(parameters);

                for (int i = 0; i < result.Length; i++)
                {
                    // Need to check curve's validation because those curves are newly generated by splitting.
                    var c = result[i];
                    if (!c.IsValid || c.GetLength() < _tolerance)
                        continue;

                    Curves.Add(c);
                }
            }
        }



        /// <summary>
        /// Both the input curves and new curves generated from  overlaped curves may have chance be shapely identical.
        /// This comparer is using the properties of curve, such as length, point and tangent to determine the equality.
        /// Using 1.0E-8 tolerance for accuracy.
        /// </summary>
        private class CurveEqualityComparer : EqualityComparer<Curve>
        {
            private readonly int _round;


            public CurveEqualityComparer(double tolerance)
            {
                _round = (int)Math.Log10(1 / tolerance);
            }


            // Proof of equality:
            /// <summary>
            /// For curve and line, the equlity condition defined blow are suffice.
            /// But as for polyline, there is one situation that two diffrent polylines meet this codition , provided the start and end segments are identical.
            /// In this case, there will have some overlap events occured, and those intersection event should have been handled already during the FindIntersection stage.
            /// Therefore, this equality comparer is suffice for all the situations.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public override bool Equals(Curve x, Curve y)
            {
                // curves with oposite direction can be equal.
                if (x == null && y == null)
                    return true;
                else if (x == null || y == null)
                    return false;
                else if (x.SpanCount == y.SpanCount && RoundLength(x.GetLength()) == RoundLength(y.GetLength()) && x.Degree == y.Degree && ((RoundPoint(x.PointAtStart) == RoundPoint(y.PointAtStart) &&
                    RoundPoint(x.PointAtEnd) == RoundPoint(y.PointAtEnd) && RoundVector(x.TangentAtStart) == RoundVector(y.TangentAtStart) && RoundVector(x.TangentAtEnd) == RoundVector(y.TangentAtEnd)) ||
                    (RoundPoint(x.PointAtStart) == RoundPoint(y.PointAtEnd) && RoundPoint(x.PointAtEnd) == RoundPoint(y.PointAtStart)
                    && RoundVector(x.TangentAtStart) == RoundVector(y.TangentAtEnd) && RoundVector(x.TangentAtEnd) == RoundVector(y.TangentAtStart))))
                    return true;
                else
                    return false;
            }

            public override int GetHashCode(Curve c)
            {
                var curveLength = RoundLength(c.GetLength());
                var pStart = RoundPoint(c.PointAtStart);
                var pEnd = RoundPoint(c.PointAtEnd);
                var tStart = RoundVector(c.TangentAtStart);
                var tEnd = RoundVector(c.TangentAtEnd);

                // MSDN docs recommend XOR'ing the internal values to get a hash code
                int hCode = c.SpanCount.GetHashCode() ^ curveLength.GetHashCode() ^ c.Degree.GetHashCode() ^ pStart.GetHashCode()
                    ^ pEnd.GetHashCode() ^ tStart.GetHashCode() ^ tEnd.GetHashCode();
                return hCode;
            }

            private double RoundLength(double length)
            {
                return Math.Round(length, _round);
            }

            private Point3d RoundPoint(Point3d pt)
            {
                return new Point3d(Math.Round(pt.X, _round), Math.Round(pt.Y, _round), Math.Round(pt.Z, _round));
            }

            private Vector3d RoundVector(Vector3d v)
            {
                return new Vector3d(Math.Round(v.X, _round), Math.Round(v.Y, _round), Math.Round(v.Z, _round));
            }

        }


        /// <summary>
        /// Two event types: Start-event and End-event.
        /// </summary>
        private enum EventType
        {
            Start = 0,
            End = 1
        }

        /// <summary>
        /// Event used in Sweepline algorithm for finding the intersections amonge a set of rectangles.
        /// </summary>
        private class Event : IComparable<Event>
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
    }
}
