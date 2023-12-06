using System;
using NetTopologySuite.Geometries;


namespace MetaCity.DataProcessing
{
    public class Vertice
    {
        private readonly Coordinate coord;
        private readonly int row;
        private readonly int col;
        private readonly bool withinBuffer;

        public Coordinate Coordinate => coord;

        public int Row => row;

        public int Col => col;

        public bool WithinBuffer => withinBuffer;

        public Vertice(Grid[,] allGrids, Envelope envelope, double grid_size, double x, double y)
        {
            coord = new Coordinate(x, y);
            row = (int)Math.Truncate((x - envelope.MinX) / grid_size);
            col = (int)Math.Truncate((y - envelope.MinY) / grid_size);

            if (x < envelope.MinX || y < envelope.MinY || x > envelope.MaxX || y > envelope.MaxY)
                withinBuffer = false;
            else if (allGrids[row, col].BufferUnionTrimmed == null ||
                allGrids[row, col].BufferUnionTrimmed.GeometryType == Geometry.TypeNameGeometryCollection)
                withinBuffer = false;
            else
                withinBuffer = allGrids[row, col].BufferUnionTrimmedPrepared.Contains(new Point(coord));
        }

        public Vertice(Polygon polygon, Envelope envelope, double x, double y)
        {
            coord = new Coordinate(x, y);

            if (x < envelope.MinX || y < envelope.MinY || x > envelope.MaxX || y > envelope.MaxY)
                withinBuffer = false;
            else
                withinBuffer = polygon.Contains(new Point(coord));
        }
    }
}
