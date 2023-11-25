using System;
using System.Collections.Generic;
using System.Diagnostics;
using NetTopologySuite.Geometries;
using NetTopologySuite.Densify;
using NetTopologySuite.Precision;
using System.Linq;
using System.Collections.Concurrent;

namespace UrbanX.DataProcessing
{
    public class RidgeFilters
    {
        public static ICollection<Coordinate> InterpolatePointsInBuffers(IEnumerable<Polygon> buffers, double interpolate_dist,
            double PRECISION)
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            GeometryPrecisionReducer gpr = new GeometryPrecisionReducer(new PrecisionModel(Math.Pow(10, PRECISION)))
            {
                ChangePrecisionModel = true,
                RemoveCollapsedComponents = true
            };

            List<Coordinate> res = new List<Coordinate>();
            foreach (Polygon p in buffers)
            {
                Densifier densifier = new Densifier(p)
                {
                    Validate = true,
                    DistanceTolerance = interpolate_dist
                };
                Geometry geom = densifier.GetResultGeometry();
                geom = gpr.Reduce(geom);
                foreach (Coordinate coord in geom.Coordinates)
                    res.Add(coord);
            }

            //sw.Stop();
            //Console.WriteLine("Interpolate points within buffers: {0}s", Math.Round((double)sw.ElapsedMilliseconds / 1000, 1));

            return res;
        }

        public static ICollection<Coordinate> Debug_InterpolatePointsInBuffers(Polygon buffer, double interpolate_dist,
    double PRECISION)
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            GeometryPrecisionReducer gpr = new GeometryPrecisionReducer(new PrecisionModel(Math.Pow(10, PRECISION)))
            {
                ChangePrecisionModel = true,
                RemoveCollapsedComponents = true
            };

            Densifier densifier = new Densifier(buffer)
            {
                Validate = true,
                DistanceTolerance = interpolate_dist
            };
            Geometry geom = densifier.GetResultGeometry();
            geom = gpr.Reduce(geom);

            Coordinate[] res = new Coordinate[geom.Coordinates.Length];
            System.Threading.Tasks.Parallel.For(0, geom.Coordinates.Length, i => {
                res[i] = geom.Coordinates[i];
            });                

            //sw.Stop();
            //Console.WriteLine("Interpolate points within buffers: {0}s", Math.Round((double)sw.ElapsedMilliseconds / 1000, 1));

            return res;
        }

        public static IList<LineSegment> GetRidgesInsideBuffer(Grid[,] grids, Envelope envelope, ICollection<Coordinate> coords, ICollection<LineSegment> ridges, double grid_size)
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            Vertice[] vertices = new Vertice[coords.Count];
            Dictionary<Coordinate, int> verticeDict = new Dictionary<Coordinate, int>();
            int i = 0;
            foreach (Coordinate c in coords)
            {
                vertices[i] = new Vertice(grids, envelope, grid_size, c.X, c.Y);
                verticeDict.Add(c, i);
                i++;
            }

            List<LineSegment> res = new List<LineSegment>();
            foreach (LineSegment ls in ridges)
                if (vertices[verticeDict[ls.P0]].WithinBuffer && vertices[verticeDict[ls.P1]].WithinBuffer &&
                    DoesRidgeNotIntersectBuffer(grids, vertices[verticeDict[ls.P0]], vertices[verticeDict[ls.P1]]))
                    res.Add(ls);

            //sw.Stop();
            //Console.WriteLine("Extract all ridges that lies within the buffers: {0}s", Math.Round((double)sw.ElapsedMilliseconds / 1000, 1));
            return res;
        }

        public static IList<LineSegment> Debug_GetRidgesInsideBuffer(Polygon polygon, Envelope envelope, ICollection<Coordinate> coords, ICollection<LineSegment> ridges)
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            Vertice[] vertices = new Vertice[coords.Count];
            ConcurrentDictionary<Coordinate, int> verticeDict = new ConcurrentDictionary<Coordinate, int>();
            //int i = 0;
            
            System.Threading.Tasks.Parallel.For(0, coords.Count, i => {
                Coordinate c = coords.ElementAt(i);
                vertices[i] = new Vertice(polygon, envelope, c.X, c.Y);
                verticeDict.TryAdd(c, i);
            });

            ConcurrentBag<LineSegment> res = new ConcurrentBag<LineSegment>();
            System.Threading.Tasks.Parallel.For(0, ridges.Count, i => {
                LineSegment ls = ridges.ElementAt(i);
                if (vertices[verticeDict[ls.P0]].WithinBuffer && vertices[verticeDict[ls.P1]].WithinBuffer &&
                    Debug_DoesRidgeNotIntersectBuffer(polygon, vertices[verticeDict[ls.P0]], vertices[verticeDict[ls.P1]]))
                    res.Add(ls);
            });

            //List<LineSegment> res = new List<LineSegment>();
            //foreach (LineSegment ls in ridges)
            //    if (vertices[verticeDict[ls.P0]].WithinBuffer && vertices[verticeDict[ls.P1]].WithinBuffer &&
            //        Debug_DoesRidgeNotIntersectBuffer(polygon, vertices[verticeDict[ls.P0]], vertices[verticeDict[ls.P1]]))
            //        res.Add(ls);

            //sw.Stop();
            //Console.WriteLine("Extract all ridges that lies within the buffers: {0}s", Math.Round((double)sw.ElapsedMilliseconds / 1000, 1));
            return res.ToArray();
        }

        public static IList<LineSegment> RemoveDangles(IList<LineSegment> lineSegments)
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            Dictionary<Coordinate, int> frequency = new Dictionary<Coordinate, int>();
            foreach (LineSegment ls in lineSegments)
            {
                if (!frequency.ContainsKey(ls.P0)) frequency.Add(ls.P0, 0);
                if (!frequency.ContainsKey(ls.P1)) frequency.Add(ls.P1, 0);
                frequency[ls.P0]++;
                frequency[ls.P1]++;
            }

            List<LineSegment> res = new List<LineSegment>();
            foreach (LineSegment ls in lineSegments)
                if (frequency[ls.P0] > 1 && frequency[ls.P1] > 1)
                    res.Add(ls);

            //sw.Stop();
            //Console.WriteLine("Remove dangles: {0}s", Math.Round((double)sw.ElapsedMilliseconds / 1000, 1));

            return res;
        }

        public static IList<LineSegment> RemoveDangles(IList<LineString> lineStrings)
        {
            List<LineSegment> segments = new List<LineSegment>();
            foreach (LineString ls in lineStrings)
                for (int i = 1; i < ls.CoordinateSequence.Count; i++)
                    segments.Add(new LineSegment(ls.CoordinateSequence.GetCoordinate(i - 1), ls.CoordinateSequence.GetCoordinate(i)));

            IList<LineSegment> res = RemoveDangles(segments);
            return res;
        }

        public static IList<LineSegment> Debug_RemoveDangles(IList<LineString> lineStrings)
        {
            ConcurrentBag<LineSegment> segments = new ConcurrentBag<LineSegment>();
            System.Threading.Tasks.Parallel.For(0, lineStrings.Count, i => {
                LineString ls=lineStrings[i];
                for (int j = 1; j < ls.CoordinateSequence.Count; j++)
                    segments.Add(new LineSegment(ls.CoordinateSequence.GetCoordinate(j - 1), ls.CoordinateSequence.GetCoordinate(j)));
            });
            
            IList<LineSegment> res = RemoveDangles(segments.ToArray());
            return res;
        }

        private static bool DoesRidgeNotIntersectBuffer(Grid[,] grids, Vertice origin, Vertice destin)
        {
            LineString this_line = new LineString(new Coordinate[] { origin.Coordinate, destin.Coordinate });
            for (int i = Math.Min(origin.Row, destin.Row); i <= Math.Max(origin.Row, destin.Row); i++)
                for (int j = Math.Min(origin.Col, destin.Col); j <= Math.Max(origin.Col, destin.Col); j++)
                {
                    Geometry g = this_line.Intersection(grids[i, j].GridPolygon);
                    if (g != null && g.GeometryType != Geometry.TypeNameGeometryCollection && !g.IsEmpty &&
                        grids[i, j].BufferUnionTrimmedPrepared != null &&
                        !grids[i, j].BufferUnionTrimmedPrepared.Contains(g))
                        return false;
                }
            return true;
        }

        private static bool Debug_DoesRidgeNotIntersectBuffer(Polygon polygon, Vertice origin, Vertice destin)
        {
            LineString this_line = new LineString(new Coordinate[] { origin.Coordinate, destin.Coordinate });
            
            bool flag=polygon.Contains(this_line);
            //Geometry g = this_line.Intersection(polygon);
            //if (g != null && g.GeometryType != Geometry.TypeNameGeometryCollection && !g.IsEmpty &&
            //    polygon != null &&
            //    !polygon.Contains(g))
            //    return false;
            //return true;
            return flag;
        }
    }
}
