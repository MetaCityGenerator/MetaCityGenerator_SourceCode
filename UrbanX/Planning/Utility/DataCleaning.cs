using NetTopologySuite.Dissolve;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Noding.Snapround;
using NetTopologySuite.Operation.Linemerge;
using NetTopologySuite.Operation.Overlay.Snap;
using NetTopologySuite.Operation.Union;
using NetTopologySuite.Precision;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using UrbanX.Algorithms.Geometry3D;
using UrbanX.DataStructures.Geometry3D;
using UrbanX.Planning.SpaceSyntax;

namespace UrbanX.Planning.Utility
{
    public class DataCleaning
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputLineStrings"></param>
        /// <param name="gf"></param>
        /// <returns></returns>
        public static MultiLineString CleanMultiLineString(GeometryCollection inputLineStrings , GeometryFactory gf)
        {
            // Check precision model.
            var geom = (Geometry)inputLineStrings;
            var flag = TryReduce(ref geom, gf);
            if (flag)
            {
                try
                {
                    // Means geometry has been reduced.
                    inputLineStrings = (GeometryCollection)geom;
                }
                catch
                {
                    inputLineStrings = new GeometryCollection(new Geometry[] { geom });
                }
            }

            // Check IsHomogeneous by applying geometry filter.
            if (!inputLineStrings.IsHomogeneous)
            {
                var extracter = LineStringExtracter.GetGeometry(inputLineStrings);
                inputLineStrings = (GeometryCollection)extracter;
            }

            // Snap vertices.
            var tolerance = GeometrySnapper.ComputeOverlaySnapTolerance(inputLineStrings);
            var snapgeoms = GeometrySnapper.SnapToSelf(inputLineStrings, tolerance, true);

            // UnaryUnion.

            var geoms = UnaryUnionOp.Union((GeometryCollection)snapgeoms, gf);

            // Mergering.
            LineMerger merger = new LineMerger();
            merger.Add(geoms);

            // Extract linestring from dissovled result which inherits the geometry factory from input geomFact.
            var result =LineStringExtracter.GetGeometry(new GeometryCollection(merger.GetMergedLineStrings().ToArray(), gf));

            try
            {
                return (MultiLineString)result;
            }
            catch
            {
                // Merged linestring.
                LineString[] lines = new LineString[] {(LineString)result };

                return new MultiLineString(lines);
            }
        }


        public static LineString[] CleanMultiLineString3D(LineString[] segs, GeometryFactory gf)
        {
            // Check precision model.
            if (gf.PrecisionModel.IsFloating)
            {
                PrecisionSetting.ChangePrecision(ref gf);
            }

            LineStringSnapper3D snapper = new LineStringSnapper3D(segs, gf);
            snapper.Snapp();
            var splittedRoads = RoadsSplitter3D.SplitRoads(snapper.SnappedLineStrings , gf);

            // Must round all the linestring because the floating number.
            // For graph builder, GeometryComparer3D don't use tolerance, therefore if we haven't round points, may cause error in GraphBuilder.
            return splittedRoads;
        }


        public static UPolyline[] CleanPolylines(UPolyline[] polys, double tolerance)
        {
            PolylineSnapper3D snapper = new PolylineSnapper3D(polys, tolerance);
            snapper.Snapp();

            var splittedRoads = new RoadsSplitter3Df(polys, tolerance);
            return splittedRoads.SplittedLineSegments;
        }



        /// <summary>
        /// Method for cleaning input multipolygon data.
        /// <para> 
        /// First, checking the precision model, which must be fixed, based on the given <see cref="GeometryFactory"/> .
        /// Second, checking whether input geometry is homogeneous type, if not, using <see cref="LineStringExtracter"/> as geometry filter.
        /// </para>
        /// </summary>
        /// <param name="inputPolygons"></param>
        /// <param name="geometryFactory"></param>
        /// <returns></returns>
        public static MultiPolygon CleanMultiPolygon(GeometryCollection inputPolygons, GeometryFactory geometryFactory)
        {
            // Check precision model.
            var geom = (Geometry)inputPolygons;
            var flag = TryReduce(ref geom, geometryFactory);
            if (flag)
            {
                // Means geometry has been reduced.
                inputPolygons = (GeometryCollection)geom;
            }


            // Check IsHomogeneous by applying geometry filter.
            if (!inputPolygons.IsHomogeneous)
            {
                var extracter = PolygonExtracter.GetPolygons(inputPolygons);

                inputPolygons = new GeometryCollection(extracter.ToArray());
            }


            HashSet<Polygon> validPolygons = new HashSet<Polygon>(inputPolygons.Count);
            for (int i = 0; i < inputPolygons.Count; i++)
            {
                var buffer = inputPolygons.Geometries[i].Buffer(0.0);

                if (buffer.IsValid && buffer.OgcGeometryType == OgcGeometryType.Polygon && !buffer.IsEmpty)
                {
                    var poly = (Polygon)buffer;
                    // Check CCW.
                    if (!poly.Shell.IsCCW)
                        poly= (Polygon)poly.Reverse();

                    validPolygons.Add(poly);
                }
            }

            MultiPolygon multiPolygon = new MultiPolygon(validPolygons.ToArray(), geometryFactory);
            return multiPolygon;
        }



        /// <summary>
        /// Method for cleaning input multipolygon data and modifing the attribute table accordingly.
        /// <para> 
        /// First, checking the precision model, which must be fixed, based on the given <see cref="GeometryFactory"/> .
        /// Second, checking whether input geometry is homogeneous type, if not, using <see cref="LineStringExtracter"/> as geometry filter.
        /// </para>
        /// <param name="inputPolygons"></param>
        /// <param name="geometryFactory"></param>
        /// <param name="tables"></param>
        /// <returns></returns>
        public static MultiPolygon CleanMultiPolygon(GeometryCollection inputPolygons, GeometryFactory geometryFactory, ref AttributesTable[] tables)
        {
            // During cleaning phase, several geometries may be removed, therefore we should use a new dict to store cleaned geometry and attribute table.
            List<AttributesTable> cleanedTables = new List<AttributesTable>(tables.Length);

           // Check precision model.
            var geom = (Geometry)inputPolygons;
            var flag = TryReduce(ref geom, geometryFactory);
            if (flag)
            {
                // Means geometry has been reduced.
                inputPolygons = (GeometryCollection)geom;
            }


            // Check IsHomogeneous by applying geometry filter.
            if (!inputPolygons.IsHomogeneous)
            {
                var extracter = PolygonExtracter.GetPolygons(inputPolygons);

                inputPolygons = new GeometryCollection(extracter.ToArray());
            }

            HashSet<Polygon> validPolygons = new HashSet<Polygon>(inputPolygons.Count);
            for (int i = 0; i < inputPolygons.Count; i++)
            {
                var buffer = inputPolygons.Geometries[i].Buffer(0.0);


                if (buffer.IsValid && buffer.OgcGeometryType == OgcGeometryType.Polygon && !buffer.IsEmpty)
                {
                    // Found the valid polygon.
                    var att = tables[i];

                    var poly = (Polygon)buffer;
                    // Check CCW.
                    if (!poly.Shell.IsCCW)
                        poly = (Polygon)poly.Reverse();

                    validPolygons.Add(poly);
                    cleanedTables.Add(att);
                }
            }


            MultiPolygon multiPolygon = new MultiPolygon(validPolygons.ToArray(), geometryFactory);
            tables = cleanedTables.ToArray();

            return multiPolygon;
        }





        /// <summary>
        /// Method for cleaning input multipoint data.
        /// <para> 
        /// First, checking the precision model, which must be fixed, based on the given <see cref="GeometryFactory"/> .
        /// Second, checking whether input geometry is homogeneous type, if not, using <see cref="PointExtracter"/> as geometry filter.
        /// </para>
        /// <param name="inputPoints"></param>
        /// <param name="geometryFactory"></param>
        /// <returns></returns>
        public static MultiPoint CleanMultiPoint(GeometryCollection inputPoints, GeometryFactory geometryFactory)
        {
            // Check precision model.
            var geom = (Geometry)inputPoints;
            var flag = TryReduce(ref geom, geometryFactory);
            if (flag)
            {
                // Means geometry has been reduced.
                inputPoints = (GeometryCollection)geom;
            }


            // Check IsHomogeneous by applying geometry filter.
            var pts = PointExtracter.GetPoints(inputPoints).ToArray();
            HashSet<Point> validPts = new HashSet<Point>(pts.Length);

            for (int i = 0; i < pts.Length; i++)
            {
                var pt = pts[i];
                if (pt.IsValid)
                    validPts.Add((Point)pt);
            }

            MultiPoint multiPoint = new MultiPoint(validPts.ToArray(), geometryFactory);
            return multiPoint;
        }


        //TODO: optimise this method. for geometrycollection, only need to initiate reducer once.
        /// <summary>
        /// Method for checking whether the input geometry's precision model should be reduced against a given geometry factory.
        /// </summary>
        /// <param name="geom"></param>
        /// <param name="geomFact"></param>
        /// <returns>Return true when geometry has been reduced, otherwise false.</returns>
        public static bool TryReduce(ref Geometry geom, GeometryFactory geomFact)
        {  
            if (geomFact.PrecisionModel.IsFloating)
            {
                PrecisionSetting.ChangePrecision(ref geomFact);
            }

            //For example, to specify 3 decimal places of precision, use a scale factor of 1000.
            //To specify - 3 decimal places of precision(i.e.rounding to the nearest 1000), use a scale factor of 0.001.

            GeometryPrecisionReducer reducer = new GeometryPrecisionReducer(geomFact.PrecisionModel)
            {
                ChangePrecisionModel = true
            };

            // Reduce geometry precision.
            var tempGeom = reducer.Reduce(geom);
            if (!geom.IsEmpty && (tempGeom.IsEmpty || !tempGeom.IsValid))
                return false;
            else
            {
                geom = tempGeom;
                return true;
            }
        }
    }
}
