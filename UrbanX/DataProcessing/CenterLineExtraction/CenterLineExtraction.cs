using System;
using System.Collections.Generic;
using System.Diagnostics;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Simplify;
using NetTopologySuite.Precision;
using DelaunatorSharp;
using System.Collections.Concurrent;
using MetaCityWrapper;

namespace MetaCity.DataProcessing
{
    public class CenterLineExtraction
    {
        public static FeatureCollection Extract(FeatureCollection fc, double grid_size = 500, double buffer_dist = 10,
            double interpolate_dist = 10, double epsilon = 10, double segment_threshold = 10, int PRECISION = 3)
        {
            fc = PreprocessFeatureCollection(fc);
            Grid[,] grids = Grid.GetGrids(fc, grid_size, PRECISION);
            Grid.SetGridBuffers(buffer_dist, grids);
            IEnumerable<Polygon> buffers = Grid.GetAllBufferUnion(grids);

            ICollection<Coordinate> points = RidgeFilters.InterpolatePointsInBuffers(buffers, interpolate_dist, PRECISION);
            GetVoronoiResults(points, fc.BoundingBox, out ICollection<Coordinate> vertices, out ICollection<LineSegment> ridges);

            IList<LineSegment> ridgesInsideBuffer = RidgeFilters.GetRidgesInsideBuffer(grids, fc.BoundingBox, vertices, ridges, grid_size);
            IList<LineString> allRoads = ConnectingRoadSegments.Get(ridgesInsideBuffer);
            allRoads = SimplifyRoads(allRoads, epsilon);
            IList<LineSegment> roadsWithoutDangles = RidgeFilters.RemoveDangles(allRoads);

            IList<LineSegment> roadsWithoutShortSegments = DeleteShortSegments.Delete(roadsWithoutDangles, segment_threshold, PRECISION);
            //roadsWithoutShortSegments = RidgeFilters.RemoveDangles(roadsWithoutShortSegments);
            allRoads = ConnectingRoadSegments.Get(roadsWithoutShortSegments);

            FeatureCollection results = GetFeatureCollection(allRoads);
            return results;
        }

        public static FeatureCollection Debug_Extract(Polygon buffer, out List<string> time,
    double interpolate_dist = 10, double epsilon = 10, double segment_threshold = 10, int PRECISION = 3)
        {
            Envelope envelope = buffer.EnvelopeInternal;
            time = new List<string>(6);

            ICollection<Coordinate> points = RidgeFilters.Debug_InterpolatePointsInBuffers(buffer, interpolate_dist, PRECISION);
            //time.Add(Raytracer.TimeCalculation(start, "插点\n"));

            GetVoronoiResults(points, envelope, out ICollection<Coordinate> vertices, out ICollection<LineSegment> ridges);
            //time.Add(Raytracer.TimeCalculation(start, "生成泰森多边形\n"));

            IList<LineSegment> ridgesInsideBuffer = RidgeFilters.Debug_GetRidgesInsideBuffer(buffer, envelope, vertices, ridges);
            //time.Add(Raytracer.TimeCalculation(start, "抽取内部线\n"));

            IList<LineString> allRoads = ConnectingRoadSegments.Get(ridgesInsideBuffer);
            allRoads = Debug_SimplifyRoads(allRoads, epsilon);
            //time.Add(Raytracer.TimeCalculation(start, "简化路网\n"));

            IList<LineSegment> roadsWithoutDangles = RidgeFilters.Debug_RemoveDangles(allRoads);
            //time.Add(Raytracer.TimeCalculation(start, "移除断头路\n"));

            //IList<LineSegment> roadsWithoutShortSegments = DeleteShortSegments.Delete(roadsWithoutDangles, segment_threshold, PRECISION);
            //time.Add(Raytracer.TimeCalculation(start, "移除短线\n"));

            //roadsWithoutShortSegments = RidgeFilters.RemoveDangles(roadsWithoutShortSegments);
            allRoads = ConnectingRoadSegments.Get(roadsWithoutDangles);

            FeatureCollection results = GetFeatureCollection(allRoads);
            return results;
        }

        private static FeatureCollection GetFeatureCollection(IList<LineString> lineStrings)
        {
            FeatureCollection fc = new FeatureCollection();
            foreach (LineString ls in lineStrings)
                fc.Add(new Feature(ls, new AttributesTable()));
            return fc;
        }

        private static void SetFeatureCollectionEnvelope(FeatureCollection fc)
        {
            double minX = double.PositiveInfinity;
            double minY = double.PositiveInfinity;
            double maxX = double.NegativeInfinity;
            double maxY = double.NegativeInfinity;
            foreach (Feature f in fc)
            {
                Envelope this_env = f.Geometry.EnvelopeInternal;
                if (this_env.MinX < minX) minX = this_env.MinX;
                if (this_env.MinY < minY) minY = this_env.MinY;
                if (this_env.MaxX > maxX) maxX = this_env.MaxX;
                if (this_env.MaxY > maxY) maxY = this_env.MaxY;
            }
            fc.BoundingBox = new Envelope(minX, maxX, minY, maxY);
        }

        private static FeatureCollection PreprocessFeatureCollection(FeatureCollection fc)
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            GeometryPrecisionReducer gpr = new GeometryPrecisionReducer(new PrecisionModel(1000)) { 
                ChangePrecisionModel = true,
                RemoveCollapsedComponents = true
            };
            for (int i = 0; i < fc.Count; i++)
                fc[i].Geometry = gpr.Reduce(fc[i].Geometry);

            FeatureCollection res = new FeatureCollection();
            foreach (Feature f in fc)
                foreach (Geometry g in LineStringExtracter.GetLines(f.Geometry))
                    res.Add(new Feature(g, f.Attributes));

            SetFeatureCollectionEnvelope(res);

            //sw.Stop();
            //Console.WriteLine("Preprocessing data: {0}s", Math.Round((double)sw.ElapsedMilliseconds / 1000, 1));
            return res;
        }

        private static FeatureCollection Debug_PreprocessFeatureCollection(FeatureCollection fc)
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            GeometryPrecisionReducer gpr = new GeometryPrecisionReducer(new PrecisionModel(1000))
            {
                ChangePrecisionModel = true,
                RemoveCollapsedComponents = true
            };
            for (int i = 0; i < fc.Count; i++)
                fc[i].Geometry = gpr.Reduce(fc[i].Geometry);

            FeatureCollection res = new FeatureCollection();
            foreach (Feature f in fc)
                foreach (Geometry g in PolygonExtracter.GetPolygons(f.Geometry))
                    res.Add(new Feature(g, f.Attributes));

            SetFeatureCollectionEnvelope(res);

            //sw.Stop();
            //Console.WriteLine("Preprocessing data: {0}s", Math.Round((double)sw.ElapsedMilliseconds / 1000, 1));
            return res;
        }

        public static IList<LineString> SimplifyRoads(IList<LineString> roads, double epsilon)
        {
            //Stopwatch sw = new();
            //sw.Start();

            List<LineString> res = new List<LineString>();
            foreach (LineString road in roads)
                res.Add((LineString)DouglasPeuckerSimplifier.Simplify(road, epsilon));

            //sw.Stop();
            //Console.WriteLine("Simplify roads: {0}s", Math.Round((double)sw.ElapsedMilliseconds / 1000, 1));

            return res;
        }

        public static IList<LineString> Debug_SimplifyRoads(IList<LineString> roads, double epsilon)
        {
            //Stopwatch sw = new();
            //sw.Start();

            //List<LineString> res = new List<LineString>();
            //foreach (LineString road in roads)
            //    res.Add((LineString)DouglasPeuckerSimplifier.Simplify(road, epsilon));

            LineString[] res = new LineString[roads.Count];
            System.Threading.Tasks.Parallel.For(0, roads.Count, i => {
                LineString road = roads[i];
                res[i] = (LineString)DouglasPeuckerSimplifier.Simplify(road, epsilon);
            });

            //sw.Stop();
            //Console.WriteLine("Simplify roads: {0}s", Math.Round((double)sw.ElapsedMilliseconds / 1000, 1));

            return res;
        }

        public static void GetVoronoiResults(ICollection<Coordinate> points, Envelope envelope,
            out ICollection<Coordinate> vertices, out ICollection<LineSegment> ridges)
        {
            //Stopwatch sw = new();
            //sw.Start();
            
            IPoint[] libraryPoints = new IPoint[points.Count];
            int i = 0;
            foreach (Coordinate c in points)
                libraryPoints[i++] = new DelaunatorSharp.Point(c.X, c.Y);
            Delaunator delaunator = new Delaunator(libraryPoints);
            IEnumerable<IEdge> edges = delaunator.GetVoronoiEdgesBasedOnCircumCenter();

            HashSet<Coordinate> verticeHash = new HashSet<Coordinate>();
            HashSet<LineSegment> ridgesHash = new HashSet<LineSegment>();
            foreach (IEdge e in edges)
            {
                Coordinate start = new Coordinate(e.P.X, e.P.Y);
                Coordinate end = new Coordinate(e.Q.X, e.Q.Y);
                verticeHash.Add(start);
                verticeHash.Add(end);
                LineSegment ls = new LineSegment(start, end);
                ls.Normalize();
                ridgesHash.Add(ls);
            }

            vertices = verticeHash;
            ridges = ridgesHash;

            //sw.Stop();
            //Console.WriteLine("Build the Voronoi Diagram, and extract all vertices and ridges: {0}s",
                //Math.Round((double)sw.ElapsedMilliseconds / 1000, 2));
        }
    }
}
