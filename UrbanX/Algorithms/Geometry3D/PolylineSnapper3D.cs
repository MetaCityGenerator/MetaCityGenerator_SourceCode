using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UrbanX.DataStructures.Geometry3D;

namespace UrbanX.Algorithms.Geometry3D
{

    public sealed class PolylineSnapper3D
    {

        private readonly double _tolerance;



        private readonly UPolyline[] _polys;

        private readonly Dictionary<UPoint, Stack<PointPosition>> _endPointsPosition;

        private readonly Dictionary<UPoint, Envelope> _endPoints;

        private readonly STRtree<UPoint> _rtree;


        /// <summary>
        /// Storing all the points which have been visited during snapping process.
        /// </summary>
        private readonly HashSet<UPoint> _visited;


        public UPolyline[] SnappedPolylines =>_polys;


        /// <summary>
        /// Contructor for LineString3DSnapper. This method won't reduce the precesion of geometry.
        /// </summary>
        /// <param name="polys">The LineString collection for snapping.</param>
        /// <param name="gf">Using GeometryFactory to controal tolerance.</param>
        public PolylineSnapper3D(UPolyline[] polys , double tolerance)
        {
            _tolerance = tolerance;

            HashSet<UPolyline> cullDuplicate = new HashSet<UPolyline>(polys); // cull all the duplicated items. Polyline has already been normalized.
            _polys = cullDuplicate.ToArray();

            _endPointsPosition = new Dictionary<UPoint, Stack<PointPosition>>(_polys.Length * 2); // geometries should be already normalized.
            BuildDict();

            _endPoints = new Dictionary<UPoint, Envelope>(_endPointsPosition.Count);
            _rtree = new STRtree<UPoint>(_endPointsPosition.Count);
            BuildTree();

            _visited = new HashSet<UPoint>(_endPointsPosition.Count); // points can ignore normalization.
            
        }


        /// <summary>
        /// 1. add all the end points to dict;
        /// </summary>
        private void BuildDict()
        {
            for (int i = 0; i < _polys.Length; i++)
            {
                UPoint[] endsPts = { _polys[i].First, _polys[i].Last };

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
                var ev = pt.GetEnvelope();
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
                    List<UPoint> candidates = new List<UPoint>(nodes.Count);

                    for (int i = 0; i < nodes.Count; i++)
                    {
                        var dist = pt.DistanceTo(nodes[i]);

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
                        var c = candidates[i];

                        var belongs = _endPointsPosition[c];
                        while (belongs.Count > 0)
                        {
                            var p = belongs.Pop(); // delete some items to save memory.
                            if (p.Position == PointPosition.EndPosition.Start)
                                _polys[p.LineStringId].Coordinates[0] = pt; // checked. the item of a readonly collection can be set.
                            else
                                _polys[p.LineStringId].Coordinates[_polys[p.LineStringId].NumPoints - 1] = pt;  // may be we can use first and last, because this is a reference type.
                        }
                    }
                }
            }

            _endPoints.Clear();
            _endPointsPosition.Clear();
        }
    }
}
