using Rhino.Geometry;
using Rhino.Geometry.Intersect;

using System;
using System.Collections.Generic;
using System.Linq;

using MetaCity.Algorithms.Trees;
using MetaCity.Algorithms.Utility;
using MetaCity.DataStructures.Heaps;
using MetaCity.DataStructures.Trees;


namespace MetaCity.Planning.UrbanDesign
{
    public class DesignResidential : IDisposable
    {
        private readonly ResidentialStyles _style;

        private readonly double _tolerance;

        private readonly string[] _inputTypes;

        private readonly int _averageFloor;

        // Representing the raito of sunDistance that the first row should be setback
        private const double _k = 1.0 / 3;

        /// <summary>
        /// siteArea * FAR 
        /// </summary>
        private readonly double _targetTotalArea;

        private readonly int _cityIndex;

        private readonly double _radiant;


        /// <summary>
        /// Original site curve.
        /// </summary>
        private readonly Curve _site;

        private readonly double _siteArea;

        private string _buildingTypeName;


        // Those fields storing the result from solver.

        private int[] _maxCountPerLine;

        private double _depth;

        private double _width;

        private double _areaError;

        public BuildingGeometry[] BuildingGeometries { get; private set; }

        public int BuildingCount { get; private set; }

        /// <summary>
        /// Each residential site only has one setback curve.
        /// </summary>
        public Curve SetBack { get; }


        /// <summary>
        /// Storing the building floors: no mixed-use {0,n} ; mixed-use{bottom-n , upper-n} , total_floor = bottom-n + upper-n;
        /// </summary>
        public int[] BuildingFloors { get; set; }

        public Curve[] CentreLines { get; private set; }


        // for debug
        public Curve[] _subsites;

        public DesignResidential(int cityIndex, Curve site, double radiant, string[] buildingTypes, int averageFloor, double targetTotalArea, ResidentialStyles style, double tolerance)
        {
            _style = style;
            _tolerance = tolerance;
            _site = site.DuplicateCurve();
            _siteArea = AreaMassProperties.Compute(_site).Area;

            var setbackDistance = BuildingDataset.GetSetbackRtype(averageFloor) * 2;

            DesignToolbox.SafeOffsetCurve(site, setbackDistance, tolerance, out Curve tempSetback);

            var pl = DesignToolbox.ConvertToPolyline(tempSetback, tolerance);
            pl.ReduceSegments(1);
            SetBack = pl.ToPolylineCurve();

            _inputTypes = buildingTypes;
            _averageFloor = averageFloor;
            _targetTotalArea = targetTotalArea;
            _cityIndex = cityIndex;

            ValidatingRadiant(ref radiant);
            _radiant = radiant;

            // Initialize fields and properties.
            //_maxCountPerLine = null;
            //_depth = 0;
            //_width = 0;
            //_areaError = 0;

            //BuildingGeometries = null;
            //_buildingTypeName = null;
            //BuildingCount = 0;
            BuildingFloors = new int[2];
            //CentreLines = null;

            ResidentialSolver();
        }


        #region Public methods
        /// <summary>
        /// Method for fitting buildings in a residential site.
        /// </summary>
        private void ResidentialSolver()
        {
            List<int> floors = new List<int>();
            List<int> counts = new List<int>();
            List<string> allTypes = new List<string>();

            // totalTargetArea - totalCurrentArea
            List<double> totalAreaErrors = new List<double>();

            // Abs of (totalTargetArea - totalCurrentArea)
            List<double> areaErrorsAbs = new List<double>();

            List<Curve[]> centreLinesList = new List<Curve[]>();
            List<int[]> maxCountList = new List<int[]>();

            List<double> depths = new List<double>();
            List<double> widths = new List<double>();

            // Floor limits: is the upper limit of floors.
            int inputFloor = _averageFloor > 45 ? 45 : _averageFloor;

            foreach (var type in _inputTypes)
            {
                // 0. Get building parameters.
                var bp = BuildingDataset.GetBuildingParameters(type);


                // initialFloor will be increasing during each iteration. Shallow copy of targetFloor to initialFloor.
                var initialFloor = inputFloor;

                // 1. Calculate the total building area.
                while (initialFloor <= 45)
                {
                    Curve[] centreLines = null;
                    int[] totalCount = null;
                    bool flag = false;
                    double depth = 0;
                    double width = 0;
                    int lastRowCount = 1;

                    switch (_style)
                    {
                        case ResidentialStyles.RowRadiance:
                            flag = ComputeRowRadiance(bp, initialFloor, out centreLines, out totalCount, out depth, out width, out lastRowCount);

                            break;

                        case ResidentialStyles.DotVariousHeight:
                            goto case ResidentialStyles.RowRadiance;

                            //case ResidentialStyles.DotRowMajor:
                            //    flag = ComputeDotRowMajor(bp, initialFloor, out centreLines, out totalCount, out depth, out width, out lastRowCount);

                            //    break;

                            //case ResidentialStyles.DotColumnMajor:
                            //    flag = ComputeDotColumnMajor(bp, initialFloor, out centreLines, out totalCount, out depth, out width, out lastRowCount);

                            //    break;
                    }

                    if (flag == false)
                        break;

                    // 1.3 fitting to target.
                    var targetCount = (int)Math.Round(_targetTotalArea / bp.Area / initialFloor);
                    if (targetCount == 0)
                    {
                        targetCount++;
                        initialFloor = (int)Math.Round(_targetTotalArea / bp.Area / 1.0);
                        if (initialFloor == 0)
                            initialFloor++;
                    }

                    var currentTotalCount = totalCount.Sum();

                    if (targetCount < currentTotalCount || targetCount == 1)
                    {
                        centreLinesList.Add(centreLines);
                        maxCountList.Add(totalCount);
                        depths.Add(depth);
                        widths.Add(width);

                        var areaError = _targetTotalArea - bp.Area * initialFloor * targetCount;

                        floors.Add(initialFloor);
                        totalAreaErrors.Add(areaError);
                        areaErrorsAbs.Add(Math.Abs(areaError));
                        counts.Add(targetCount);
                        allTypes.Add(type);
                        break;
                    }
                    else if (initialFloor == 45 && areaErrorsAbs.Count == 0)
                    {
                        // Handle exception when never find a option before.
                        centreLinesList.Add(centreLines);
                        maxCountList.Add(totalCount);
                        depths.Add(depth);
                        widths.Add(width);

                        var areaError = _targetTotalArea - bp.Area * initialFloor * currentTotalCount;

                        floors.Add(initialFloor);
                        totalAreaErrors.Add(areaError);
                        areaErrorsAbs.Add(Math.Abs(areaError));
                        counts.Add(currentTotalCount);
                        allTypes.Add(type);
                        break;
                    }
                    else
                    {
                        // targetCount > current count.                 
                        if (totalCount.Length > 1)
                        {
                            var totalAreaError = _targetTotalArea - bp.Area * initialFloor * totalCount.Sum();

                            int lastRowFloor = (int)Math.Round(totalAreaError / bp.Area / lastRowCount) + initialFloor;

                            if (lastRowFloor * 1.0 / initialFloor < 1.5)
                            {
                                centreLinesList.Add(centreLines);
                                maxCountList.Add(totalCount);
                                depths.Add(depth);
                                widths.Add(width);

                                // Using totalCount as current count.
                                var areaError = _targetTotalArea - bp.Area * (initialFloor * (currentTotalCount - lastRowCount) + lastRowFloor * lastRowCount);

                                // Add targetFloor in list.
                                floors.Add(initialFloor);

                                totalAreaErrors.Add(totalAreaError);
                                areaErrorsAbs.Add(Math.Abs(areaError));
                                counts.Add(currentTotalCount);
                                allTypes.Add(type);

                                break;
                            }
                            else
                            {
                                initialFloor++;
                                continue;
                            }
                        }
                        else
                        {
                            // totalCount.Length = 1
                            int targetFloor = (int)Math.Round(_targetTotalArea / bp.Area / currentTotalCount);

                            centreLinesList.Add(centreLines);
                            maxCountList.Add(totalCount);
                            depths.Add(depth);
                            widths.Add(width);

                            // Using totalCount as current count.
                            var areaError = _targetTotalArea - bp.Area * targetFloor * currentTotalCount;

                            // Add targetFloor in list.
                            floors.Add(targetFloor);
                            totalAreaErrors.Add(areaError);
                            areaErrorsAbs.Add(Math.Abs(areaError));
                            counts.Add(currentTotalCount);
                            allTypes.Add(type);

                            break;
                        }
                    }
                }
            }

            // 2. Actually, we need to find the floor count which is nearest to the target floor count.
            List<double> floorMargins = new List<double>(floors.Count);
            for (int f = 0; f < floors.Count; f++)
            {
                floorMargins.Add(Math.Abs(floors[f] - inputFloor));
            }
            var min = floorMargins.IndexOf(floorMargins.Min());

            // Get all the critical Properties and fields of current residential class.
            _areaError = totalAreaErrors[min];


            // Currently, only consider the total floor count of a building. Therefore BuildingFloor = {0,n}.
            BuildingFloors[1] = floors[min];
            BuildingCount = counts[min];
            _buildingTypeName = allTypes[min];
            CentreLines = centreLinesList[min];

            _maxCountPerLine = maxCountList[min];
            _depth = depths[min];
            _width = widths[min];

            if (_style == ResidentialStyles.DotVariousHeight)
            {
                BuildingFloors[1] = _averageFloor;
                BuildingCount = (int)Math.Round(_targetTotalArea / BuildingFloors[1] / (_width * _depth));
                _areaError = _targetTotalArea - (_averageFloor * BuildingCount * _width * _depth);
            }

            // During solver stage, building floor is [0,n]
            // Only when generating buildings will consider mix.
        }


        public void GeneratingBuildings()
        {
            var centreLineCount = DistributingCount(_maxCountPerLine, BuildingCount);

            // After distributing count, there may appear several zeros, therefore we need to clean each line again.
            List<int> cleanLineCount = new List<int>();
            List<Curve> cleanCentreLine = new List<Curve>();

            for (int i = 0; i < centreLineCount.Length; i++)
            {
                if (centreLineCount[i] != 0)
                {
                    cleanLineCount.Add(centreLineCount[i]);
                    cleanCentreLine.Add(CentreLines[i]);
                }
            }
            CentreLines = cleanCentreLine.ToArray();


            switch (_style)
            {
                case ResidentialStyles.RowRadiance:
                    BuildingGeometries = GetBuildingGeometriesInRowRadiance(cleanLineCount.ToArray());
                    break;

                case ResidentialStyles.DotVariousHeight:
                    BuildingGeometries = GetBuildingGeometriesInDotVarious();
                    break;

                    //case ResidentialStyles.DotRowMajor:
                    //    BuildingGeometries = GetBuildingGeometries1(cleanLineCount.ToArray());
                    //    break;

                    //case ResidentialStyles.DotColumnMajor:
                    //    BuildingGeometries = GetBuildingGeometries2(cleanLineCount.ToArray());
                    //    break;
            }
        }


        /// <summary>
        /// Method for calculating the second line's length of the boundingbox. 
        /// </summary>
        /// <param name="site">CCurrent curve.</param>
        /// <param name="radiant">Radiant for getting boundingbox. Radiant need to be validated.</param>
        /// <returns>Edge length.</returns>
        public static double GetEdgeLength(Curve site, double radiant)
        {
            ValidatingRadiant(ref radiant);
            var edges = SiteBoundingRect.GetEdges(site, radiant);
            return edges[1].Length;
        }

        #endregion



        #region Three main methods for computing.
        private bool ComputeRowRadiance(BuildingParameters bp, int tempFloor, out Curve[] centreLines, out int[] totalCount, out double depth, out double width, out int lastRowCount)
        {
            var edges = SiteBoundingRect.GetEdges(SetBack, _radiant);


            // 1.0 Getting parameters.
            var height = bp.FloorHeight * tempFloor;

            // CorrectedSunDistance is the minimum distance between two parallel buildings. Notice: this is not the north to south distance.
            // CorrectedSunDistance is parallel to edge[1].
            var correctedSunDistance = BuildingDataset.GetSunlightDistance(height, _cityIndex) * Math.Cos(_radiant);
            var setback = BuildingDataset.GetSetbackRtype(tempFloor) * 2;

            var loDepth = bp.Depth[0];
            var hiDepth = bp.Depth[1];
            depth = (loDepth + hiDepth) * 0.5;


            // Math.Cos(_radiant)*sunDistance:d ; depth: D ; count : n ; edges[1].Length : L ;
            // k is const representing the raito of sunDistance that the first row should be setback.
            // (2k +n-1) *d + n*D <= L
            // n = (L-(2k-1)d)/(D+d);
            // D = (L-(2k-1)d)/n - d;


            // input sunDistance: sd ; calculated distance :d ; depth: D ; count : n ; edges[1].Length : L
            // (2k +n-1) *d + n*D <= L
            // d >= sd
            // Therefore for k: kmax = (L-nD-(n-1)sd)/2sd;
            // if kmax< _k ; _k == kmax;
            // Then we need to calculate d based on new k;
            // d = (L - n*D) / (2k+n-1);


            // Important reminder:
            // 1. Using formula above, calculate line count n ; 
            // 2. Based on line count n to calculate building depth D;
            // 3. Recalculate building depth D and line n when former depth is out of standard interval.

            // 4. Based on altered line count n to split current site's setback curve.
            // 5. Based on altered line count to calculate max k.
            // 6. Base on altered k to calcuate partitioning distance, then getting central lines.
            // Based on constant _k to calculate line count n which may be altered later.
            // In "ParallelSplit" function, _k may be altered as well when calculated distance is smaller than sundistance.

            // Using Floor to make sure calculated distance is larger than corrected sun distance.
            var lineCount = (int)Math.Floor((edges[1].Length - (2 * _k - 1) * correctedSunDistance)
                / (correctedSunDistance + depth));



            // Handle the excpation where edge.length is smaller than depth.
            if (lineCount < 0)
            {
                centreLines = null;
                totalCount = null;
                width = 0;
                lastRowCount = 0;
                return false;
            }
            else
            {
                depth = Math.Round((edges[1].Length - (2 * _k - 1) * Math.Cos(_radiant) * correctedSunDistance) / lineCount - Math.Cos(_radiant) * correctedSunDistance, 3);

                if (depth > hiDepth)
                {
                    depth = hiDepth;
                    // Recalculate line count.
                    lineCount = (int)Math.Floor((edges[1].Length - (2 * _k - 1) * correctedSunDistance)
                        / (correctedSunDistance + depth));
                }

                if (depth < loDepth)
                {
                    depth = loDepth;
                    // Recalculate line count.
                    lineCount = (int)Math.Floor((edges[1].Length - (2 * _k - 1) * correctedSunDistance)
                        / (correctedSunDistance + depth));
                }


                width = Math.Round(bp.Area / depth, 2);


                // Lines' order is 0,1,2,3,4,5....  from bottom to up. Important.  
                // Just doing splitting,actually there is no need to consider the situation where splitted distance is smaller than corrected sun distance,
                // since we use Floor to round the linecount n.
                // But in the Parallelsplit function, there is a formula to calculate k max, which is used to double check the sundistance.
                centreLines = ParallelSplitV(SetBack, edges, lineCount, depth, correctedSunDistance, _tolerance);


                // 1.2 Getting the total count of how many buildings each line can hold. Maxsium capcaity.
                // And clean the  centrelines as well. 
                // If one line can hold no building, delete this line.
                totalCount = CalculateTotalCountInRowRadiance(ref centreLines, width, setback);
                lastRowCount = totalCount[totalCount.Length - 1];

                return true;
            }
        }

        /// <summary>
        /// Method for calculating the total count in rowRadiant style.
        /// </summary>
        /// <param name="centreLines"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        private int[] CalculateTotalCountInRowRadiance(ref Curve[] centreLines, double width, double setback)
        {
            List<int> result = new List<int>();
            List<Curve> cResult = new List<Curve>();
            for (int i = 0; i < centreLines.Length; i++)
            {
                var lineLengh = centreLines[i].GetLength();
                var tempCount = Math.Floor(lineLengh / width);


                if (tempCount > 3)
                {
                    var slutsCount = Math.Floor(tempCount / 3.0);
                    if (tempCount % 3 == 0)
                        slutsCount--;

                    lineLengh -= slutsCount * setback;
                }

                // Handle exception 1: c may be zero.
                var c = (int)Math.Floor(lineLengh / width);
                if (c != 0)
                {
                    result.Add(c);
                    cResult.Add(centreLines[i]);
                }
            }

            // Handle exception 2: result may have no value, which will cause error. Select the middle curve and extend itself to hold builidng.
            // In this case, site only has one row of buildings.
            if (result.Sum() == 0)
            {
                var selectCurve = centreLines[centreLines.Length / 2];

                var vector = new Vector3d(selectCurve.PointAtEnd - selectCurve.PointAtStart);
                var spt = selectCurve.PointAtStart;
                var ept = spt + vector * width;
                cResult.Add(new PolylineCurve(new Point3d[] { spt, ept }));
                result.Add(1);
            }


            centreLines = cResult.ToArray();
            return result.ToArray();
        }


        /// <summary>
        /// Method for generating all the residential building geometry in rowRadiant style.
        /// </summary>
        /// <param name="centreLineCount"></param>
        /// <returns></returns>
        private BuildingGeometry[] GetBuildingGeometriesInRowRadiance(int[] centreLineCount)
        {
            List<BuildingGeometry> result = new List<BuildingGeometry>();
            List<Curve[]> totalOutlines = new List<Curve[]>();
            IntervalTree intervalTree = new IntervalTree();
            List<IntervalNode> treeNodes = new List<IntervalNode>();

            //var totalWidth = _width * Math.Cos(_radiant) + _depth * Math.Abs(Math.Sin(_radiant));

            // Calculating line by line.
            for (int i = 0; i < CentreLines.Length; i++)
            {
                // Some lines may be too short for fitting a building. Actually,this case has already been handled during the 
                // process of getting centrelines.
                if (centreLineCount[i] == 0)
                    continue;

                // Calculate the count of sluts.
                var slutsCount = 0;

                slutsCount += centreLineCount[i] / 3;
                if (centreLineCount[i] % 3 == 0)
                    slutsCount--;

                // Calculate the standard dash line.
                int[] dash = new int[slutsCount + 1];
                for (int d = 0; d < dash.Length; d++)
                {
                    if (d == dash.Length - 1)
                    {
                        dash[d] = centreLineCount[i] - 3 * d;
                        break;
                    }

                    dash[d] = 3;
                }


                // Randomly altering the standard dash line.
                RandomAlter(ref dash, dash.Length);

                // Split line by dash.
                var parameters = GetSplitParameters(dash, CentreLines[i].Domain);
                Curve[] dashLines;


                if (parameters.Length == 0)
                {
                    dashLines = new Curve[] { CentreLines[i] };
                }
                else
                {
                    dashLines = CentreLines[i].Split(parameters);
                }


                // Get outline for each dash line. This method returns only for one line, while totalOutlines is a collection for all the lines.
                var outlines = GetOutlineForDash0(dashLines, dash, _width, _depth, out Interval[][] intervals);

                for (int n = 0; n < outlines.Length; n++)
                {
                    // For each group.
                    List<double> leftX = new List<double>();
                    List<double> rightX = new List<double>();

                    totalOutlines.Add(outlines[n]);

                    for (int d = 0; d < outlines[n].Length; d++)
                    {
                        // For each outline.        
                        var interval = intervals[n][d];

                        leftX.Add(interval.Min);
                        rightX.Add(interval.Max);
                    }

                    IntervalNode node = new IntervalNode(new UInterval(leftX.Min(), rightX.Max()), totalOutlines.Count - 1);
                    intervalTree.InsertNode(node);
                    treeNodes.Add(node);
                }
            }

            // Due to the detial of outline may smaller than original rectangle, we need to calibrate the areaError.
            var outlineError = _width * _depth - AreaMassProperties.Compute(totalOutlines[0][0]).Area;
            _areaError += outlineError * BuildingCount * BuildingFloors[1];


            // areaError>0 ; areaError<0 ; areError = 0
            // Create a new list for last row. Buildings locating in the last row should be higher.
            List<Curve> lastOrFirstRowOutlines = new List<Curve>();

            for (int n = 0; n < totalOutlines.Count; n++)
            {
                // For each group.
                var currentNode = treeNodes[n];
                var overlapedNodes = intervalTree.SearchOverlaps(currentNode, intervalTree.Root);

                List<double> outlineCentreY = new List<double>();
                foreach (var item in overlapedNodes)
                {
                    // Overlaped nodes includes the target node itself.
                    if (item._id == n)
                        continue;

                    // Warning: item._id represents the overlaped group not outline.
                    outlineCentreY.Add(GetCentroidY(totalOutlines[item._id][0]) + GetCentroidY(totalOutlines[item._id][totalOutlines[item._id].Length - 1]));
                }

                if (outlineCentreY.Count == 0)
                {
                    // Find the last row outline.
                    lastOrFirstRowOutlines.AddRange(totalOutlines[n]);
                    continue;
                }

                // Sorting this list by using default comparer: 1,2,3,4,5,6
                outlineCentreY.Sort();

                int reverse = 1;
                if (_areaError <= 0)
                {
                    reverse *= -1;
                    outlineCentreY.Reverse();
                }


                if ((GetCentroidY(totalOutlines[n][0]) + GetCentroidY(totalOutlines[n][totalOutlines[n].Length - 1])) * reverse > outlineCentreY[outlineCentreY.Count - 1] * reverse)
                {
                    // Find the last row outline.
                    lastOrFirstRowOutlines.AddRange(totalOutlines[n]);
                    continue;
                }
                else
                {
                    // Rest rows outlines. It's ready for extruding.
                    foreach (var outline in totalOutlines[n])
                    {
                        var bType = new BuildingType(_buildingTypeName, BuildingFloors, _siteArea);
                        var building = new BuildingGeometry(bType, _tolerance);
                        building.GeneratingResidentialAloneStyle(outline);
                        result.Add(building);
                    }
                }
            }


            // Find the special row items.
            var floorsCopy = new int[BuildingFloors.Length];
            BuildingFloors.CopyTo(floorsCopy, 0);

            var extraFloors = (int)Math.Round(_areaError / (_width * _depth * lastOrFirstRowOutlines.Count));


            floorsCopy[1] += extraFloors;

            foreach (var lastOuline in lastOrFirstRowOutlines)
            {

                var bType = new BuildingType(_buildingTypeName, floorsCopy, _siteArea);
                var building = new BuildingGeometry(bType, _tolerance);
                building.GeneratingResidentialAloneStyle(lastOuline);
                result.Add(building);
            }

            return result.ToArray();
        }


        private BuildingGeometry[] GetBuildingGeometriesInDotVarious()
        {
            List<BuildingGeometry> result = new List<BuildingGeometry>(BuildingCount);

            // Step 0: get the accurate sun distance.
            var bp = BuildingDataset.GetBuildingParameters(_buildingTypeName);
            var height = bp.FloorHeight * BuildingFloors.Sum();
            var sunDistance = BuildingDataset.GetSunlightDistance(height, _cityIndex);
            // coefficient : coe
            var coe = sunDistance / BuildingFloors.Sum();
            var totalWidth = _width * Math.Cos(_radiant) + _depth * Math.Abs(Math.Sin(_radiant));

            // Step 1: split current site into sub_sites recursively.
            double[] ratios = new double[BuildingCount];
            double[] priorities = new double[BuildingCount];
            double[] scores = { 1, 1, 1, 1 };

            for (int i = 0; i < BuildingCount; i++)
            {
                ratios[i] = 1;
                priorities[i] = 1;
            }

            var subSites = DesignToolbox.SplitSiteByRatios(SetBack, ratios, priorities, scores, radiant: _radiant, renewRadiant: false, _tolerance);
            _subsites = subSites;
            // Step 2: get all the outlines based on the centroid for each subsite.    

            #region instantiate all the necessary collections.
            var outlines = new Curve[BuildingCount];

            IntervalTree intervalTree = new IntervalTree();
            MaxPriorityQueue<IntervalNode, double> maxQueue = new MaxPriorityQueue<IntervalNode, double>(BuildingCount);
            MinPriorityQueue<IntervalNode, double> minQueue = new MinPriorityQueue<IntervalNode, double>(BuildingCount);
            Dictionary<int, Vector3d> fixedOutlines = new Dictionary<int, Vector3d>();

            // Collection for storing north outlines.
            HashSet<int> northIndex = new HashSet<int>();
            // Collection for storing overlaped nodes, using y coordinates for sorting items to determine which position current node is : upper, middle, down.
            // key is y coordinate, value is outline index.
            RBTree<OutlineYDict> sortedNodes = new RBTree<OutlineYDict>();


            // Collections with same index of outlines.  
            // Collection for storing centroid for each outline. Unsorted.
            Point3d[] outlineCentroids = new Point3d[BuildingCount];
            // Collection for quering OutlineYDict strut which will sort RBtree sorting overlaped outlines based on Y coordinate.
            OutlineYDict[] outlineYDicts = new OutlineYDict[BuildingCount];
            int[][] outlineFloors = new int[BuildingCount][];
            #endregion 

            var moveRatio = 1 - _k;

            // Get v1 and v2.
            // v1 directs from left to right. v2 is from bottom to up.
            Vector3d v1 = new Vector3d(CentreLines[0].PointAtEnd - CentreLines[0].PointAtStart);
            v1.Unitize();
            Vector3d v2 = new Vector3d(v1);
            v2.Rotate(Math.PI / 2.0, Plane.WorldXY.ZAxis);

            // Initialization. Important.
            for (int i = 0; i < BuildingCount; i++)
            {
                // Actually, need to consider a rare case when centroid is out of current site. In this case, we need to move centroid into current site.
                var basePt = AreaMassProperties.Compute(subSites[i]).Centroid;
                var outline = GetOutlineFromPts(basePt, _width, _depth, v1, v2, out Interval interval).ToPolylineCurve();

                outlines[i] = outline;
                outlineCentroids[i] = basePt;
                outlineYDicts[i] = new OutlineYDict(basePt.Y, i);
                outlineFloors[i] = new int[] { BuildingFloors[0], BuildingFloors[1] };

                // Step 3: Construct priorityqueue.
                var e = totalWidth - interval.Length;
                IntervalNode node = new IntervalNode(new UInterval(interval.Min - 0.5 * e, interval.Max + 0.5 * e), i);
                intervalTree.InsertNode(node);
                maxQueue.Enqueue(node, basePt.Y);
            }

            // Step 4: First sweep from up to bottom.
            var sundist = sunDistance + _depth / Math.Cos(_radiant);
            var rayLength = sunDistance + 0.5 * _depth / Math.Cos(_radiant);

            Vector3d vl = new Vector3d(-0.5 * _width, 0.5 * _depth, 0);
            Vector3d vr = new Vector3d(0.5 * _width, 0.5 * _depth, 0);

            vl.Rotate(_radiant, Vector3d.ZAxis);
            vr.Rotate(_radiant, Vector3d.ZAxis);

            var toNorthV = new Vector3d(0, 1, 0);
            var toSouthV = new Vector3d(0, -1, 0);


            // In the first sweep process, when moving building horizentally, need to       
            while (!maxQueue.IsEmpty)
            {
                var currentNode = maxQueue.DequeueMax();
                var currentOutline = outlines[currentNode._id]; // reference curve.
                var currentPt = outlineCentroids[currentNode._id];
                var currentY = currentPt.Y;
                var currentSite = subSites[currentNode._id];

                var overlapedNodes = intervalTree.SearchOverlaps(currentNode, intervalTree.Root);
                for (int o = 0; o < overlapedNodes.Count; o++)
                {
                    var outlineIndex = overlapedNodes[o]._id;
                    sortedNodes.Insert(outlineYDicts[outlineIndex]);
                }

                // determine current sub site position: inner or outer. outer postion: subSite and setbackSite will intersect.  
                var events = Intersection.CurveCurve(SetBack, currentSite, _tolerance, _tolerance);


                // There are there positions : top , middle, bottom .
                if (currentNode._id == sortedNodes.FindMax().OutlineIndex && events.Count > 0)
                {
                    // top position. Moving building to north.
                    northIndex.Add(currentNode._id);

                    // get the topleft and topright point of current outline.
                    var pTL = currentPt + vl;
                    var pTR = currentPt + vr;

                    var leftDist = DesignToolbox.GetDistance(currentSite, toNorthV, pTL, _tolerance);
                    var rightDist = DesignToolbox.GetDistance(currentSite, toNorthV, pTR, _tolerance);


                    var moveDist = leftDist < rightDist ? leftDist : rightDist;
                    currentOutline.Translate(moveRatio * moveDist * toNorthV);

                    // renew item in all the collections. Becasue those builidngs have gone up.
                    minQueue.Enqueue(currentNode, currentY + moveRatio * moveDist);
                    //outlines: outline is reference type, it's already renewed.
                    //outlineCentroids
                    outlineCentroids[currentNode._id].Y += moveRatio * moveDist;
                    //outlineYDicts
                    outlineYDicts[currentNode._id].YCoodinate = outlineCentroids[currentNode._id].Y;

                }
                else if (currentNode._id == sortedNodes.FindMin().OutlineIndex && events.Count > 0)
                {
                    // bottom position. Moving building to south.
                    var pBL = currentPt - vl;
                    var pBR = currentPt - vr;

                    var leftDist = DesignToolbox.GetDistance(currentSite, toSouthV, pBL, _tolerance);
                    var rightDist = DesignToolbox.GetDistance(currentSite, toSouthV, pBR, _tolerance);


                    var moveDist = leftDist < rightDist ? leftDist : rightDist;
                    currentOutline.Translate(moveRatio * moveDist * toSouthV);


                    // renew item in all the collections. Becasue those builidngs have gone up.
                    minQueue.Enqueue(currentNode, currentY - moveRatio * moveDist);
                    //outlines: outline is reference type, it's already renewed.
                    //outlineCentroids
                    outlineCentroids[currentNode._id].Y -= moveRatio * moveDist;
                    //outlineYDicts
                    outlineYDicts[currentNode._id].YCoodinate = outlineCentroids[currentNode._id].Y;
                }
                else
                {
                    // middle position. Moving builidng horizentally.
                    // Finding floor and ceilling nodes.
                    var currentODict = outlineYDicts[currentNode._id];
                    var fd = sortedNodes.FindFloor(currentODict);
                    var cd = sortedNodes.FindCeiling(currentODict);
                    if (fd == null || cd == null)
                        continue;

                    var floorOId = fd.Value.OutlineIndex;
                    var ceilOId = cd.Value.OutlineIndex;

                    Vector3d hV;

                    if (!fixedOutlines.ContainsKey(currentNode._id))
                    {
                        // Moving building.
                        // construct a line.
                        Line splitLine = new Line(outlineCentroids[floorOId], outlineCentroids[ceilOId]);
                        var cloestPt = splitLine.ClosestPoint(currentPt, true);
                        hV = currentPt.X > cloestPt.X ? Vector3d.XAxis : -Vector3d.XAxis;
                    }
                    else
                    {
                        // fixed outlines contains current node.
                        hV = fixedOutlines[currentNode._id];
                    }

                    if (!fixedOutlines.ContainsKey(floorOId))
                    {
                        fixedOutlines.Add(floorOId, -hV);
                    }


                    Point3d pT, pB;
                    if (hV.X > 0)
                    {
                        // choose right side points.
                        pT = currentPt + vr;
                        pB = currentPt - vr;
                    }
                    else
                    {
                        // choose left side points.
                        pT = currentPt + vl;
                        pB = currentPt - vl;
                    }

                    var topDist = DesignToolbox.GetDistance(currentSite, hV, pT, _tolerance);
                    var bottomDist = DesignToolbox.GetDistance(currentSite, hV, pB, _tolerance);

                    var moveDist = topDist < bottomDist ? topDist : bottomDist;
                    moveDist = moveDist > totalWidth ? totalWidth : moveDist;

                    currentOutline.Translate(moveRatio * moveDist * hV);
                    currentNode.ChangeInterval(new UInterval(currentNode._i._low + moveRatio * moveDist * hV.X,
                                                             currentNode._i._high + moveRatio * moveDist * hV.X));


                    // renew item in all the collections. Becasue those builidngs have gone up.
                    minQueue.Enqueue(currentNode, currentY);
                    //outlines: outline is reference type, it's already renewed.
                    //outlineCentroids
                    outlineCentroids[currentNode._id].X += moveRatio * moveDist * hV.X;
                    //outlineYDicts：did not change Y coordinate.    
                }

                // clear sortednodes for next loop.
                sortedNodes.Clear();
            }



            // Step 5: Second sweep from bottom to up.
            // Record how many floors have been reduced.
            int reducedFloorsSum = 0;
            while (!minQueue.IsEmpty)
            {
                // clear sortednodes for this loop.
                sortedNodes.Clear();

                var testNode = minQueue.DequeueMin();

                var overlapedNodes = intervalTree.SearchOverlaps(testNode, intervalTree.Root);
                for (int o = 0; o < overlapedNodes.Count; o++)
                {
                    var outlineIndex = overlapedNodes[o]._id;
                    sortedNodes.Insert(outlineYDicts[outlineIndex]);
                }

                var testODict = outlineYDicts[testNode._id];

                var cODict = sortedNodes.FindCeiling(testODict);

                if (cODict == null)// test node is the northest outline. Need to clear sortedNodes before going to next loop.
                {
                    northIndex.Add(testNode._id);
                    continue;
                }

                var ceilOId = cODict.Value.OutlineIndex;


                //var rayOutline = outlines[ceilOId]; // reference curve.
                var rayPt = outlineCentroids[ceilOId];
                var rayY = rayPt.Y;

                // get shadow area longest distance, including sundistance and Two distances from centroid to edge.
                var yLimit = rayY - sundist;
                // Means: below line y = yLimit , none of the outlines have chance the intersect with current outline.
                // get all the selected outlines.

                List<Curve> selected = new List<Curve>();
                List<int> selectedCurveIndices = new List<int>();
                for (int o = 0; o < outlineCentroids.Length; o++)
                {
                    var tempY = outlineCentroids[o].Y;
                    if (tempY >= yLimit && tempY < rayY)
                    {
                        // Means outline is within the Y_interval.
                        selected.Add(outlines[o]);
                        selectedCurveIndices.Add(o);
                    }
                }

                //  0.759218 is the radians value of 43.50' degree. 9:00~15:00.
                // Using outline's centroid as base point, shouting 36 rays with rayLength. Calculate the count of 
                // rays intersecting with current site.
                // Shouting one ray per 10 mins, lasting 6 hours in total.
                // ValidHours  = 6* (36-nd)/36;
                // shortest length means the closested distance between ray outline and selected outlines.
                var validHours = GetSunRayHours(selected.ToArray(), rayPt, rayLength, out double reduceLen);
                if (validHours < 3)
                {
                    var reduceFloors = (int)Math.Round(reduceLen / coe);
                    var totalFloors = BuildingFloors.Sum();
                    if (reduceFloors> totalFloors)
                    {
                        // Means deleting this building.
                        outlineFloors[testNode._id][0] = 0;
                        outlineFloors[testNode._id][1] = 0;
                        reducedFloorsSum += totalFloors;
                    }
                    else
                    {
                        if (BuildingFloors[1] > reduceFloors)
                        {
                            outlineFloors[testNode._id][1] = BuildingFloors[1] - reduceFloors;
                        }
                        else
                        {
                            var rest = reduceFloors - BuildingFloors[1];
                            outlineFloors[testNode._id][0] = BuildingFloors[0]-rest;
                            outlineFloors[testNode._id][1] = 0;
                        }
                        reducedFloorsSum += reduceFloors;
                    }
                }
            }

            // Step : corebarating area errors.
            var extraFloorsSum = (int)Math.Round(_areaError / (_width * _depth)) + reducedFloorsSum;
            var extraFloorsNorth = (int)Math.Round(extraFloorsSum * 1.0 / northIndex.Count);

            // Step 4: Generate geometries.
            for (int i = 0; i < BuildingCount; i++)
            {
                var outline = outlines[i];
                if (northIndex.Contains(i))
                {
                    outlineFloors[i][1] += extraFloorsNorth;
                }

                if (outlineFloors[i].Sum() == 0) // Means deleting this building.
                    continue;

                var bType = new BuildingType(_buildingTypeName, outlineFloors[i], _siteArea);
                var building = new BuildingGeometry(bType, _tolerance);
                building.GeneratingResidentialAloneStyle(outline);

                result.Add(building);
            }

            return result.ToArray();
        }


        /// <summary>
        /// Important method for calculating sun hours.
        /// </summary>
        /// <param name="testCurves"></param>
        /// <param name="basePt"></param>
        /// <param name="rayLength"></param>
        /// <param name="reduceLen">if sun hour is larger than 2 , return 0;  otherwise, return the lenght which need to be reduced for getting more sun rays</param>
        /// <returns></returns>
        private double GetSunRayHours(Curve[] testCurves, Point3d basePt, double rayLength, out double reduceLen)
        {
            //TODO: 如果时间小于标准， 求n个最大len长度， 里面的最小就是可以降低层数的地方。

            // 0.759218 is the radians value of 43.50' degree.
            // Shouting one ray per 10 mins, lasting 6 hours in total.
            // ValidHours  = 6*(36-nd)/36;
            var delta = -0.759218;
            reduceLen = 0;

            Vector3d toNorth = new Vector3d(0, -1, 0);
            var rayV = toNorth;
            rayV.Rotate(delta, Plane.WorldXY.ZAxis);

            int shotCount = 0;
            List<double> shotLens = new List<double>();

            for (int i = 0; i <= 36; i++)
            {
                if (i != 0)
                    rayV.Rotate(delta / 36, Plane.WorldXY.ZAxis);

                for (int t = 0; t < testCurves.Length; t++)
                {
                    var c = testCurves[t];
                    var len = DesignToolbox.GetDistance(c, rayV, basePt, _tolerance, rayLength);

                    if (len < rayLength)
                    {
                        shotCount++;
                        shotLens.Add(len);
                    }
                }
            }

            // 2 hours mean at least 12 sluts 13 rays are intact. 
            // 13- (37 - shotCount) = shotCount -24 (needed)
            var hours = 6 * (37 - shotCount) / 37d;
            if (hours < 2)
            {
                shotLens.Sort();
                var moreRays = shotCount - 24;
                var targetLen = shotLens[shotLens.Count - moreRays];
                reduceLen = rayLength - targetLen;
            }

            return hours;
        }



        #region May Obsolete
        private bool ComputeDotRowMajor(BuildingParameters bp, int tempFloor, out Curve[] centreLines, out int[] totalCount, out double depth, out double width, out int lastRowCount)
        {
            var edges = SiteBoundingRect.GetEdges(_site, _radiant);

            // 1.0 Getting parameters.
            var height = bp.FloorHeight * tempFloor;
            var sunDistance = BuildingDataset.GetSunlightDistance(height, _cityIndex);
            double setback = BuildingDataset.GetSetbackRtype(tempFloor) * 2.0;

            var loDepth = bp.Depth[0];
            var hiDepth = bp.Depth[1];
            depth = (loDepth + hiDepth) * 0.5;

            // From bottom to up: (depth+sunDist) / (depth+sunDist) / (depth+sunDist) / (depth+sunDist) / ....../(depth) 
            var lineCount = (int)Math.Round((edges[1].Length + sunDistance * Math.Cos(_radiant)) / (sunDistance * Math.Cos(_radiant) + depth / Math.Cos(_radiant)));  //Check

            // Handle the excpation where edge.length is smaller than depth.
            if (lineCount < 0)
            {
                centreLines = null;
                totalCount = null;
                width = 0;
                lastRowCount = 0;
                return false;
            }

            else
            {
                depth = Math.Round(((edges[1].Length + sunDistance * Math.Cos(_radiant)) / lineCount - sunDistance * Math.Cos(_radiant)) * Math.Cos(_radiant), 2); //Check

                if (depth > hiDepth)
                {
                    depth = hiDepth;
                    // Recalculate line count.
                    lineCount = (int)Math.Floor((edges[1].Length + sunDistance * Math.Cos(_radiant)) / (sunDistance * Math.Cos(_radiant) + depth / Math.Cos(_radiant)));
                }

                if (depth < loDepth)
                {
                    depth = loDepth;
                    // Recalculate line count.
                    lineCount = (int)Math.Floor((edges[1].Length + sunDistance * Math.Cos(_radiant)) / (sunDistance * Math.Cos(_radiant) + depth / Math.Cos(_radiant)));
                }

                width = Math.Round(bp.Area / depth, 2);

                // Lines' order is 0,1,2,3,4,5....  from bottom to up. Important.  
                centreLines = ParallelSplitV(_site, edges, lineCount, depth / Math.Cos(_radiant), sunDistance * Math.Cos(_radiant), _tolerance);

                // 1.2 Getting the total count of how many buildings each line can hold.
                totalCount = CalculateTotalCount1(centreLines, width, setback);
                lastRowCount = totalCount[totalCount.Length - 1];

                return true;
            }
        }


        private int[] CalculateTotalCount1(Curve[] centreLines, double width, double setback)
        {
            int[] result = new int[centreLines.Length];
            for (int i = 0; i < centreLines.Length; i++)
            {
                var lineLengh = centreLines[i].GetLength();

                result[i] = (int)Math.Floor((lineLengh + setback / Math.Cos(_radiant)) / (width / Math.Cos(_radiant) + setback / Math.Cos(_radiant)));
            }

            return result;
        }


        private BuildingGeometry[] GetBuildingGeometries1(int[] centreLineCount)
        {
            List<BuildingGeometry> result = new List<BuildingGeometry>();
            List<Curve> totalOutlines = new List<Curve>();
            IntervalTree intervalTree = new IntervalTree();
            List<IntervalNode> treeNodes = new List<IntervalNode>();

            // Calculating line by line.
            for (int i = 0; i < CentreLines.Length; i++)
            {
                // Some lines may be too short for fitting a building.
                if (centreLineCount[i] == 0)
                    continue;

                // Calculate the standard dash line.
                double splitLength;

                if (centreLineCount[i] == 1)
                {
                    splitLength = CentreLines[i].GetLength();
                }
                else
                {
                    splitLength = (CentreLines[i].GetLength() - _width / Math.Cos(_radiant)) / (centreLineCount[i] - 1);
                }


                double[] splitLengths = new double[centreLineCount[i]];
                for (int s = 0; s < splitLengths.Length; s++)
                {
                    if (s == splitLengths.Length - 1)
                    {
                        splitLengths[s] = _width / Math.Cos(_radiant);
                        break;
                    }
                    splitLengths[s] = splitLength;
                }


                // Split line by dash.
                var parameters = GetSplitParameters(splitLengths, CentreLines[i].Domain);
                Curve[] dashLines;


                if (parameters.Length == 0)
                {
                    dashLines = new Curve[] { CentreLines[i] };
                }
                else
                {
                    if (parameters.Length == 1)
                    {
                        dashLines = CentreLines[i].Split(parameters[0]);
                    }
                    else
                    {
                        dashLines = CentreLines[i].Split(parameters);
                    }
                }


                // Get outline for each dash line.
                var outlines = GetOutlineForDash1(dashLines, _width, _depth, 0);


                // For this style, the oritation of building is zero radiance.
                var totalWidth = _width * Math.Cos(Math.Abs(0)) + _depth * Math.Sin(Math.Abs(0));

                foreach (var outline in outlines)
                {
                    totalOutlines.Add(outline);

                    var pt = AreaMassProperties.Compute(outline).Centroid;
                    IntervalNode node = new IntervalNode(new UInterval(pt.X - 0.5 * totalWidth, pt.X + 0.5 * totalWidth), totalOutlines.Count - 1);
                    intervalTree.InsertNode(node);
                    treeNodes.Add(node);
                }
            }


            // areaError>0 ; areaError<0 ; areError = 0
            if (_areaError > 0)
            {
                // Create a new list for last row.
                List<Curve> lastRowOutlines = new List<Curve>();


                for (int n = 0; n < totalOutlines.Count; n++)
                {
                    var currentNode = treeNodes[n];
                    var overlapedNodes = intervalTree.SearchOverlaps(currentNode, intervalTree.Root);

                    // Have overlaped nodes includes the target node itself.
                    List<double> outlineCentreY = new List<double>();
                    foreach (var item in overlapedNodes)
                    {
                        if (item._id == n)
                            continue;
                        outlineCentreY.Add(GetCentroidY(totalOutlines[item._id]));
                    }

                    if (outlineCentreY.Count == 0)
                    {
                        // Find the last row outline.
                        lastRowOutlines.Add(totalOutlines[n]);
                        continue;
                    }


                    if (GetCentroidY(totalOutlines[n]) > outlineCentreY.Max())
                    {
                        // Find the last row outline.
                        lastRowOutlines.Add(totalOutlines[n]);
                        continue;
                    }
                    else
                    {
                        // Non-last row outlines.
                        //var breps = GetGeometries(_buildingTypeName, totalOutlines[n], BuildingFloors, out string[] functions, out Curve[] layers);
                        //result.Add(new BuildingGeometry(breps, functions, layers));
                        var bType = new BuildingType(_buildingTypeName, BuildingFloors, _siteArea);
                        var building = new BuildingGeometry(bType, _tolerance);
                        building.GeneratingResidentialAloneStyle(totalOutlines[n]);
                        result.Add(building);
                    }
                }

                // Find the last row items.
                var floorsCopy = new int[BuildingFloors.Length];
                BuildingFloors.CopyTo(floorsCopy, 0);

                var extraFloors = (int)Math.Round(_areaError / (_width * _depth * lastRowOutlines.Count));
                floorsCopy[1] += extraFloors;

                foreach (var lastOuline in lastRowOutlines)
                {
                    //var breps = GetGeometries(_buildingTypeName, lastOuline, floorsCopy, out string[] functions, out Curve[] layers);
                    //result.Add(new BuildingGeometry(breps, functions, layers));

                    var bType = new BuildingType(_buildingTypeName, floorsCopy, _siteArea);
                    var building = new BuildingGeometry(bType, _tolerance);
                    building.GeneratingResidentialAloneStyle(lastOuline);
                    result.Add(building);
                }
            }
            else
            {
                for (int n = 0; n < totalOutlines.Count; n++)
                {
                    //var breps = GetGeometries(_buildingTypeName, totalOutlines[n], BuildingFloors, out string[] functions, out Curve[] layers);
                    //result.Add(new BuildingGeometry(breps, functions, layers));

                    var bType = new BuildingType(_buildingTypeName, BuildingFloors, _siteArea);
                    var building = new BuildingGeometry(bType, _tolerance);
                    building.GeneratingResidentialAloneStyle(totalOutlines[n]);
                    result.Add(building);
                }
            }

            return result.ToArray();
        }


        private bool ComputeDotColumnMajor(BuildingParameters bp, int tempFloor, out Curve[] centreLines, out int[] totalCount, out double depth, out double width, out int lastRowCount)
        {
            var edges = SiteBoundingRect.GetEdges(_site, _radiant);

            // 1.0 Getting parameters.
            var height = bp.FloorHeight * tempFloor;
            var sunDistance = BuildingDataset.GetSunlightDistance(height, _cityIndex);
            // sunDistance is flowing  sourth to north direction, while corrected sun distance is flowing the direction of each central line.
            var correctedSunDist = sunDistance * Math.Cos(_radiant);
            double setback = BuildingDataset.GetSetbackRtype(tempFloor) * 2;

            var loDepth = bp.Depth[0];
            var hiDepth = bp.Depth[1];

            depth = (loDepth + hiDepth) * 0.5;
            width = Math.Round(bp.Area / depth, 2);

            //var wd = bp.Area / depth * Math.Cos(_radiant) + depth * Math.Abs(Math.Sin(_radiant));

            // (width + setback) * n < L
            // L = edges[0].length
            // In this method, lines are the columns.

            // Using Floor to make sure calculated distance is larger than setback distance.
            var lineCount = (int)Math.Floor(edges[0].Length / (width + setback));

            // Handle the excpation where edge.length is smaller than depth.
            if (lineCount < 0)
            {
                centreLines = null;
                totalCount = null;
                width = 0;
                lastRowCount = 0;
                return false;
            }
            else
            {
                // Recalculate width based on the given line count.
                // width = L/n - setback
                width = Math.Round(edges[0].Length / lineCount - setback, 2);
                depth = Math.Round(bp.Area / width, 2);

                if (depth > hiDepth)
                {
                    depth = hiDepth;
                    width = Math.Round(bp.Area / depth, 2);
                    // Recalculate line count.
                    lineCount = (int)Math.Floor(edges[0].Length / (width + setback));
                }

                if (depth < loDepth)
                {
                    depth = loDepth;
                    width = Math.Round(bp.Area / depth, 2);
                    // Recalculate line count.
                    lineCount = (int)Math.Floor(edges[0].Length / (width + setback));
                }


                // Lines' order is 0,1,2,3  from left to right.
                centreLines = ParallelSplitH(_site, edges, lineCount, width, setback, _tolerance);

                // 1.2 Getting the total count of how many buildings each line can hold.
                // And clean the  centrelines as well. 
                // If one line can hold no building, delete this line.
                totalCount = CalculateTotalCount2(ref centreLines, depth, correctedSunDist);
                lastRowCount = totalCount.Length;

                return true;
            }
        }



        private int[] CalculateTotalCount2(ref Curve[] centreLines, double depth, double correctedSunDist)
        {
            // Math.Cos(_radiant)*sunDistance:d ; depth: D ; count : n ; edges[1].Length : L ;
            // k is const representing the raito of sunDistance that the first row should be setback.
            // (2k +n-1) *d + n*D <= L
            // n = (L-(2k-1)d)/(D+d); $$$$$$$
            // D = (L-(2k-1)d)/n - d;
            // d = (L - n*D) / (2k+n-1);

            List<int> result = new List<int>(centreLines.Length);
            List<Curve> cResult = new List<Curve>(centreLines.Length);

            for (int i = 0; i < centreLines.Length; i++)
            {
                var lineLengh = centreLines[i].GetLength();

                // C represents the maxsium capacity of a line for holding buildings.
                // During the getbuildinggeometries stage, building count for each line may be less than c.
                // Therefore, we need to recalculate splitting distance d later.
                var c = (int)Math.Floor((lineLengh - (2 * _k - 1) * correctedSunDist) / (depth + correctedSunDist));

                if (c != 0)
                {
                    result.Add(c);
                    cResult.Add(centreLines[i]);
                }
            }

            // Handle exception 2: result may have no value, which will cause error. Select the middle curve and extend itself to hold builidng.
            // In this case, site only has one row of buildings.
            if (result.Sum() == 0)
            {
                var selectCurve = centreLines[centreLines.Length / 2];

                // In this case, only need to find the middle point of this middle line as the base point to generate one building.
                cResult.Add(selectCurve);
                result.Add(1);
            }

            centreLines = cResult.ToArray();
            return result.ToArray();
        }



        private BuildingGeometry[] GetBuildingGeometries2(int[] centreLineCount)
        {
            List<BuildingGeometry> result = new List<BuildingGeometry>();
            List<Curve> totalOutlines = new List<Curve>();
            IntervalTree intervalTree = new IntervalTree();
            List<IntervalNode> treeNodes = new List<IntervalNode>();

            // Get building height and correct sun distance.
            var bp = BuildingDataset.GetBuildingParameters(_buildingTypeName);
            var height = bp.FloorHeight * BuildingFloors[1];
            var sunDistance = BuildingDataset.GetSunlightDistance(height, _cityIndex);
            // sunDistance is flowing  sourth to north direction, while corrected sun distance is flowing the direction of each central line.
            var correctedSunDist = sunDistance * Math.Cos(_radiant);

            // Calculating line by line.
            for (int i = 0; i < CentreLines.Length; i++)
            {
                // Math.Cos(_radiant)*sunDistance:d ; depth: D ; count : n ; edges[1].Length : L ;
                // k is const representing the raito of sunDistance that the first row should be setback.
                // (2k +n-1) *d + n*D <= L
                // n = (L-(2k-1)d)/(D+d); 
                // D = (L-(2k-1)d)/n - d;
                // d = (L - n*D) / (2k+n-1); $$$$$$$ d will be different for each line.
                // But we want to keep the start and end position fixed, please use formula below:
                // correctSunDistance : s  , which is a constant.
                // splitting Distance : d , varies for each line.
                // d = (L-2ks-nD)/(n-1)


                // Calculate the splitting distance of current line.
                var currentLine = CentreLines[i];
                var lineLengh = currentLine.GetLength();


                double[] splitLengths = new double[centreLineCount[i] + 1];

                if (centreLineCount[i] == 1)
                {
                    splitLengths[0] = lineLengh * 0.5;
                    splitLengths[1] = lineLengh * 0.5;
                }
                else
                {
                    //var splittingDist = (lineLengh - centreLineCount[i] * _depth) / (2 * _k + centreLineCount[i] - 1);
                    var splittingDist = (lineLengh - centreLineCount[i] * _depth - 2 * _k * correctedSunDist) / (centreLineCount[i] - 1);

                    for (int s = 0; s < splitLengths.Length; s++)
                    {
                        if (s == 0 || s == splitLengths.Length - 1)
                        {
                            // using costant splitting distance for first and last rows.
                            splitLengths[s] = _k * correctedSunDist + 0.5 * _depth;
                            continue;
                        }

                        splitLengths[s] = splittingDist + _depth;
                    }
                }



                // Important: splitLengths.Length - parameter.Length = 1 .
                var parameters = GetSplitParameters(splitLengths, currentLine.Domain);
                Point3d[] basePts = new Point3d[parameters.Length];
                for (int p = 0; p < parameters.Length; p++)
                {
                    // Getting the base point which is the centroid of outline.
                    basePts[p] = currentLine.PointAt(parameters[p]);
                }

                // Get outline for each dash line. This method returns only for one line, while totalOutlines is a collection for all the lines.
                var outlines = GetOutlineForDash2(currentLine, basePts, _width, _depth, out Interval[] intervals);

                for (int o = 0; o < outlines.Length; o++)
                {
                    totalOutlines.Add(outlines[o]);
                    // Push this node into interval tree.
                    //var leftX = basePts[o].X - 0.5 * totalWidth;
                    //var rightX = basePts[o].X + 0.5 * totalWidth;
                    var leftX = intervals[o].Min;
                    var rightX = intervals[o].Max;


                    IntervalNode node = new IntervalNode(new UInterval(leftX, rightX), totalOutlines.Count - 1);
                    intervalTree.InsertNode(node);
                    treeNodes.Add(node);
                }
            }
            // Finishing getting all the outlines for each line. 
            // Then we need to get the "Last" row of building.

            // Due to the detial of outline may smaller than original rectangle, we need to calibrate the areaError.
            var outlineError = _width * _depth - AreaMassProperties.Compute(totalOutlines[0]).Area;
            _areaError += outlineError * BuildingCount * BuildingFloors[1];


            // areaError>0 ; areaError<0 ; areError = 0
            // Create a new list for last row. Buildings locating in the last row can be higher for reaching the total building area.
            List<Curve> lastOrFirstRowOutlines = new List<Curve>();

            for (int n = 0; n < totalOutlines.Count; n++)
            {
                // For each group.
                var currentOutline = totalOutlines[n];
                var currentNode = treeNodes[n];
                var overlapedNodes = intervalTree.SearchOverlaps(currentNode, intervalTree.Root);

                List<double> outlineCentreY = new List<double>();
                foreach (var item in overlapedNodes)
                {
                    // Overlaped nodes includes the target node itself.
                    if (item._id == n)
                        continue;

                    // Warning: item._id represents the outline.
                    outlineCentreY.Add(GetCentroidY(totalOutlines[item._id]));
                }

                if (outlineCentreY.Count == 0)
                {
                    // First case when current node is in last row.
                    // Find the last row outline.
                    lastOrFirstRowOutlines.Add(currentOutline);
                    continue;
                }

                // Sorting this list by using default comparer: 1,2,3,4,5,6
                outlineCentreY.Sort();

                int reverse = 1;
                if (_areaError <= 0)
                {
                    reverse *= -1;
                    outlineCentreY.Reverse();
                }


                if (GetCentroidY(currentOutline) * reverse > outlineCentreY[outlineCentreY.Count - 1] * reverse)
                {
                    // Second case when current node is in last row.
                    // Find the last row outline.
                    lastOrFirstRowOutlines.Add(currentOutline);
                    continue;
                }
                else
                {
                    // Rest rows outlines. It's redy for extruding.
                    var bType = new BuildingType(_buildingTypeName, BuildingFloors, _siteArea);
                    var building = new BuildingGeometry(bType, _tolerance);
                    building.GeneratingResidentialAloneStyle(currentOutline);
                    result.Add(building);
                }
            }


            // Find the special row items.
            var floorsCopy = new int[BuildingFloors.Length];
            BuildingFloors.CopyTo(floorsCopy, 0);

            var extraFloors = (int)Math.Round(_areaError / (_width * _depth * lastOrFirstRowOutlines.Count));
            // floors will be checked in the BuildingType struct. Do worry that floor[1] < 0 .
            floorsCopy[1] += extraFloors;

            foreach (var lastOuline in lastOrFirstRowOutlines)
            {
                var bType = new BuildingType(_buildingTypeName, floorsCopy, _siteArea);
                var building = new BuildingGeometry(bType, _tolerance);
                building.GeneratingResidentialAloneStyle(lastOuline);
                result.Add(building);
            }

            return result.ToArray();
        }
        #endregion May Obsolete

        #endregion


        #region Help methods for Generating Builidngs

        /// <summary>
        /// Total count may be less than the maxsium capacity. This method is for distributing buildings.
        /// </summary>
        /// <param name="totalCount"></param>
        /// <param name="typeCount"></param>
        /// <returns></returns>
        private int[] DistributingCount(int[] totalCount, int typeCount)
        {
            if (totalCount.Sum() < typeCount)
                return totalCount;

            int[] result = new int[totalCount.Length];
            for (int i = 0; i < totalCount.Length; i++)
            {
                var temp = Math.Round(totalCount[i] * 1.0 / totalCount.Sum() * typeCount, 0);

                // Handle exceptions.
                if (temp > totalCount[i] || temp == 0)
                    result[i] = totalCount[i];
                else
                    result[i] = (int)temp;
            }

            // Handle the error.
            int error = typeCount - result.Sum();
            if (error > 0)
            {
                for (int i = 0; i < result.Length; i++)
                {
                    if (result[i] < totalCount[i])
                    {
                        result[i]++;
                        error--;
                    }

                    if (error == 0)
                        break;
                }
                return result;
            }
            else if (error < 0)
            {
                while (error < 0)
                {
                    // Dynamically find the largest number and decrease itself.
                    var biggest = result.ToList().IndexOf(result.Max());
                    if (result[biggest] > 0)
                    {
                        // When all the item in result list are "1" , may yield zero for several sluts, 
                        // therefore cleaning up LineCount after is necessary.
                        result[biggest]--;
                        error++;
                    }
                }
                return result;
            }
            else
            {
                return result;
            }
        }


        /// <summary>
        /// Helper method for splitting line. If double[].length ==0, means there is no need for splitting.
        /// </summary>
        /// <param name="dash"></param>
        /// <returns></returns>
        private double[] GetSplitParameters(int[] dash, Interval domain)
        {
            double[] parameters = new double[dash.Length - 1];
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i == 0)
                {
                    parameters[i] = dash[i] * 1.0 / dash.Sum() * domain.Length + domain.Min;
                    continue;
                }

                parameters[i] = dash[i] * 1.0 / dash.Sum() * domain.Length + parameters[i - 1];
            }

            return parameters;
        }

        /// <summary>
        /// Using the length array to get the split parameters.
        /// </summary>
        /// <param name="splitLength"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
        private double[] GetSplitParameters(double[] splitLength, Interval domain)
        {
            double[] parameters = new double[splitLength.Length - 1];
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i == 0)
                {
                    parameters[i] = splitLength[i] / splitLength.Sum() * domain.Length + domain.Min;
                    continue;
                }

                parameters[i] = splitLength[i] / splitLength.Sum() * domain.Length + parameters[i - 1];
            }

            return parameters;
        }


        private void RandomAlter(ref int[] array, int iteration)
        {
            array.Shuffle();

            Random random = new Random();
            for (int i = 0; i < iteration; i++)
            {
                int n = random.Next(array.Length);
                int m = random.Next(array.Length);

                if (array[n] >= 4 || array[m] <= 2)
                    continue;

                array[n]++;
                array[m]--;
            }

            List<int> temp = new List<int>();
            int count = 0;

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != 1)
                {
                    temp.Add(array[i]);
                }
                else
                {
                    count++;
                }
            }

            while (count > 0)
            {
                if (temp.Count == 0)
                {
                    temp.Add(count);
                    break;
                }

                var min = temp.IndexOf(temp.Min());
                temp[min]++;
                count--;
            }

            array = temp.ToArray();
        }


        /// <summary>
        /// Help method for getting y coordinate for comparasion.
        /// </summary>
        /// <param name="x">Must be closed planar curve.</param>
        /// <returns></returns>
        private double GetCentroidY(Curve x)
        {
            var ptx = AreaMassProperties.Compute(x).Centroid;

            // Only need to compare the value of y coordinate of centroid.
            return ptx.Y;
        }


        /// <summary>
        /// Help method for getting the centroid of input curve.
        /// </summary>
        /// <param name="x">Must be closed planar curve.</param>
        /// <returns></returns>
        private Point3d GetCentroid(Curve x) => (AreaMassProperties.Compute(x).Centroid);



        private Curve[][] GetOutlineForDash0(Curve[] dashLines, int[] dash, double width, double depth, out Interval[][] intervals)
        {
            List<Curve[]> result = new List<Curve[]>(dashLines.Length);
            List<Interval[]> intervalResult = new List<Interval[]>(dashLines.Length);

            for (int i = 0; i < dashLines.Length; i++)
            {
                // v1 is the direction from left to right. v2 is the direction from bottom to up.
                Vector3d v1 = new Vector3d(dashLines[0].PointAtEnd - dashLines[0].PointAtStart);
                v1.Unitize();
                Vector3d v2 = new Vector3d(v1);
                v2.Rotate(Math.PI / 2.0, Plane.WorldXY.ZAxis);


                if (i % 2 == 0)
                {
                    // From left to right.
                    int index = i / 2;

                    Curve[] groupCurves = new Curve[dash[index]];
                    Interval[] groupIntervals = new Interval[dash[index]];

                    for (int d = 0; d < dash[index]; d++)
                    {
                        // i == 0 means the first outline adjacent to boundary.
                        var margin = (dashLines[index].GetLength() - width * dash[index]) / 2.0;
                        if (i == 0) margin = 0;

                        var centroid = dashLines[index].PointAtStart + v1 * (width * (d + 0.5) + margin);


                        Polyline outline = GetOutlineFromPts(centroid, width, depth, v1, v2, out Interval interval);
                        groupCurves[d] = outline.ToPolylineCurve();
                        groupIntervals[d] = interval;
                    }
                    result.Add(groupCurves);
                    intervalResult.Add(groupIntervals);
                }
                else
                {
                    // From right to left.
                    int index = dashLines.Length - (int)Math.Ceiling(i / 2.0);

                    Curve[] groupCurves = new Curve[dash[index]];
                    Interval[] groupIntervals = new Interval[dash[index]];

                    for (int d = 0; d < dash[index]; d++)
                    {
                        // i == 0 means the first outline adjacent to boundary.
                        var margin = (dashLines[index].GetLength() - width * dash[index]) / 2.0;
                        if (i == 1) margin = 0;

                        var centroid = dashLines[index].PointAtEnd - v1 * (width * (d + 0.5) + margin);

                        Polyline outline = GetOutlineFromPts(centroid, width, depth, v1, v2, out Interval interval);
                        //result.Add(outline.ToPolylineCurve());
                        groupCurves[d] = outline.ToPolylineCurve();
                        groupIntervals[d] = interval;
                    }
                    result.Add(groupCurves);
                    intervalResult.Add(groupIntervals);
                }
            }
            intervals = intervalResult.ToArray();

            return result.ToArray();
        }


        /// <summary>
        /// Method for creating the outline from point lists. 
        /// </summary>
        /// <param name="basePt"></param>
        /// <param name="width"></param>
        /// <param name="depth"></param>
        /// <param name="vLeftToRight"></param>
        /// <param name="vBottmToUp"></param>
        /// <returns></returns>
        private Polyline GetOutlineFromPts(Point3d centroid, double width, double depth, Vector3d vLeftToRight, Vector3d vBottmToUp, out Interval interval)
        {
            // Translate centroid to basePt.
            var basePt = centroid - vLeftToRight * width * 0.5;

            var pt0 = basePt - vBottmToUp * depth * 0.4;
            var pt1 = pt0 + vLeftToRight * width * 0.12;
            var pt2 = pt1 - vBottmToUp * depth * 0.1;
            var pt3 = pt2 + vLeftToRight * width * 0.76;
            var pt4 = pt3 + vBottmToUp * depth * 0.1;
            var pt5 = pt4 + vLeftToRight * width * 0.12;

            var pt6 = pt5 + vBottmToUp * depth * 0.9;
            var pt7 = pt6 - vLeftToRight * width;


            Polyline outline = new Polyline(new Point3d[] { pt0, pt1, pt2, pt3, pt4, pt5, pt6, pt7, pt0 });

            interval = new Interval(pt0.X, pt5.X);

            return outline;
        }



        /// <summary>
        /// Each dash line only has one outline. Base point the middle point of each dash.
        /// </summary>
        /// <param name="dashLines"></param>
        /// <param name="width"></param>
        /// <param name="depth"></param>
        /// <param name="radiant"></param>
        /// <returns></returns>
        private Curve[] GetOutlineForDash1(Curve[] dashLines, double width, double depth, double radiant)
        {
            List<Curve> result = new List<Curve>();

            for (int i = 0; i < dashLines.Length; i++)
            {
                // v1 is the direction from left to right. v2 is the direction from bottom to up.
                Vector3d v1 = Vector3d.XAxis;

                v1.Rotate(radiant, Vector3d.ZAxis);

                Vector3d v2 = new Vector3d(v1);
                v2.Rotate(Math.PI / 2.0, Vector3d.ZAxis);

                var basePt = dashLines[i].PointAtStart;
                basePt += Vector3d.YAxis * 0.5 * Math.Tan(_radiant) * width;

                var pt0 = basePt - v2 * depth * 0.5;
                var pt1 = pt0 + v1 * width;
                var pt2 = pt1 + v2 * depth;
                var pt3 = pt2 - v1 * width;

                Polyline outline = new Polyline(new Point3d[] { pt0, pt1, pt2, pt3, pt0 });
                result.Add(outline.ToPolylineCurve());
            }
            return result.ToArray();
        }


        /// <summary>
        /// Generating the outlines for ComputeDotColumnMajor case.
        /// </summary>
        /// <param name="currentLine"></param>
        /// <param name="basePts"></param>
        /// <param name="width"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        private Curve[] GetOutlineForDash2(Curve currentLine, Point3d[] basePts, double width, double depth, out Interval[] intervals)
        {
            List<Curve> result = new List<Curve>(basePts.Length);
            intervals = new Interval[basePts.Length];

            // Get v1 and v2.
            // v1 is the direction from left to right. v2 is the direction from bottom to up.
            Vector3d v2 = new Vector3d(currentLine.PointAtStart - currentLine.PointAtEnd);
            v2.Unitize();
            Vector3d v1 = new Vector3d(v2);
            v1.Rotate(-Math.PI / 2.0, Plane.WorldXY.ZAxis);

            for (int i = 0; i < basePts.Length; i++)
            {
                var basePt = basePts[i];

                Polyline outline = GetOutlineFromPts(basePt, width, depth, v1, v2, out Interval interval);
                result.Add(outline.ToPolylineCurve());
                intervals[i] = interval;
            }

            return result.ToArray();
        }

        #endregion


        #region Help methods for ResidentialSolver

        /// <summary>
        /// Method to make sure radiant is within -30~ 30.
        /// </summary>
        /// <param name="radiant"></param>
        private static void ValidatingRadiant(ref double radiant)
        {
            if (Math.Abs(radiant) > Math.PI / 2)
            {
                var remin = radiant % (Math.PI / 2);

                radiant = remin;
            }

            if (radiant > Math.PI / 4)
            {
                radiant -= Math.PI / 2;
            }

            if (radiant < -Math.PI / 4)
            {
                radiant += Math.PI / 2;
            }

            if (radiant > Math.PI / 6 || radiant < -Math.PI / 6)
            {
                radiant = radiant / Math.Abs(radiant) * Math.PI / 6;
            }
        }



        /// <summary>
        /// Spliting current site into sevevarl central lines in the bottom-up order vertically .
        /// </summary>
        /// <param name="edges"></param>
        /// <param name="count"></param>
        /// <param name="depth"></param>
        /// <param name="correctedSunDist"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        private static Curve[] ParallelSplitV(Curve curve, Line[] edges, int count, double depth, double correctedSunDist, double tolerance)
        {
            // Handle exception.
            count = count < 1 ? 1 : count;

            var brep = Brep.CreatePlanarBreps(curve, tolerance)[0];

            // Cut the edges[1] and edges[3]
            List<Curve> tempList = new List<Curve>(count);
            Curve baseline = edges[0].ToNurbsCurve().DuplicateCurve();
            var vector = edges[1].Direction;
            vector.Unitize();
            var len = edges[1].Length;

            // input correctedSunDistance: sd ; calculated distance :d ; depth: D ; count : n ; edges[1].Length : L
            // (2k +n-1) *d + n*D <= L
            // d >= sd
            // Therefore for k: kmax = (L-nD-(n-1)sd)/2sd;
            // if kmax< _k ; _k == kmax;
            // Then we need to calculate d based on new k;
            // d = (L - n*D) / (2k+n-1);

            var kmax = (len - count * depth - (count - 1) * correctedSunDist) / (2 * correctedSunDist);
            var k = _k > kmax ? kmax : _k;
            //var k = _k;

            //TODO: check density.

            var d = (len - count * depth) / (2 * k + count - 1);


            if (count == 1)
            {
                baseline.Translate(vector * 0.5 * len);
            }
            else
            {
                // d should be larger than sunDistance input. 
                //d = d < sunDistance ? sunDistance : d;

                baseline.Translate(vector * (0.5 * depth + k * d));
            }

            // Lines' order is bottom-up.
            for (int i = 0; i < count; i++)
            {
                var temp = baseline.DuplicateCurve();

                temp.Translate(vector * (depth + d) * i);
                Intersection.CurveBrep(temp, brep, tolerance, out Curve[] overlape, out _);

                // move overlape curve up and down.
                // For L-shape or U-shape site, overlape for one test line may yield more than one segment.
                if (overlape.Length != 0)
                {
                    for (int o = 0; o < overlape.Length; o++)
                    {
                        var tempTest = overlape[o].DuplicateCurve();
                        tempTest.Translate(vector * 0.5 * depth);
                        var flag1 = Intersection.CurveBrep(tempTest, brep, tolerance, out Curve[] upperCurves, out _);
                        tempTest.Translate(vector * -1 * depth);
                        var flag2 = Intersection.CurveBrep(tempTest, brep, tolerance, out Curve[] downCurves, out _);

                        if (!flag1 || !flag2) continue;

                        // Important.
                        // not only consider center line, also bring the thickness of a building into consideration.
                        foreach (var uCurve in upperCurves)
                        {
                            temp.ClosestPoint(uCurve.PointAtStart, out double ut0);
                            Point3d uStart = temp.PointAt(ut0);

                            temp.ClosestPoint(uCurve.PointAtEnd, out double ut1);
                            Point3d uEnd = temp.PointAt(ut1);

                            foreach (var dCurve in downCurves)
                            {
                                temp.ClosestPoint(dCurve.PointAtStart, out double dt0);
                                Point3d dStart = temp.PointAt(dt0);

                                temp.ClosestPoint(dCurve.PointAtEnd, out double dt1);
                                Point3d dEnd = temp.PointAt(dt1);

                                // Compare upper and down
                                var pStart = uStart.X > dStart.X ? uStart : dStart;
                                var pEnd = uEnd.X < dEnd.X ? uEnd : dEnd;

                                // Compare p and temp
                                pStart = pStart.X > temp.PointAtStart.X ? pStart : temp.PointAtStart;
                                pEnd = pEnd.X < temp.PointAtEnd.X ? pEnd : temp.PointAtEnd;

                                tempList.Add(new PolylineCurve(new Point3d[] { pStart, pEnd }));
                            }
                        }
                    }
                }

            }

            return tempList.ToArray();
        }


        private static Curve[] ParallelSplitH(Curve curve, Line[] edges, int count, double width, double setback, double tolerance)
        {
            // Handle exception.
            count = count < 1 ? 1 : count;

            var brep = Brep.CreatePlanarBreps(curve, tolerance)[0];

            // Cut the edges[0] and edges[2]
            List<Curve> tempList = new List<Curve>(count);

            // Important: baseline direction is from up to bottom.
            Curve baseline = edges[3].ToNurbsCurve().DuplicateCurve(); // startpoint is higher than endpoint.
            var vector = edges[0].Direction;
            vector.Unitize();
            var len = edges[0].Length;

            var d = width + setback;


            if (count == 1)
            {
                baseline.Translate(vector * 0.5 * len);
            }
            else
            {
                baseline.Translate(vector * (0.5 * d));
            }

            // Lines' order is left to right.
            for (int i = 0; i < count; i++)
            {
                var temp = baseline.DuplicateCurve();

                temp.Translate(vector * d * i);
                Intersection.CurveBrep(temp, brep, tolerance, out Curve[] overlape, out _);

                // move overlape curve left and right.
                // For L-shape or U-shape site, overlape for one test line may yield more than one segment.
                if (overlape.Length != 0)
                {
                    for (int o = 0; o < overlape.Length; o++)
                    {
                        var tempTest = overlape[o].DuplicateCurve();
                        tempTest.Translate(vector * 0.5 * width);
                        var flag1 = Intersection.CurveBrep(tempTest, brep, tolerance, out Curve[] rightCurves, out _);
                        tempTest.Translate(vector * -1 * width);
                        var flag2 = Intersection.CurveBrep(tempTest, brep, tolerance, out Curve[] leftCurves, out _);

                        if (!flag1 || !flag2) continue;

                        // Important.
                        // not only consider center line, also bring the thickness of a building into consideration.
                        foreach (var rCurve in rightCurves)
                        {
                            temp.ClosestPoint(rCurve.PointAtStart, out double ut0);
                            Point3d rStart = temp.PointAt(ut0);

                            temp.ClosestPoint(rCurve.PointAtEnd, out double ut1);
                            Point3d rEnd = temp.PointAt(ut1);

                            foreach (var lCurve in leftCurves)
                            {
                                temp.ClosestPoint(lCurve.PointAtStart, out double dt0);
                                Point3d lStart = temp.PointAt(dt0);

                                temp.ClosestPoint(lCurve.PointAtEnd, out double dt1);
                                Point3d lEnd = temp.PointAt(dt1);

                                // Compare upper and down
                                var pStart = rStart.Y < lStart.Y ? rStart : lStart;
                                var pEnd = rEnd.Y > lEnd.Y ? rEnd : lEnd;

                                // Compare p and temp, actually there is no need doing this comparison.
                                //pStart = pStart.X > temp.PointAtStart.X ? pStart : temp.PointAtStart;
                                //pEnd = pEnd.X < temp.PointAtEnd.X ? pEnd : temp.PointAtEnd;

                                tempList.Add(new PolylineCurve(new Point3d[] { pStart, pEnd }));
                            }
                        }
                    }
                }

            }

            return tempList.ToArray();
        }

        [Obsolete]
        private static Curve[] ParallelSplit(Curve curve, Line[] edges, int count, double tolerance)
        {
            var brep = Brep.CreatePlanarBreps(curve, tolerance)[0];

            // Cut the edges[1] and edges[3]
            List<Curve> tempList = new List<Curve>(count);
            Curve baseline = edges[0].ToNurbsCurve().DuplicateCurve();

            var vector = edges[1].Direction;
            vector.Unitize();

            var distance = edges[1].Length / count;

            baseline.Translate(vector * 0.5 * distance);

            // Lines' order is bottom-up.
            for (int i = 0; i < count; i++)
            {
                var temp = baseline.DuplicateCurve();

                temp.Translate(vector * distance * i);
                Intersection.CurveBrep(temp, brep, tolerance, out Curve[] overlape, out Point3d[] pts);

                // For L-shape or U-shape site, overlape for one test line may yield more than one segment.
                if (overlape.Length != 0)
                    tempList.AddRange(overlape);
            }

            // Change the order of centrelines: eg. 6 lines, 0,5,1,4,2,3 . 
            var result = new Curve[tempList.Count];
            for (int i = 0; i < tempList.Count; i++)
            {
                if (i % 2 == 0)
                {
                    result[i] = tempList[i / 2];
                }
                else
                {
                    var index = tempList.Count - (int)Math.Ceiling(i / 2.0);
                    result[i] = tempList[index];
                }
            }

            return result;
        }




        public void Dispose()
        {
            _site.Dispose();
            BuildingGeometries = null;
            SetBack.Dispose();
            BuildingFloors = null;
            CentreLines = null;
        }


        /// <summary>
        /// Comparer used for finding the same outline in the column major style.
        /// </summary>
        private class OutlineEqualityComparer : EqualityComparer<Curve>
        {
            public override bool Equals(Curve x, Curve y)
            {
                return AreaMassProperties.Compute(x).Centroid == AreaMassProperties.Compute(y).Centroid;
            }

            public override int GetHashCode(Curve obj)
            {
                var pt = AreaMassProperties.Compute(obj).Centroid;
                return pt.X.GetHashCode() ^ pt.Y.GetHashCode();
            }
        }

        private struct OutlineYDict : IComparable<OutlineYDict>
        {
            public double YCoodinate { get; set; }

            public int OutlineIndex { get; }

            public OutlineYDict(double YValue, int index)
            {
                YCoodinate = YValue;
                OutlineIndex = index;
            }

            public int CompareTo(OutlineYDict other)
            {
                return this.YCoodinate.CompareTo(other.YCoodinate);
            }
        }

        #endregion
    }
}
