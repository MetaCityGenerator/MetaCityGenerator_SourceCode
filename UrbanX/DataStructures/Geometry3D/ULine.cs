using NetTopologySuite.Geometries;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MetaCity.DataStructures.Geometry3D
{
    [StructLayout(LayoutKind.Auto)]
    [DebuggerDisplay("({_from}, {_to})")]
    public readonly struct ULine: IEquatable<ULine>
    {
        private readonly UPoint _from;

        private readonly UPoint _to;


        private static readonly ULine _unset = new ULine(in UPoint.Unset,in UPoint.Unset);
        #region Properties.


        //TODO: all the properties should be marked as readonly in version higher than 8.0.
        /// <summary>
        /// Start point of line segment.
        /// </summary>
        public UPoint From => _from;


        /// <summary>
        /// End point of line segment.
        /// </summary>
        public UPoint To => _to;


        public static ref readonly ULine Unset => ref _unset;


        /// <summary>
        /// Gets a value indicating whether or not this line is valid. 
        /// Valid lines must have valid start and end points.
        /// </summary>
        public bool IsValid => _from.IsValid && _to.IsValid && _from.DistanceTo(_to) > 0;


        /// <summary>
        /// Gets the length of this line segment. 
        /// </summary>
        public double Length => _from.DistanceTo(_to);


        /// <summary>
        /// Gets the normalized direction of this line segment. 
        /// </summary>
        public UVector3 Direction => UPoint.Direction(_from, _to);


        #endregion


        #region Constructor.

        /// <summary>
        /// Constructs a new normalized line segment between two points.
        /// </summary>
        /// <param name="start">Start point of line.</param>
        /// <param name="end">End point of line.</param>
        public ULine(in UPoint start,in UPoint end )
        {
            if( start > end)
            {
                _from = end;
                _to = start;
            }
            else
            {
                _from = start;
                _to = end;
            }
        }


        /// <summary>
        /// Constructs a new line segment from start point and span vector.
        /// </summary>
        /// <param name="start">Start point of line segment.</param>
        /// <param name="span">Direction and length of line segment.</param>
        public ULine(in UPoint start, in UVector3 span)
        {
            var end = start + span;

            if (start > end)
            {
                _from = end;
                _to = start;
            }
            else
            {
                _from = start;
                _to = end;
            }
        }



        /// <summary>
        /// Constructs a new line segment from start point, direction and length.
        /// </summary>
        /// <param name="start">Start point of line segment.</param>
        /// <param name="direction">Direction of line segment.</param>
        /// <param name="length">Length of line segment.</param>
        public ULine(in UPoint start, in UVector3 direction , double length)
        {
            var end = start + length * UVector3.Normalize(direction);
            _from = start;
            _to = end;

            if (start > end)
            {
                _from = end;
                _to = start;
            }
            else
            {
                _from = start;
                _to = end;
            }
        }



        /// <summary>
        /// Constructs a new line segment between two points.
        /// </summary>
        /// <param name="x0">The X coordinate of the first point.</param>
        /// <param name="y0">The Y coordinate of the first point.</param>
        /// <param name="z0">The Z coordinate of the first point.</param>
        /// <param name="x1">The X coordinate of the second point.</param>
        /// <param name="y1">The Y coordinate of the second point.</param>
        /// <param name="z1">The Z coordinate of the second point.</param>
        /// <param name="normalize">Whether put the line segment into a normalized form wherer start point is smaller than end point.</param>
        public ULine(double x0, double y0, double z0, double x1, double y1, double z1)
        {
            var start = new UPoint(x0, y0, z0);
            var end = new UPoint(x1, y1, z1);

            if (start > end)
            {
                _from = end;
                _to = start;
            }
            else
            {
                _from = start;
                _to = end;
            }
        }

        #endregion


        #region Methods.

        /// <summary>
        /// Determines whether an object is a line that has the same value as this line.
        /// </summary>
        /// <param name="obj">An object.</param>
        public override bool Equals(object obj)
        {
            return obj is ULine p && this == p;
        }

        /// <summary>
        /// Determines whether a line has the same value as this line. LIne has already been normalized during construction stage.
        /// </summary>
        /// <param name="other">A line.</param>
        /// <returns>true if other has the same coordinates as this; otherwise false.</returns>
        public bool Equals(ULine other)
        {
            return this == other;
        }

        public bool EqualsExact(in ULine other, double tolerance)
        {
            if (tolerance == 0)
                return Equals(other);
            else
            {
                if (!this._from.EqualsExact(in other._from,tolerance)|| !this._to.EqualsExact(in other._to, tolerance))
                    return false;
                else
                    return true;
            }
        }


        /// <summary>
        /// Computes a hash number that represents this line.
        /// </summary>
        /// <returns>A number that is not unique to the value of this line.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(_from.GetHashCode(), _to.GetHashCode());
        }

        /// <summary>
        /// Get a fliped line segment.
        /// </summary>
        /// <returns></returns>
        public ULine FlipLine()
        {
            return new ULine(_to, _from);
        }


        /// <summary>
        /// Get the point on line at the specific parameter.
        /// </summary>
        /// <param name="t">Parameter to evaluate line segment at. Line parameters are normalized parameters(0~1).</param>
        /// <returns>0 for start point, 1 for end point.</returns>
        public UPoint PointAt(double t)
        {
            var v = t * this.Length * this.Direction;
            return this._from + v;
        }


        public LineString ToLineString(GeometryFactory gf)
        {
            Coordinate[] arr = new Coordinate[] { (Coordinate)this._from, (Coordinate)this._to };
            return gf.CreateLineString(arr);
        }

        /// <summary>
        /// Transform a line by a specified 4x4 matrix.
        /// </summary>
        /// <param name="xform"></param>
        /// <returns>The transformed line.</returns>
        public ULine Transform( in Matrix4x4 xform)
        {
            var start = this._from.Transform(xform);
            var end = this._to.Transform(xform);
            return new ULine(start, end);
        }


        /// <summary>
        /// Moving line with a specified vector.
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public ULine Translate(in UVector3 vector)
        {
            var start = this._from + vector;
            var end = this._to + vector;
            return new ULine(start, end);
        }


        /// <summary>
        /// Moving line with a specified direction and distance.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public ULine Translate(in UVector3 direction, double length)
        {
            var v = direction * length;
            return this.Translate(v);
        }


        /// <summary>
        /// Return a new line with rounded coordinates.
        /// </summary>
        /// <param name="round"></param>
        /// <returns></returns>
        public ULine ReducePrecision(int round)
        {
            var start = this._from.ReducePrecision(round);
            var end = this._to.ReducePrecision(round);
            return new ULine(start, end);
        }


        /// <summary>
        /// Get the NTS envelope for this line.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public Envelope GetEnvelope()
        {
            return new Envelope((Coordinate)this.From, (Coordinate)this.To);
        }

        #endregion



        #region Operators.
        /// <summary>
        /// Determines whether two lines have the same value. 
        /// </summary>
        /// <param name="a">A line.</param>
        /// <param name="b">Another line.</param>
        /// <returns>true if a has the same coordinates as b in the same order; otherwise false.</returns>
        public static bool operator ==(in ULine a,in ULine b)
        {
            return a._from == b._from && a._to == b._to;
        }

        /// <summary>
        /// Determines whether two lines have different values.
        /// </summary>
        /// <param name="a">A line.</param>
        /// <param name="b">Another line.</param>
        /// <returns>true if a has any coordinate that distinguishes it from b; otherwise false.</returns>
        public static bool operator !=(in ULine a,in ULine b)
        {
            return a._from != b._from || a._to != b._to;
        }


        public static explicit operator LineString(in ULine l)
        {
            Coordinate[] arr = new Coordinate[] { (Coordinate)l._from, (Coordinate)l._to };
            return new LineString(arr);
        }


        #endregion
    }
}
