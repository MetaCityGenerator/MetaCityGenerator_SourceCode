using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Threading.Tasks;

namespace UrbanX.DataStructures.Geometry3D
{
    public static class Extension
    {
        public static double AngleBetween(this UVector3 v, UVector3 u)
        {
            //v • u =|𝐯||u| cos𝜃
            //var d = v.Dot(u);
            //var l = v.Length() * u.Length();
            var vn = UVector3.Normalize(v);
            var un = UVector3.Normalize(u);

            // the result is the abosolute value of angle, dispite the direction between to vectors.
            // due to the floating, dot may larger or smaller than 1 or -1.
            var dot = UVector3.Dot(vn, un);
            dot = dot > 1 ? 1 : dot;
            dot = dot < -1 ? -1 : dot;

            return Math.Acos(dot);
        }
    }
}
