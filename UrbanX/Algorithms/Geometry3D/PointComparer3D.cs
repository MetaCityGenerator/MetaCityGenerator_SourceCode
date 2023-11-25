using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetTopologySuite.Geometries;
using NetTopologySuite.Precision;

namespace UrbanX.Algorithms.Geometry3D
{
    /// <summary>
    /// This point comparer need to set the tolerance.
    /// If CoordinateZ.Distance3D is smaller than the input tolerance, two points are considered as equal.
    /// </summary>
    public sealed class PointComparer3D : EqualityComparer<Point> , IComparer<Point>
    {
        private readonly double _tolerance;



        public PointComparer3D() : this(0d) { }


        public PointComparer3D(double tolerance)
        {
            _tolerance = tolerance;

        }



        public int Compare(Point x, Point y)
        {
            int c1 = x.Coordinate.X.CompareTo(y.Coordinate.X);

            if(c1 == 0)
            {
                int c2 = x.Coordinate.Y.CompareTo(y.Coordinate.Y);
                if (c2 == 0)
                {
                    int c3 = x.Coordinate.Z.CompareTo(y.Coordinate.Z);
                    return c3;
                }
                else
                {
                    return c2;
                }
            }
            else
            {
                return c1;
            }
        }



        public override bool Equals(Point x, Point y)
        {
            CoordinateZ xz = x.Coordinate is CoordinateZ xx ? xx : new CoordinateZ(x.Coordinate);
            CoordinateZ yz = y.Coordinate is CoordinateZ yy ? yy : new CoordinateZ(y.Coordinate);

            return xz.Distance3D(yz) < _tolerance;
        }


        public override int GetHashCode(Point obj)
        {
            var c = obj.Coordinate;

            if (c is CoordinateZ z)
            {
                return GeometryComparer3D.GetCoordinateZHashCode(z);
            }
            else
            {
                return c.GetHashCode();
            }
        }
    }
}
