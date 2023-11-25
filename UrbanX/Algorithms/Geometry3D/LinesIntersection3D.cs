using System;
using System.Collections.Generic;
using System.Numerics;
using UrbanX.DataStructures.Geometry3D;

namespace UrbanX.Algorithms.Geometry3D
{

    /// <summary>
    /// Test the intersection between two 3D line segments by calculating the minimum distance between two segment.
    /// </summary>
    public sealed class LinesIntersection3D
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


        private Stack<UPoint> _pInterections;

        private Stack<UPoint> _qInterections;

        private bool _isParallel;

        private bool _hasIntersection ;

        private bool _isCollinear;

        private bool _isProper;


        /// <summary>
        /// The intersected points on the first input line segment.
        /// </summary>
        public UPoint[] PIntersections => _pInterections.ToArray();

        /// <summary>
        /// The intersected points on the second input line segment.
        /// </summary>
        public UPoint[] QIntersections => _qInterections.ToArray();


        /// <summary>
        /// Tests whether the input geometries intersect.
        /// </summary>
        public bool HasIntersection => _hasIntersection;



        /// <summary>
        /// True if the computed intersection is collinear.
        /// </summary>
        public bool IsCollinear => _isCollinear;


        public bool IsParallel => _isParallel;


        /// <summary>
        ///  The intersection between two line segments is considered proper if they intersect in a single point in the interior of both segments 
        /// </summary>
        public bool IsProper => _isProper;


        public LinesIntersection3D(double tolerance)
        {
            _tolerance = tolerance;
            SMALL_NUM = _tolerance * _tolerance;
        }

        public bool Compute(UPoint p1, UPoint p2, UPoint q1, UPoint q2)
        {
            // for small case to improve the performance, we instantiate this class only once, then use this compute method multiple times, 
            // therefore we need to reset all the fields to default.
            _isParallel = false;
            _hasIntersection = false;
            _isCollinear = false;
            _isProper = false;

            UVector3 u = (UVector3)p2 - (UVector3)p1;
            UVector3 v = (UVector3)q2 - (UVector3)q1;
            UVector3 w = (UVector3)p1 - (UVector3)q1;

            //double a = u.Dot(u); // always >= 0
            decimal a = (decimal)UVector3.Dot(u, u);
            decimal b = (decimal)UVector3.Dot(u, v);
            decimal c = (decimal)UVector3.Dot(v, v);
            decimal d = (decimal)UVector3.Dot(u, w);
            decimal e = (decimal)UVector3.Dot(v, w);

            var D =a * c - b * b; // always >= 0
            
            decimal sc, sN, sD = D; // sc = sN/sD, default sD = D >= 0;
            decimal tc, tN, tD = D; // tc = tN/tD, default tD = D >= 0;

            // Impotant: using angle to determine if two vectors are parallel or not.
            var radi = u.AngleBetween(v);
            var gap = Math.Sin(radi) * Math.Min(u.Length(), v.Length()); // Calculating the ditance between the smaller vector's end point to the longer vector.
 
            // compute the line parameters of the two closest points.
            if (gap < _tolerance) // the lines are almost parallel
            {
                _isParallel = true;
                // force using point q1 on segment s1 to prevent possible division by 0 later.
                sN = 0m;
                sD = 1m;
                tN = e;
                tD = c;
            }
            else
            {
                // get the closest points on the infinite lines.
                sN = b * e - c * d; // may return floating number.
                tN = a * e - b * d; // may return floating number.


                if (sN < 0m)
                {
                    sN = 0m;
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


            if (tN < 0m)
            {
                tN = 0m;

                if (-d < 0m)
                {
                    sN = 0m;
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
                if(-d+b < 0m)
                {
                    sN = 0m;
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
            sc = Math.Abs(sN) < (decimal)SMALL_NUM ? 0m : sN / sD;  
            tc = Math.Abs(tN) < (decimal)SMALL_NUM? 0m : tN / tD;

            double scd = Math.Min(Math.Max(0, (double)sc), 1); // 0~1.
            double tcd = Math.Min(Math.Max(0, (double)tc), 1);// 0~1.

            var p = p1.Translate(u * scd);
            if (scd > 0.9)
            {
                // p2. Check the approximation to p2.
                var margin = p2.DistanceTo(p);
                if (margin < _tolerance)
                {
                    p = p2;
                    scd = 1;
                }
                    
            }
            else if (scd < 0.1)
            {
                // p1. Check the approximation to p1.
                var margin = p1.DistanceTo(p);
                if (margin < _tolerance)
                {
                    p = p1;
                    scd = 0;
                }
                    
            }

            var q = q1.Translate(v * tcd);
            if(tcd > 0.9)
            {
                var margin = q2.DistanceTo(q);
                if (margin < _tolerance)
                {
                    q = q2;
                    tcd = 1;
                }
            }
            else if(tcd < 0.1)
            {
                var margin = q1.DistanceTo(q);
                if (margin < _tolerance)
                {
                    q = q1;
                    tcd = 0;
                }
            }


            var l = p.DistanceTo(q);

            if(l< _tolerance) // _tolerance for handle the distance error. 
            {
                // intersection.
                _hasIntersection = true;

                _pInterections = new Stack<UPoint>(2); // for non-intersection, we don't need to initiate this collection.
                _qInterections = new Stack<UPoint>(2);

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
                    var pComparer = new Point3DComparer(_tolerance);
                    SortedSet<UPoint> order = new SortedSet<UPoint>(pComparer)
                    {
                        p1,p2,q1,q2
                    };


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
                    if(scd >0 && scd <1 && tcd >0 && tcd < 1)
                    {
                        // proper intersection.
                        _isProper = true;
                        var x = 0.5 *(p+q);
                        _pInterections.Push(x);
                        _qInterections.Push(x);
                    }
                    else if ((scd == 0|| scd ==1)&&(tcd ==0||tcd ==1))
                    {
                        // intersecting with end points of two segments.
                        //if (l != 0)
                            //throw new Exception("Please using LineString3DSnapper to snap all the endpoints first."); // p and q should be equal.
                    }
                    else if((scd == 0 || scd == 1) && (tcd > 0 && tcd < 1))
                    {
                        // intersecting on the first segment's end point. using p as output. Add the intersection to second line only.
                        _qInterections.Push(p);
                    }
                    else if ((scd > 0 && scd < 1) && (tcd == 0 || tcd == 1))
                    {
                        // intersecting on the second segment's end point. using q as output.Add the intersection to first line only.
                        _pInterections.Push(q);
                    }
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
