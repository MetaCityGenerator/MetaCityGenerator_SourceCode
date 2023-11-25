using System;
using System.Collections.Generic;

using System.Threading.Tasks;

using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;



namespace UrbanX.Algorithms.Geometry3D
{

    /// <summary>
    /// Test the intersection between two 3D line segments by calculating the minimum distance between two segment.
    /// </summary>
    public sealed class SegmentsIntersection3D
    {
        /// <summary>
        /// Global tolerance inheited from upper code.
        /// </summary>
        private readonly double _tolerance;


        /// <summary>
        /// The small number for checking the vectors.
        /// Because we will use vector dot method , small number = tolerance^2.
        /// </summary>
        private readonly double SMALL_NUM;

        /// <summary>
        /// For D, sN and tN shoube be rounded based on the small number to handle the floating issue.
        /// </summary>
        private readonly int _round;

        private readonly GeometryFactory _gf;

        private readonly CoordinateZ[] _pts = new CoordinateZ[4];

        private  PointComparer3D _pComparer;

        private Stack<Point> _pInterections;

        private Stack<Point> _qInterections;

        private bool _isParallel = false;

        private bool _hasIntersection = false ;

        private bool _isCollinear = false;

        private bool _isProper = false;


        /// <summary>
        /// The intersected points on the first input line segment.
        /// </summary>
        public Point[] PIntersections => _pInterections.ToArray();

        /// <summary>
        /// The intersected points on the second input line segment.
        /// </summary>
        public Point[] QIntersections => _qInterections.ToArray();


        /// <summary>
        /// Tests whether the input geometries intersect.
        /// </summary>
        public bool HasIntersection => _hasIntersection;



        /// <summary>
        /// True if the computed intersection is collinear.
        /// </summary>
        public bool IsCollinear => _isCollinear;



        /// <summary>
        ///  The intersection between two line segments is considered proper if they intersect in a single point in the interior of both segments 
        /// </summary>
        public bool IsProper => _isProper;


        public SegmentsIntersection3D(double tolerance , GeometryFactory gf)
        {
            _tolerance = tolerance;
            SMALL_NUM = _tolerance * _tolerance;
            _round = (int)Math.Abs(Math.Log10(SMALL_NUM));
            _gf = gf;
        }

        public bool Compute(Coordinate p1, Coordinate p2, Coordinate q1, Coordinate q2)
        {
            // for small case to improve the performance, we instantiate this class only once, then use this compute method multiple times, 
            // therefore we need to reset all the fields to default.
            _isParallel = false;
            _hasIntersection = false;
            _isCollinear = false;
            _isProper = false;

            _pts[0] = p1 is CoordinateZ p1z ? p1z : new CoordinateZ(p1);
            _pts[1] = p2 is CoordinateZ p2z ? p2z : new CoordinateZ(p2);
            _pts[2] = q1 is CoordinateZ q1z ? q1z : new CoordinateZ(q1);
            _pts[3] = q2 is CoordinateZ q2z ? q2z : new CoordinateZ(q2);

            Vector3D u = new Vector3D(_pts[0], _pts[1]);
            Vector3D v = new Vector3D(_pts[2], _pts[3]);
            Vector3D w = new Vector3D(_pts[2], _pts[0]);

            double a = u.Dot(u); // always >= 0
            double b = u.Dot(v);
            double c = v.Dot(v); // always >= 0
            double d = u.Dot(w);
            double e = v.Dot(w);
            double D = a * c - b * b; // always >= 0

            D = Math.Round(D, _round); // impoartant to handle floating number.
            

            double sc, sN, sD = D; // sc = sN/sD, default sD = D >= 0;
            double tc, tN, tD = D; // tc = tN/tD, default tD = D >= 0;

            // Impotant: using angle to determine if two vectors are parallel or not.
            var radiant = Math.Acos(b / (u.Length() * v.Length())); // Calculating the angle between two vector3d.
            var gap = Math.Sin(radiant) * Math.Min(u.Length(), v.Length()); // Calculating the ditance between the smaller vector's end point to the longer vector.
 
            // compute the line parameters of the two closest points.
            //if (D < SMALL_NUM) // Condition from book, but is not working well with tolerance.
            if (gap < _tolerance) // the lines are almost parallel
            {
                _isParallel = true;
                // force using point q1 on segment s1 to prevent possible division by 0 later.
                sN = 0d;
                sD = 1d;
                tN = e;
                tD = c;
            }
            else
            {
                // get the closest points on the infinite lines.
                sN = b * e - c * d; // may return floating number.
                tN = a * e - b * d; // may return floating number.

                sN = Math.Round(sN, _round);// impoartant to handle floating number.
                tN = Math.Round(tN, _round);// impoartant to handle floating number.

                if (sN < 0.0)
                {
                    sN = 0.0;
                    tN = e;
                    tD = c;
                }
                else if (sN > sD)
                {
                    sN = sD;
                    tN = e + b;
                    tD = c;
                }
            }


            if (tN < 0.0)
            {
                tN = 0.0;

                if (-d < 0.0)
                {
                    sN = 0.0;
                }
                else if (-d > a)
                {
                    sN = sD;
                }
                else
                {
                    sN = -d;
                    sD = a;
                }
            }
            else if (tN > tD)
            {
                tN = tD;
                if(-d+b < 0.0)
                {
                    sN = 0.0;
                }
                else if ( -d + b > a)
                {
                    sN = sD;
                }
                else
                {
                    sN = -d + b;
                    sD = a;
                }
            }


            // finally do the division to get sc and tc. When sc or tc equals 1 or 0, means the closest points are the end points.
            sc = Math.Abs(sN) < SMALL_NUM ? 0.0 : sN / sD;  
            tc = Math.Abs(tN) < SMALL_NUM? 0.0 : tN / tD;


            var p = _gf.CreatePoint(_pts[0].Translate(u * sc));
            var q = _gf.CreatePoint(_pts[2].Translate(v * tc)) ;

            var dp = w + (u * sc) - (v * tc);
            var l =  dp.Length();
    

            if(l< _tolerance) // _tolerance for handle the distance error. 
            {
                // intersection.
                _hasIntersection = true;

                _pInterections = new Stack<Point>(2); // for non-intersection, we don't need to initiate this collection.
                _qInterections = new Stack<Point>(2);

                if (_isParallel)
                {
                    // CollinearIntersection.
                    // proof for handle tolerance error.
                    // For collinear intersection, there are  cases.
                    // Line A : a=>a' ; Line B : b => b'
                    // case 1: a b b' a' => output: b b', Line A will be splitted into ab bb' b'a' , Line B will stay the same.
                    // case 2: a b a' b' => output: b a', Line A will be splitted into ab ba', Line B will be splitted into ba' a'b'.
                    // case 3: a a' b b' => output: a'|| b , both lines stays the same.

                    _isCollinear = true;
                    _pComparer = new PointComparer3D(_tolerance);
                    SortedSet<Point> order = new SortedSet<Point>(_pComparer);
                    for (int i = 0; i < _pts.Length; i++)
                    {
                        order.Add(_gf.CreatePoint(_pts[i]));
                    }

                    //case0: has three item: 0 1 2, select 1. middle one.
                    //case1: has four item: 0 1 2 3 , select 1 2. middle two.
                    //case2: has two item: 0 1, select non.
                    order.Remove(order.Min);
                    order.Remove(order.Max);
                    foreach (var x in order)
                    {
                        _pInterections.Push(x);
                        _qInterections.Push(x);
                    }
                }
                else
                {
                    // PointIntersection
                    Point x = null;

                    if(sc >0 && sc<1 && tc>0 && tc < 1)
                    {
                        // proper intersection.
                        _isProper = true;
                        x = _gf.CreatePoint(new CoordinateZ(0.5*(p.X + q.X), 0.5*(p.Y + q.Y), 0.5*(p.Z + q.Z)));
                    }
                    else if ((sc == 0|| sc ==1)&&(tc ==0||tc ==1))
                    {
                        // intersecting with end points of two segments.
                        //if (l != 0)
                            //throw new Exception("Please using LineString3DSnapper to snap all the endpoints first."); // p and q should be equal.

                        x = p;
                    }
                    else if((sc == 0 || sc == 1) && (tc > 0 && tc < 1))
                    {
                        // intersecting on the first segment's end point. using p as output.
                        x = p;
                    }
                    else if ((sc > 0 && sc < 1) && (tc == 0 || tc == 1))
                    {
                        // intersecting on the second segment's end point. using q as output.
                        x = q;
                    }

                    _pInterections.Push(x);
                    _qInterections.Push(x);
                }
                return true;
            }
            else
            {
                // NoIntersection.
                return false;
            }
        }
    }
}
