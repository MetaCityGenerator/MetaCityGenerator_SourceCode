using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.LinearReferencing;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.Distance;
using NetTopologySuite.Operation.Distance3D;
using NetTopologySuite.Precision;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MetaCity.Planning.UrbanDesign;



namespace MetaCity.Planning.Utility
{
    public static class Extensions
    {
        public static bool Islegal(this string str)
        {
            Regex regExp = new Regex("[~!@#$%^&*()=+[\\]{}''\";:/?.,><`|！·￥…—（）\\-、；：。，》《]");
            return !regExp.IsMatch(str.Trim());
        }

        public static Coordinate Translate (this Coordinate coordinate, Vector2D v)
        {
            return new Coordinate(coordinate.X+v.X, coordinate.Y+v.Y );
        }


        public static LineSegment Translate( this LineSegment l, Vector2D v)
        {
            return new LineSegment(l.P0.Translate(v), l.P1.Translate(v));
        }


        public static Polygon Translate(this Polygon pl, Vector2D v)
        {
            Coordinate[] pts = new Coordinate[pl.Coordinates.Length];
            for (int i = 0; i < pts.Length; i++)
            {
                pts[i] = pl.Coordinates[i].Translate(v);
            }

            LinearRing ring = new LinearRing(pts);
            return new Polygon(ring);
        }

        public static Polygon ForceCCW(this Polygon pl)
        {
            if (pl.IsEmpty)
                return pl;

            if (!pl.Shell.IsCCW)
            {
                var temp = (Polygon)pl.Reverse();
                pl = temp;
            }
            return pl;                
        }

        public static LinearRing ForceCCW(this LinearRing pl)
        {
            if (pl.IsEmpty)
                return pl;

            if (!pl.IsCCW)
            {
                var temp = (LinearRing)pl.Reverse();
                pl = temp;
            }
            return pl;
        }



        /// <summary>
        /// Exetension for linestring to perform splitting method by a single point. Only for 2D.
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="splitter"></param>
        /// <returns></returns>
        public static LineString[] SplitByPoint(this LineString curve, Point splitter)
        {
            // create reducer.
            GeometryPrecisionReducer reducer = new GeometryPrecisionReducer(curve.PrecisionModel)
            {
                ChangePrecisionModel = true
            };

            var splitId = LocationIndexOfPoint.IndexOf(curve, splitter.Coordinate); // can not use this in 3D.
            var startId = LocationIndexOfPoint.IndexOf(curve, curve.StartPoint.Coordinate);
            var endId = LocationIndexOfPoint.IndexOf(curve, curve.EndPoint.Coordinate);


            var l1 = ExtractLineByLocation.Extract(curve, startId, splitId);
            var l2 = ExtractLineByLocation.Extract(curve, splitId, endId);

            return new LineString[] {(LineString)reducer.Reduce(l1), (LineString)reducer.Reduce(l2)};
        }


        /// <summary>
        /// Exetension for linestring to perform splitting method by multi-points. Only for 2D.
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="splitter"></param>
        /// <returns></returns>
        public static LineString[] SplitByMultiPoint(this LineString curve, MultiPoint splitter, GeometryFactory gf)
        {

            var pm = gf.PrecisionModel.IsFloating ? PrecisionSetting._precision : gf.PrecisionModel;

            // create reducer.
            GeometryPrecisionReducer reducer = new GeometryPrecisionReducer(pm)
            {
                ChangePrecisionModel = true
            };


            // Sort all the indices.
            SortedSet<LinearLocation> indices = new SortedSet<LinearLocation>();
            for (int i = 0; i < splitter.Count; i++)
            {
                var splitId = LocationIndexOfPoint.IndexOf(curve, splitter[i].Coordinate);
                indices.Add(splitId);
            }

            var indicesArray = indices.ToArray();

            var startId = LocationIndexOfPoint.IndexOf(curve, curve.StartPoint.Coordinate); // can not use this in 3D.
            var endId = LocationIndexOfPoint.IndexOf(curve, curve.EndPoint.Coordinate);

            List<LineString> result = new List<LineString>(splitter.Count + 1);
            for (int i = 0; i < splitter.Count + 1; i++)
            {
                if (i == 0)
                {
                    // Add first sublinestring into result.
                    var l1 = ExtractLineByLocation.Extract(curve, startId, indicesArray[i]);
                    result.Add((LineString)reducer.Reduce(l1));

                    //result.Add((LineString)l1);
                }
                else if (i == splitter.Count)
                {
                    // Add last sublinestring into result.
                    var l2 = ExtractLineByLocation.Extract(curve, indicesArray[i - 1], endId);
                    result.Add((LineString)reducer.Reduce(l2));
                    //result.Add((LineString)l2);
                }
                else
                {
                    // Add interior sublinestrings.
                    var l = ExtractLineByLocation.Extract(curve, indicesArray[i - 1], indicesArray[i]);
                    result.Add((LineString)reducer.Reduce(l));
                    //result.Add((LineString)l);

                    //Important: 
                    //var test1 = curve.Factory.CreateLineString(l.Coordinates); //won't round coordinates.
                    //var test2 = (LineString)reducer.Reduce(l); //round coordinates.
                }
            }

            return result.ToArray();
        }


        /// <summary>
        /// Find k nearest geometries from <see cref="Geometry"/> array.
        /// Internaly using STRtree.NearestNeighbour method.
        /// </summary>
        /// <param name="geoms"></param>
        /// <param name="needle"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static Geometry[] KNN(this Geometry[] geoms, Geometry needle, int k)
        {
            // Build STRtree.
            STRtree<Geometry> rtree = new STRtree<Geometry>(geoms.Length);
            foreach (var geo in geoms)
            {
                rtree.Insert(geo.EnvelopeInternal, geo);
            }

            return rtree.NearestNeighbour(needle.EnvelopeInternal, needle, new GeometryItemDistance(), k);
        }


        /// <summary>
        /// Get the major oritation of current polygon.
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns>Angle in radiant, -π/4 ≤ θ ≤ π/4. </returns>
        public static double GetPolygonRadiant(this Polygon polygon)
        {
            //Get minimum rectangle.
            var rect = (Polygon)MinimumDiameter.GetMinimumRectangle(polygon);
            LineSegment line1 = new LineSegment(rect.Coordinates[0], rect.Coordinates[1]);
            LineSegment line2 = new LineSegment(rect.Coordinates[1], rect.Coordinates[2]);

            var radiant = line1.Length > line2.Length ? line1.Angle : line2.Angle;

            /*
            An angle, θ, measured in radians, such that -π ≤ θ ≤ π, and tan(θ) = y / x, where (x, y) is a point in the Cartesian plane. Observe the following:
            For (x, y) in quadrant 1, 0 < θ < π/2.
            For (x, y) in quadrant 2, π/2 < θ ≤ π.
            For (x, y) in quadrant 3, -π < θ < -π/2.
            For (x, y) in quadrant 4, -π/2 < θ < 0.
            */

            SiteMinimumRectangle.ValidatingSiteRadiant(ref radiant );

            return radiant;
        }



        /// <summary>
        /// Get the minimum rotated rectangle for current polygon based on an input radiant.
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="radiant"></param>
        /// <returns>Coordinates with four items in CCW order, starting from left-bottom point.</returns>
        public static Coordinate[] GetMinimumRoatatedRect(this Polygon polygon, double radiant)
        {
            // minmumRect in CCW order.
            Coordinate[] minmumRect = new Coordinate[4];
            
            if(radiant == 0)
            {
                var env = polygon.EnvelopeInternal;
                minmumRect[0] = new Coordinate(env.MinX, env.MinY);
                minmumRect[1] = new Coordinate(env.MaxX, env.MinY);
                minmumRect[2] = new Coordinate(env.MaxX, env.MaxY);
                minmumRect[3] = new Coordinate(env.MinX, env.MaxY);
            }
            else
            {
                var convex = (Polygon)polygon.ConvexHull();
                var centroid = convex.Centroid;

                var hK = Math.Tan(radiant);
                var vK = Math.Tan(radiant + Math.PI * 0.5);

                // y-y1 = k(x-x1)
                // y = kx + y1-kx1
                // Another point could be: (0, y1-kx1)
                // special case : centroid == anotherPt

                var hPtA_x = centroid.X == 0 ? 1 : 0;
                var hPtA_y = hK * hPtA_x + centroid.Y - hK * centroid.X;
                var vPtA_y = vK * hPtA_x + centroid.Y - vK * centroid.X;

                // Using centroid as pointB for both lines.
                Coordinate hPtA = new Coordinate(hPtA_x, hPtA_y);
                Coordinate hPtB = centroid.Coordinate;
                Coordinate vPtA = new Coordinate(hPtA_x, vPtA_y);
                Coordinate vPtB = hPtB.Copy();

                // Reorder ptA, ptB , ptA should be smallar.
                if (hPtA.X > hPtB.X)
                {
                    // hptA is at left.
                    var temp = hPtA.Copy();
                    hPtA = hPtB;
                    hPtB = temp;
                }

                if (vPtA.Y > vPtB.Y)
                {
                    // vptA is at bottom.
                    var temp = vPtA.Copy();
                    vPtA = vPtB;
                    vPtB = temp;
                }

                // Divide pts into right and left groups.
                HashSet<Coordinate> up = new HashSet<Coordinate>();
                HashSet<Coordinate> bottom = new HashSet<Coordinate>();
                HashSet<Coordinate> left = new HashSet<Coordinate>();
                HashSet<Coordinate> right = new HashSet<Coordinate>();

                for (int i = 0; i < convex.Coordinates.Length - 1; i++)
                {
                    var tempPt = convex.Coordinates[i];

                    var hOrient = Orientation.Index(hPtA, hPtB, tempPt);

                    if (hOrient == OrientationIndex.Left)
                    {
                        up.Add(tempPt);
                    }
                    else if (hOrient == OrientationIndex.Right)
                    {
                        bottom.Add(tempPt);
                    }

                    var vOrient = Orientation.Index(vPtA, vPtB, tempPt);

                    if (vOrient == OrientationIndex.Left)
                    {
                        left.Add(tempPt);
                    }
                    else if (vOrient == OrientationIndex.Right)
                    {
                        right.Add(tempPt);
                    }
                }

                // Find farest pt.
                var maxUp = FindFarestPt(up, hPtA, hPtB);
                var maxBottom = FindFarestPt(bottom, hPtA, hPtB);
                var maxLeft = FindFarestPt(left, vPtA, vPtB);
                var maxRight = FindFarestPt(right, vPtA, vPtB);

                // Find four corners of minimum rectangle.
                // yb = hkx + bottom.Y - hk*bottom.X .
                // yu = hkx + up.Y - hk*up.X .
                // yl = vkx + left.Y - vk*left.X .
                // yr = vkx + right.Y - vk*right. X .

                // Therefore. 
                // Intersection point bottom_left : yb = yl => x = ((left.Y - vk*left.X)-(bottom.Y - hk*bottom.X) )/ (hk-vk)
                var lb_x = (maxLeft.Y - vK * maxLeft.X - maxBottom.Y + hK * maxBottom.X) / (hK - vK);
                var lb_y = hK * lb_x + maxBottom.Y - hK * maxBottom.X;
                minmumRect[0] = new Coordinate(lb_x, lb_y);

                // Intersection point bottom_right : yb = yr => x = ((right.Y - vk*right.X)-(bottom.Y - hk*bottom.X) )/ (hk-vk)
                var rb_x = (maxRight.Y - vK * maxRight.X - maxBottom.Y + hK * maxBottom.X) / (hK - vK);
                var rb_y = hK * rb_x + maxBottom.Y - hK * maxBottom.X;
                minmumRect[1] = new Coordinate(rb_x, rb_y);

                // Intersection point up_right : yu = yr => x = ((right.Y - vk*right.X)-(up.Y - hk*up.X) )/ (hk-vk)
                var ru_x = (maxRight.Y - vK * maxRight.X - maxUp.Y + hK * maxUp.X) / (hK - vK);
                var ru_y = hK * ru_x + maxUp.Y - hK * maxUp.X;
                minmumRect[2] = new Coordinate(ru_x, ru_y);

                // Intersection point up_left : yu = yl => x = ((left.Y - vk*left.X)-(up.Y - hk*up.X) )/ (hk-vk)
                var lu_x = (maxLeft.Y - vK * maxLeft.X - maxUp.Y + hK * maxUp.X) / (hK - vK);
                var lu_y = hK * lu_x + maxUp.Y - hK * maxUp.X;
                minmumRect[3] = new Coordinate(lu_x, lu_y);
            }

            return minmumRect;
        }

        /// <summary>
        /// Static method for getting the nearest roads id for current site(polygon). 
        /// This method is different to the method in SiteMinimumRectangle class, therefore migrate this method into extension class.
        /// </summary>
        /// <param name="site"></param>
        /// <param name="minRotatedRect"></param>
        /// <param name="rtree"></param>
        /// <param name="roadsId"></param>
        /// <returns></returns>
        public static int[] GetNearestRoadsId( this Polygon site, Coordinate[] minRotatedRect, STRtree<Geometry> rtree, Dictionary<Geometry, int> roadsId)
        {
            int[] ids = new int[4];

            // Step 1: get the middle points for each edge of the minimum rectangle.
            var midPts =SiteMinimumRectangle.GetEdgesMidPoints(minRotatedRect);

            // Step 2: get the point on site which is closested to each middle point on minimum rectangle.
            var closestPtsOnSite = new Coordinate[midPts.Length];
            for (int i = 0; i < midPts.Length; i++)
            {
                var pt = midPts[i];
                closestPtsOnSite[i] = DistanceOp.NearestPoints(pt, site)[1];
            }

            // Step 3: find the nearest linestring for each cleseted points.
            for (int i = 0; i < closestPtsOnSite.Length; i++)
            {
                if (i == 4) // to make sure only return four items.
                    break;

                var pt = site.Factory.CreatePoint(closestPtsOnSite[i]);
                var key = rtree.NearestNeighbour(pt.EnvelopeInternal, pt, new GeometryItemDistance());

                ids[i] = roadsId[key];
            }

            return ids;
        }




        public static double Length3D(this LineString lineString)
        {
            double d = 0;
            var pts = lineString.Coordinates;

            for (int i = 1; i < pts.Length; i++)
            {
                d += CGAlgorithms3D.Distance(pts[i],pts[i-1]);
            }
            return d;
        }

        public static double Length3D(this Polygon poly)
        {
            double d = 0;
            var pts = poly.Coordinates;

            for (int i = 1; i < pts.Length; i++)
            {
                d += CGAlgorithms3D.Distance(pts[i], pts[i - 1]);
            }
            return d;
        }

        /// <summary>
        /// Calculate the angle between to 3d vectors.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="u"></param>
        /// <returns>An angle measured in radians.</returns>
        public static double AngleBetween(this Vector3D v, Vector3D u)
        {
            //v • u =|𝐯||u| cos𝜃
            //var d = v.Dot(u);
            //var l = v.Length() * u.Length();

            var vn= v.Normalize();
            var un = u.Normalize();

            // the result is the abosolute value of angle, dispite the direction between to vectors.
            // due to the floating, dot may larger or smaller than 1 or -1.
            var dot = Math.Round(vn.Dot(un), 9);
            dot = dot > 1 ? 1 : dot;
            dot = dot < -1 ? -1 : dot;

            return Math.Acos(dot);
        }

        public static double AngleBetween(this Vector2D v, Vector2D u)
        {
            //v • u =|𝐯||u| cos𝜃


            var vn = v.Normalize();
            var un = u.Normalize();

            // the result is the abosolute value of angle, dispite the direction between to vectors.
            // due to the floating, dot may larger or smaller than 1 or -1.
            var dot = Math.Round(vn.Dot(un) ,9);
            dot = dot > 1 ? 1 : dot;
            dot = dot < -1 ? -1 : dot;

            return Math.Acos(dot);
        }


        /// <summary>
        /// Private method to finding the farest pt based on a given line for <see cref="GetMinimumRoatatedRect"/>.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <param name="ptA"></param>
        /// <param name="ptB"></param>
        /// <returns>The farest point.</returns>
        private static Coordinate FindFarestPt(IEnumerable<Coordinate> coordinates, Coordinate ptA , Coordinate ptB)
        {
            Coordinate farestPt = new Coordinate();
            double maxDist = 0;

            foreach (var pt in coordinates)
            {
                var dist = DistanceComputer.PointToLinePerpendicular(pt, ptA, ptB);

                if (dist > maxDist)
                {
                    maxDist = dist;
                    farestPt = pt;
                }
            }

            return farestPt;
        }
    }
}
