using Rhino.Geometry;
using Rhino.Geometry.Intersect;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UrbanX.Algorithms.Mathematics;
using UrbanX.DataStructures.Trees;

namespace UrbanX.Planning.UrbanDesign
{
    public static class DesignToolbox
    {
        // One thread

        //public static SiteParameters[] ComputeParameters(Curve[] allSites, Curve[] allRoads, double[] allScores, double tolerance)
        //{
        //    var result = new SiteParameters[allSites.Length];

        //    var allMidPts = GetMidPoints(allRoads);

        //    for (int i = 0; i < allSites.Length; i++)
        //    {
        //        result[i] = new SiteParameters(allSites[i], allMidPts, allScores, tolerance);
        //    }

        //    return result;
        //}


        /// <summary>
        /// Computing site parameters using multi-tasks.
        /// </summary>
        /// <param name="allSites"></param>
        /// <param name="allRoads"></param>
        /// <param name="allScores"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static SiteParameters[] ComputeParameters(Curve[] allSites, Curve[] allRoads, double[] allScores, double tolerance)
        {
            // Checking if site curve is valided.
            var cleaned = new List<Curve>(allSites.Length);

            for (int i = 0; i < allSites.Length; i++)
            {
                var c = allSites[i];

                if (c.IsClosed && c.IsValid && c.IsPlanar())
                    cleaned.Add(c);
            }

            cleaned.TrimExcess();

            var result = new SiteParameters[cleaned.Count];
            var allMidPts = GetMidPoints(allRoads);

            Parallel.For(0, cleaned.Count, i =>
            {
                result[i] = new SiteParameters(cleaned[i], allMidPts, allScores, tolerance);
            });

            return result;
        }



        /// <summary>
        /// Method to divide site into several subsites when the original site is way to large. Exclude residential site.
        /// </summary>
        /// <returns></returns>
        public static SiteParameters[][] RefineParameters(SiteParameters[] siteParameters, double tolerance)
        {
            SiteParameters[][] result = new SiteParameters[siteParameters.Length][];

            for (int i = 0; i < siteParameters.Length; i++)
            {
                // Check site orientation. Must be clockwise.
                if (siteParameters[i].Site.ClosedCurveOrientation() == CurveOrientation.CounterClockwise)
                    siteParameters[i].Site.Reverse();


                double siteArea = AreaMassProperties.Compute(siteParameters[i].Site).Area;
                SiteTypes siteType = (SiteTypes)siteParameters[i].SiteType;

                var ratio = siteParameters[i].MixRatio * SiteDataset.GetMixedCorCoefficients(siteType);

                int count = (int)Math.Round(siteArea / (SiteDataset.GetMaxAreaByType(siteType) / (1 - ratio)));

                if (count <= 0)
                {
                    // actuall, this scenario is not exist.
                    result[i] = new SiteParameters[] { siteParameters[i] };
                }
                else
                {
                    // Split current site curve into n subsites. n equals count.

                    // Splitting site by using binary partition tree.
                    var nodes = new BSPTreeNode[count];
                    for (int c = 0; c < count; c++)
                    {
                        // Create new node by using value and priority from building type.
                        nodes[c] = new BSPTreeNode(c, 1, 1);
                    }

                    BSPTree bspTree = new BSPTree(nodes);


                    LinkedList<Curve> curvesResult = new LinkedList<Curve>();
                    LinkedList<double> radiantsResult = new LinkedList<double>();
                    LinkedList<double[]> scoresResult = new LinkedList<double[]>();
                    LinkedList<int> nodeKeys = new LinkedList<int>();

                    SplitRecursive(siteParameters[i].Site, bspTree.Root, siteParameters[i].Scores, siteParameters[i].Radiant, true, tolerance, ref curvesResult, ref radiantsResult, ref scoresResult, ref nodeKeys);


                    double averageHeight = siteParameters[i].FAR / siteParameters[i].Density * 4.5;

                    double initalDistance = BuildingDataset.GetSetbackOhterType(averageHeight) * 1.8;

                    LinkedList<SiteParameters> subSiteParameters = new LinkedList<SiteParameters>();

                    for (int b = 0; b < count; b++)
                    {
                        Curve brepLoop = curvesResult.ToArray()[b];

                        SafeOffsetCurve(brepLoop, initalDistance, tolerance, out Curve setback);


                        var siteAreaRenew = AreaMassProperties.Compute(setback).Area;
                        double densityRenew = siteArea / count * siteParameters[i].Density / siteAreaRenew;
                        double farRenew = siteArea / count * siteParameters[i].FAR / siteAreaRenew;


                        SiteParameters siteRenew = new SiteParameters(setback, radiantsResult.ToArray()[b], scoresResult.ToArray()[b], densityRenew, farRenew
                            , siteParameters[i].SiteType, siteParameters[i].MixRatio, siteParameters[i].BuildingStyle);

                        subSiteParameters.AddLast(siteRenew);
                    }

                    result[i] = subSiteParameters.ToArray();
                }
            }

            return result;
        }


        public static Point3d[] GetMidPoints(Curve[] allRoads)
        {
            Point3d[] result = new Point3d[allRoads.Length];
            for (int i = 0; i < allRoads.Length; i++)
            {
                result[i] = allRoads[i].PointAtNormalizedLength(0.5);
            }
            return result;
        }

        public static Curve[] SplitSiteByRatios(Curve site, double[] ratios, double[] priorities, double[] scores, double radiant, bool renewRadiant, double tolerance)
        {
            // Check site curve orientation, should be cw.
            if (site.ClosedCurveOrientation() == CurveOrientation.CounterClockwise)
                site.Reverse();

            // Split brep.
            var nodes = new BSPTreeNode[ratios.Length];
            for (int c = 0; c < ratios.Length; c++)
            {
                // Create new node by using value and priority from building type.
                nodes[c] = new BSPTreeNode(c, ratios[c], priorities[c]);
            }

            BSPTree bspTree = new BSPTree(nodes);
            LinkedList<Curve> curvesResult = new LinkedList<Curve>();
            LinkedList<double> radiantsResult = new LinkedList<double>();
            LinkedList<double[]> scoresResult = new LinkedList<double[]>();
            LinkedList<int> nodeKeys = new LinkedList<int>();

            SplitRecursive(site, bspTree.Root, scores, radiant, renewRadiant, tolerance, ref curvesResult, ref radiantsResult, ref scoresResult, ref nodeKeys);

            // Correct the order of brepsResult to make sure the order of typesRepresent and subSites are the same.
            Curve[] result = new Curve[ratios.Length];

            for (int k = 0; k < nodeKeys.Count; k++)
            {
                var key = nodeKeys.ToArray()[k];
                var tempBrep = curvesResult.ToArray()[k];
                result[key] = tempBrep;
            }

            // Clear tree.
            bspTree.Clear();
            return result;
        }


        public static void SplitRecursive(Curve curve, BSPTreeNode node, double[] currentScores, double radiant, bool renewRadiant, double tolerance, ref LinkedList<Curve> result, ref LinkedList<double> radiants, ref LinkedList<double[]> scores, ref LinkedList<int> nodeKeys)
        {

            if (node.IsLeafNode())
            {
                result.AddLast(curve);
                // if renew radiant, for leaf node, we still need to get the radiant.

                radiants.AddLast(radiant);
                scores.AddLast(currentScores);
                nodeKeys.AddLast(node.Key);
            }
            else
            {
                if (node.HasOnlyLeftChild())
                {
                    double leftRadiant;
                    if (!node.LeftChild.IsLeafNode() && renewRadiant)
                    {
                        leftRadiant = GetRadiant(curve);
                        // TODO: shift order of current scores.
                    }
                    else
                    {
                        leftRadiant = radiant;
                    }

                    SplitRecursive(curve, node.LeftChild, currentScores, leftRadiant, renewRadiant, tolerance, ref result, ref radiants, ref scores, ref nodeKeys);
                }
                else
                {
                    // has both child, left child has higher priority than right child.
                    var ratio = node.LeftChild.Value / (node.LeftChild.Value + node.RightChild.Value);

                    var curves = FindRootRatio(curve, ratio, currentScores, radiant, tolerance, out double[][] childrenScores);

                    double leftRadiant, rightRadiant;

                    if (!node.LeftChild.IsLeafNode() && renewRadiant)
                    {
                        leftRadiant = GetRadiant(curves[0]);
                    }
                    else
                    {
                        leftRadiant = radiant;
                    }

                    SplitRecursive(curves[0], node.LeftChild, childrenScores[0], leftRadiant, renewRadiant, tolerance, ref result, ref radiants, ref scores, ref nodeKeys);


                    if (!node.RightChild.IsLeafNode() && renewRadiant)
                    {
                        rightRadiant = GetRadiant(curves[1]);
                    }
                    else
                    {
                        rightRadiant = radiant;
                    }

                    SplitRecursive(curves[1], node.RightChild, childrenScores[1], rightRadiant, renewRadiant, tolerance, ref result, ref radiants, ref scores, ref nodeKeys);
                }
            }
        }


        private static Curve[] FindRootRatio(Curve curve, double ratio, double[] currentScores, double radiant, double tolerance, out double[][] childernScores)
        {
            // total area is the site area.
            double targetArea = AreaMassProperties.Compute(curve).Area * ratio;

            var siteRect = new SiteBoundingRect(curve, radiant, currentScores);

            double f(double x)
            {
                var curves = siteRect.SplitPlanarCurveFace(x, tolerance);

                // The first brep corresponds to the ratio (left child).

                // For debug.
                //var a = breps[0].GetArea();
                //var b = breps[1].GetArea();

                return AreaMassProperties.Compute(curves[0]).Area;
            }

            double ratioFound = RootFinding.Brent(new FunctionOfOneVariable(f), 0.01, 0.99, targetArea, out int iteration, out double error, tolerance);

            var result = siteRect.SplitPlanarCurveFace(ratioFound, tolerance);

            childernScores = siteRect.GetChildsScores();

            siteRect.Dispose();

            return result;
        }



        /// <summary>
        /// Help method used in splitrecursively.
        /// </summary>
        /// <param name="site"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        private static double GetRadiant(Curve site)
        {
            // Get control polygon count.
            var polyline = site.ToNurbsCurve().Points.ControlPolygon();


            var segs = polyline.GetSegments();

            List<double> lens = new List<double>(segs.Length);
            for (int i = 0; i < segs.Length; i++)
            {
                lens.Add(segs[i].Length);
            }

            var max = segs[lens.IndexOf(lens.Max())];

            Vector3d v = new Vector3d(max.To - max.From);
            v.Unitize();
            if (v.Y < 0)
                v.Reverse();

            double radiant = Math.Acos(Math.Max(Math.Min(v * Vector3d.XAxis, 1.0), -1.0));


            // Transform radiant into first quadrant'
            radiant = radiant > Math.PI * 0.5 ? radiant - Math.PI * 0.5 : radiant;

            // Consider the negtive and postive.
            radiant = radiant > Math.PI * 0.25 ? -(Math.PI * 0.5 - radiant) : radiant;

            return radiant;
        }


        /// <summary>
        /// Important method to asure that the centre point is within the curve.
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static Point3d GetCurveCentre(Curve curve, double tolerance)
        {
            var result = AreaMassProperties.Compute(curve).Centroid;

            // To determine whether this centroid is inside brep. Curve should be clock wise, because tagent vector need to be rotated.
            if (curve.ClosedCurveOrientation() == CurveOrientation.CounterClockwise)
                curve.Reverse();

            // Must define the direction point, otherwise offset will be failure.
            if (curve.Contains(result, Plane.WorldXY, tolerance) != PointContainment.Inside)
            {
                curve.ClosestPoint(result, out double t, int.MaxValue);
                var cloPt = curve.PointAt(t);
                var tagent = curve.TangentAt(t);

                tagent.Unitize();
                tagent.Rotate(Math.PI * -0.5, Plane.WorldXY.ZAxis);

                Point3d temp = cloPt + tagent * 1E+10;
                Line ray = new Line(cloPt, temp);

                var cs = Intersection.CurveCurve(ray.ToNurbsCurve(), curve, tolerance, tolerance);
                if (cs.Count < 2)
                {
                    // This situation is really rare.
                    result = cloPt + tagent * tolerance;
                }
                else
                {
                    result = (cloPt + cs[1].PointA) / 2;
                }
            }

            return result;
        }


        /// <summary>
        /// Safely generating the offset curve based on a given distance and direction point. The orientation of curve doesn't affect result.
        /// Using centre point to determine the offset direction.
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="distance"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static bool SafeOffsetCurve(Curve curve, double distance, double tolerance, out Curve setback)
        {

            var directionPt = GetCurveCentre(curve, tolerance);
            var curves = curve.Offset(directionPt, Plane.WorldXY.Normal, distance, tolerance, CurveOffsetCornerStyle.Sharp);

            if (curves == null)
            {
                setback = curve;
                return false;
            }


            setback = Curve.JoinCurves(curves)[0];

            // Determine whether this setback is valided.
            var sections = Intersection.CurveCurve(curve, setback, tolerance, tolerance);
            if (sections.Count != 0)
            {
                setback = curve;
                return false;
            }

            if (!setback.IsClosed)
            {
                var flag = setback.MakeClosed(tolerance);
                if (flag)
                {
                    if (setback.ClosedCurveOrientation() == CurveOrientation.CounterClockwise)
                        setback.Reverse();

                    return true;
                }
                else
                {
                    setback = curve;
                    return false;
                }
            }


            if (setback.ClosedCurveOrientation() == CurveOrientation.CounterClockwise)
                setback.Reverse();

            return true;
        }

        public static Polyline ConvertToPolyline(Curve curve, double tolerance)
        {
            // Do not change the orientation of original curve.

            //Try get polyline.        
            var flag = curve.TryGetPolyline(out Polyline pl);
            if (!flag)
            {
                // Need to convert current curve to polyline.
                var length = curve.GetLength();
                pl = curve.ToPolyline(tolerance, tolerance, length / 60, length / 3).ToPolyline();
            }

            return pl;
        }


        /// <summary>
        /// Intersect a test curve with a list of regions, return all the inside segments.
        /// </summary>
        /// <param name="testCurve"></param>
        /// <param name="regions"></param>
        /// <param name="tolerance"></param>
        /// <param name="insideCurvesLength"></param>
        /// <returns></returns>
        public static Curve[] IntersectCurveRegions(Curve testCurve, Curve[] regions, double tolerance, out double insideCurvesLength)
        {
            insideCurvesLength = 0;
            if (testCurve == null || regions == null || regions.Length == 0)
                return null;

            List<double> ts = new List<double>();
            HashSet<Curve> result = new HashSet<Curve>();


            for (int i = 0; i < regions.Length; i++)
            {
                var region = regions[i];
                if (!region.IsClosed)
                    continue;

                var events = Intersection.CurveCurve(testCurve, region, tolerance, tolerance);
                if (events != null && events.Count != 0)
                {
                    for (int e = 0; e < events.Count; e++)
                    {
                        ts.Add(Math.Round(events[e].ParameterA, 6));
                    }
                }
            }

            ts.Sort();
            var hs = new HashSet<double>(ts);

            var splittedCurves = testCurve.Split(hs);
            foreach (var seg in splittedCurves)
            {
                var pt = seg.PointAt(seg.Domain.Mid);
                foreach (var reg in regions)
                {
                    if (reg.Contains(pt, Plane.WorldXY, tolerance) == PointContainment.Inside && !result.Contains(seg))
                    {
                        result.Add(seg);
                        insideCurvesLength += seg.GetLength();
                    }
                }
            }

            return result.ToArray();
        }


        /// <summary>
        /// Shuting a ray from a base point to a tested curve in a certain direction , get the distance between base point and intersected point.
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="vector"></param>
        /// <param name="basePt"></param>
        /// <param name="tolerance"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static double GetDistance(Curve curve, Vector3d vector, Point3d basePt, double tolerance, double length = 1E+10)
        {
            if (!vector.IsUnitVector)
                vector.Unitize();

            Point3d temp = basePt + vector * length;

            PolylineCurve plc = new PolylineCurve(new Point3d[] { basePt, temp });

            // Must using CurveCurve, if using CurveLine , line is regarded as inifinate line.
            var cs = Intersection.CurveCurve(curve, plc, tolerance, tolerance);
            if (cs.Count == 0)
                return length;
            else
            {
                var point2 = cs[0].PointA;
                return basePt.DistanceTo(point2);
            }
        }


        /// <summary>
        /// Method to calculate the length of offset polygon.
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="distance"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static double GetOffsetCurveLength(Curve curve, double distance, double tolerance, out double k)
        {
            // Check direction of curve. Must be ccw for calculating angle of two segments.
            if (curve.ClosedCurveOrientation() == CurveOrientation.Clockwise)
                curve.Reverse();


            //Try get polyline.        
            var pl = ConvertToPolyline(curve, tolerance);

            // Calculate total tangent angle.
            k = 0.0;
            var segments = pl.GetSegments();
            List<Line> segs = new List<Line>(segments)
            {
                segments[0]
            };

            for (int i = 0; i < segs.Count - 1; i++)
            {
                Vector3d v1 = new Vector3d(segs[i].From - segs[i].To);
                Vector3d v2 = new Vector3d(segs[i + 1].To - segs[i + 1].From);

                double radiant = Math.Acos(v1 * v2 / (v1.Length * v2.Length));

                // Check ccw.
                var p1 = segs[i].From;
                var p2 = segs[i].To;
                var p3 = segs[i + 1].To;
                int coe = 1;

                double area2 = (p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X);
                if (area2 < 0)
                {
                    coe *= 1;
                }
                else if (area2 > 0)
                {
                    coe *= -1;
                }
                else
                {
                    coe *= 0;
                }

                k -= coe * (1 + Math.Cos(radiant)) / Math.Sin(radiant);
            }

            return pl.Length - 2 * distance * k;
        }



        /// <summary>
        /// Main method for computing design. This is the only method should be using for external program.
        /// </summary>
        /// <param name="siteParametersInput"></param>
        /// <param name="cityIndex"></param>
        /// <param name="tolerance"></param>
        /// <param name="edgesd"></param>
        /// <param name="scoresd"></param>
        /// <returns></returns>
        public static DesignResult[] ComputingDesign(SiteParameters[] siteParametersInput, int cityIndex, double tolerance, out List<Line> edgesd, out List<double> scoresd , ref string log)
        {
            DesignResult[] siteResults = new DesignResult[siteParametersInput.Length];

            // For debug.
            StringBuilder ErrorLog = new StringBuilder();
            ConcurrentBag<Line> edgeBag = new ConcurrentBag<Line>();
            ConcurrentBag<double> scoreBag = new ConcurrentBag<double>();


            // Split site into subsite when the original site is too large. For Residential, there is no need to to so.
            var siteParameters = RefineParameters(siteParametersInput, tolerance);

            for (int s = 0; s < siteParametersInput.Length; s++)
            {
                // Get site type and building style.
                SiteTypes siteType = (SiteTypes)siteParametersInput[s].SiteType;

                MixTypes mixType;

                if (siteParametersInput[s].MixRatio <= 0.05)
                {
                    mixType = MixTypes.None;
                }
                else if (siteParametersInput[s].MixRatio > 0.12)
                {
                    mixType = MixTypes.Horizontal;
                }
                else
                {
                    mixType = MixTypes.Vertical;
                }

                ResidentialStyles residentialStyle = ResidentialStyles.RowRadiance;
                NonResidentialStyles nonResidentialStyle = NonResidentialStyles.Alone;

                if (siteType == SiteTypes.R)
                {
                    residentialStyle = (ResidentialStyles)siteParametersInput[s].BuildingStyle;
                }
                else if (siteType == SiteTypes.M || siteType == SiteTypes.W)
                {
                    nonResidentialStyle = NonResidentialStyles.Alone;
                }
                else
                {
                    nonResidentialStyle = (NonResidentialStyles)siteParametersInput[s].BuildingStyle;
                }


                // For each site, create a new siteResult.
                Curve[] subSites = new Curve[siteParameters[s].Length];
                double[] subSiteScores = new double[siteParameters[s].Length];
                Curve[][] subSiteSetbacks = new Curve[siteParameters[s].Length][];


                BuildingGeometry[][] buildingGeometriesResult = new BuildingGeometry[siteParameters[s].Length][];

                for(int i = 0; i < siteParameters[s].Length; i ++)
                //Parallel.For(0, siteParameters[s].Length, i =>
                {
                    try
                    {
                        var siteFilleted = DesignUtilities.FilletPolylineCorners(siteParameters[s][i].Site, 6, tolerance);
                        subSites[i] = siteFilleted;
                        subSiteScores[i] = siteParameters[s][i].Scores.Sum();

                        DesignCalculator calculator = new DesignCalculator(siteParameters[s][i].Site, siteType, siteParameters[s][i].Density, siteParameters[s][i].FAR, mixType, siteParameters[s][i].MixRatio);

                        if (siteType == SiteTypes.R)
                        {
                            var resident = calculator.CalculateResidentialTypes(cityIndex, siteParameters[s][i].Radiant, siteParameters[s][i].Scores, residentialStyle, tolerance, out Curve NRsite, out double NRFar);
                            resident.GeneratingBuildings();

                            Curve[] setbacks = new Curve[] { resident.SetBack.DuplicateCurve() };
                            subSiteSetbacks[i] = setbacks;
                            buildingGeometriesResult[i] = resident.BuildingGeometries;


                            int bbb = 0;
                            // For debug.
                            foreach (var b in resident.BuildingGeometries)
                            {
                                var area = b.BuildingArea;

                                bbb++;
                            }

                            resident.Dispose();

                            // For mixused site. Input: BuildingType[] NRtypes, Curve NRsite
                            if (NRsite != null && NRFar != 0)
                            {
                                DesignCalculator calculatorMix = new DesignCalculator(NRsite, SiteTypes.C, siteParameters[s][i].Density, NRFar);
                                var nonResidentialMix = calculatorMix.CalculateTypes(siteParameters[s][i].Radiant, siteParameters[s][i].Scores, tolerance, ref nonResidentialStyle);

                                nonResidentialMix.GeneratingBuildings(nonResidentialStyle);
                                var setbacksNR = nonResidentialMix.SetBacks;
                                var geometriesNR = nonResidentialMix.BuildingGeometries;

                                List<Curve> setbacksAll = new List<Curve>(setbacks.Length + setbacksNR.Length);
                                setbacksAll.AddRange(setbacks);
                                setbacksAll.AddRange(setbacksNR);

                                List<BuildingGeometry> geometriesAll = new List<BuildingGeometry>(buildingGeometriesResult[i].Length + geometriesNR.Length);
                                geometriesAll.AddRange(buildingGeometriesResult[i]);
                                geometriesAll.AddRange(geometriesNR);

                                subSiteSetbacks[i] = setbacksAll.ToArray();
                                buildingGeometriesResult[i] = geometriesAll.ToArray();
                            }
                        }
                        else
                        {
                            double[] scores = new double[4] { 1, 1, 1, 1 };

                            var nonResidential = calculator.CalculateTypes(siteParameters[s][i].Radiant, scores, tolerance, ref nonResidentialStyle);
                            //var nonResidential = calculator.CalculateTypes(siteParameters[s][i].Radiant, siteParameters[s][i].Scores, tolerance, ref nonResidentialStyle);


                            nonResidential.GeneratingBuildings(nonResidentialStyle);
                            subSiteSetbacks[i] = nonResidential.SetBacks;

        
                            buildingGeometriesResult[i] = nonResidential.BuildingGeometries;

                            
                     

                            foreach (var edge in nonResidential.Edges)
                            {
                                edgeBag.Add(edge);
                            }
                            foreach (var score in nonResidential.Scoresd)
                            {
                                scoreBag.Add(score);
                            }

                            nonResidential.Dispose();
                        }

                        calculator.Dispose();
                    }
                    catch
                    {
                        ErrorLog.AppendLine($"Site_{i} failed to generate buildings.");
                    }
                }//); 


                // Gone through current site's all subsites.
                DesignResult siteResult = new DesignResult(siteParametersInput[s].Site, siteParametersInput[s].Scores, siteType.ToString(), subSites.ToArray(), subSiteScores.ToArray(), subSiteSetbacks.ToArray(), buildingGeometriesResult);
                siteResults[s] = siteResult;
            }

            edgesd = edgeBag.ToList();
            scoresd = scoreBag.ToList();
            log = ErrorLog.ToString();

            return siteResults;
        }
    }
}
