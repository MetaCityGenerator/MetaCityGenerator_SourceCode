using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrbanX.Algorithms.Geometry3D
{
    public static class Extension3D
    {
        public static CoordinateZ Translate(this CoordinateZ coordinate, Vector3D v)
        {
            //coordinate.X += v.X;  // using this method will change the orignial coordinate.
            //coordinate.Y += v.Y;
            //coordinate.Z += v.Z;
            return new CoordinateZ(coordinate.X + v.X, coordinate.Y + v.Y, coordinate.Z + v.Z);
        }


        public static void Reduce3D(this Geometry geom, int round)
        {
            for (int i = 0; i < geom.Coordinates.Length; i++)
            {
                geom.Coordinates[i].Reduce3D(round);
            }
        }

        public static void Reduce3D(this Geometry[] geoms, int round)
        {
            for (int i = 0; i < geoms.Length; i++)
            {
                geoms[i].Reduce3D(round);
            }
        }



        public static void Reduce3D(this Coordinate[] cs, int round)
        {
            for (int i = 0; i < cs.Length; i++)
            {
                cs[i].Reduce3D(round);
            }
        }

        public static void Reduce3D(this Coordinate c, int round)
        {
            c.X = Math.Round(c.X, round);
            c.Y = Math.Round(c.Y, round);
            c.Z = Math.Round(c.Z, round); // is c = Nan , round = NaN.
        }
    }
}
