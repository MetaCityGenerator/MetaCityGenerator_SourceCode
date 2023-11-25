using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;


using NetTopologySuite.Geometries;

namespace UrbanX.DataStructures.Geometry3D
{
    /// <summary>
    /// Represents the three coordinates of a immutable point in three-dimensional space,
    /// using <see cref="double"/>-precision floating point numbers.
    /// </summary> 
    [StructLayout(LayoutKind.Auto)]
    [DebuggerDisplay("({_vector.X}, {_vector.Y}, {_vector.Z})")]
    public readonly struct UPoint: IEquatable<UPoint>, IComparable<UPoint>
    {
        private readonly UVector3 _vector;

        private static readonly UPoint _orgin = new UPoint(0, 0, 0);

        private static readonly UPoint _unset = new UPoint(double.NaN, double.NaN, double.NaN);

        #region Properties.
        public double X => _vector.X;

        public double Y => _vector.Y;

        public double Z => _vector.Z;



        public bool IsValid => IsValidDouble( _vector.X) && IsValidDouble(_vector.Y) && IsValidDouble( _vector.Z);



        /// <summary>
        /// Gets the value of a point at location 0,0,0.
        /// </summary>
        public static ref readonly UPoint Origin => ref _orgin;

        public static ref readonly UPoint Unset => ref _unset;


        #endregion


        #region Constructor.
        /// <summary>
        /// Initializes a new point by defining the X, Y and Z coordinates.
        /// </summary>
        /// <param name="x">The value of the X (first) coordinate.</param>
        /// <param name="y">The value of the Y (second) coordinate.</param>
        /// <param name="z">The value of the Z (third) coordinate.</param>
        public UPoint(double x, double y, double z)
        {
            _vector = new UVector3(x, y, z);
        }


        public UPoint( UVector3 vector)
        {
            _vector = vector;
        }

        #endregion


        #region Methods.
        public bool Equals(UPoint other)
        {
            return this._vector == other._vector;
        }

        public bool EqualsExact(in UPoint other , double tolerance)
        {
            if (tolerance == 0)
                return this.Equals(other);
            else
                return this.DistanceTo(in other) < tolerance;
        }


        public int CompareTo(UPoint other)
        {
            if (_vector.X < other.X)
                return -1;
            if (_vector.X > other.X)
                return 1;

            if (_vector.Y < other.Y)
                return -1;
            if (_vector.Y > other.Y)
                return 1;

            if (_vector.Z < other.Z)
                return -1;
            if (_vector.Z > other.Z)
                return 1;

            return 0;
        }

        public override bool Equals(object obj)
        {
            return obj is UPoint p &&this == p;
        }


        public override int GetHashCode()
        {
            return this._vector.GetHashCode();
        }


        public double DistanceTo(in UPoint other)
        {
            return UVector3.Distance(this._vector, other._vector);
        }


        public Point ToNTSPoint(GeometryFactory gf) => gf.CreatePoint((Coordinate)this);


        // Transform , Translate, Reduce methods.
        // For value type (struct) , return a new value. For reference type, change the item itself.

        /// <summary>
        /// Transform a point by a specified 4x4 matrix.
        /// </summary>
        /// <param name="xform">The transformed point.</param>
        /// <returns></returns>
        public UPoint Transform(in Matrix4x4 xform)
        {
            var v = UVector3.Transform(this._vector, xform);
            return new UPoint(v);
        }

        
        /// <summary>
        /// Moving point with a specified vector.
        /// </summary>
        /// <param name="vector"></param>
        /// <returns>The translated point.</returns>
        public UPoint Translate(in UVector3 vector) => this + vector;


        /// <summary>
        /// Moving point with a specified direction and distance.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public UPoint Translate(in UVector3 direction, double length)
        {
            var v = direction * length;
            return this.Translate(v);
        }


        /// <summary>
        /// Return a new point with rounded coordinates.
        /// </summary>
        /// <param name="round"></param>
        /// <returns></returns>
        public UPoint ReducePrecision(int round)
        {
            var x = Math.Round(_vector.X, round);
            var y = Math.Round(_vector.Y, round);
            var z = Math.Round(_vector.Z, round);
            return new UPoint(x, y, z);
        }


        public static UVector3 Direction(in UPoint from ,in UPoint to)
        {
            var v = to._vector - from._vector;
            return UVector3.Normalize(v);
        }


        /// <summary>
        /// Get the NTS envelope for this point.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public Envelope GetEnvelope()
        {
            return new Envelope((Coordinate)this);
        }


        private static bool IsValidDouble(double x)
        {
            return !double.IsInfinity(x) && !double.IsNaN(x);
        }


        #endregion



        #region Operators.
        public static bool operator ==(in UPoint a,in UPoint b)
        {
            return a._vector == b._vector;
        }


        public static bool operator !=(in UPoint a,in UPoint b)
        {
            return a._vector != b._vector;
        }


        public static bool operator < (in UPoint a, in UPoint b)
        {
            return a.CompareTo(b)<0;
        }


        public static bool operator <=(in UPoint a, in UPoint b)
        {
            return a.CompareTo(b) <= 0;
        }


        public static bool operator >(in UPoint a, in UPoint b)
        {
            return a.CompareTo(b) > 0;
        }


        public static bool operator >=(in UPoint a, in UPoint b)
        {
            return a.CompareTo(b) >= 0;
        }


        public static UPoint operator +(in UPoint a, in UPoint b)
        {
            var v = a._vector + b._vector;
            return new UPoint(v);
        }

        public static UPoint operator +(in UPoint a, in UVector3 vector)
        {
            var v = a._vector + vector;
            return new UPoint(v);
        }


        public static UPoint operator -(in UPoint a, in UPoint b)
        {
            var v = a._vector - b._vector;
            return new UPoint(v);
        }


        public static UPoint operator -(in UPoint a, in UVector3 vector)
        {
            var v = a._vector - vector;
            return new UPoint(v);
        }



        public static UPoint operator *(in UPoint a, double d)
        {
            var v = a._vector*d;
            return new UPoint(v);
        }



        public static UPoint operator *(double d, in UPoint a)
        {
            var v = d* a._vector;
            return new UPoint(v);
        }


        public static explicit operator UPoint(in Coordinate c)
        {
            var z = double.IsNaN(c.Z) ? 0 : c.Z;
            return new UPoint(c.X, c.Y, z);
        }


        public static explicit operator Coordinate(in UPoint p)
        {
            return new CoordinateZ(p.X, p.Y, p.Z);
        }



        public static explicit operator Point(in UPoint p)
        {
            return new Point(p.X, p.Y, p.Z);
        }



        public static explicit operator UVector3(in UPoint p)
        {
            return p._vector;
        }

        #endregion
    }


    public sealed class Point3DComparer : EqualityComparer<UPoint>, IComparer<UPoint>
    {
        private readonly double _tolerance;


        public Point3DComparer() : this(0d) { }


        public Point3DComparer(double tolerance)
        {
            _tolerance = tolerance;

        }

        public int Compare(UPoint x, UPoint y)
        {
            return x.CompareTo(y);
        }

        public override bool Equals(UPoint x, UPoint y)
        {
            return x.EqualsExact(y, _tolerance);
        }

        public override int GetHashCode(UPoint obj)
        {
            return obj.GetHashCode();
        }
    }

}
