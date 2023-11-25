using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetTopologySuite.Geometries;

namespace UrbanX.Algorithms.Geometry3D
{ 
    /// <summary>
    /// The custom comparer for 3d geometry. This comparer could be used in HashSet, Dictionary.
    /// This comparer doesn't handle tolerance error.
    /// If all the geometries have been normalized, can choose false in constructor.
    /// </summary>
    public sealed class GeometryComparer3D : EqualityComparer<Geometry> 
    {
        private bool _normalize;


        public GeometryComparer3D() : this(true) { }
 

        public GeometryComparer3D(bool normalize)
        {
            _normalize = normalize;
        }

        public void ChangeNormalize(bool value) => _normalize = value;


        public override bool Equals(Geometry x, Geometry y)
        {
            if (_normalize)
            {
                // normalize geom first. Using this comparer should normalize all the geometry.
                if (x is LineString l && TestVertical(l))
                    NormalizVerticalLine(l);
                else
                    x.Normalize();

                if (y is LineString s && TestVertical(s))
                    NormalizVerticalLine(s);
                else
                    y.Normalize();
            }
       
            if (x.Coordinates.Length != y.Coordinates.Length) // simple eqaulity test. may be expensive(need to check the performance).
                return false;
            else
            {
                // 3D equality. already handled the direction by normalization.
                var xs = x.Coordinates;
                var ys = y.Coordinates;

                for (int i = 0; i < xs.Length; i++) // should has same length.
                {
                    CoordinateZ xz = xs[i] is CoordinateZ xi ? xi : new CoordinateZ(xs[i]);
                    CoordinateZ yz = ys[i] is CoordinateZ yi ? yi : new CoordinateZ(ys[i]);

                    if (!xz.Equals3D(yz))
                    {
                        return false; // find one coordinateZ is not equal, then return false.
                    }
                }

                return true;
            }
        }

        public override int GetHashCode( Geometry obj) // When adding item into dict, call this method first.
        {
            if (_normalize)
            {
                // normalize geom first. Using this comparer should normalize all the geometry.
                if (obj is LineString l && TestVertical(l))
                    NormalizVerticalLine(l);
                else
                    obj.Normalize();
            }


            var cz = obj.Coordinates;

            int hcode =0;
            for (int i = 0; i < cz.Length; i++)
            {
                int h;
                var c = cz[i];
                if(c is CoordinateZ z)
                {
                    h = GetCoordinateZHashCode(z);
                }
                else 
                {
                    h = c.GetHashCode();
                }

                if (i == 0)
                {
                    hcode = h;
                }
                else
                {
                    hcode ^= h*(i*3+31);
                }
            }

            return hcode;
        }


        //First try: using snap to handle tolerance handle. This should work.
        //Second: using comparer handle tolerance error if suitable for points, but not working for point-line situation.
        public static int GetCoordinateZHashCode(CoordinateZ pt)
        {
            int hcode = (pt.X.GetHashCode()*31) ^ (pt.Y.GetHashCode()*37) ^ (pt.Z.GetHashCode()*41); // works when z is NaN.
            return hcode;
        }


        private bool TestVertical(LineString l)
        {
            if (l.Count == 2)
            {
                return l.StartPoint.Equals(l.EndPoint);
            }
            else
                return false;
        }


        private void NormalizVerticalLine(LineString l)
        {
            var comparer = l.StartPoint.Z.CompareTo(l.EndPoint.Z);

            if (comparer > 0)
            {
                var temp = l.StartPoint.Coordinate;
                l.Coordinates[0] = l.Coordinates[1];
                l.Coordinates[1] = temp;
            }
                
        }

    }
}
