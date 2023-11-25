using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Operation.Distance3D;
using NetTopologySuite.Precision;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UrbanX.Planning.Utility;

namespace UrbanX.Algorithms.Geometry3D
{
    public struct PointPosition
    {
        public int LineStringId { get; }

        public EndPosition Position { get; }

        public enum EndPosition: byte
        {
            Start,
            End
        }

        public PointPosition(int lineId, EndPosition position)
        {
            LineStringId = lineId;
            Position = position;
        }
    }

    public sealed class LineStringSnapper3D
    {
        private readonly GeometryFactory _gf;

        private readonly double _tolerance;

        private static readonly GeometryComparer3D _comparer3D = new GeometryComparer3D();   

        private readonly LineString[] _lineStrings;

        private readonly Dictionary<Point, Stack<PointPosition>> _endPointsPosition;

        private readonly Dictionary<Point, Envelope> _endPoints;

        private readonly STRtree<Point> _rtree;


        /// <summary>
        /// Storing all the points which have been visited during snapping process.
        /// </summary>
        private readonly HashSet<Point> _visited;


        public LineString[] SnappedLineStrings =>_lineStrings;


        /// <summary>
        /// Contructor for LineString3DSnapper. This method won't reduce the precesion of geometry.
        /// </summary>
        /// <param name="curves3D">The LineString collection for snapping.</param>
        /// <param name="gf">Using GeometryFactory to controal tolerance.</param>
        public LineStringSnapper3D(LineString[] curves3D , GeometryFactory gf)
        {
            // Check precision model.
            if (gf.PrecisionModel.IsFloating)
                PrecisionSetting.ChangePrecision(ref gf);

            _gf = gf;
            //_reducer = new GeometryPrecisionReducer(_gf.PrecisionModel) { ChangePrecisionModel = true };
            _tolerance = 1.0 / _gf.PrecisionModel.Scale;

            HashSet<LineString> cullDuplicate = new HashSet<LineString>(curves3D, _comparer3D); // cull all the duplicated items and normalize all the linestring, therefore startpoint > endpoint should be true.
            _lineStrings = cullDuplicate.ToArray();

            _comparer3D.ChangeNormalize(false); 

            _endPointsPosition = new Dictionary<Point, Stack<PointPosition>>(_lineStrings.Length * 2, _comparer3D); // geometries should be already normalized.
            BuildDict();

            _endPoints = new Dictionary<Point, Envelope>(_endPointsPosition.Count, _comparer3D);
            _rtree = new STRtree<Point>(_endPointsPosition.Count);
            BuildTree();

            _visited = new HashSet<Point>(_endPointsPosition.Count, _comparer3D); // points can ignore normalization.
            
        }


        /// <summary>
        /// 1. add all the end points to dict;
        /// </summary>
        private void BuildDict()
        {
            //Span<LineString> lsSpan = new Span<LineString>(_lineStrings);

            for (int i = 0; i < _lineStrings.Length; i++)
            {
                Point[] endsPts = { _lineStrings[i].StartPoint, _lineStrings[i].EndPoint };

                int e = 0;
                foreach (var pt in endsPts)
                {
                    if (!_endPointsPosition.ContainsKey(pt))
                    {
                        _endPointsPosition.Add(pt, new Stack<PointPosition>());
                    }

                    PointPosition.EndPosition ep = e == 0 ? PointPosition.EndPosition.Start : PointPosition.EndPosition.End;
                    PointPosition pp = new PointPosition(i, ep); // use point position to query the geom on the linestring, then we can change this point more efficiently during snapping process.

                    _endPointsPosition[pt].Push(pp);
                    e++;
                }
            }
        }


        /// <summary>
        /// 2. add all the end points into strtree.
        /// </summary>
        private void BuildTree()
        {
            foreach (var item in _endPointsPosition)
            {
                var pt = item.Key;
                var ev = pt.EnvelopeInternal;
                ev.ExpandBy(_tolerance*0.5); // expand towards both sides.

                _rtree.Insert(ev, pt);
                _endPoints.Add(pt, ev);
            }
        }

        

        /// <summary>
        /// Snapping all the end points for input LineString collection.
        /// </summary>
        public void Snapp()
        {
            foreach (var item in _endPoints)
            {
                var pt = item.Key;

                if (_visited.Contains(pt))
                    continue;

                var ev = item.Value;
                _visited.Add(pt);
                _rtree.Remove(ev, pt); // Remove current point from Rtree to avoid query this point.

                var nodes = _rtree.Query(ev);

                if (nodes.Count > 0) // Because we already removed the current point, the minimum count of queried nodes should be zero.
                {
                    // means there are some points need to be snapped.
                    List<Point> candidates = new List<Point>(nodes.Count);

                    for (int i = 0; i < nodes.Count; i++)
                    {
                        //if (_comparer3D.Equals(nodes[i], pt)) // need to change the current point itself. 
                        //    continue;

                        var dist = Distance3DOp.Distance(nodes[i], pt);
                        if (dist < _tolerance)
                        {
                            candidates.Add(nodes[i]);
                            _visited.Add(nodes[i]); 
                            _rtree.Remove(_endPoints[nodes[i]], nodes[i]); // remove item in rtree should accelerate the quering speed.
                        }
                    }

                    // found all the candidates, then we need to snap them to current point.
                    for (int i = 0; i < candidates.Count; i++)
                    {
                        //var target = (Point)_reducer.Reduce(pt); // just use the original pt as target.

                        var c = candidates[i];

                        var belongs = _endPointsPosition[c];
                        while (belongs.Count > 0)
                        {
                            var p = belongs.Pop(); // delete some items to save memory.
                            if (p.Position == PointPosition.EndPosition.Start)
                                _lineStrings[p.LineStringId].Coordinates[0] = pt.Coordinate; // checked. the item of a readonly collection can be set.
                            else
                                _lineStrings[p.LineStringId].Coordinates[_lineStrings[p.LineStringId].Count - 1] = pt.Coordinate;
                        }
                    }
                }
            }

            _endPoints.Clear();
            _endPointsPosition.Clear();
        }
    }
}
