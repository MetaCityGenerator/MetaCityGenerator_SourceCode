using Rhino.Geometry;
using Rhino.Geometry.Intersect;

using System;
using System.Collections.Generic;
using System.Linq;

using MetaCity.Algorithms.Trees;
using MetaCity.DataStructures.Heaps;

namespace MetaCity.Planning.SpatialAnalysis
{
    public class CuttingPreparation
    {
        const double tolerance = 0.0001;
        private readonly List<Line> _rays;

        private readonly List<Line> _totalLines;

        private readonly BinaryMinHeap<Event> _minHeap;

        private readonly IntervalTree _intervalTree;

        public readonly Dictionary<Line, Point3d> _raysDic;

        private readonly Dictionary<Line, List<Line>> _raysTemp;

        public Dictionary<Line, double> Outlines { get; }
        public Dictionary<Line, double> OutlinesNormilized { get; }

        public CuttingPreparation(List<Line> curves, List<Point3d> pts, bool normalized = true, double radius = 500, int raysCount = 10)
        {
            var segment = (int)Math.Round(360d / raysCount);
            _rays = DrawRays(pts, radius, segment);

            // In case there are several identical curves in input collection.
            _raysDic = new Dictionary<Line, Point3d>(_rays.Count);
            _raysTemp = new Dictionary<Line, List<Line>>(_rays.Count);
            Outlines = new Dictionary<Line, double>(curves.Count);
            OutlinesNormilized = new Dictionary<Line, double>(curves.Count);

            for (int i = 0; i < curves.Count; i++)
            {
                Outlines.Add(curves.ElementAt(i), 0);
            }
            for (int i = 0; i < _rays.Count; i++)
            {
                _raysDic.Add(_rays[i], _rays[i].To);
                _raysTemp.Add(_rays[i], new List<Line>());
            }

            _totalLines = new List<Line>(curves.Count + _rays.Count);
            _totalLines.AddRange(Outlines.Keys);
            _totalLines.AddRange(_rays);

            // Instantiating all the fields.
            // For dictionary and array, GetHashCode method is from the original Curve class which derived from object class.
            _minHeap = new BinaryMinHeap<Event>(_totalLines.Count * 2);
            _intervalTree = new IntervalTree();

            Initialize();

            Sweepline();

            Normalized(normalized);
        }
        private void Initialize()
        {
            for (int i = 0; i < _totalLines.Count; i++)
            {
                var c = _totalLines[i];
                //if (!(c.Length < _tolerance))
                //    continue;

                // Create interval node.
                //c.BoundingBox.Get(out double[] Max, out double[] Min);
                var boundingBox = c.BoundingBox;
                double[] Max = new double[] { boundingBox.Max.X, boundingBox.Max.Y };
                double[] Min = new double[] { boundingBox.Min.X, boundingBox.Min.Y };
                IntervalNode node = new IntervalNode(new UInterval(Min[1], Max[1]), i);

                Event start = new Event(node, EventType.Start, Min[0]);
                Event end = new Event(node, EventType.End, Max[0]);

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
                            var debug = current.IntervalNode._id;

                            var currentCurve = _totalLines[debug];

                            var cid = interval._id;
                            var ctemp = _totalLines[cid];

                            if (Outlines.ContainsKey(currentCurve) && _raysDic.ContainsKey(ctemp))
                            {
                                var result = Intersection.CurveCurve(ctemp.ToNurbsCurve(), currentCurve.ToNurbsCurve(), tolerance, tolerance);
                                if (result.Count > 0)
                                {
                                    Point3d intersectPt = result[0].PointA;
                                    if (_raysTemp[ctemp].Count == 0)
                                    {
                                        _raysTemp[ctemp].Add(currentCurve);
                                        Outlines[currentCurve] += 1;
                                    }

                                    if ((ctemp.From.DistanceTo(new Point3d(intersectPt)) <= ctemp.From.DistanceTo(_raysDic[ctemp])))
                                    {
                                        Outlines[_raysTemp[ctemp][0]] -= 1;
                                        _raysDic[ctemp] = new Point3d(intersectPt);
                                        _raysTemp[ctemp][0] = currentCurve;
                                        Outlines[currentCurve] += 1;
                                    }

                                }
                            }
                            else if (Outlines.ContainsKey(ctemp) && _raysDic.ContainsKey(currentCurve))
                            {
                                //var result = LineSegementsIntersect(ctemp, currentCurve, out Vector3d intersectPt);
                                var result = Intersection.CurveCurve(ctemp.ToNurbsCurve(), currentCurve.ToNurbsCurve(), tolerance, tolerance);
                                if (result.Count > 0)
                                {
                                    Point3d intersectPt = result[0].PointA;
                                    if (_raysTemp[currentCurve].Count == 0)
                                    {
                                        _raysTemp[currentCurve].Add(ctemp);
                                        Outlines[ctemp] += 1;
                                    }
                                    if (result.Count > 0 && (currentCurve.From.DistanceTo(new Point3d(intersectPt)) <= currentCurve.From.DistanceTo(_raysDic[currentCurve])))
                                    {
                                        Outlines[_raysTemp[currentCurve][0]] -= 1;
                                        _raysDic[currentCurve] = new Point3d(intersectPt);
                                        _raysTemp[currentCurve][0] = ctemp;
                                        Outlines[ctemp] += 1;
                                    }
                                }
                            }
                            else { continue; }
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
                //TimeCalculation(_ts1, "单一sweep模块");
            }
        }

        private void Normalized(bool flag)
        {
            if (flag)
            {
                var tempKeys = Outlines.Keys.ToList();
                var tempValues = Outlines.Values.ToList();

                for (int i = 0; i < tempValues.Count; i++)
                {
                    var times = tempValues[i];
                    var ptg = Math.Round(times / tempKeys[i].Length, 4);

                    OutlinesNormilized.Add(tempKeys[i], ptg);
                }
            }
            return;
        }


        private static List<Line> DrawRays(List<Point3d> centerPointLists, double radius, int stepInDegree)
        {
            var ptListCount = centerPointLists.Count;
            List<Line> lineCollections = new List<Line>((360 / stepInDegree) * ptListCount);

            for (int i = 0; i < ptListCount; i++)
            {
                var centerPoint = centerPointLists[i];
                Point3d pointUp = new Point3d(centerPoint.X, centerPoint.Y + radius, 0d);

                for (int j = 0; j < 360; j += stepInDegree)
                {
                    var pt = RotatePoint(pointUp, centerPoint, j);
                    var line = new Line(centerPoint, new Point3d(pt.X, pt.Y, 0d));
                    lineCollections.Add(line);
                }
            }
            return lineCollections;
        }

        private static Point3d RotatePoint(Point3d pointToRotate, Point3d centerPoint, double angleInDegrees, int roundCount = 6)
        {
            double angleInRadians = angleInDegrees * (Math.PI / 180);
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);
            return new Point3d
            {
                X = Math.Round((cosTheta * (pointToRotate.X - centerPoint.X) -
                    sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X), roundCount)
                    ,
                Y = Math.Round((sinTheta * (pointToRotate.X - centerPoint.X) +
                    cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y), roundCount)
            };
        }

        /// <summary>
        /// Two event types: Start-event and End-event.
        /// </summary>
        /// 

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

        private void TimeCalculation(TimeSpan ts1, string topic)
        {
            //执行某操作
            TimeSpan ts2 = new TimeSpan(DateTime.Now.Ticks);

            TimeSpan ts = ts2.Subtract(ts1).Duration(); //时间差的绝对值

            double spanTotalSeconds = double.Parse(ts.TotalSeconds.ToString()); //执行时间的总秒数
            Console.WriteLine("{0}模块：计算用时  {1}s", topic, Math.Round(spanTotalSeconds, 2));
        }
    }
}
