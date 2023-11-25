using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Linemerge;
using NetTopologySuite.Operation.Union;
using NetTopologySuite.Precision;

using Rhino.Geometry;

using System.Collections.Generic;

using UrbanX.Planning.Utility;
using UrbanX.DataStructures.Geometry3D;


namespace UrbanX.IO.OpenNURBS
{
    public class GeometryConverter
    {

        private readonly GeometryFactory _gf;


        public GeometryConverter()
        {
        }


        public GeometryConverter(GeometryFactory gf)
        {
            _gf = gf;
        }




        #region RhinoToNts: must input gemetry factory for constructor.
        public LineString ToLineString3D(Polyline pl )
        {
            CoordinateZ[] pts = new CoordinateZ[pl.Count];

            for (int i = 0; i < pl.Count; i++)
            {
                pts[i] = new CoordinateZ(pl[i].X, pl[i].Y, pl[i].Z);
            }

            var l = _gf.CreateLineString(pts);
            return l;
        }


        public LineString ToLineString( Polyline pl)
        {
            Coordinate[] pts = new Coordinate[pl.Count];

            for (int i = 0; i < pl.Count; i++)
            {
                pts[i] = new Coordinate(pl[i].X, pl[i].Y);
            }

            var l = _gf.CreateLineString(pts);

            return l;
        }


        public Polygon ToPolygon( Polyline pl)
        {
            List<Coordinate> pts = new List<Coordinate>(pl.Count);

            for (int i = 0; i < pl.Count; i++)
            {
                pts.Add(new Coordinate(pl[i].X, pl[i].Y));
            }

            if (!pl.IsClosed)
            {
                pts.Add(pts[0]);
            }

            var ring = _gf.CreateLinearRing(pts.ToArray());
            var poly = _gf.CreatePolygon(ring);

            return  poly.ForceCCW();
        }


        public Polygon ToPolygon3D(Polyline pl)
        {
            List<CoordinateZ> pts = new List<CoordinateZ>(pl.Count);
            pl.SetAllZ(pl.CenterPoint().Z); // flatten pl.

            for (int i = 0; i < pl.Count; i++)
            {
                pts.Add(new CoordinateZ(pl[i].X, pl[i].Y, pl[i].Z));
            }

            if (!pl.IsClosed)
            {
                pts.Add(pts[0]);
            }

            var ring = _gf.CreateLinearRing(pts.ToArray());
            var poly = _gf.CreatePolygon(ring);

            return poly.ForceCCW();
        }



        #endregion

        #region NtsToRhino : Nts to rhino won't need reducing coordinates.

        public Polyline ToPolyline(LineString l)
        {
            Point3d[] pts = new Point3d[l.NumPoints];

            for (int i = 0; i < l.NumPoints; i++)
            {
                var z = l[i].Z;
                if (double.IsNaN(z))
                    z = 0;

                pts[i] = new Point3d(l[i].X, l[i].Y, z);
            }

            return new Polyline(pts);
        }



        public Polyline ToPolyline(Polygon poly)
        {
            Point3d[] pts = new Point3d[poly.NumPoints];

            var cos = poly.Coordinates;
            for (int i = 0; i < poly.NumPoints; i++)
            {
                var z = cos[i].Z;
                if (double.IsNaN(z))
                    z = 0;

                pts[i] = new Point3d(cos[i].X, cos[i].Y, z);
            }

            pts[pts.Length - 1] = pts[0]; // make sure polygon is closed.
            return new Polyline(pts);
        }


        #endregion


        #region Rhino<-->UrbanX
        public UPolyline ToPolyline3D(Polyline pl)
        {
            UPoint[] arr = new UPoint[pl.Count];
            for (int i = 0; i < pl.Count; i++)
            {
                arr[i] = new UPoint(pl[i].X, pl[i].Y, pl[i].Z);
            }
            return new UPolyline(arr);
        }

        public Polyline ToPolyline(UPolyline pl3)
        {
            Point3d[] arr = new Point3d[pl3.NumPoints];
            for (int i = 0; i < pl3.NumPoints; i++)
            {
                arr[i] = new Point3d(pl3[i].X, pl3[i].Y, pl3[i].Z);
            }
            return new Polyline(arr);
        }


        #endregion

        public static LineString[] CleanRoads(Geometry[] geos)
        {
            var ungeo = UnaryUnionOp.Union(geos);

            LineMerger merger = new LineMerger();
            merger.Add(ungeo);
            var segs = merger.GetMergedLineStrings();

            return GeometryFactory.ToLineStringArray(segs);
        }




        public Polyline[] Dissolve(Polyline[] pls)
        {
            LineString[] lss = new LineString[pls.Length];
            for (int i = 0; i < pls.Length; i++)
            {
                lss[i] = ToLineString3D(pls[i]);
            }

            var cleaned = CleanRoads(lss);

            Polyline[] result = new Polyline[cleaned.Length];
            for (int i = 0; i < cleaned.Length; i++)
            {
                result[i] = ToPolyline(cleaned[i]);
            }

            return result;
        }

    }

}
