using NetTopologySuite.Geometries;

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;


namespace UrbanX.DataStructures.Geometry3D
{
    [DebuggerDisplay("({First.ToString()}, {Last.ToString()})")]
    public sealed class UPolyline : IEquatable<UPolyline>
    {
        /// <summary>
        /// Internal array to store all the points of polyline.
        /// </summary>
        private readonly UPoint[] _pts;


        #region Properties.

        /// <summary>
        /// Returns an array containing all the points of polyline.
        /// </summary>
        public UPoint[] Coordinates => _pts;


        /// <summary>
        /// Returns a reference to specified point of the polyline.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ref UPoint this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return ref _pts[index];
            }
        }


        /// <summary>
        /// Returns a reference of the first point.
        /// </summary>
        public ref UPoint First => ref _pts[0];


        /// <summary>
        /// Returns a reference of the last point.
        /// </summary>
        public ref UPoint Last => ref _pts[_pts.Length - 1];


        /// <summary>
        /// Gets the total length of the polyline.
        /// </summary>
        public double Length
        {
            get
            {
                if (_pts.Length < 2) return 0;

                double l = 0d;
                for (int i = 0; i < _pts.Length - 1; i++)
                {
                    l += _pts[i].DistanceTo(_pts[i + 1]);
                }

                return l;
            }
        }


        /// <summary>
        /// Gets the points number of current polyline.
        /// </summary>
        public int NumPoints => _pts.Length;



        /// <summary>
        /// Gets the number of segments for this polyline.
        /// </summary>
        public int SegmentCount => Math.Max(_pts.Length - 1, 0);



        /// <summary>
        /// Gets a value that indicates whether this polyline is closed. 
        /// <para>The polyline is considered to be closed if its start is 
        /// identical to its endpoint.</para>
        /// </summary>
        public bool IsClosed => _pts.Length > 3 && _pts[0] == _pts[_pts.Length - 1];



        /// <summary>
        /// Gets a value that indicates whether this polyline is valid. 
        /// <para>Valid polylines have at least one segment, no Invalid points and no zero length segments.</para>
        /// <para>Closed polylines with only two segments are also not considered valid.</para>
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (_pts.Length < 2 || !_pts[0].IsValid) return false;
                if (_pts.Length == 3 && _pts[0] == _pts[_pts.Length - 1]) return false;

                for (int i = 1; i < _pts.Length; i++)
                {
                    if (!_pts[i].IsValid) return false;
                    if (_pts[i] == _pts[i - 1]) return false;
                }

                return true;
            }
        }


        #endregion

        #region Constructor.

        /// <summary>
        /// Initializes a new empty polyline.
        /// </summary>
        public UPolyline()
        {
        }

        /// <summary>
        /// Initializes a new polyline from a collection of <see cref="UPoint"/>. Pollyline will be normalized automatically during instantiation.
        /// </summary>
        /// <param name="pts">A collection of points to construct the polyline. </param>
        public UPolyline(UPoint[] pts)
        {
            _pts = pts;
            Normalize();
        }

        #endregion

        #region Methods.

        /// <summary>
        /// A normalized <see cref="UPolyline"/>
        /// has the first <see cref="UPoint"/> which is not equal to it's reflected <see cref="UPoint"/>
        /// less than the reflected <see cref="UPoint"/>.
        /// </summary>
        public void Normalize()
        {
            for (int i = 0; i < _pts.Length / 2; i++)
            {
                int j = _pts.Length - 1 - i;

                // skip equal points on both ends
                if (_pts[i] != _pts[j])
                {
                    if (_pts[i] > _pts[j]) // current start is larger than current end.
                    {
                        this.Reverse();
                    }
                    return;
                }
            }
        }


        /// <summary>
        /// Method to reverse the <see cref="UPoint"/> order of current <see cref=" UPolyline"/>.
        /// </summary>
        public void Reverse()
        {
            Array.Reverse(_pts);
        }

        /// <summary>
        /// Gets the line segment at the given index.
        /// </summary>
        /// <param name="index">Index of segment to retrieve.</param>
        /// <returns>Line segment at index or Line.Unset on failure.</returns>
        public ULine SegmentAt(int index)
        {
            if (index < 0 || index >= _pts.Length - 1) return ULine.Unset;
            return new ULine(_pts[index], _pts[index + 1]);
        }


        /// <summary>
        /// Constructs an array of line segments that make up the entire polyline.
        /// </summary>
        /// <returns>An array of line segments or null if the polyline contains fewer than 2 points.</returns>
        public ULine[] GetSegments()
        {
            if (_pts.Length < 2)
                return null;

            ULine[] segs = new ULine[_pts.Length - 1];

            for (int i = 0; i < _pts.Length - 1; i++)
            {
                segs[i] = new ULine(_pts[i], _pts[i + 1]);
            }

            return segs;
        }


        /// <summary>
        /// Compute the center point of the polyline as the weighted average of all segments.
        /// </summary>
        /// <returns>The weighted average of all segments.</returns>
        public UPoint CenterPoint()
        {
            if (_pts.Length == 0) return UPoint.Unset;
            if (_pts.Length == 1) return this._pts[0];

            UPoint center = UPoint.Origin;
            double weight = 0d;

            for (int i = 0; i < _pts.Length - 1; i++)
            {
                var d = _pts[i].DistanceTo(_pts[i + 1]);
                center += d * 0.5f * (_pts[i] + _pts[i + 1]);
                weight += d;
            }

            if (weight == 0) return this._pts[0];
            return center * (1 / weight);
        }


        /// <summary>
        /// Transform a polyline by a specified 4x4 matrix.
        /// </summary>
        /// <param name="xform">The transformed polyline.</param>
        public void Transform(in Matrix4x4 xform)
        {
            for (int i = 0; i < _pts.Length; i++)
            {
                _pts[i] = _pts[i].Transform(xform);
            }
        }


        /// <summary>
        /// Moving polyline with a specified vector.
        /// </summary>
        /// <param name="vector"></param>
        public void Translate(in UVector3 vector)
        {
            for (int i = 0; i < _pts.Length; i++)
            {
                _pts[i] = _pts[i].Translate(vector);
            }
        }


        /// <summary>
        /// Moving polyline with a specified direction and distance.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="length"></param>
        public void Translate(in UVector3 direction, double length)
        {
            var v = direction * length;
            for (int i = 0; i < _pts.Length; i++)
            {
                _pts[i] = _pts[i].Translate(v);
            }
        }


        /// <summary>
        /// Reduce all the coordinates precision.
        /// </summary>
        /// <param name="round"></param>
        public void ReducePrecision(int round)
        {
            for (int i = 0; i < _pts.Length; i++)
            {
                _pts[i] = _pts[i].ReducePrecision(round);
            }
        }


        public bool Equals(UPolyline other)
        {
            if (this.NumPoints != other.NumPoints)
                return false;

            for (int i = 0; i < this._pts.Length; i++)
            {
                if (this._pts[i] != other._pts[i])
                    return false;
            }
            return true;
        }



        public bool EqualsExact(in UPolyline other, double tolerance) // in only describle the design intent.
        {
            if (tolerance == 0)
                return this.Equals(other);
            else
            {
                if (this.NumPoints != other.NumPoints)
                    return false;

                for (int i = 0; i < this._pts.Length; i++)
                {
                    if (!this._pts[i].EqualsExact(other._pts[i], tolerance))
                        return false;
                }
                return true;
            }
        }



        public override int GetHashCode()
        {
            var hash = new HashCode();
            for (int i = 0; i < _pts.Length; i++)
            {
                hash.Add(_pts[i]);
            }
            return hash.ToHashCode();
        }



        /// <summary>
        /// Returns a deep copy of this polyline instance.
        /// </summary>
        /// <returns></returns>
        public UPolyline Duplicate() => new UPolyline(this._pts);


        public LineString ToLineString(GeometryFactory gf)
        {
            Coordinate[] arr = new Coordinate[_pts.Length];
            for (int i = 0; i < _pts.Length; i++)
            {
                arr[i] = (Coordinate)_pts[i];
            }
            return gf.CreateLineString(arr);
        }


        public static explicit operator LineString(in UPolyline p)
        {
            Coordinate[] arr = new Coordinate[p._pts.Length];
            for (int i = 0; i < p._pts.Length; i++)
            {
                arr[i] = (Coordinate)p._pts[i];
            }
            return new LineString(arr);
        }

        public static explicit operator UPolyline(in LineString l)
        {
            UPoint[] pts = new UPoint[l.NumPoints];
            for (int i = 0; i < l.NumPoints; i++)
            {
                pts[i] = (UPoint)l[i];
            }
            return new UPolyline(pts);
        }


        public override bool Equals(object obj)
        {
            return Equals(obj as UPolyline);
        }

        #endregion
    }
}
