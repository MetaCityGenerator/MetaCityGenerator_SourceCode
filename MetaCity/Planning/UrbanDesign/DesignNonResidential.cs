using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;

using MetaCity.DataStructures.Trees;



// Reminder: closed curve orientation.
// ccw: GetBreps(extruding) , getoffsetCurveLength()
// cw: offset, brep.Split(left is the first brep)
namespace MetaCity.Planning.UrbanDesign
{
    /// <summary>
    /// Using binary space partition tree to split site into sub sites.
    /// Each class only handle one site.
    /// </summary>
    public class DesignNonResidential : IDisposable
    {
        private readonly Curve _site;

        private Curve[] _subSites;

        private BuildingType[] _subSiteTypes;

        private double[] _subSiteRadiants;

        private double[][] _subSiteScores;


        private readonly double _totalBuildingArea;

        private readonly double _tolerance;

        public readonly double _radiant;


        // Input loop curves for group style.
        private readonly Curve _lo;

        private readonly Curve _li;

        private readonly double _minSetback;


        public Curve[] SetBacks { get; private set; }

        public BuildingGeometry[] BuildingGeometries { get; private set; }


        // For debugging only.
        public List<Line> Edges { get; }

        // For debugging only.
        public List<double> Scoresd { get; }


        /// <summary>
        /// Site partitioner for Non-Residential land type.
        /// Using binary space partition tree.
        /// </summary>
        /// <param name="site"></param>
        /// <param name="types"></param>
        /// <param name="scores"></param>
        /// <param name="radiant"></param>
        /// <param name="tolerance"></param>
        public DesignNonResidential(Curve site, double totalBuildingArea, BuildingType[] types, double[] scores, double radiant, double tolerance, Curve loopOutter = null, Curve loopInner = null, double minSetbackDistance = 0)
        {
            // Curve's orientation must be clock wise.
            if (site.ClosedCurveOrientation() == CurveOrientation.CounterClockwise)
                site.Reverse();

            //_tolerance = tolerance * 1E-3 < 1E-8 ? 1E-8 : tolerance * 1E-3;
            _tolerance = tolerance;
            _radiant = radiant;

            _site = site;
            _totalBuildingArea = totalBuildingArea;

            // Fields for Group style.
            _lo = loopOutter;
            _li = loopInner;
            _minSetback = minSetbackDistance;

            // Sorting the types by priority to make sure than building with same function will be near each other.
            var temp = types.ToList();
            temp.Sort();
            _subSiteTypes = temp.ToArray();
            // For debug.
            Edges = new List<Line>();
            Scoresd = new List<double>();

            _subSites = new Curve[types.Length];
            _subSiteRadiants = new double[types.Length];
            _subSiteScores = new double[types.Length][];

            SetBacks = new Curve[types.Length];
            BuildingGeometries = null;

            SplitSite(scores);
        }


        private void SplitSite(double[] scores)
        {
            var nodes = new BSPTreeNode[_subSiteTypes.Length];
            for (int i = 0; i < _subSiteTypes.Length; i++)
            {
                // Create new node by using value and priority from building type.
                nodes[i] = new BSPTreeNode(i, _subSiteTypes[i].Ratio, _subSiteTypes[i].Priority);
            }

            BSPTree bspTree = new BSPTree(nodes);

            LinkedList<Curve> curvesResult = new LinkedList<Curve>();
            LinkedList<double> radiantsResult = new LinkedList<double>();
            LinkedList<double[]> scoresResult = new LinkedList<double[]>();
            LinkedList<int> nodeKeys = new LinkedList<int>();
            DesignToolbox.SplitRecursive(_site, bspTree.Root, scores, _radiant, false, _tolerance, ref curvesResult, ref radiantsResult, ref scoresResult, ref nodeKeys);

            // Sorting results.
            for (int i = 0; i < nodeKeys.Count; i++)
            {
                var key = nodeKeys.ToArray()[i];
                _subSites[key] = curvesResult.ToArray()[i];
                _subSiteRadiants[key] = radiantsResult.ToArray()[i];
                _subSiteScores[key] = scoresResult.ToArray()[i];
            }

            for (int i = 0; i < _subSites.Length; i++)
            {
                Edges.AddRange(SiteBoundingRect.GetEdges(_subSites[i], _subSiteRadiants[i]));
                Scoresd.AddRange(_subSiteScores[i]);
            }
        }


        public void GeneratingBuildings(NonResidentialStyles nonResidentialStyle)
        {
            LinkedList<BuildingGeometry> result = new LinkedList<BuildingGeometry>();

            switch (nonResidentialStyle)
            {
                case NonResidentialStyles.Alone:

                    for (int s = 0; s < _subSites.Length; s++)
                    {
                        var bType = _subSiteTypes[s];
                        var brepLoop = _subSites[s];

                        // Each subsite may hold builidng with different hight, therefore the setbackdistance need to be calculated every time.
                        var setbackdistance = BuildingDataset.GetSetbackOhterType(bType.Floors.Sum() * bType.Parameters.FloorHeight);

                        DesignToolbox.SafeOffsetCurve(brepLoop, setbackdistance, _tolerance, out Curve setback);
                        SetBacks[s] = setback;

                        var building = new BuildingGeometry(bType, _tolerance);
                        building.GeneratingNonResidentialAloneStyle(setback, _radiant);

                        if (building.Breps != null)
                        {
                            result.AddLast(building);
                        }
                    }

                    BuildingGeometries = result.ToArray();
                    break;

                case NonResidentialStyles.Group:
                    for (int s = 0; s < _subSites.Length; s++)
                    {
                        var bType = _subSiteTypes[s];
                        var brepLoop = _subSites[s];

                        // For group style, all the subsites share the same minmium setback distance.

                        DesignToolbox.SafeOffsetCurve(brepLoop, _minSetback, _tolerance, out Curve setback);
                        SetBacks[s] = setback;

                        var building = new BuildingGeometry(bType, _tolerance);
                        building.GeneratingNonResidentialGroupStyle(_lo, _li, setback);

                        if (building.Breps != null)
                        {
                            result.AddLast(building);
                        }
                    }

                    BuildingGeometries = result.ToArray();
                    break;

                case NonResidentialStyles.Mixed:
                    for (int s = 0; s < _subSites.Length; s++)
                    {
                        var bType = _subSiteTypes[s];
                        var brepLoop = _subSites[s];

                        // For mixed style, all the subsites share the same minmium setback distance.

                        DesignToolbox.SafeOffsetCurve(brepLoop, _minSetback, _tolerance, out Curve setback);
                        SetBacks[s] = setback;

                        // Using goldon ratio to split outter loop. Tower should have higher priority.   
                        var towerOutline = DesignToolbox.SplitSiteByRatios(setback, new double[] { 0.382, 0.618 }, new double[] { 10, 1 }, _subSiteScores[s], _subSiteRadiants[s], false, _tolerance)[0];

                        // Randomly reduce the area of current tempoutline.
                        Random random = new Random();
                        int reduceTimes = random.Next(0, 3);
                        if (_subSiteTypes.Length == 2 && reduceTimes == 0)
                            reduceTimes++;

                        for (int i = 0; i < reduceTimes; i++)
                        {
                            var cutRatio = 0.382 / _subSiteTypes.Length;
                            towerOutline = DesignToolbox.SplitSiteByRatios(towerOutline, new double[] { cutRatio, 1 - cutRatio }, new double[] { 10, 1 }, _subSiteScores[s], _subSiteRadiants[s], false, _tolerance)[1];
                        }

                        var buildingMixed = new BuildingGeometry(bType, _tolerance);
                        buildingMixed.GeneratingNonResidentialMixedStyle(_lo, _li, setback, towerOutline, _totalBuildingArea / _subSites.Length);

                        if (buildingMixed.Breps != null)
                        {
                            result.AddLast(buildingMixed);
                        }
                    }

                    BuildingGeometries = result.ToArray();
                    break;
            }
        }

        public void Dispose()
        {
            _site.Dispose();
            _subSiteTypes = null;

            if (_lo != null)
                _lo.Dispose();
            if (_li != null)
                _li.Dispose();

            _subSites = null;
            SetBacks = null;
            _subSiteRadiants = null;
            _subSiteScores = null;
            BuildingGeometries = null;
        }
    }
}
