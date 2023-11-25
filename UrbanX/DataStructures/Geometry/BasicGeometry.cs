using System;


namespace UrbanX.DataStructures.Geometry
{
    /// <summary>
    /// Represents the two coordinates of a point in two-dimensional space.
    /// </summary>
    public struct Point : IEquatable<Point>, IComparable<Point>
    {
        public double X { get; }
        public double Y { get; }

        public Point(double x, double y) => (X, Y) = (x, y);

        public double DistanceTo(Point other)
        {
            return Math.Sqrt(Math.Pow(other.Y - this.Y, 2) + Math.Pow(other.X - this.X, 2));
        }

        public override string ToString() => $" [{X}, {Y}]";

        public bool Equals(Point other)
        {
            // For using contains method in collection.
            return this.X == other.X && this.Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Point))
                return false;

            Point p = (Point)obj;
            return this.Equals(p);
        }

        public override int GetHashCode()
        {
            // For using Hashtale
            // MSDN docs recommend XOR'ing the internal values to get a hash code
            return this.X.GetHashCode() ^ this.Y.GetHashCode();
        }

        public int CompareTo(Point other)
        {
            var c = this.X.CompareTo(other.X);
            return c == 0 ? this.Y.CompareTo(other.Y) : c;
        }
    }


    public struct Vector : IEquatable<Vector>
    {

        public double X { get; }
        public double Y { get; }

        public double Length { get { return Math.Sqrt(Math.Pow(this.X, 2) + Math.Pow(this.Y, 2)); } }

        public Vector(double x, double y) => (X, Y) = (x, y);

        public Vector(Point start, Point end) => (X, Y) = (end.X - start.X, end.Y - start.Y);


        public bool Equals(Vector other)
        {
            // For using contains method in collection.
            return this.X == other.X && this.Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Vector))
                return false;

            Vector v = (Vector)obj;
            return this.Equals(v);
        }

        public override int GetHashCode()
        {
            // For using Hashtale
            // MSDN docs recommend XOR'ing the internal values to get a hash code
            return this.X.GetHashCode() ^ this.Y.GetHashCode();
        }
    }


    /// <summary>
    /// For 2d Line.
    /// </summary>
    public struct Line : IEquatable<Line>
    {
        public Point From { get; }
        public Point To { get; }

        public double Length { get { return From.DistanceTo(To); } }

        public Line(Point start, Point end) => (From, To) = (start, end);



        public double GetLineSlope()
        {
            var xRun = To.X - From.X;
            var yRun = To.Y - From.Y;

            if (xRun == 0)
                return double.PositiveInfinity;

            return yRun / xRun;
        }


        public override string ToString() => $"Line:{From}-->{To}";

        public bool Equals(Line other)
        {
            return this.From.Equals(other.From) && this.To.Equals(other.To);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Line))
                return false;

            Line l = (Line)obj;
            return this.Equals(l);
        }

        public override int GetHashCode()
        {
            // For using Hashtale
            // MSDN docs recommend XOR'ing the internal values to get a hash code
            return this.From.GetHashCode() ^ this.To.GetHashCode();
        }
    }
}
