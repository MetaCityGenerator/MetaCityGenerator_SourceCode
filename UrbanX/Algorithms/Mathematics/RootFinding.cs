using System;

namespace UrbanX.Algorithms.Mathematics
{
    public delegate double FunctionOfOneVariable(double x);

    public class RootFinding
    {
        const int maxIterations = 60;

        #region Bisect
        public static double Bisect(FunctionOfOneVariable formula, double left, double right, double target, out int iterationsUsed, out double errorEstimate, double tolerance = 1E-6)
        {
            if (tolerance <= 0.0)
            {
                string msg = string.Format("Tolerance must be positive. Recieved {0}.", tolerance);
                throw new ArgumentOutOfRangeException(msg);
            }

            iterationsUsed = 0;
            errorEstimate = double.MaxValue;

            // Standardize the problem.  To solve f(x) = target,
            // solve g(x) = 0 where g(x) = f(x) - target.
            double f(double x) { return formula(x) - target; }


            double g_left = f(left);  // evaluation of f at left end of interval
            double g_right = f(right);
            double mid;
            double g_mid;
            if (g_left * g_right >= 0.0)
            {
                string str = "Invalid starting bracket. Function must be above target on one end and below target on other end.";
                string msg = string.Format("{0} Target: {1}. f(left) = {2}. f(right) = {3}", str, target, g_left + target, g_right + target);
                throw new ArgumentException(msg);
            }

            double intervalWidth = right - left;

            for
            (
                iterationsUsed = 0;
                iterationsUsed < maxIterations && intervalWidth > tolerance;
                iterationsUsed++
            )
            {
                intervalWidth *= 0.5;
                mid = left + intervalWidth;

                if ((g_mid = f(mid)) == 0.0)
                {
                    errorEstimate = 0.0;
                    return mid;
                }
                if (g_left * g_mid < 0.0)           // g changes sign in (left, mid)    
                    g_right = f(right = mid);
                else                            // g changes sign in (mid, right)
                    g_left = f(left = mid);
            }
            errorEstimate = right - left;
            return left;
        }

        #endregion

        #region Brent
        public static double Brent(FunctionOfOneVariable formula, double left, double right, double target, out int iterationsUsed, out double errorEstimate, double tolerance = 1E-6)
        {
            if (tolerance <= 0.0)
            {
                string msg = string.Format("Tolerance must be positive. Recieved {0}.", tolerance);
                throw new ArgumentOutOfRangeException(msg);
            }

            errorEstimate = double.MaxValue;

            // Standardize the problem.  To solve g(x) = target,
            // solve f(x) = 0 where f(x) = g(x) - target.
            double f(double x) { return formula(x) - target; }

            // Implementation and notation based on Chapter 4 in
            // "Algorithms for Minimization without Derivatives"
            // by Richard Brent.

            double c, d, e, fa, fb, fc, tol, m, p, q, r, s;

            // set up aliases to match Brent's notation
            double a = left; double b = right; double t = tolerance;
            iterationsUsed = 0;

            fa = f(a);
            fb = f(b);

            if (fa * fb > 0.0)
            {
                string str = "Invalid starting bracket. Function must be above target on one end and below target on other end.";
                string msg = string.Format("{0} Target: {1}. f(left) = {2}. f(right) = {3}", str, target, fa + target, fb + target);
                throw new ArgumentException(msg);
            }

        label_int:
            c = a; fc = fa; d = e = b - a;
        label_ext:
            if (Math.Abs(fc) < Math.Abs(fb))
            {
                a = b; b = c; c = a;
                fa = fb; fb = fc; fc = fa;
            }

            iterationsUsed++;

            tol = 2.0 * t * Math.Abs(b) + t;
            errorEstimate = m = 0.5 * (c - b);
            if (Math.Abs(m) > tol && fb != 0.0) // exact comparison with 0 is OK here
            {
                // See if bisection is forced
                if (Math.Abs(e) < tol || Math.Abs(fa) <= Math.Abs(fb))
                {
                    d = e = m;
                }
                else
                {
                    s = fb / fa;
                    if (a == c)
                    {
                        // linear interpolation
                        p = 2.0 * m * s; q = 1.0 - s;
                    }
                    else
                    {
                        // Inverse quadratic interpolation
                        q = fa / fc; r = fb / fc;
                        p = s * (2.0 * m * q * (q - r) - (b - a) * (r - 1.0));
                        q = (q - 1.0) * (r - 1.0) * (s - 1.0);
                    }
                    if (p > 0.0)
                        q = -q;
                    else
                        p = -p;
                    s = e; e = d;
                    if (2.0 * p < 3.0 * m * q - Math.Abs(tol * q) && p < Math.Abs(0.5 * s * q))
                        d = p / q;
                    else
                        d = e = m;
                }
                a = b; fa = fb;
                if (Math.Abs(d) > tol)
                    b += d;
                else if (m > 0.0)
                    b += tol;
                else
                    b -= tol;
                if (iterationsUsed == maxIterations)
                    return b;

                fb = f(b);
                if ((fb > 0.0 && fc > 0.0) || (fb <= 0.0 && fc <= 0.0))
                    goto label_int;
                else
                    goto label_ext;
            }
            else
                return b;
        }

        #endregion

        #region Newton

        public static double Newton(FunctionOfOneVariable formula, FunctionOfOneVariable fprime, double guess, double target, out int iterationsUsed, out double errorEstimate, double tolerance = 1E-6)
        {
            if (tolerance <= 0)
            {
                string msg = string.Format("Tolerance must be positive. Recieved {0}.", tolerance);
                throw new ArgumentOutOfRangeException(msg);
            }

            errorEstimate = double.MaxValue;

            // Standardize the problem.  To solve f(x) = target,
            // solve g(x) = 0 where g(x) = f(x) - target.
            // Note that f(x) and g(x) have the same derivative.
            double f(double x) { return formula(x) - target; }

            double oldX, newX = guess;
            for
            (
                iterationsUsed = 0;
                iterationsUsed < maxIterations && errorEstimate > tolerance;
                iterationsUsed++
            )
            {
                oldX = newX;
                double gx = f(oldX);
                double gprimex = fprime(oldX);
                double absgprimex = Math.Abs(gprimex);
                if (absgprimex > 1.0 || Math.Abs(gx) < double.MaxValue * absgprimex)
                {
                    // The division will not overflow
                    newX = oldX - gx / gprimex;
                    errorEstimate = Math.Abs(newX - oldX);
                }
                else
                {
                    newX = oldX;
                    errorEstimate = double.MaxValue;
                    break;
                }
            }
            return newX;
        }

        #endregion
    }
}
