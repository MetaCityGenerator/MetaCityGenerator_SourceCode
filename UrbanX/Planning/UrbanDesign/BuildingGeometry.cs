using Rhino.Geometry;
using Rhino.Geometry.Intersect;

using System;
using System.Collections.Generic;
using System.Linq;

namespace UrbanX.Planning.UrbanDesign
{
    public class BuildingGeometry : IDisposable
    {
        private readonly double _tolerance;

        private BuildingType _buildingType;


        // Collections below have same item count( Breps' count).
        /// <summary>
        /// All the breps in this BuildingGeometry, using in Rhino display.
        /// </summary>
        public Brep[] Breps { get; private set; }



        /// <summary>
        /// Residential\Hotel\Office\Commercial\Manufactory\Warehouse.
        /// </summary>
        public string[] BrepFunctions { get; private set; }



        /// <summary>
        /// Outlines for all the breps in this BuildingGeometry.
        /// </summary>
        public Curve[] BrepOutlines { get; private set; }



        /// <summary>
        /// Heights for all the breps in this BuildingGeometry.
        /// </summary>
        public double[] BrepHeights { get; private set; }


        /// <summary>
        /// Floor counts for all the breps in this BuildingGeometry.
        /// </summary>
        public int[] BrepFloors { get; private set; }



        /// <summary>
        /// Total building areas for all the breps in this BuildingGeometry.
        /// </summary>
        public double[] BrepAreas { get; private set; }



        // Collection below has same item count( Layers' count).
        /// <summary>
        /// Layer curves representing each floor.
        /// </summary>
        public Curve[] Layers { get; private set; }


        // Collections below have same item count( RoofCurves' count).
        /// <summary>
        /// All the roof curves in this BuildingGeometry.
        /// </summary>
        public Curve[] RoofCurves { get; private set; }

        /// <summary>
        /// Total floor count of current building. Floors' count = RoofCurves' count.
        /// </summary>
        public int[] Floors { get; private set; }


        /// <summary>
        /// Total foot print area of all the buildings in this BuildingGeometry.
        /// </summary>
        public double FootPrintArea { get; private set; }

        /// <summary>
        /// Total building area for all the floors in this BuildingGeometry.
        /// </summary>
        public double BuildingArea { get; private set; }



        /// <summary>
        /// Constructor for BuildingGeometry.
        /// </summary>
        /// <param name="buildingType"></param>
        /// <param name="tolerance"></param>
        public BuildingGeometry(BuildingType buildingType, double tolerance)
        {
            //_tolerance = tolerance * 1E-3 < 1E-8 ? 1E-8 : tolerance * 1E-3;
            _tolerance = tolerance;
            _buildingType = buildingType;
        }


        #region Method for Generating building geometries for different styles.
        public void GeneratingResidentialAloneStyle(Curve outline)
        {
            Breps = GetBreps(outline, _buildingType, out string[] brepFunctions, out Curve[] brepOutlines, out double[] brepHeights, out int[] brepFloors, out double[] brepAreas, out Curve[] layers, out Curve roofOutline, out double footPrintArea, out double totalBrepsArea);
            if (Breps.Length != 0)
            {
                BrepFunctions = brepFunctions;
                BrepOutlines = brepOutlines;
                BrepHeights = brepHeights;
                BrepFloors = brepFloors;
                BrepAreas = brepAreas;

                Layers = layers;

                RoofCurves = new Curve[] { roofOutline };
                Floors = new int[] { layers.Length };

                FootPrintArea = footPrintArea;
                BuildingArea = totalBrepsArea;
            }
        }


        public void GeneratingNonResidentialAloneStyle(Curve setback, double radiant)
        {
            // site may be way to small or flat to get a setback curve.
            if (setback != null)
            {
                var basePlane = GetPlane(DesignToolbox.GetCurveCentre(setback, _tolerance), radiant);
                // When Rect: Y>=X, rotate base plane.
                RotatePlane(ref basePlane, setback);

                // Translate the basePlane to the center of setback curve.
                //basePlane.Origin = SiteMainMethods.GetCurveCentre(setback, _tolerance);

                // Outline should be ccw for extruding.
                var outline = GetOutline(_buildingType, basePlane, setback);
                if (outline.ClosedCurveOrientation() == CurveOrientation.Clockwise)
                    outline.Reverse();

                EliminateError(ref _buildingType, outline);

                Breps = GetBreps(outline, _buildingType, out string[] brepFunctions, out Curve[] brepOutlines, out double[] brepHeights, out int[] brepFloors, out double[] brepAreas, out Curve[] layers, out Curve roofOutline, out double footPrintArea, out double totalBrepsArea);
                if (Breps.Length != 0)
                {
                    BrepFunctions = brepFunctions;
                    BrepOutlines = brepOutlines;
                    BrepHeights = brepHeights;
                    BrepFloors = brepFloors;
                    BrepAreas = brepAreas;

                    Layers = layers;

                    RoofCurves = new Curve[] { roofOutline };
                    Floors = new int[] { layers.Length };

                    FootPrintArea = footPrintArea;
                    BuildingArea = totalBrepsArea;
                }
            }
        }



        public void GeneratingNonResidentialGroupStyle(Curve lo, Curve li, Curve setback)
        {
            // outlines should be input.
            var outlines = GetGroupOutlines(lo, li, setback);

            // Create collections for properties.
            List<Brep> brepsList = new List<Brep>();
            List<string> brepFunctionsList = new List<string>();
            List<Curve> brepOutlinesList = new List<Curve>();
            List<double> brepHeightsList = new List<double>();
            List<int> brepFloorsList = new List<int>();
            List<double> brepAreasList = new List<double>();

            List<Curve> layerlist = new List<Curve>();

            List<Curve> roofOutlinesList = new List<Curve>();
            List<int> floorsList = new List<int>();

            double totalFootPrintAreas = 0;
            double totalBuildingAreas = 0;

            // TargetTotalbuilding area.
            for (int i = 0; i < outlines.Length; i++)
            {
                var outline = outlines[i];

                brepsList.AddRange(GetBreps(outline, _buildingType, out string[] brepFunctions, out Curve[] brepOutlines, out double[] brepHeights, out int[] brepFloors, out double[] brepAreas, out Curve[] layers, out Curve roofOutline, out double footPrintArea, out double totalBrepsArea));
                brepFunctionsList.AddRange(brepFunctions);
                brepOutlinesList.AddRange(brepOutlines);
                brepHeightsList.AddRange(brepHeights);
                brepFloorsList.AddRange(brepFloors);
                brepAreasList.AddRange(brepAreas);

                layerlist.AddRange(layers);

                roofOutlinesList.Add(roofOutline);
                floorsList.Add(layers.Length);

                totalFootPrintAreas += footPrintArea;
                totalBuildingAreas += totalBrepsArea;
            }

            Breps = brepsList.ToArray();
            BrepFunctions = brepFunctionsList.ToArray();
            BrepOutlines = brepOutlinesList.ToArray();
            BrepHeights = brepHeightsList.ToArray();
            BrepFloors = brepFloorsList.ToArray();
            BrepAreas = brepAreasList.ToArray();

            Layers = layerlist.ToArray();

            RoofCurves = roofOutlinesList.ToArray();
            Floors = floorsList.ToArray();

            FootPrintArea = totalFootPrintAreas;
            BuildingArea = totalBuildingAreas;
        }


        public void GeneratingNonResidentialMixedStyle(Curve lo, Curve li, Curve setback, Curve towerOutline, double targetTotalBuildingArea)
        {
            // outlines should be input.
            var outlines = GetGroupOutlines(lo, li, setback);

            // Create collections for properties.
            List<Brep> brepsList = new List<Brep>();
            List<string> brepFunctionsList = new List<string>();
            List<Curve> brepOutlinesList = new List<Curve>();
            List<double> brepHeightsList = new List<double>();
            List<int> brepFloorsList = new List<int>();
            List<double> brepAreasList = new List<double>();

            List<Curve> layerlist = new List<Curve>();

            List<Curve> roofOutlinesList = new List<Curve>();
            List<int> floorsList = new List<int>();

            double totalFootPrintAreas = 0;
            double totalBuildingAreas = 0;


            GetMixStyleGeometries(_buildingType, outlines, towerOutline, targetTotalBuildingArea, ref brepsList, ref brepFunctionsList, ref brepOutlinesList, ref brepHeightsList, ref brepFloorsList, ref brepAreasList,
                    ref layerlist, ref roofOutlinesList, ref floorsList, ref totalFootPrintAreas, ref totalBuildingAreas);

            Breps = brepsList.ToArray();
            BrepFunctions = brepFunctionsList.ToArray();
            BrepOutlines = brepOutlinesList.ToArray();
            BrepHeights = brepHeightsList.ToArray();
            BrepFloors = brepFloorsList.ToArray();
            BrepAreas = brepAreasList.ToArray();

            Layers = layerlist.ToArray();

            RoofCurves = roofOutlinesList.ToArray();
            Floors = floorsList.ToArray();

            FootPrintArea = totalFootPrintAreas;
            BuildingArea = totalBuildingAreas;
        }

        #endregion

        private void GetMixStyleGeometries(BuildingType buildingType, Curve[] outlines, Curve towerOutline, double totalBuildingArea,
            ref List<Brep> brepsList, ref List<string> brepFunctionsList, ref List<Curve> brepOutlinesList, ref List<double> brepHeightsList, ref List<int> brepFloorsList, ref List<double> brepAreasList, ref List<Curve> layerlist, ref List<Curve> roofOutlinesList, ref List<int> floorsList,
            ref double totalFootPrintAreas, ref double totalBuildingAreas)
        {
            // For mixed style, need to find the difference to the towerOutline.
            List<Curve> outlinesUpdate = new List<Curve>();
            double towerArea = AreaMassProperties.Compute(towerOutline).Area;
            double bottomArea = 0; // Area for boolean difference area.

            foreach (var outline in outlines)
            {
                var outlineArea = AreaMassProperties.Compute(outline).Area;

                var temps = Curve.CreateBooleanDifference(outline, towerOutline, _tolerance);
                if (temps != null)
                {
                    foreach (var temp in temps)
                    {
                        var tempArea = AreaMassProperties.Compute(temp).Area;

                        if (tempArea > 0.12 * outlineArea)
                        {
                            outlinesUpdate.Add(temp);
                            bottomArea += tempArea;
                        }
                    }
                }
            }

            // Getting tower floor and bottom(group) floor.
            Random random = new Random();
            int bottomFloor = random.Next(2, 5);
            bottomFloor = bottomFloor > buildingType.Floors.Sum() ? buildingType.Floors.Sum() : bottomFloor;

            double towerTotalArea = totalBuildingArea - bottomArea * bottomFloor;
            int towerFloor = (int)Math.Round(towerTotalArea / towerArea);

            // Calculating the mixuse. There are two scinarios: bottom covered all the mixuse; bottom + part of tower building to cover the mixuse area.
            double mixArea = totalBuildingArea * (buildingType.Floors[0] * 1.0 / buildingType.Floors.Sum());
            BuildingType bottomType, towerBottomType, towerUpperType;

            if (bottomArea * bottomFloor > mixArea)
            {
                // bottom group building has coverd the mixuse area. 
                // In this case : BuildingRoofs {tower , group} ; BuildingFloors {towerFloor , bottomFloor} ; totalFootPrintAreas + groupOutline + towerOutline.

                int mixBottomFloor = (int)Math.Round(mixArea / bottomArea);
                mixBottomFloor = mixBottomFloor > bottomFloor ? bottomFloor : mixBottomFloor;

                bottomType = new BuildingType(buildingType.TypeName, new int[] { mixBottomFloor, bottomFloor - mixBottomFloor }, buildingType._siteArea);
                towerBottomType = new BuildingType(buildingType.TypeName, new int[] { 0, bottomFloor }, buildingType._siteArea);
                towerUpperType = new BuildingType(buildingType.TypeName, new int[] { 0, towerFloor - bottomFloor }, buildingType._siteArea);
            }
            else
            {
                // bottom group builiding can't cover all the mixuse area, so we need several floors from tower to cover mix use.
                // In this case : BuildingRoofs {tower , group} ; BuildingFloors {towerFloor , bottomFloor} ; totalFootPrintAreas + groupOutline + towerOutline.

                double mixTowerArea = mixArea - bottomArea * bottomFloor;
                int mixTowerFloor = (int)Math.Round(mixTowerArea / (towerArea));

                // Create two sitebuilding types for bottom and tower.
                bottomType = new BuildingType(buildingType.TypeName, new int[] { bottomFloor, 0 }, buildingType._siteArea);

                if (mixTowerFloor < bottomFloor)
                {
                    towerBottomType = new BuildingType(buildingType.TypeName, new int[] { mixTowerFloor, bottomFloor - mixTowerFloor }, buildingType._siteArea);
                    towerUpperType = new BuildingType(buildingType.TypeName, new int[] { 0, towerFloor - bottomFloor }, buildingType._siteArea);
                }
                else
                {
                    towerBottomType = new BuildingType(buildingType.TypeName, new int[] { bottomFloor, 0 }, buildingType._siteArea);
                    towerUpperType = new BuildingType(buildingType.TypeName, new int[] { mixTowerFloor - bottomFloor, towerFloor - mixTowerFloor }, buildingType._siteArea);
                }
            }

            // Add bottom breps. Bottom brep has higher height.
            foreach (var outline in outlinesUpdate)
            {

                brepsList.AddRange(GetBreps(outline, bottomType, out string[] brepFunctions, out Curve[] brepOutlines, out double[] brepHeights, out int[] brepFloors, out double[] brepAreas,
                    out Curve[] layers, out Curve roofOutline, out double footPrintArea, out double totalBrepsArea, true));

                brepFunctionsList.AddRange(brepFunctions);
                brepOutlinesList.AddRange(brepOutlines);
                brepHeightsList.AddRange(brepHeights);
                brepFloorsList.AddRange(brepFloors);
                brepAreasList.AddRange(brepAreas);

                layerlist.AddRange(layers);

                roofOutlinesList.Add(roofOutline);
                floorsList.Add(layers.Length);

                totalFootPrintAreas += footPrintArea;
                totalBuildingAreas += totalBrepsArea;
            }

            // Add tower bottom breps.
            // Handle exception when buildingType floors.sum <=0.
            if (towerBottomType.Floors.Sum() > 0)
            {
                brepsList.AddRange(GetBreps(towerOutline, towerBottomType, out string[] brepFunctionsBottom, out Curve[] brepOutlinesBottom, out double[] brepHeightsBottom, out int[] brepFloorsBottom, out double[] brepAreasBottom,
                    out Curve[] layersBottom, out Curve roofOutlineBottom, out double footPrintAreaBottom, out double totalBrepsAreaBottom, true));

                brepFunctionsList.AddRange(brepFunctionsBottom);
                brepOutlinesList.AddRange(brepOutlinesBottom);
                brepHeightsList.AddRange(brepHeightsBottom);
                brepFloorsList.AddRange(brepFloorsBottom);
                brepAreasList.AddRange(brepAreasBottom);

                layerlist.AddRange(layersBottom);

                //roofOutlinesList.Add(roofOutline1);
                //floorsList.Add(layers1.Length);

                // Add bottom foot print area.
                totalFootPrintAreas += footPrintAreaBottom;
                totalBuildingAreas += totalBrepsAreaBottom;

                if (towerUpperType.Floors.Sum() > 0)
                {
                    // Add tower upper breps.
                    brepsList.AddRange(GetBreps(roofOutlineBottom, towerUpperType, out string[] brepFunctionsUpper, out Curve[] brepOutlinesUpper, out double[] brepHeightsUpper, out int[] brepFloorsUpper, out double[] brepAreasUpper,
                            out Curve[] layersUpper, out Curve roofOutlineUpper, out _, out double totalBrepsAreaUpper));

                    brepFunctionsList.AddRange(brepFunctionsUpper);
                    brepOutlinesList.AddRange(brepOutlinesUpper);
                    brepHeightsList.AddRange(brepHeightsUpper);
                    brepFloorsList.AddRange(brepFloorsUpper);
                    brepAreasList.AddRange(brepAreasUpper);

                    layerlist.AddRange(layersUpper);

                    roofOutlinesList.Add(roofOutlineUpper);
                    floorsList.Add(towerFloor);

                    totalBuildingAreas += totalBrepsAreaUpper;
                }
                else
                {
                    // No uuper tower. Using bottom tower roof as building roof.
                    roofOutlinesList.Add(roofOutlineBottom);
                    floorsList.Add(layersBottom.Length);
                }
            }
        }


        private Plane GetPlane(Point3d center, double radiant)
        {
            Vector3d x_axis = Plane.WorldXY.XAxis;
            Vector3d y_axis = Plane.WorldXY.YAxis;

            x_axis.Rotate(radiant, Plane.WorldXY.ZAxis);
            y_axis.Rotate(radiant, Plane.WorldXY.ZAxis);

            Plane result = new Plane(center, x_axis, y_axis);

            return result;
        }

        private void RotatePlane(ref Plane basePlane, Curve curve)
        {
            var transform = Transform.ChangeBasis(Plane.WorldXY, basePlane);
            var box = curve.GetBoundingBox(transform);
            // This bounding box is only for comparing depth and width, therefore changing the coordinate to locate box is not needed.

            var xdelta = box.Max.X - box.Min.X;
            var ydelta = box.Max.Y - box.Min.Y;

            if (ydelta > xdelta)
                basePlane.Rotate(Math.PI / 2, Plane.WorldXY.ZAxis);
        }


        private Curve[] GetGroupOutlines(Curve lo, Curve li, Curve setback)
        {
            List<Curve> result = new List<Curve>();
            var temp = Curve.CreateBooleanIntersection(setback, lo, _tolerance);

            for (int i = 0; i < temp.Length; i++)
            {
                var outlines = Curve.CreateBooleanDifference(temp[i], li, _tolerance);
                foreach (var outline in outlines)
                {
                    if (AreaMassProperties.Compute(outline).Area > 60)
                    {
                        result.Add(outline);
                    }
                }
            }
            return result.ToArray();
        }



        private Curve GetOutline(BuildingType buildingType, Plane basePlane, Curve setback)
        {
            // Get initial outline based on the current parameters.
            double area = buildingType.Area;

            // Area of building is smaller than the setback curve.
            double minDepth = buildingType.Parameters.Depth[0];
            double maxDepth = buildingType.Parameters.Depth[1];
            double minWidth = Math.Round(area / maxDepth, 3);

            // Choose the stype of rectangle : flatten style ; dot style.
            var outline = GetRectangle(minWidth, maxDepth, basePlane);

            // For specific situation where the area of building is larger than setback area, the largest boolean intersection can only be the setback curve itself.

            // Check if this outline feasible.
            var inside = Curve.PlanarClosedCurveRelationship(outline, setback, basePlane, _tolerance);

            if (inside == RegionContainment.AInsideB)
                return outline;
            else if (inside == RegionContainment.Disjoint)
                return setback;
            else
            {
                // 0. Containers for recording the information for each alteration.
                var areas = new List<double>();
                var booleans = new List<Curve>();
                var outlines = new List<Curve>();

                // 1. Fit outline into feasible region by altering the shape of outline
                double stepCount = 6;
                double dStep = (maxDepth - minDepth) / stepCount;

                for (int i = 1; i <= stepCount; i++)
                {
                    var tempDepth = maxDepth - dStep * i;
                    var tempWidth = Math.Round(area / tempDepth, 3);
                    var tempOutline = GetRectangle(tempWidth, tempDepth, basePlane);

                    var inside1 = Curve.PlanarClosedCurveRelationship(tempOutline, setback, basePlane, _tolerance);
                    if (inside1 == RegionContainment.Disjoint)
                        continue;
                    else if (inside1 == RegionContainment.AInsideB)
                        return tempOutline;

                    else
                    {
                        var cs = Curve.CreateBooleanIntersection(tempOutline, setback, _tolerance);

                        var tempBoolen = cs.First();
                        var tempArea = AreaMassProperties.Compute(tempBoolen).Area;
                        areas.Add(tempArea);
                        booleans.Add(tempBoolen);
                        outlines.Add(tempOutline);
                    }
                }

                Curve maxOutine;
                if (areas.Count == 0)
                {
                    maxOutine = outline;
                }
                else
                {
                    int max1 = areas.IndexOf(areas.Max());
                    maxOutine = outlines[max1];
                }

                // 2. Translate plane to fit outline.
                var tryFit = Intersection.CurveCurve(maxOutine, setback, _tolerance, _tolerance);

                Vector3d vector = new Vector3d();
                for (int i = 0; i < tryFit.Count; i++)
                {
                    var pt = tryFit[i].PointA;
                    Vector3d v = new Vector3d(basePlane.Origin - pt);

                    // Must unitize vector for getting bivector.
                    v.Unitize();
                    vector += v;
                    vector.Unitize();
                }

                if (vector.Length <= _tolerance)
                {
                    // Vector may equals {0,0,0}
                    return Curve.CreateBooleanIntersection(maxOutine, setback, _tolerance)[0];
                }


                // Calculate the step.
                double distanceToOutline = GetDistance(maxOutine, vector, basePlane.Origin);
                double distanceToSetback = GetDistance(setback, vector, basePlane.Origin);
                double margine = distanceToSetback - distanceToOutline;

                // Outline in vector direction may be far away than setback, need to change the direction of vector.
                if (margine < 0)
                {
                    vector.Reverse();
                    margine = GetDistance(setback, vector, basePlane.Origin) - GetDistance(outline, vector, basePlane.Origin);
                }


                double vStep = Math.Abs(margine / 3.0);

                for (int i = 1; i <= 3; i++)
                {
                    Vector3d tempVector = vector * vStep * i;
                    var tempOutline = maxOutine.DuplicateCurve();
                    tempOutline.Translate(tempVector);

                    var inside2 = Curve.PlanarClosedCurveRelationship(tempOutline, setback, basePlane, _tolerance);

                    if (inside2 == RegionContainment.Disjoint)
                        break;
                    else if (inside2 == RegionContainment.AInsideB)
                        return tempOutline;

                    else
                    {
                        var cs = Curve.CreateBooleanIntersection(tempOutline, setback, _tolerance);

                        var tempBoolen = cs.First();
                        var tempArea = AreaMassProperties.Compute(tempBoolen).Area;

                        booleans.Add(tempBoolen);
                        outlines.Add(tempOutline);
                        areas.Add(tempArea);
                    }
                }

                // 3. Get the most suitable outline.
                int max2 = areas.IndexOf(areas.Max());
                if (areas.Max() / area > 0.75)
                {
                    return booleans[max2];
                }
                else
                {
                    return outlines[max2];
                }
            }
        }


        private double GetDistance(Curve curve, Vector3d vector, Point3d point)
        {
            if (!vector.IsUnitVector)
                vector.Unitize();

            Point3d temp = point + vector * 1E+10;
            Line line = new Line(point, temp);

            var cs = Intersection.CurveCurve(line.ToNurbsCurve(), curve, _tolerance, _tolerance);
            if (cs.Count == 0)
                return 0;
            else
            {
                var point2 = cs[0].PointA;
                return point.DistanceTo(point2);
            }
        }


        private Curve GetRectangle(double width, double depth, Plane plane)
        {
            Interval w = new Interval(-1 * width / 2, width / 2);
            Interval d = new Interval(-1 * depth / 2, depth / 2);

            Rectangle3d outline = new Rectangle3d(plane, w, d);

            return outline.ToPolyline().ToPolylineCurve();
        }


        private void EliminateError(ref BuildingType buildingType, Curve outline)
        {
            double areaOutline = AreaMassProperties.Compute(outline).Area;

            double areaError = (buildingType.Area - areaOutline) * buildingType.Floors.Sum();

            int floorsAdd = (int)Math.Round(areaError / areaOutline);

            buildingType.Floors[1] += floorsAdd;

            if (buildingType.Floors[1] < 0)
            {
                buildingType.Floors[1] = 0;
            }

            if (buildingType.Floors.Sum() == 0)
            {
                buildingType.Floors[1] = 1;
            }
        }



        private Brep[] GetBreps(Curve outline, BuildingType buildingType,
            out string[] brepFunctions, out Curve[] brepOutlines, out double[] brepHeights, out int[] brepFloors, out double[] brepAreas, out Curve[] layers, out Curve roofOutline, out double footPrintArea, out double totalBrepsArea, bool isBottom = false)
        {
            // Using total layers.count to calculate building floors.s

            // For extruding breps, outline should be ccw.
            if (outline.ClosedCurveOrientation() == CurveOrientation.Clockwise)
                outline.Reverse();

            if (!outline.IsClosed)
                outline.MakeClosed(_tolerance);


            var tempOutline = outline.DuplicateCurve();
            var parameters = buildingType.Parameters;


            int[] floors = buildingType.Floors;

            footPrintArea = AreaMassProperties.Compute(outline).Area;

            LinkedList<Brep> brepsResult = new LinkedList<Brep>();
            LinkedList<string> functionsResult = new LinkedList<string>();
            LinkedList<Curve> outlinesResult = new LinkedList<Curve>();
            LinkedList<double> heightsResult = new LinkedList<double>();
            LinkedList<int> floorsResult = new LinkedList<int>();
            LinkedList<double> areasResult = new LinkedList<double>();

            // Bottom building has higher floor height.
            double coe = 1;
            if (isBottom) coe *= 1.2;

            for (int i = 0; i < 2; i++)
            {
                var floor = floors[i];
                if (floor != 0)
                {
                    outlinesResult.AddLast(tempOutline.DuplicateCurve());
                    var height = floor * parameters.FloorHeight * coe;
                    heightsResult.AddLast(Math.Round(height, 3));
                    floorsResult.AddLast(floor);
                    areasResult.AddLast(Math.Round(footPrintArea * floor, 3));

                    tempOutline.TryGetPlane(out Plane plane);
                    int reverse = 1;

                    // Extrusion.Create method will first use tryGetPlane to determine the current plane for this curve.
                    //When plane is directing dowards, extrusion will be below plane.
                    if (plane.ZAxis.Z < 0) reverse *= -1;

                    Extrusion e = Extrusion.Create(tempOutline, height * reverse, true);
                    Brep building = e.ToBrep();
                    e.Dispose();


                    brepsResult.AddLast(building);

                    Vector3d v = new Vector3d(0, 0, height);
                    tempOutline.Translate(v);

                    if (i == 0)
                    {
                        functionsResult.AddLast("C");
                    }
                    else
                    {
                        functionsResult.AddLast(buildingType.Parameters.Function);
                    }
                }
            }

            brepFunctions = functionsResult.ToArray();
            brepOutlines = outlinesResult.ToArray();
            brepHeights = heightsResult.ToArray();
            brepFloors = floorsResult.ToArray();
            brepAreas = areasResult.ToArray();
            totalBrepsArea = areasResult.Sum();

            layers = GetLayers(outline, buildingType, coe, out roofOutline);

            return brepsResult.ToArray();
        }


        private Curve[] GetLayers(Curve outline, BuildingType buildingType, double coe, out Curve roofOutline)
        {
            List<Curve> layers = new List<Curve>();
            var parameters = buildingType.Parameters;


            var floors = buildingType.Floors.Sum();

            for (int i = 0; i < floors + 1; i++)
            {
                Vector3d v = new Vector3d(0, 0, parameters.FloorHeight * coe * i);
                var temp = outline.DuplicateCurve();
                temp.Translate(v);
                layers.Add(temp);
            }

            roofOutline = layers.Last();
            layers.RemoveAt(layers.Count - 1);

            return layers.ToArray();
        }



        public void Dispose()
        {
            Breps = null;
            BrepFunctions = null;
            Layers = null;
        }
    }
}
