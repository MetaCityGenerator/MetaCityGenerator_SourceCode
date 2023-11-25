using Rhino.Geometry;

using System;


namespace UrbanX.Planning.UrbanDesign
{
    /// <summary>
    /// Utility class including fillet curve corners and other useful methods.
    /// </summary>
    public class DesignUtilities
    {
        public static PolyCurve FilletPolylineCorners(Curve c, double radius, double tolerance)
        {
            Polyline p = DesignToolbox.ConvertToPolyline(c, tolerance);

            bool flag = p.First.DistanceTo(p.Last) < 0.001 * tolerance;
            PolyCurve polyCurve = new PolyCurve();
            if (flag)
            {
                Arc arc = FilletSegments(p[p.Count - 2], p[0], p[1], radius);
                if (arc.IsValid)
                {
                    polyCurve.Append(arc);
                }
            }
            double num = 1E-12;
            for (int i = 1; i < p.Count - 1; i++)
            {
                Arc arc2 = FilletSegments(p[i - 1], p[i], p[i + 1], radius);
                if (arc2.IsValid)
                {
                    if (polyCurve.SegmentCount == 0)
                    {
                        if (p[0].DistanceTo(arc2.StartPoint) > num)
                        {
                            polyCurve.Append(new Line(p[0], arc2.StartPoint));
                        }
                        polyCurve.Append(arc2);
                    }
                    else
                    {
                        if (polyCurve.PointAtEnd.DistanceTo(arc2.StartPoint) > num)
                        {
                            polyCurve.Append(new Line(polyCurve.PointAtEnd, arc2.StartPoint));
                        }
                        polyCurve.Append(arc2);
                    }
                }
            }
            if (flag)
            {
                if (polyCurve.PointAtEnd.DistanceTo(polyCurve.PointAtStart) > num)
                {
                    polyCurve.Append(new Line(polyCurve.PointAtEnd, polyCurve.PointAtStart));
                }
            }
            else if (polyCurve.PointAtEnd.DistanceTo(p.Last) > num)
            {
                polyCurve.Append(new Line(polyCurve.PointAtEnd, p.Last));
            }
            return polyCurve;
        }

        private static Arc FilletSegments(Point3d A, Point3d B, Point3d C, double radius)
        {
            double num = A.DistanceTo(B);
            double num2 = B.DistanceTo(C);

            if (num == 0.0 || num2 == 0.0)
            {
                return Arc.Unset;
            }

            Vector3d vector3d = A - B;
            Vector3d vector3d2 = C - B;
            vector3d.Unitize();
            vector3d2.Unitize();
            double num3 = UnitVectorAngle(vector3d, vector3d2);
            double num4 = radius / Math.Tan(0.5 * num3);
            if (num4 > 0.5 * num)
            {
                radius *= 0.5 * num / num4;
                num4 = 0.5 * num;
            }
            if (num4 > 0.5 * num2)
            {
                radius *= 0.5 * num2 / num4;
                num4 = 0.5 * num2;
            }
            return ARC_SED(B + vector3d * num4, B + vector3d2 * num4, B - A);
        }

        private static double UnitVectorAngle(Vector3d BA, Vector3d BC)
        {
            double num = (BA.X - BC.X) * (BA.X - BC.X) + (BA.Y - BC.Y) * (BA.Y - BC.Y) + (BA.Z - BC.Z) * (BA.Z - BC.Z);
            num = Math.Sqrt(num);
            return 2.0 * Math.Asin(0.5 * num);
        }

        private static Arc ARC_SED(Point3d S, Point3d E, Vector3d D)
        {
            Vector3d vector3d = D;
            Vector3d vector3d2 = E - S;
            if (vector3d2.Length.Equals(0.0))
            {
                return Arc.Unset;
            }
            vector3d2.Unitize();
            vector3d.Unitize();
            Vector3d vector3d3 = vector3d2 + vector3d;
            vector3d3.Unitize();
            vector3d3 *= 0.5 * S.DistanceTo(E) / (vector3d3 * vector3d);
            return new Arc(S, S + vector3d3, E);
        }
    }
}
