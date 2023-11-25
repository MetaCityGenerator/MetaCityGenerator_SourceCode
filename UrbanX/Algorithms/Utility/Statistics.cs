using System;

namespace UrbanX.Algorithms.Utility
{
    public static class Statistics
    {
        // LinearRegression method
        public static void LinearRegression(double[] xVals, double[] yVals, out double rSquared, out double yIntercept, out double slope)
        {
            if (xVals.Length != yVals.Length)
            {
                throw new Exception("xVals and yVals should have the same length.");
            }

            // Declare sum(xy), sumX, sumY, sumX^2,sumY^2
            double sumX = 0;
            double sumY = 0;
            double sumXY = 0;
            double sumX2 = 0;
            double sumY2 = 0;
            var count = xVals.Length;

            for (int i = 0; i < count; i++)
            {
                var x = xVals[i];
                var y = yVals[i];
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += Math.Pow(x, 2);
                sumY2 += Math.Pow(y, 2);
            }

            // Calculate m(slope) and b(intercept) by using the best fit equation.

            slope = (count * sumXY - (sumX * sumY)) / (count * sumX2 - Math.Pow(sumX, 2));
            yIntercept = (sumY - slope * sumX) / count;
            rSquared = slope * ((count * sumXY - (sumX * sumY)) / (count * sumY2 - Math.Pow(sumY, 2)));
        }


        // PowerRegression method
        public static void PowerRegression(double[] xVals, double[] yVals, out double R2, out double a, out double b)
        {
            if (xVals.Length != yVals.Length)
            {
                throw new Exception("xVals and yVals should have the same length.");
            }

            var count = xVals.Length;
            double lnxSum = 0;
            double lnySum = 0;

            double lnx2Sum = 0;
            double lny2Sum = 0;

            double lnxlnySum = 0;

            for (int i = 0; i < count; i++)
            {
                double lnxi = Math.Log(xVals[i]);
                double lnyi = Math.Log(yVals[i]);

                lnxSum += lnxi;
                lnySum += lnyi;

                lnx2Sum += Math.Pow(lnxi, 2);
                lny2Sum += Math.Pow(lnyi, 2);

                lnxlnySum += lnxi * lnyi;
            }

            double lnxMean = lnxSum / count;
            double lnyMean = lnySum / count;

            double Sxx = lnx2Sum - Math.Pow(lnxMean, 2) * count;
            double Syy = lny2Sum - Math.Pow(lnyMean, 2) * count;
            double Sxy = lnxlnySum - count * lnxMean * lnyMean;

            R2 = Math.Pow(Sxy, 2) / (Sxx * Syy);
            b = Sxy / Sxx;
            a = Math.Exp(lnyMean - b * lnxMean);

        }
        // PowerRegression estimate
        public static double PowerEstimate(double a, double b, double x)
        {
            double y = a * Math.Pow(x, b);
            return y;
        }



        // ExponentialRegression method 

        public static void ExponentRegression(double[] xVals, double[] yVals, out double R2, out double a, out double b)
        {
            // y = a*b^x 
            if (xVals.Length != yVals.Length)
            {
                throw new Exception("xVals and yVals should have the same length.");
            }

            var count = xVals.Length;

            double xSum = 0;
            double lnySum = 0;

            double x2Sum = 0;
            double lny2Sum = 0;

            double xlnySum = 0;

            for (int i = 0; i < count; i++)
            {
                double xi = xVals[i];
                double lnyi = Math.Log(yVals[i]);

                xSum += xi;
                lnySum += lnyi;

                x2Sum += Math.Pow(xi, 2);
                lny2Sum += Math.Pow(lnyi, 2);

                xlnySum += xi * lnyi;
            }

            double xMean = xSum / count;
            double lnyMean = lnySum / count;

            double Sxx = x2Sum - Math.Pow(xMean, 2) * count;
            double Syy = lny2Sum - Math.Pow(lnyMean, 2) * count;
            double Sxy = xlnySum - count * xMean * lnyMean;


            R2 = Math.Pow(Sxy, 2) / (Sxx * Syy);
            b = Math.Exp(Sxy / Sxx);
            a = Math.Exp(lnyMean - xMean * Math.Log(b));
        }

        // ExponentRegression estimate
        public static double ExponetEstimate(double a, double b, double x)
        {
            double y = a * Math.Pow(b, x);
            return y;
        }
    }
}
