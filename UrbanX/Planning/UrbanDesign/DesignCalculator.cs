using Microsoft.SolverFoundation.Services;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;

using UrbanX.Algorithms.Mathematics;


namespace UrbanX.Planning.UrbanDesign
{

    public class DesignCalculator : IDisposable
    {
        private readonly Curve _site;

        private readonly double _siteArea;

        private double _density;

        private double _far;

        private readonly SiteTypes _siteType;

        private readonly MixTypes _mixType;

        private readonly double _mixRatio;

        private readonly List<string> _mainBuildingTypes;

        private readonly List<string> _mixedBuildingTypes;

        public DesignCalculator(Curve site, SiteTypes siteType, double density, double far, MixTypes mix = MixTypes.None, double mixRatio = 0)
        {
            _site = site.DuplicateCurve();
            _siteArea = AreaMassProperties.Compute(site).Area;
            _siteType = siteType;

            _density = density;
            _far = far;

            _mixType = mix;
            _mixRatio = mixRatio;

            _mainBuildingTypes = new List<string>();
            _mixedBuildingTypes = new List<string>();
        }


        /// <summary>
        /// For Non-residential type of sites.
        /// </summary>
        /// <param name="radiant"></param>
        /// <param name="scores"></param>
        /// <param name="tolerance"></param>
        /// <param name="nonResidentialStyle"></param>
        /// <param name="database"></param>
        /// <returns> DesignNonResidential class: for site partitioning. </returns>
        public DesignNonResidential CalculateTypes(double radiant, double[] scores, double tolerance, ref NonResidentialStyles nonResidentialStyle, bool database = true)
        {
            // Using database to select fesible types.
            if (database)
                SelectTypes(_far, _density);
            // If using customized building types, there is no need to select types.
            // TODO: _mainBuildingTypes = inputs


            switch (nonResidentialStyle)
            {
                case NonResidentialStyles.Alone:
                    if (_mixType == MixTypes.Horizontal)
                    {
                        var siteBuildingTypes = new List<BuildingType>();
                        var mainBuildingCandidates = _mainBuildingTypes.ToArray();
                        var mixedBuildingCandidates = _mixedBuildingTypes.ToArray();

                        // _mixRatio should less than 30%.
                        var ratio = _mixRatio * SiteDataset.GetMixedCorCoefficients(_siteType);
                        var mainSiteArea = (1 - ratio) * _siteArea;
                        var mixedSiteArea = ratio * _siteArea;

                        // Step1: for main type buildings.
                        var mainBuildingCounts = GetCounts(mainBuildingCandidates, mainSiteArea);
                        var mainBuildingFloors = GetFloors(mainBuildingCandidates, mainBuildingCounts, 1 - _mixRatio);
                        var mainSiteBTypes = CreateSiteBuildingTypes(mainBuildingCandidates, mainBuildingCounts, mainBuildingFloors);

                        // Step2: for mixed type buildings.
                        var mixedBuildingCounts = GetCounts(mixedBuildingCandidates, mixedSiteArea);
                        var mixedBuildingFloors = GetFloors(mixedBuildingCandidates, mixedBuildingCounts, _mixRatio);
                        var mixedSiteBTypes = CreateSiteBuildingTypes(mixedBuildingCandidates, mixedBuildingCounts, mixedBuildingFloors);

                        // Combine two groups.
                        siteBuildingTypes.AddRange(mainSiteBTypes);
                        siteBuildingTypes.AddRange(mixedSiteBTypes);

                        var sp = new DesignNonResidential(_site, _far * _siteArea, siteBuildingTypes.ToArray(), scores, radiant, tolerance);
                        return sp;
                    }
                    else if (_mixType == MixTypes.Vertical)
                    {
                        //mixtype == vertical
                        var candidates = _mainBuildingTypes.ToArray();
                        var counts = GetCounts(candidates, _siteArea);

                        var bottomFloors = GetFloors(candidates, counts, _mixRatio);
                        var upperFloors = GetFloors(candidates, counts, 1 - _mixRatio);
                        var siteBTypes = CreateSiteBuildingTypes(candidates, counts, upperFloors, bottomFloors);

                        var sp = new DesignNonResidential(_site, _far * _siteArea, siteBTypes, scores, radiant, tolerance);
                        return sp;
                    }
                    else
                    {
                        // Has no mix use.
                        var candidates = _mainBuildingTypes.ToArray();
                        var counts = GetCounts(candidates, _siteArea);
                        var buildingFloors = GetFloors(candidates, counts, 1);
                        var siteBTypes = CreateSiteBuildingTypes(candidates, counts, buildingFloors);

                        var sp = new DesignNonResidential(_site, _far * _siteArea, siteBTypes, scores, radiant, tolerance);
                        return sp;
                    }

                case NonResidentialStyles.Group:
                    var flag = CorrectGroupStyleDensity(_site, radiant, _far, tolerance, ref _density, out double[] outterDistanceRange, out double[] thicknessRange, out double minSetbackDistance);
                    if (!flag)
                    {
                        nonResidentialStyle = NonResidentialStyles.Alone;
                        goto case NonResidentialStyles.Alone;
                    }
                    var flagF = FitGroupStyleParameters(_site, thicknessRange, outterDistanceRange, _density, _far, minSetbackDistance, tolerance, out Curve lo, out Curve li,
                            out int blockCount, out int floorCount);

                    if (!flagF)
                    {
                        nonResidentialStyle = NonResidentialStyles.Alone;
                        goto case NonResidentialStyles.Alone;
                    }

                    List<BuildingType> types = new List<BuildingType>(blockCount);
                    var typeString = _mainBuildingTypes[0];
                    if (_mixRatio != 0)
                    {
                        // Has mix-use.
                        int mixFloors = (int)Math.Round(blockCount * floorCount * _mixRatio);
                        if (mixFloors == 0) mixFloors++;
                        int averageMix = (int)Math.Round(mixFloors * 1.0 / blockCount);
                        for (int b = 0; b < blockCount; b++)
                        {
                            if (b == blockCount - 1)
                            {
                                int correctFloor = mixFloors - averageMix * b;
                                // Create the last sitebuildingtype.
                                types.Add(new BuildingType(typeString, new int[] { correctFloor, floorCount - correctFloor }, _siteArea));
                                break;
                            }
                            types.Add(new BuildingType(typeString, new int[] { averageMix, floorCount - averageMix }, _siteArea));
                        }
                    }
                    else
                    {
                        // No mix-use.
                        for (int b = 0; b < blockCount; b++)
                        {
                            types.Add(new BuildingType(typeString, new int[] { 0, floorCount }, _siteArea));
                        }
                    }
                    // Create sitePartioner for output.
                    var result = new DesignNonResidential(_site, _far * _siteArea, types.ToArray(), scores, radiant, tolerance, lo, li, minSetbackDistance);
                    return result;

                case NonResidentialStyles.Mixed:
                    goto case NonResidentialStyles.Group;
            }

            return null;
        }

        private bool CorrectGroupStyleDensity(Curve currentSite, double radiant, double far, double tolerance, ref double density, out double[] outterDistanceRange, out double[] thicknessRange, out double minSetbackDistance)
        {
            // Check radiant
            radiant = radiant > Math.PI * 0.5 ? Math.PI - radiant : radiant;
            radiant = radiant > Math.PI * 0.25 ? Math.PI * 0.5 - radiant : radiant;


            // outterDistanceRange represent the largest outter loop and smallest outter loop.

            // First we need to check if site is big enough for using Group style. If not, change the style to Alone.
            var edges = SiteBoundingRect.GetEdges(currentSite, radiant);
            var edgeLengths = new double[edges.Length];
            var siteArea = AreaMassProperties.Compute(currentSite).Area;

            for (int i = 0; i < edges.Length; i++)
            {
                edgeLengths[i] = edges[i].Length;
            }
            var minLength = edgeLengths.Min();


            var averageFloor = far / density;
            var depthRange = BuildingDataset.GetGroupStyleParameters(out double minVoidDepth, out double floorHeight);
            var minSetback = BuildingDataset.GetSetbackOhterType(averageFloor * floorHeight); // only half distance.

            // If site is too small, return false to indicate the nonresdiential style should be changed. And also terminate this method.
            // If return false, goto case 1.
            if (minLength < minVoidDepth + 2 * (depthRange[0] + minSetback))
            {
                outterDistanceRange = null;
                thicknessRange = null;
                minSetbackDistance = 0;
                return false;
            }


            // If this site is big enough for group style, start to Offset current site curve.
            // IMPORTANT: using current site for splitting. SiteSetbackCurve is the largest outter loop.

            //var siteSetbackCurve = SiteMainMethods.SafeOffsetCurve(currentSite, minSetback, tolerance);
            var flag1 = DesignToolbox.SafeOffsetCurve(currentSite, minSetback, tolerance, out Curve siteSetbackCurve);

            if (!flag1)
            {
                outterDistanceRange = null;
                thicknessRange = null;
                minSetbackDistance = 0;
                return false;
            }

            thicknessRange = new double[2];

            // From blow: all the offset method will be using siteSetbackCurve as base curve.
            // For maximum density.
            double innerSetbackDistance = (minLength - minVoidDepth) * 0.5 - minSetback;

            innerSetbackDistance = innerSetbackDistance < depthRange[1] ? Math.Round(innerSetbackDistance, 3) : depthRange[1];


            Curve innerLoop = null;
            while (innerSetbackDistance > depthRange[0])
            {
                var flag = DesignToolbox.SafeOffsetCurve(siteSetbackCurve, innerSetbackDistance, tolerance, out Curve loop);
                if (!flag || AreaMassProperties.Compute(loop).Area < minVoidDepth * minVoidDepth)
                {
                    innerSetbackDistance--;
                    continue;
                }
                else
                {
                    innerLoop = loop;
                    break;
                }
            }
            innerSetbackDistance = innerSetbackDistance < depthRange[0] ? depthRange[0] : innerSetbackDistance;
            double innerArea = innerLoop == null ? minVoidDepth * minVoidDepth : AreaMassProperties.Compute(innerLoop).Area;

            thicknessRange[1] = innerSetbackDistance;

            var siteSetbackArea = AreaMassProperties.Compute(siteSetbackCurve).Area;
            var maxBuildingArea = siteSetbackArea - innerArea - 2 * innerSetbackDistance * minSetback;
            var maxDensity = maxBuildingArea / siteArea;

            // For minimum density.
            Curve outterLoop = siteSetbackCurve;
            var outterSetbackDistance = innerSetbackDistance - depthRange[0]; // Thinest depth. Largest outterDistance.
            while (outterSetbackDistance > 0)
            {
                var flag = DesignToolbox.SafeOffsetCurve(siteSetbackCurve, outterSetbackDistance, tolerance, out Curve loop);

                if (!flag)
                {
                    outterSetbackDistance--;
                    continue;
                }
                else
                {
                    outterLoop = loop;
                    break;
                }
            }
            outterSetbackDistance = outterSetbackDistance < 0 ? 0 : outterSetbackDistance;

            var minBuildingArea = AreaMassProperties.Compute(outterLoop).Area - innerArea - 4 * (innerSetbackDistance - outterSetbackDistance) * minSetback;
            var minDensity = minBuildingArea / siteArea;

            outterDistanceRange = new double[] { 0, outterSetbackDistance };
            thicknessRange[0] = innerSetbackDistance - outterSetbackDistance;

            // Correcting current density.
            if (density < minDensity)
                density = minDensity;

            if (density > maxDensity)
                density = maxDensity;

            var correctAverageFloor = far / density;
            minSetbackDistance = BuildingDataset.GetSetbackOhterType(correctAverageFloor * floorHeight) + 1.5; // only half distance.

            var margin = minSetbackDistance - minSetback;
            outterDistanceRange[1] -= margin;
            thicknessRange[1] -= margin;
            return true;
        }



        /// <summary>
        /// Method to find the suitable outter loop and inner loop for group style building.
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="depthRange"></param>
        /// <param name="outterDistanceRange"></param>
        /// <param name="density"></param>
        /// <param name="minSetbackDistance"></param>
        /// <param name="tolerance"></param>
        /// <param name="lo"></param>
        /// <param name="li"></param>
        /// <param name="blockCount"></param>
        private bool FitGroupStyleParameters(Curve curve, double[] depthRange, double[] outterDistanceRange, double density, double far, double minSetbackDistance, double tolerance, out Curve lo, out Curve li, out int blockCount, out int floorCount)
        {
            var siteArea = AreaMassProperties.Compute(curve).Area;
            var targetArea = siteArea * density;
            DesignToolbox.SafeOffsetCurve(curve, minSetbackDistance, tolerance, out Curve loopOutter);
            DesignToolbox.GetOffsetCurveLength(loopOutter, 0, tolerance, out double k);

            // Random blockcount[2,5].
            Random random = new Random();
            blockCount = random.Next(2, 5);

            // Check blockcout, if site is too small, blockcount should be minimum.
            var maxSubSiteArea = SiteDataset.GetMaxAreaByType(_siteType);
            if (siteArea < maxSubSiteArea * 0.6)
            {
                blockCount = 2;
            }

            double o = 0;
            double d = depthRange[0];
            while (d <= depthRange[1])
            {
                o = (loopOutter.GetLength() - blockCount * minSetbackDistance) / (2 * k) - targetArea / (2 * k * d) - d / 2;
                if (outterDistanceRange[0] <= o && o <= outterDistanceRange[1])
                    break;
                d++;
            }
            o = o < 0 ? 0 : o;
            d = d > depthRange[1] ? depthRange[1] : d;


            DesignToolbox.SafeOffsetCurve(loopOutter, o, tolerance, out lo);
            var flagI = DesignToolbox.SafeOffsetCurve(lo, d, tolerance, out li);

            if (flagI)
            {
                var blockArea = (lo.GetLength() + li.GetLength()) * d * 0.5 - minSetbackDistance * d * blockCount * 2;
                floorCount = (int)Math.Round(AreaMassProperties.Compute(curve).Area * far / blockArea);
                if (floorCount == 0) floorCount++;

                return true;
            }
            else
            {
                floorCount = 0;
                return false;
            }

        }



        /// <summary>
        /// Calculating the suitable building types' counts and floors for residential site.
        /// For horizontal mix-use, residential site need to be splitted into two subsites.
        /// </summary>
        /// <param name="cityIndex"></param>
        /// <param name="radiant"></param>
        /// <param name="scores"></param>
        /// <param name="residentialStyle"></param>
        /// <param name="tolerance"></param>
        /// <param name="NRTypes"></param>
        /// <param name="NRsite"></param>
        /// <returns>SiteResidential class.</returns>
        public DesignResidential CalculateResidentialTypes(int cityIndex, double radiant, double[] scores, ResidentialStyles residentialStyle, double tolerance, out Curve NRsite, out double NRFar)
        {
            //CorrectResitentialDensity(cityIndex, ref _far, ref _density, out int initialFloors);
            CorrectResitentialDensity1(cityIndex, _site, radiant, ref _far, ref _density, out int initialFloors);

            SelectTypes(_far, _density);

            // Double check the average floor.
            int averageFloor = (int)Math.Round(_far / _density) > initialFloors ? (int)Math.Round(_far / _density) : initialFloors;

            if (_mixType == MixTypes.Horizontal)
            {

                var mainBuildingCandidates = _mainBuildingTypes.ToArray();
                //var mixedBuildingCandidates = _mixedBuildingTypes.ToArray();

                // _mixRatio should less than 30%.
                var ratio = _mixRatio * SiteDataset.GetMixedCorCoefficients(_siteType);
                var mainSiteArea = (1 - ratio) * _siteArea;
                var mixedSiteArea = ratio * _siteArea;

                // Step0: split current site.
                double[] areas = new double[] { mainSiteArea, mixedSiteArea };
                double[] priorities = new double[] { 1, 10 }; // Residential has lower priority in terms of the accessiblity.

                // Step1: for main type buildings.
                var sites = DesignToolbox.SplitSiteByRatios(_site, areas, priorities, scores, radiant, true, tolerance);

                DesignResidential resident = new DesignResidential(cityIndex, sites[0], radiant, mainBuildingCandidates, averageFloor, _siteArea * _far * (1 - _mixRatio), residentialStyle, tolerance);

                NRsite = sites[1];
                NRFar = _far * _mixRatio / ratio;

                return resident;
            }
            else if (_mixType == MixTypes.Vertical)
            {
                //mixtype == vertical
                var candidates = _mainBuildingTypes.ToArray();
                DesignResidential resident = new DesignResidential(cityIndex, _site, radiant, candidates, averageFloor, _siteArea * _far, residentialStyle, tolerance);

                var bottomFloor = (int)Math.Round(resident.BuildingFloors[1] * _mixRatio);
                var upperFloor = resident.BuildingFloors[1] - bottomFloor;

                // alterating building floors.
                resident.BuildingFloors = new int[] { bottomFloor, upperFloor };
                NRFar = 0;
                NRsite = null;

                return resident;
            }
            else
            {
                // Has no mix use.
                var candidates = _mainBuildingTypes.ToArray();
                DesignResidential resident = new DesignResidential(cityIndex, _site, radiant, candidates, averageFloor, _siteArea * _far, residentialStyle, tolerance);

                NRFar = 0;
                NRsite = null;

                return resident;
            }
        }

        [Obsolete]
        private void CorrectResitentialDensity0(int cityIndex, ref double far, ref double density, out int initialFloors)
        {
            // inital depth of a building t.
            double t = 26;
            int floorUpperLimit = 150;

            // Calculate k: sundistance = k*x , x=1.
            double testHeight = 3.0 * 1;
            var k = BuildingDataset.GetSunlightDistance(testHeight, cityIndex) / 1;

            // N: target count to suffice FAR ; n: max count this site can hold.
            // x > F*t/(t-k*F) && x> t/k.

            if (t - k * far < 0)
            {
                far = (t - 1) / k;
            }

            var flag0 = far * t / (t - k * far);
            var flag1 = t / k;

            initialFloors = flag0 > flag1 ? (int)Math.Ceiling(flag0) : (int)Math.Ceiling(flag1);
            initialFloors = initialFloors > floorUpperLimit ? floorUpperLimit : initialFloors;

            double maxDensity = far / initialFloors;
            density = density > maxDensity ? maxDensity : density;
        }


        private void CorrectResitentialDensity1(int cityIndex, Curve site, double radiant, ref double far, ref double density, out int initialFloors)
        {
            // inital depth of a building t.
            double t = 26;
            double delta = 0.9;

            // _k equals the const double _k in DesignResidential class. \ beta = 1-2*_k , Important: _k has no relationship with k below.
            double _k = 1.0 / 3;
            double beta = 1 - 2 * _k;

            int floorUpperLimit = 45;

            // Calculate k: sundistance = k*x , x=1. x is the floor count.
            double testHeight = 3.0 * 1;
            var k = BuildingDataset.GetSunlightDistance(testHeight, cityIndex) / 1;

            // Calculate l: second edge of the bounding box.
            var l = DesignResidential.GetEdgeLength(site, radiant);


            // Far max  : f <= (ktx^2 + tlx) /(tl+ klx)
            // F_m=δ∗(tl∗x_m+kt∗x_m^2)/(tl+kl∗x_m )
            // F_m=δ∗(tl∗x+ktβx^2)/(tl+klx)

            var fmax = delta * (beta * t * k * Math.Pow(floorUpperLimit, 2) + l * t * floorUpperLimit) / (l * k * floorUpperLimit + l * t);
            far = far > fmax ? fmax : far;


            // N: target count to suffice FAR ; n: max count this site can hold.
            // n=((1+ s/l)*A) / ((1+s/l)*a)
            // Goal: N/n>1
            // Formula: x2 + (l/k -lf/t)x -lf/k>=0 
            // x^2+(l/k−(lf_t)/δt)∗x −(lf_t)/δk≥0
            // x^2+(l/kβ−lf/δβt)x −lf/δβk≥0


            var a = 1.0;
            var b = l / (k * beta) - l * far / (t * delta * beta);
            var c = -l * far / (delta * beta * k);

            SolveQuadratic.Compute(a, b, c, out double[] roots);

            // getting the minimum floor.
            var flag0 = roots.Max();
            // x > F*t/(t-k*F) && x> t/k.
            var flag1 = t / k;

            initialFloors = flag0 > flag1 ? (int)Math.Ceiling(flag0) : (int)Math.Ceiling(flag1);
            initialFloors = initialFloors > floorUpperLimit ? floorUpperLimit : initialFloors;


            // maxium density for current far.
            double maxDensity = far / initialFloors;
            density = density > maxDensity ? maxDensity : density;

            // compare far/density
            var targetFloor = (int)Math.Round(far / density);
            initialFloors = initialFloors < targetFloor ? targetFloor : initialFloors;
        }

        /// <summary>
        /// Public method for calculating residential limitation. Using same code from above, only expose sevearl parameters.
        /// </summary>
        /// <param name="site"></param>
        /// <param name="cityIndex"></param>
        /// <param name="buildingDepth"></param>
        /// <param name="floorUpperLimit"></param>
        /// <param name="radiant"></param>
        /// <param name="far"></param>
        /// <param name="density"></param>
        /// <param name="initialFloors"></param>
        public static void CorrectResitentialDensity(Curve site, int cityIndex, double buildingDepth, int floorUpperLimit, double floorHeight, double radiant, out double maxFar, out double maxDensity)
        {
            // inital depth of a building t.
            double t = buildingDepth;
            double delta = 0.9;

            // _k equals the const double _k in DesignResidential class. \ beta = 1-2*_k , Important: _k has no relationship with k below.
            double _k = 1.0 / 3;
            double beta = 1 - 2 * _k;


            // Calculate k: sundistance = k*x , x=1. x is the floor count.
            double testHeight = floorHeight * 1;
            var k = BuildingDataset.GetSunlightDistance(testHeight, cityIndex) / 1;

            // Calculate l: second edge of the bounding box.
            var l = DesignResidential.GetEdgeLength(site, radiant);


            // Far max  : f <= (ktx^2 + tlx) /(tl+ klx)
            // F_m=δ∗(tl∗x_m+kt∗x_m^2)/(tl+kl∗x_m )
            // F_m=δ∗(tl∗x+ktβx^2)/(tl+klx)

            maxFar = delta * (beta * t * k * Math.Pow(floorUpperLimit, 2) + l * t * floorUpperLimit) / (l * k * floorUpperLimit + l * t);



            // N: target count to suffice FAR ; n: max count this site can hold.
            // n=((1+ s/l)*A) / ((1+s/l)*a)
            // Goal: N/n>1
            // Formula: x2 + (l/k -lf/t)x -lf/k>=0 
            // x^2+(l/k−(lf_t)/δt)∗x −(lf_t)/δk≥0
            // x^2+(l/kβ−lf/δβt)x −lf/δβk≥0


            var a = 1.0;
            var b = l / (k * beta) - l * maxFar / (t * delta * beta);
            var c = -l * maxFar / (delta * beta * k);

            SolveQuadratic.Compute(a, b, c, out double[] roots);

            // getting the minimum floor.
            var flag0 = roots.Max();
            // x > F*t/(t-k*F) && x> t/k.
            var flag1 = t / k;

            var initialFloors = flag0 > flag1 ? (int)Math.Ceiling(flag0) : (int)Math.Ceiling(flag1);
            initialFloors = initialFloors > floorUpperLimit ? floorUpperLimit : initialFloors;

            // maximum density for current far.
            maxDensity = maxFar / initialFloors;
        }




        private void SelectTypes(double far, double density)
        {
            var main = SiteDataset.GetMainBuildingTypes(_siteType);
            var mixed = SiteDataset.GetMixedBuildingTypes(_siteType);

            double averageFloor = far / density;
            //int averageFloor = SiteResidential.CalculateAverageFloor(_siteArea, 27, _far);

            foreach (var type in main)
            {
                var floorRange = BuildingDataset.GetBuildingParameters(type).FloorRange;
                if (averageFloor >= floorRange[0] && averageFloor <= floorRange[1])
                    _mainBuildingTypes.Add(type);
            }

            foreach (var type in mixed)
            {
                var floorRange = BuildingDataset.GetBuildingParameters(type).FloorRange;
                if (averageFloor >= floorRange[0] && averageFloor <= floorRange[1])
                    _mixedBuildingTypes.Add(type);
            }

            if (_mainBuildingTypes.Count == 0)
                _mainBuildingTypes.AddRange(main);

            if (_mixedBuildingTypes.Count == 0)
                _mixedBuildingTypes.AddRange(mixed);


            // If types.count>=3, only select 3 types randomly to reduce the computing complexity.
            if (_mainBuildingTypes.Count > 3)
            {
                var c = _mainBuildingTypes.Count - 3;
                Random r = new Random();
                for (int i = 0; i < c; i++)
                {
                    int index = r.Next(_mainBuildingTypes.Count);
                    _mainBuildingTypes.RemoveAt(index);
                }
            }
            if (_mixedBuildingTypes.Count > 3)
            {
                var c = _mixedBuildingTypes.Count - 3;
                Random r = new Random();
                for (int i = 0; i < c; i++)
                {
                    int index = r.Next(_mixedBuildingTypes.Count);
                    _mixedBuildingTypes.RemoveAt(index);
                }
            }
        }


        private int[] GetCounts(string[] buildingTypes, double siteArea)
        {
            int[] result = new int[buildingTypes.Length];

            SolverContext context = new SolverContext();
            Model model = context.CreateModel();

            // Create decisions.
            Decision[] xs = new Decision[buildingTypes.Length];
            for (int i = 0; i < xs.Length; i++)
            {
                xs[i] = new Decision(Domain.IntegerNonnegative, $"x{i}");
            }

            // Add decisions.
            model.AddDecisions(xs);

            // Add constraints.
            var upperBound = siteArea * _density;

            Term totalArea = 0;
            for (int i = 0; i < xs.Length; i++)
            {
                totalArea += xs[i] * GetFoortPrintArea(buildingTypes, i);

                // larger and equal to zero means some types may not be selected.
                model.AddConstraint($"{xs[i].Name}Domain", xs[i] >= 0);
            }

            model.AddConstraint("AreaLimit", totalArea <= upperBound);

            // Add goal.
            model.AddGoal("TotalFloorArea", GoalKind.Maximize, totalArea);


            // Solving.
            context.Solve(new SimplexDirective());


            // xs may be zero.
            int zeroCount = 0;
            for (int i = 0; i < xs.Length; i++)
            {
                result[i] = (int)xs[i].ToDouble();

                if (result[i] == 0)
                    zeroCount++;
            }

            if (zeroCount == xs.Length)
                result[0] = 1;

            return result;
        }


        public static int[] GetBuildingCounts(double[] buildingAreas, double siteArea, double density)
        {
            int[] result = new int[buildingAreas.Length];

            SolverContext context = new SolverContext();
            Model model = context.CreateModel();

            // Create decisions.
            Decision[] xs = new Decision[buildingAreas.Length];
            for (int i = 0; i < xs.Length; i++)
            {
                xs[i] = new Decision(Domain.IntegerNonnegative, $"x{i}");
            }

            // Add decisions.
            model.AddDecisions(xs);

            // Add constraints.
            var upperBound = siteArea * density;

            Term totalArea = 0;
            for (int i = 0; i < xs.Length; i++)
            {
                totalArea += xs[i] * buildingAreas[i];

                // larger and equal to zero means some types may not be selected.
                model.AddConstraint($"{xs[i].Name}Domain", xs[i] >= 0);
            }

            model.AddConstraint("AreaLimit", totalArea <= upperBound);

            // Add goal.
            model.AddGoal("TotalFloorArea", GoalKind.Maximize, totalArea);


            // Solving.
            context.Solve(new SimplexDirective());


            // xs may be zero.
            int zeroCount = 0;
            for (int i = 0; i < xs.Length; i++)
            {
                result[i] = (int)xs[i].ToDouble();

                if (result[i] == 0)
                    zeroCount++;
            }

            if (zeroCount == xs.Length)
                result[0] = 1;

            return result;
        }



        /// <summary>
        /// Getting the total floor count of current building dispite the vertical mixuse.
        /// </summary>
        /// <param name="buildingTypes"></param>
        /// <param name="typeCounts"></param>
        /// <param name="ratio"></param>
        /// <returns> Total floor count for each building type. </returns>
        private int[] GetFloors(string[] buildingTypes, int[] typeCounts, double ratio)
        {
            int[] result = new int[buildingTypes.Length];

            SolverContext context = new SolverContext();
            Model model = context.CreateModel();

            // Create decisions.
            Decision[] fs = new Decision[buildingTypes.Length];
            for (int i = 0; i < fs.Length; i++)
            {
                fs[i] = new Decision(Domain.IntegerNonnegative, $"f{i}");
            }

            // Add decisions.
            model.AddDecisions(fs);

            // Add constraints.
            var upperBound = _siteArea * _far * ratio;


            Term totalArea = 0;
            for (int i = 0; i < fs.Length; i++)
            {
                totalArea += fs[i] * GetFoortPrintArea(buildingTypes, i) * typeCounts[i];

                // limits
                model.AddConstraints($"{fs[i].Name}Domain", fs[i] >= BuildingDataset.GetBuildingParameters(buildingTypes[i]).FloorRange[0],
                    fs[i] <= BuildingDataset.GetBuildingParameters(buildingTypes[i]).FloorRange[1]);
            }

            model.AddConstraint("AreaLimit", totalArea <= upperBound);


            // Add goal.
            model.AddGoal("TotalFloorArea", GoalKind.Maximize, totalArea);

            // Solving
            context.Solve(new SimplexDirective());

            for (int i = 0; i < fs.Length; i++)
            {
                result[i] = (int)Math.Round(fs[i].ToDouble());

                // In case the result is smaller than the lower bound of constrant, which the solver result is zero.
                if (result[i] == 0) result[i]++;
                //result[i] = BuildingDataset.GetBuildingParameters(buildingTypes[i]).FloorRange[0];
            }

            return result;
        }


        public static int[] GetBuildingFloors(double[] buildingAreas, int[] typeCounts, Interval[] floorRanges, double siteArea, double far)
        {
            int[] result = new int[buildingAreas.Length];

            SolverContext context = new SolverContext();
            Model model = context.CreateModel();

            // Create decisions.
            Decision[] fs = new Decision[buildingAreas.Length];
            for (int i = 0; i < fs.Length; i++)
            {
                fs[i] = new Decision(Domain.IntegerNonnegative, $"f{i}");
            }

            // Add decisions.
            model.AddDecisions(fs);

            // Add constraints.
            var upperBound = siteArea * far;


            Term totalArea = 0;
            for (int i = 0; i < fs.Length; i++)
            {
                totalArea += fs[i] * buildingAreas[i] * typeCounts[i];

                // limits
                model.AddConstraints($"{fs[i].Name}Domain", fs[i] >= (int)floorRanges[i].Min, fs[i] <= (int)floorRanges[i].Max);
            }

            model.AddConstraint("AreaLimit", totalArea <= upperBound);


            // Add goal.
            model.AddGoal("TotalFloorArea", GoalKind.Maximize, totalArea);

            // Solving
            context.Solve(new SimplexDirective());

            for (int i = 0; i < fs.Length; i++)
            {
                result[i] = (int)Math.Round(fs[i].ToDouble(), 0);

                // In case the result is smaller than the lower bound of constrant, which the solver result is zero.
                if (result[i] == 0)
                    result[i] = (int)floorRanges[i].Min;
            }

            return result;
        }




        private BuildingType[] CreateSiteBuildingTypes(string[] buildingTypes, int[] counts, int[] upperFloors, int[] bottomFloors = null)
        {
            LinkedList<BuildingType> result = new LinkedList<BuildingType>();

            for (int i = 0; i < buildingTypes.Length; i++)
            {
                int bottomFloor = 0;
                var upperFloor = upperFloors[i];

                if (bottomFloors != null)
                {
                    bottomFloor += bottomFloors[i];

                }

                BuildingType type = new BuildingType(buildingTypes[i], new int[] { bottomFloor, upperFloor }, _siteArea);
                var count = counts[i];

                while (count > 0)
                {
                    result.AddLast(type);
                    count--;
                }
            }

            return result.ToArray();
        }


        private double GetFoortPrintArea(string[] buildingTypes, int index)
        {
            var type = buildingTypes[index];
            return BuildingDataset.GetBuildingParameters(type).Area;
        }

        public void Dispose()
        {
            _site.Dispose();
            _mainBuildingTypes.Clear();
            _mixedBuildingTypes.Clear();
        }
    }
}
