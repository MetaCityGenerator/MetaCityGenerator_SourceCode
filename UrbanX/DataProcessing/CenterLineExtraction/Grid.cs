using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.Features;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.Union;


namespace UrbanX.DataProcessing
{
    public class Grid
    {
        private static readonly int[,] NEXT_STEPS = new int[8, 2] { { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 }, { -1, 0 }, { -1, -1 }, { 0, -1 }, { 1, -1 } };
        private readonly Coordinate bottomLeft;
        private readonly Coordinate topRight;
        private Polygon grid;
        private LineString[] roads;
        private Geometry buffer = null;
        private Geometry bufferUnion = null;
        private Geometry bufferUnionTrimmed = null;
        private PreparedPolygon bufferUnionTrimmedPrepared = null;
        private readonly GeometryFactory gf;

        public Coordinate BottomLeft => bottomLeft;

        public Coordinate TopRight => topRight;

        public Geometry BufferUnionTrimmed => bufferUnionTrimmed;

        public PreparedPolygon BufferUnionTrimmedPrepared => bufferUnionTrimmedPrepared;

        public Polygon GridPolygon => grid;

        public Grid(double xmin, double ymin, double xmax, double ymax, int PRECISION)
        {
            bottomLeft = new Coordinate(xmin, ymin);
            topRight = new Coordinate(xmax, ymax);
            gf = new GeometryFactory(new PrecisionModel(Math.Pow(10, PRECISION)));
            SetGridPolygon(xmin, ymin, xmax, ymax);
        }

        public void SetRoads(IList<LineString> lines)
        {
            roads = new LineString[lines.Count];
            for (int i = 0; i < lines.Count; i++) roads[i] = lines[i];
        }

        public void SetBuffer(double dist)
        {
            buffer = gf.CreateMultiLineString(roads).Buffer(dist);
            if (buffer.GeometryType == Geometry.TypeNameGeometryCollection || buffer.IsEmpty) buffer = null;
        }

        public void SetBufferUnion(ICollection<Geometry> buffers)
        {
            buffers.Add(this.buffer);
            List<Geometry> allBuffers = new List<Geometry>();
            foreach (Geometry g in buffers)
            {
                if (g == null) continue;
                allBuffers.Add(BufferOp.Buffer(g, 0));
            }

            if (allBuffers.Count == 0) return;
            this.bufferUnion = UnaryUnionOp.Union(allBuffers);
            if (this.bufferUnion.GeometryType == Geometry.TypeNamePolygon)
                this.bufferUnion = RemoveTinyHoles((Polygon)this.bufferUnion);
            else
            {
                int geomNums = this.bufferUnion.NumGeometries;
                Polygon[] polygons = new Polygon[geomNums];
                MultiPolygon mp = (MultiPolygon)this.bufferUnion;
                for (int i = 0; i < geomNums; i++) polygons[i] = RemoveTinyHoles((Polygon)mp[i]);
                this.bufferUnion = new MultiPolygon(polygons);
            }

            this.bufferUnionTrimmed = this.bufferUnion.Intersection(this.grid);
            if (this.bufferUnionTrimmed.IsEmpty || this.bufferUnionTrimmed.GeometryType == Geometry.TypeNameGeometryCollection)
                this.bufferUnionTrimmed = null;
            if (this.bufferUnionTrimmed != null)
                this.bufferUnionTrimmedPrepared = new PreparedPolygon((IPolygonal)this.bufferUnionTrimmed);
        }

        public static Grid[,] GetGrids(FeatureCollection fc, double grid_size, int PRECISION)
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            double xmin = fc.BoundingBox.MinX;
            double ymin = fc.BoundingBox.MinY;
            double xmax = fc.BoundingBox.MaxX;
            double ymax = fc.BoundingBox.MaxY;
            int rowCount = GetGridNumOnOneDimension(xmin, xmax, grid_size);
            int colCount = GetGridNumOnOneDimension(ymin, ymax, grid_size);
            Grid[,] grids = new Grid[rowCount, colCount];
            for (int i = 0; i < rowCount; i++)
                for (int j = 0; j < colCount; j++)
                    grids[i, j] = new Grid(xmin + grid_size * i, ymin + grid_size * j,
                        xmin + grid_size * (i + 1), ymin + grid_size * (j + 1),
                        PRECISION);

            IList<LineString> roads = FCToList(fc);
            SetGridParams(roads, 0, rowCount, colCount, 0, ref grids);

            //sw.Stop();
            //Console.WriteLine("Creating grids: {0}s", Math.Round((double)sw.ElapsedMilliseconds / 1000, 1));

            return grids;
        }

        public static void SetGridBuffers(double dist, Grid[,] grids)
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            OrderablePartitioner<Tuple<int, int>> rangePartitioner = Partitioner.Create(0, grids.GetLength(0));
            Parallel.ForEach(rangePartitioner, (range, loopState) =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                    for (int j = 0; j < grids.GetLength(1); j++)
                        grids[i, j].SetBuffer(dist);
            });

            //Console.WriteLine(sw.ElapsedMilliseconds / 1000);

            for (int i = 0; i<grids.GetLength(0); i++)
                for (int j = 0; j<grids.GetLength(1); j++)
                {
                    List<Geometry> bufferList = new List<Geometry>();
                    for (int k = 0; k < NEXT_STEPS.GetLength(0); k++)
                        if ((i + NEXT_STEPS[k, 0] >= 0) && (i + NEXT_STEPS[k, 0] < grids.GetLength(0)) &&
                            (j + NEXT_STEPS[k, 1] >= 0) && (j + NEXT_STEPS[k, 1] < grids.GetLength(1)))
                            bufferList.Add(grids[i + NEXT_STEPS[k, 0], j + NEXT_STEPS[k, 1]].buffer);
                    grids[i, j].SetBufferUnion(bufferList);
                }

            //sw.Stop();
            //Console.WriteLine("Set the buffer of each grid: {0}s", Math.Round((double)sw.ElapsedMilliseconds / 1000, 1));
        }

        public static IEnumerable<Polygon> GetAllBufferUnion(Grid[,] grids)
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            List<Geometry> allBuffers = new List<Geometry>();
            for (int i = 0; i < grids.GetLength(0); i++)
                for (int j = 0; j < grids.GetLength(1); j++)
                    if (grids[i, j].BufferUnionTrimmed != null) allBuffers.Add(grids[i, j].BufferUnionTrimmed);
            Geometry whole = UnaryUnionOp.Union(allBuffers);

            List<Polygon> res = new List<Polygon>();
            if (whole.GeometryType == Geometry.TypeNamePolygon)
                res.Add((Polygon)whole);
            else
                foreach (Polygon p in ((MultiPolygon)whole).Geometries)
                    res.Add(p);

            //sw.Stop();
            //Console.WriteLine("Union all buffers: {0}s", Math.Round((double)sw.ElapsedMilliseconds / 1000, 1));

            return res;
        }

        private static void SetGridParams(IList<LineString> roads, int leftX, int rightX, int topY, int bottomY, ref Grid[,] grids)
        {
            if (leftX + 1 == rightX && bottomY + 1 == topY)
                grids[leftX, bottomY].SetRoads(roads);
            else if (leftX + 1 == rightX)
            {
                int midY = (topY + bottomY) / 2;
                double leftX_coord = grids[leftX, bottomY].BottomLeft.X;
                double rightX_coord = grids[leftX, bottomY].TopRight.X;
                double midY_coord = grids[leftX, midY].BottomLeft.Y;
                LineString hline = new LineString(new Coordinate[2] { new Coordinate(leftX_coord, midY_coord), new Coordinate(rightX_coord, midY_coord) });
                List<LineString> top_part = new List<LineString>(roads.Count);
                List<LineString> bottom_part = new List<LineString>(roads.Count);
                foreach (LineString this_road in roads)
                {
                    int topOrBottom = IsTopBottom(midY_coord, this_road);
                    if (topOrBottom == 1)
                        top_part.Add(this_road);
                    else if (topOrBottom == 0)
                        bottom_part.Add(this_road);
                    else
                        foreach (LineString this_split in GetSplitResults(this_road, hline))
                            if (IsTopBottom(midY_coord, this_split) == 1)
                                top_part.Add(this_split);
                            else
                                bottom_part.Add(this_split);
                }
                SetGridParams(bottom_part, leftX, rightX, midY, bottomY, ref grids);
                SetGridParams(top_part, leftX, rightX, topY, midY, ref grids);
            }
            else
            {
                int midX = (leftX + rightX) / 2;
                double bottomY_coord = grids[leftX, bottomY].BottomLeft.Y;
                double topY_coord = grids[leftX, topY - 1].TopRight.Y;
                double midX_coord = grids[midX, bottomY].BottomLeft.X;
                LineString vline = new LineString(new Coordinate[2] { new Coordinate(midX_coord, bottomY_coord), new Coordinate(midX_coord, topY_coord) });
                List<LineString> left_part = new List<LineString>(roads.Count);
                List<LineString> right_part = new List<LineString>(roads.Count);
                foreach (LineString this_road in roads)
                {
                    int leftOrRight = IsLeftRight(midX_coord, this_road);
                    if (leftOrRight == 1)
                        right_part.Add(this_road);
                    else if (leftOrRight == 0)
                        left_part.Add(this_road);
                    else
                        foreach (LineString this_split in GetSplitResults(this_road, vline))
                            if (IsLeftRight(midX_coord, this_split) == 1)
                                right_part.Add(this_split);
                            else
                                left_part.Add(this_split);
                }
                SetGridParams(left_part, leftX, midX, topY, bottomY, ref grids);
                SetGridParams(right_part, midX, rightX, topY, bottomY, ref grids);
            }
        }

        private void SetGridPolygon(double xmin, double ymin, double xmax, double ymax)
        {
            Coordinate bL = new Coordinate(xmin, ymin);
            Coordinate bR = new Coordinate(xmax, ymin);
            Coordinate tL = new Coordinate(xmin, ymax);
            Coordinate tR = new Coordinate(xmax, ymax);
            grid = new Polygon(new LinearRing(new Coordinate[] { bL, bR, tR, tL, bL }));
        }

        private static Polygon RemoveTinyHoles(Polygon p, double areaThreshold = 200, double widthThreshold = 20)
        {
            List<LinearRing> inners = new List<LinearRing>();
            foreach (LineString ls in p.InteriorRings)
                if ((new Polygon((LinearRing)ls)).Area > areaThreshold && (new MinimumDiameter(ls)).Length > widthThreshold)
                    inners.Add((LinearRing)ls);
            Polygon res = new Polygon((LinearRing)p.ExteriorRing, inners.ToArray());
            return res;
        }

        private static IEnumerable<LineString> GetSplitResults(LineString road, LineString line)
        {
            Geometry splitResults = road.Difference(line);
            List<LineString> res = new List<LineString>();
            if (splitResults.GeometryType == Geometry.TypeNameLineString)
                res.Add((LineString)splitResults);
            else
                foreach (LineString ls in ((MultiLineString)splitResults).Geometries)
                    res.Add(ls);
            return res;
        }

        private static int IsTopBottom(double midY_coord, LineString road)
        {
            if (road.EnvelopeInternal.MinY >= midY_coord)
                return 1;
            else if (road.EnvelopeInternal.MaxY <= midY_coord)
                return 0;
            else
                return -1;
        }

        private static int IsLeftRight(double midX_coord, LineString road)
        {
            if (road.EnvelopeInternal.MinX >= midX_coord)
                return 1;
            else if (road.EnvelopeInternal.MaxX <= midX_coord)
                return 0;
            else
                return -1;
        }

        private static int GetGridNumOnOneDimension(double xmin, double xmax, double grid_size)
        {
            int count = (int)Math.Truncate((xmax - xmin) / grid_size);
            if (count * grid_size < xmax - xmin)
                count++;
            return count;
        }

        private static IList<LineString> FCToList(FeatureCollection fc)
        {
            LineString[] res = new LineString[fc.Count];
            for (int i = 0; i < fc.Count; i++)
                res[i] = (LineString)fc[i].Geometry;
            return res;
        }
    }
}
