using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.Buffer;

using System;
using System.Collections.Generic;
using System.Linq;

using UrbanX.Algorithms.Mathematics;
using UrbanX.DataStructures.Trees;
using UrbanX.Planning.Utility;

namespace UrbanX.Planning.UrbanDesign
{
    public static class Toolbox
    {
        #region Accuratly splitting.
        /// <summary>
        /// The core method for splitting a polygon into several sub-polygons based on a given area ratios accuratly.
        /// The internal data structure for this method is binary-space-partion tree.
        /// </summary>
        /// <param name="site">Input site polygon.</param>
        /// <param name="ratios">The array of ratios representing each sub-sites' target area.</param>
        /// <param name="priorities">The array of priorities which will determine the location for each sub-site.</param>
        /// <param name="scores">The accessibility scores for orignial site polygon's minimum rotated rectangle.</param>
        /// <param name="radiant">The main orientation for the building will be facing of this site. </param>
        /// <param name="renewRadiant">Whether recalculating the radiant for current sub-site. If true, each step of splitting may be using various radiant. </param>
        /// <returns>The sub-sites result after splitting process, which shares the same order with input ratios and priorities. </returns>
        public static Polygon[] SplitSiteByRatiosAccuratly(Polygon site, double[] ratios, double[] priorities, double[] scores, double radiant, bool renewRadiant)
        {
            // Check site curve orientation, should be cw.
            if (!site.Shell.IsCCW)
                site = (Polygon)site.Reverse();


            // Split brep.
            var nodes = new BSPTreeNode[ratios.Length];
            for (int c = 0; c < ratios.Length; c++)
            {
                // Create new node by using value and priority from building type.
                nodes[c] = new BSPTreeNode(c, ratios[c], priorities[c]);
            }

            BSPTree bspTree = new BSPTree(nodes);
            List<Polygon> curvesResult = new List<Polygon>(ratios.Length);
            List<double> radiantsResult = new List<double>(ratios.Length);
            List<Coordinate[]> coordinatesResult = new List<Coordinate[]>(ratios.Length);
            List<int> nodeKeys = new List<int>(ratios.Length);
            List<double[]> scoresResult = new List<double[]>(ratios.Length);


            var siteCorners = site.GetMinimumRoatatedRect(radiant);

            SplitRecursiveAccuratly(site, bspTree.Root, siteCorners, scores, radiant, renewRadiant, ref curvesResult, ref radiantsResult, ref coordinatesResult, ref scoresResult, ref nodeKeys);

            // Correct the order of brepsResult to make sure the order of typesRepresent and subSites are the same.
            Polygon[] result = new Polygon[ratios.Length];

            for (int k = 0; k < nodeKeys.Count; k++)
            {
                var key = nodeKeys[k];
                var tempBrep = curvesResult[k];
                result[key] = tempBrep;
            }

            // Clear tree.
            bspTree.Clear();
            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="site"></param>
        /// <param name="ratios"></param>
        /// <param name="priorities"></param>
        /// <param name="scores"></param>
        /// <param name="radiant"></param>
        /// <param name="renewRadiant"></param>
        /// <param name="subSitesCorners"></param>
        /// <param name="subSitesSplitters"></param>
        /// <returns></returns>
        public static Polygon[] SplitSiteByRatiosAccuratly(Polygon site, double[] ratios, double[] priorities, double[] scores, double radiant, bool renewRadiant, out Coordinate[][] subSitesCorners, out LineString[] subSitesSplitters)
        {
            // Check site curve orientation, should be cw.
            if (!site.Shell.IsCCW)
                site = (Polygon)site.Reverse();


            // Split brep.
            var nodes = new BSPTreeNode[ratios.Length];
            for (int c = 0; c < ratios.Length; c++)
            {
                // Create new node by using value and priority from building type.
                nodes[c] = new BSPTreeNode(c, ratios[c], priorities[c]);
            }

            BSPTree bspTree = new BSPTree(nodes);
            List<Polygon> curvesResult = new List<Polygon>(ratios.Length);
            List<double> radiantsResult = new List<double>(ratios.Length);
            List<Coordinate[]> coordinatesResult = new List<Coordinate[]>(ratios.Length);
            List<int> nodeKeys = new List<int>(ratios.Length);
            List<double[]> scoresResult = new List<double[]>(ratios.Length);
            List<LineString> splittersResult = new List<LineString>(ratios.Length);

            var siteCorners = site.GetMinimumRoatatedRect(radiant);

            SplitRecursiveAccuratly(site, bspTree.Root, siteCorners, scores, radiant, renewRadiant, ref curvesResult, ref radiantsResult, ref coordinatesResult, ref scoresResult, ref nodeKeys, ref splittersResult);

            // Correct the order of brepsResult to make sure the order of typesRepresent and subSites are the same.
            Polygon[] result = new Polygon[ratios.Length];
            subSitesCorners = new Coordinate[ratios.Length][];

            for (int k = 0; k < nodeKeys.Count; k++)
            {
                var key = nodeKeys[k];
                var tempBrep = curvesResult[k];
                var tempCorners = coordinatesResult[k];
                result[key] = tempBrep.ForceCCW();
                subSitesCorners[key] = tempCorners;
            }

            subSitesSplitters = splittersResult.ToArray();

            // Clear tree.
            bspTree.Clear();
            return result;
        }




        /// <summary>
        /// The recursive method for splitting a polygon in an accurate manner.
        /// </summary>
        /// <param name="polygon">Input site polygon. </param>
        /// <param name="node">The current BSPTreeNode.</param>
        /// <param name="currentCorners">The four corners representing the current polygon's minimum rotated rectangle. </param>
        /// <param name="currentScores">The four accessibility scores for current polygon. </param>
        /// <param name="radiant">The main orientation for the building will be facing of this site.</param>
        /// <param name="renewRadiant">Whether recalculating the radiant for current sub-site. If true, each step of splitting may be using various radiant. </param>
        /// <param name="allPolygons">A collection storing the splitting result for all the sub-sites. </param>
        /// <param name="allRadiants">A collection storing all the radiants for each splitting process (not the sub-sites). </param>
        /// <param name="allCorners">A collection storing all the corners for each splitting process (not the sub-sites).</param>
        /// <param name="allScores">A collection storing all the scores for each splitting process (not the sub-sites).</param>
        /// <param name="nodeKeys">A collection storing all the node key for each the sub-sites, which will be using to reorder the result of splitting.</param>
        public static void SplitRecursiveAccuratly(Polygon polygon, BSPTreeNode node, Coordinate[] currentCorners, double[] currentScores, double radiant, bool renewRadiant,
            ref List<Polygon> allPolygons, ref List<double> allRadiants, ref List<Coordinate[]> allCorners, ref List<double[]> allScores, ref List<int> nodeKeys)
        {
            if (node.IsLeafNode())
            {
                var leafRadiant = renewRadiant ? polygon.GetPolygonRadiant() : radiant;

                allPolygons.Add(polygon);
                allRadiants.Add(leafRadiant);
                allScores.Add(currentScores);
                allCorners.Add(currentCorners);
                nodeKeys.Add(node.Key);
            }
            else
            {
                // Has at least one child.
                if (node.HasOnlyLeftChild())
                {
                    // Only has left child, just go down this node to find out if it has other children.
                    double leftRadiant = renewRadiant ? polygon.GetPolygonRadiant() : radiant;

                    SplitRecursiveAccuratly(polygon, node.LeftChild, currentCorners, currentScores, leftRadiant, renewRadiant, ref allPolygons, ref allRadiants, ref allCorners, ref allScores, ref nodeKeys);
                }
                else
                {
                    // has both child, left child has higher priority than right child.
                    var ratio = node.LeftChild.Value / (node.LeftChild.Value + node.RightChild.Value);

                    // Only get sub-polygons and children scores representively.
                    var subPolygons = RootFindingSplit(polygon, ratio, currentCorners, currentScores, out double[][] childrenScores);

                    // Get radiants.
                    double leftRadiant = renewRadiant ? subPolygons[0].GetPolygonRadiant() : radiant;
                    double rightRadiant = renewRadiant ? subPolygons[1].GetPolygonRadiant() : radiant;

                    // Get corners.
                    var leftCorners = subPolygons[0].GetMinimumRoatatedRect(leftRadiant);
                    var rightCorners = subPolygons[1].GetMinimumRoatatedRect(rightRadiant);

                    SplitRecursiveAccuratly(subPolygons[0], node.LeftChild, leftCorners, childrenScores[0], leftRadiant, renewRadiant, ref allPolygons, ref allRadiants, ref allCorners, ref allScores, ref nodeKeys);
                    SplitRecursiveAccuratly(subPolygons[1], node.RightChild, rightCorners, childrenScores[1], rightRadiant, renewRadiant, ref allPolygons, ref allRadiants, ref allCorners, ref allScores, ref nodeKeys);
                }
            }
        }



        public static void SplitRecursiveAccuratly(Polygon polygon, BSPTreeNode node, Coordinate[] currentCorners, double[] currentScores, double radiant, bool renewRadiant,
            ref List<Polygon> allPolygons, ref List<double> allRadiants, ref List<Coordinate[]> allCorners, ref List<double[]> allScores, ref List<int> nodeKeys, ref List<LineString> allSplitters)
        {
            if (node.IsLeafNode())
            {
                var leafRadiant = renewRadiant ? polygon.GetPolygonRadiant() : radiant;

                allPolygons.Add(polygon);
                allRadiants.Add(leafRadiant);
                allScores.Add(currentScores);
                allCorners.Add(currentCorners);
                nodeKeys.Add(node.Key);
            }
            else
            {
                // Has at least one child.
                if (node.HasOnlyLeftChild())
                {
                    // Only has left child, just go down this node to find out if it has other children.
                    double leftRadiant = renewRadiant ? polygon.GetPolygonRadiant() : radiant;

                    SplitRecursiveAccuratly(polygon, node.LeftChild, currentCorners, currentScores, leftRadiant, renewRadiant, ref allPolygons, ref allRadiants, ref allCorners, ref allScores, ref nodeKeys, ref allSplitters);
                }
                else
                {
                    // has both child, left child has higher priority than right child.
                    var ratio = node.LeftChild.Value / (node.LeftChild.Value + node.RightChild.Value);

                    // Only get sub-polygons and children scores representively.
                    var subPolygons = RootFindingSplit(polygon, ratio, currentCorners, currentScores, out double[][] childrenScores, out LineString[] splitters);
                    allSplitters.AddRange(splitters);


                    // Get radiants.
                    double leftRadiant = renewRadiant ? subPolygons[0].GetPolygonRadiant() : radiant;
                    double rightRadiant = renewRadiant ? subPolygons[1].GetPolygonRadiant() : radiant;

                    // Get corners.
                    var leftCorners = subPolygons[0].GetMinimumRoatatedRect(leftRadiant);
                    var rightCorners = subPolygons[1].GetMinimumRoatatedRect(rightRadiant);

                    SplitRecursiveAccuratly(subPolygons[0], node.LeftChild, leftCorners, childrenScores[0], leftRadiant, renewRadiant, ref allPolygons, ref allRadiants, ref allCorners, ref allScores, ref nodeKeys, ref allSplitters);
                    SplitRecursiveAccuratly(subPolygons[1], node.RightChild, rightCorners, childrenScores[1], rightRadiant, renewRadiant, ref allPolygons, ref allRadiants, ref allCorners, ref allScores, ref nodeKeys, ref allSplitters);
                }
            }
        }



        /// <summary>
        /// Base method for splitting polgyon accuratly based on the ratio of first sub-polygon's area to total polgon's area by using Brent algorithm.
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="ratio"></param>
        /// <param name="corners"></param>
        /// <param name="scores"></param>
        /// <param name="childrenScores"></param>
        /// <returns></returns>
        public static Polygon[] RootFindingSplit(Polygon polygon, double ratio, Coordinate[] corners, double[] scores, out double[][] childrenScores)
        {
            // Using corners as the minimum rectangle for splitting.
            // total area is the site area.
            double targetArea = polygon.Area * ratio;

            SiteMinimumRectangle siteMinimum = new SiteMinimumRectangle(corners, scores);

            double f(double x)
            {
                var subPolygons = siteMinimum.SplitPolygon(polygon, x);

                // The first brep corresponds to the ratio (left child).

                return subPolygons[0].Area;
            }

            double ratioFound = RootFinding.Brent(new FunctionOfOneVariable(f), 0.01, 0.99, targetArea, out _, out _);

            var result = siteMinimum.SplitPolygon(polygon, ratioFound);
            childrenScores = siteMinimum.GetChildrenScores();

            return result;
        }

        public static Polygon[] RootFindingSplit(Polygon polygon, double ratio, Coordinate[] corners, double[] scores, out double[][] childrenScores, out LineString[] splitters)
        {
            // Using corners as the minimum rectangle for splitting.
            // total area is the site area.
            double targetArea = polygon.Area * ratio;

            SiteMinimumRectangle siteMinimum = new SiteMinimumRectangle(corners, scores);

            double f(double x)
            {
                var subPolygons = siteMinimum.SplitPolygon(polygon, x);

                // The first brep corresponds to the ratio (left child).

                return subPolygons[0].Area;
            }

            double ratioFound = RootFinding.Brent(new FunctionOfOneVariable(f), 0.01, 0.99, targetArea, out _, out _);

            var result = siteMinimum.SplitPolygon(polygon, ratioFound, out splitters);
            childrenScores = siteMinimum.GetChildrenScores();

            return result;
        }



        #endregion Accuratly splitting.



        #region Non-accuratly splitting.

        /// <summary>
        /// Using difference operation with internal roads to split the original site into several sub-sites.
        /// </summary>
        /// <param name="site"></param>
        /// <param name="splitters"></param>
        /// <param name="roadsBiDist"></param>
        /// <param name="offsetSiteDist"></param>
        /// <returns></returns>
        public static Polygon[] SplitSiteByInternalRoads(Polygon site, LineString[] splitters, double roadsBiDist = 6, double offsetSiteDist = 0)
        {
            // Create buffer.
            GeometryCollection gc = new GeometryCollection(splitters);


            BufferParameters bpR = new BufferParameters(18, EndCapStyle.Round, JoinStyle.Round, 0.6);
            var roadBuffer = gc.Buffer(roadsBiDist);
            // Prepare greenbuffer.
            PreparedPolygon prepared = new PreparedPolygon((IPolygonal)roadBuffer);

            var offset = site.Buffer(offsetSiteDist);
            var subSites = offset.Difference(prepared.Geometry);


            var extracter = PolygonExtracter.GetPolygons(subSites);

            Polygon[] result = new Polygon[extracter.Count];
            for (int i = 0; i < extracter.Count; i++)
            {
                var temp = (Polygon)extracter[i];


                var shrink = temp.Buffer(-2 * roadsBiDist);


                if (shrink.NumGeometries > 1 || shrink.IsEmpty)
                {
                    result[i] = temp.ForceCCW();
                }
                else
                {
                    var block = (Polygon)shrink.Buffer(2 * roadsBiDist, bpR);
                    result[i] = block.ForceCCW();
                }
            }

            return result;
        }



        /// <summary>
        /// The main method for getting the internal roads of the original site polygon based on the splitting process.
        /// This method using the non-accurate manner for splitting to improve the performance of computation.
        /// </summary>
        /// <param name="site">Input site polygon.</param>
        /// <param name="ratios">The array of ratios representing each sub-sites' target area. </param>
        /// <param name="priorities">The array of priorities which will determine the location for each sub-site. <param>
        /// <param name="scores">The accessibility scores for orignial site polygon's minimum rotated rectangle.</param>
        /// <param name="radiant">The main orientation for the building will be facing of this site.</param>
        /// <param name="renewRadiant">Whether recalculating the radiant for current sub-site. If true, each step of splitting may be using various radiant. <param>
        /// <returns></returns>
        public static LineString[] GetInternalRoadsBySplitting(Polygon site, double[] ratios, double[] priorities, double[] scores, double radiant, bool renewRadiant)
        {
            // Check site curve orientation, should be cw.
            if (!site.Shell.IsCCW)
                site = (Polygon)site.Reverse();


            // Split brep.
            var nodes = new BSPTreeNode[ratios.Length];
            for (int c = 0; c < ratios.Length; c++)
            {
                // Create new node by using value and priority from building type.
                nodes[c] = new BSPTreeNode(c, ratios[c], priorities[c]);
            }

            BSPTree bspTree = new BSPTree(nodes);
            List<LineString> splitters = new List<LineString>(ratios.Length);

            var siteCorners = site.GetMinimumRoatatedRect(radiant);

            SplitRecursiveNonAccuratly(site, bspTree.Root, siteCorners, scores, radiant, renewRadiant, ref splitters);


            // Clear tree.
            bspTree.Clear();
            return splitters.ToArray();
        }



        /// <summary>
        /// The curical recursive method for splitting a polygon in a non-accurate manner.
        /// </summary>
        /// <param name="polygon">Input site polygon. </param>
        /// <param name="node">The current BSPTreeNode.</param>
        /// <param name="currentCorners">The four corners representing the current polygon's minimum rotated rectangle. </param>
        /// <param name="currentScores">The four accessibility scores for current polygon. </param>
        /// <param name="radiant">The main orientation for the building will be facing of this site.</param>
        /// <param name="renewRadiant">Whether recalculating the radiant for current sub-site. If true, each step of splitting may be using various radiant. </param>
        /// <param name="allSplitters">A collection for all the splitters during each splitting process, which representing all the internal roads for the original site. </param>
        public static void SplitRecursiveNonAccuratly(Polygon polygon, BSPTreeNode node, Coordinate[] currentCorners, double[] currentScores, double radiant, bool renewRadiant,
           ref List<LineString> allSplitters)
        {
            if (node.HasChildren())
            {
                // Has at least one child.
                if (node.HasOnlyLeftChild())
                {
                    // Only has left child.
                    double leftRadiant = renewRadiant ? polygon.GetPolygonRadiant() : radiant;

                    SplitRecursiveNonAccuratly(polygon, node.LeftChild, currentCorners, currentScores, leftRadiant, renewRadiant, ref allSplitters);
                }
                else
                {
                    // has both child, left child has higher priority than right child.
                    var ratio = node.LeftChild.Value / (node.LeftChild.Value + node.RightChild.Value);

                    // Only get sub-polygons and children scores representively.
                    var splitters = FindRectangleSplitters(polygon, ratio, currentCorners, currentScores, out Polygon[] subPolygons, out double[][] childrenScores);
                    allSplitters.AddRange(splitters);

                    // Get radiants.
                    double leftRadiant = renewRadiant ? subPolygons[0].GetPolygonRadiant() : radiant;
                    double rightRadiant = renewRadiant ? subPolygons[1].GetPolygonRadiant() : radiant;

                    // Get corners.
                    var leftCorners = subPolygons[0].GetMinimumRoatatedRect(leftRadiant);
                    var rightCorners = subPolygons[1].GetMinimumRoatatedRect(rightRadiant);

                    SplitRecursiveNonAccuratly(subPolygons[0], node.LeftChild, leftCorners, childrenScores[0], leftRadiant, renewRadiant, ref allSplitters);
                    SplitRecursiveNonAccuratly(subPolygons[1], node.RightChild, rightCorners, childrenScores[1], rightRadiant, renewRadiant, ref allSplitters);
                }
            }
        }



        /// <summary>
        /// Base method for splitting polgyon non-accuratly based on the ratio of minimum rectangle area.
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="ratio"></param>
        /// <param name="corners"></param>
        /// <param name="scores"></param>
        /// <param name="subPolygons"></param>
        /// <param name="childrenScores"></param>
        /// <returns></returns>
        public static LineString[] FindRectangleSplitters(Polygon polygon, double ratio, Coordinate[] corners, double[] scores, out Polygon[] subPolygons, out double[][] childrenScores)
        {
            SiteMinimumRectangle siteMinimum = new SiteMinimumRectangle(corners, scores);

            //var result = siteMinimum.SplitPolygonNonAccuratly(polygon, ratio,  out subPolygons);
            subPolygons = siteMinimum.SplitPolygon(polygon, ratio, out LineString[] splitters);

            childrenScores = siteMinimum.GetChildrenScores();

            return splitters;
        }




        //newly added.


        public static Polygon[] SplitSiteByRatiosNonAccuratly2(Polygon site, double[] ratios, double[] priorities, double[] scores, double radiant, bool renewRadiant)
        {
            // Check site curve orientation, should be cw.
            if (!site.Shell.IsCCW)
                site = (Polygon)site.Reverse();


            // Split brep.
            var nodes = new BSPTreeNode[ratios.Length];
            for (int c = 0; c < ratios.Length; c++)
            {
                // Create new node by using value and priority from building type.
                nodes[c] = new BSPTreeNode(c, ratios[c], priorities[c]);
            }

            BSPTree bspTree = new BSPTree(nodes);
            List<Polygon> curvesResult = new List<Polygon>(ratios.Length);
            List<double> radiantsResult = new List<double>(ratios.Length);
            List<Coordinate[]> coordinatesResult = new List<Coordinate[]>(ratios.Length);
            List<int> nodeKeys = new List<int>(ratios.Length);
            List<double[]> scoresResult = new List<double[]>(ratios.Length);


            var siteCorners = site.GetMinimumRoatatedRect(radiant);

            SplitRecursiveNonAccuratly2(site, bspTree.Root, siteCorners, scores, radiant, renewRadiant, ref curvesResult, ref radiantsResult, ref coordinatesResult, ref scoresResult, ref nodeKeys);

            // Correct the order of brepsResult to make sure the order of typesRepresent and subSites are the same.
            Polygon[] result = new Polygon[ratios.Length];

            for (int k = 0; k < nodeKeys.Count; k++)
            {
                var key = nodeKeys[k];
                var tempBrep = curvesResult[k];
                result[key] = tempBrep;
            }

            // Clear tree.
            bspTree.Clear();
            return result;
        }


        public static void SplitRecursiveNonAccuratly2(Polygon polygon, BSPTreeNode node, Coordinate[] currentCorners, double[] currentScores, double radiant, bool renewRadiant,
            ref List<Polygon> allPolygons, ref List<double> allRadiants, ref List<Coordinate[]> allCorners, ref List<double[]> allScores, ref List<int> nodeKeys)
        {
            if (node.IsLeafNode())
            {
                var leafRadiant = renewRadiant ? polygon.GetPolygonRadiant() : radiant;

                allPolygons.Add(polygon);
                allRadiants.Add(leafRadiant);
                allScores.Add(currentScores);
                allCorners.Add(currentCorners);
                nodeKeys.Add(node.Key);
            }
            else
            {
                // Has at least one child.
                if (node.HasOnlyLeftChild())
                {
                    // Only has left child, just go down this node to find out if it has other children.
                    double leftRadiant = renewRadiant ? polygon.GetPolygonRadiant() : radiant;

                    SplitRecursiveNonAccuratly2(polygon, node.LeftChild, currentCorners, currentScores, leftRadiant, renewRadiant, ref allPolygons, ref allRadiants, ref allCorners, ref allScores, ref nodeKeys);
                }
                else
                {
                    // has both child, left child has higher priority than right child.
                    var ratio = node.LeftChild.Value / (node.LeftChild.Value + node.RightChild.Value);

                    // Only get sub-polygons and children scores representively.
                    var subPolygons = FindRectanglePolygons(polygon, ratio, currentCorners, currentScores, out double[][] childrenScores);

                    // Get radiants.
                    double leftRadiant = renewRadiant ? subPolygons[0].GetPolygonRadiant() : radiant;
                    double rightRadiant = renewRadiant ? subPolygons[1].GetPolygonRadiant() : radiant;

                    // Get corners.
                    var leftCorners = subPolygons[0].GetMinimumRoatatedRect(leftRadiant);
                    var rightCorners = subPolygons[1].GetMinimumRoatatedRect(rightRadiant);

                    SplitRecursiveNonAccuratly2(subPolygons[0], node.LeftChild, leftCorners, childrenScores[0], leftRadiant, renewRadiant, ref allPolygons, ref allRadiants, ref allCorners, ref allScores, ref nodeKeys);
                    SplitRecursiveNonAccuratly2(subPolygons[1], node.RightChild, rightCorners, childrenScores[1], rightRadiant, renewRadiant, ref allPolygons, ref allRadiants, ref allCorners, ref allScores, ref nodeKeys);
                }
            }
        }



        public static Polygon[] FindRectanglePolygons(Polygon polygon, double ratio, Coordinate[] corners, double[] scores, out double[][] childrenScores)
        {
            SiteMinimumRectangle siteMinimum = new SiteMinimumRectangle(corners, scores);

            var subPolygons = siteMinimum.SplitPolygon(polygon, ratio);

            childrenScores = siteMinimum.GetChildrenScores();

            return subPolygons;
        }

        #endregion Non-accuratly splitting.





        #region Enclosure.

        /// <summary>
        /// Method for calculating the area of enclosure building. Negative distance means offsetting initial polygon inwards, otherwise outwards.
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="distance"></param>
        /// <param name="slutsCount"></param>
        /// <param name="slutsWidth"></param>
        /// <returns></returns>
        public static double GetEnclosureArea(Polygon polygon, double distance, int slutsCount, double slutsWidth)
        {
            // if distance >0 , getting the outter loop.
            // if distance < 0, getting the intner loop.

            var loopLen = GetOffsetCurveLength(polygon, distance, out _);
            var totalArea = (polygon.Length + loopLen) * Math.Abs(distance) * 0.5;

            return totalArea - slutsCount * slutsWidth * Math.Abs(distance);
        }


        /// <summary>
        /// Method to calculate the length of offset polygon.
        /// Warning: if offset distance is larger than segment length, result will be slightly non-accurate.
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="distance"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static double GetOffsetCurveLength(Polygon polygon, double distance, out double k)
        {
            // Check direction of curve. Must be ccw for calculating angle of two segments.
            // Check site curve orientation, should be ccw.
            if (!polygon.Shell.IsCCW)
                polygon = (Polygon)polygon.Reverse(); // Polygon is the reference type, therefore the orientation of this instance will also change.


            // Calculate total tangent angle.
            k = 0.0;

            for (int i = 0; i < polygon.NumPoints - 1; i++)
            {
                var p1 = polygon.Coordinates[i];
                var p2 = polygon.Coordinates[i + 1];
                var p3 = i == polygon.NumPoints - 2 ? polygon.Coordinates[1] : polygon.Coordinates[i + 2];


                Vector2D v1 = new Vector2D(p2, p1);
                Vector2D v2 = new Vector2D(p2, p3);


                double radiant = Math.Abs(v2.AngleTo(v1));


                int coe = 1;

                var ori = Orientation.Index(p1, p2, p3);
                switch (ori)
                {
                    case OrientationIndex.Right:

                        coe *= -1;
                        break;

                    case OrientationIndex.Left:
                        coe *= 1;
                        break;

                    case OrientationIndex.Straight:
                        coe *= 0;
                        break;
                }


                k += coe * (1 + Math.Cos(radiant)) / Math.Sin(radiant);

            }

            return polygon.Length + 2 * distance * k;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="targetArea"></param>
        /// <returns></returns>
        public static double GetOffsetDist(Polygon polygon, double targetArea)
        {
            // Check direction of curve. Must be ccw for calculating angle of two segments.
            // Check site curve orientation, should be ccw.
            if (!polygon.Shell.IsCCW)
                polygon = (Polygon)polygon.Reverse(); // Polygon is the reference type, therefore the orientation of this instance will also change.


            // Calculate total tangent angle.
            var k = 0.0;

            for (int i = 0; i < polygon.NumPoints - 1; i++)
            {
                var p1 = polygon.Coordinates[i];
                var p2 = polygon.Coordinates[i + 1];
                var p3 = i == polygon.NumPoints - 2 ? polygon.Coordinates[1] : polygon.Coordinates[i + 2];


                Vector2D v1 = new Vector2D(p2, p1);
                Vector2D v2 = new Vector2D(p2, p3);


                double radiant = Math.Abs(v2.AngleTo(v1));


                int coe = 1;

                var ori = Orientation.Index(p1, p2, p3);
                switch (ori)
                {
                    case OrientationIndex.Right:

                        coe *= -1;
                        break;

                    case OrientationIndex.Left:
                        coe *= 1;
                        break;

                    case OrientationIndex.Straight:
                        coe *= 0;
                        break;
                }


                k += coe * (1 + Math.Cos(radiant)) / Math.Sin(radiant);

            }


            if (targetArea < polygon.Area)
            {
                // d < 0 ; inwards
                // kd^2 + Polygon.Len* d+ (Polygon.Area-TargetArea ) = 0.
                var a = k;
                var b = polygon.Length;
                var c = polygon.Area - targetArea;

                var flag = SolveQuadratic.Compute(a, b, c, out double[] roots);
                if (flag)
                {
                    return roots.Max();
                }
                else
                    return double.PositiveInfinity;
            }
            else
            {
                // d> 0 ; outwards
                // kd^2 + Polygon.Len* d+ (Polygon.Area-TargetArea )  = 0.
                var a = k;
                var b = polygon.Length;
                var c = polygon.Area - targetArea;

                var flag = SolveQuadratic.Compute(a, b, c, out double[] roots);
                if (flag)
                {
                    return roots.Max();
                }
                else
                    return double.PositiveInfinity;
            }
        }


        #endregion Enclosure.
    }
}
