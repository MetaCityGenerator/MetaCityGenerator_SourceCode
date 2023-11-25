using System;


namespace UrbanX.Algorithms.Mathematics
{
    public static class SolveQuadratic
    {

        //ax2 + bx + c = 0
        public static bool Compute(double a, double b, double c, out double[] roots)
        {
            if (a == 0 && b != 0)
            {
                var root = -c / b;
                roots = new double[] { root, root };

                return true;
            }



            double sqrtpart = b * b - 4 * a * c;

            double x, x1, x2; //img;

            if (sqrtpart > 0)

            {

                x1 = (-b + Math.Sqrt(sqrtpart)) / (2 * a);

                x2 = (-b - Math.Sqrt(sqrtpart)) / (2 * a);

                roots = new double[] { x1, x2 };
                return true;

            }
            else if (sqrtpart < 0)
            {
                //sqrtpart = -sqrtpart;
                //x = -b / (2 * a);
                //img = Math.Sqrt(sqrtpart) / (2 * a);

                roots = null;
                return false;
            }
            else
            {
                x = (-b + Math.Sqrt(sqrtpart)) / (2 * a);
                roots = new double[] { x, x };
                return true;
            }
        }
    }
}
