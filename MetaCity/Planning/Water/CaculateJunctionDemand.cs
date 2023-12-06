using Rhino.Geometry;
using Rhino.Geometry.Intersect;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MetaCity.Algorithms.Geometry2D;
using MetaCity.DataStructures.Geometry;

namespace MetaCity.Planning.Water
{
    public enum DemandCalculateMethod
    {
        AreaRatioWithTotalDemand = 0,
        AreaFarWithCoefficients = 1
    }


    internal class PlanSite
    {
        public Polyline Block { get; }

        //public string Type { get; }

        public double Area
        {
            get
            {
                // Polyline's orientation must be counter clock wise.
                var xlist = Block.X;
                var ylist = Block.Y;

                double area = 0;
                for (int i = 0; i < Block.Count - 1; i++)
                {
                    area += (xlist[i] * ylist[i + 1] - xlist[i + 1] * ylist[i]) * 0.5;
                }

                return area;
            }
        }

        //public double Far { get;  }
        public double WaterDemand { get; internal set; }
        public double DistributedDemand { get; internal set; }


        public PlanSite(Polyline polyline)
        {
            // Make sure curve is Clockwise.
            if (polyline.ToPolylineCurve().ClosedCurveOrientation() == CurveOrientation.CounterClockwise)
                polyline.Reverse();

            // Make sure curve is planar.
            polyline.SetAllZ(0);

            Block = polyline;
            //Type = type;
            //Far = FAR;
        }

    }

    public class CaculateSiteDemand
    {
        private readonly Dictionary<string, double> _totalAreaByTpye;

        public double[] SitesDemand { get; }

        // Two way of constructor


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="blocks"></param>
        /// <param name="blockType"></param>
        /// <param name="landuseType"></param>
        /// <param name="totalDemandByType"></param>
        public CaculateSiteDemand(IList<Polyline> blocks, IList<string> blockType, IList<string> landuseType, IList<double> totalDemandByType)
        {
            _totalAreaByTpye = new Dictionary<string, double>(landuseType.Count);

            foreach (var type in landuseType)
            {
                _totalAreaByTpye.Add(type, 0);
            }

            SumAreaByType(blocks, blockType, ref _totalAreaByTpye);

            SitesDemand = CalSiteDemandByAreaRatio(blocks, blockType, _totalAreaByTpye, landuseType, totalDemandByType);
        }


        private double[] CalSiteDemandByAreaRatio(IList<Polyline> curves, IList<string> blockType, Dictionary<string, double> totalAreaByTpye, IList<string> landuseType, IList<double> totalDemandByType)
        {
            double[] siteDemand = new double[curves.Count];
            for (int i = 0; i < curves.Count; i++)
            {
                var curve = curves[i];
                var type = blockType[i];

                var typeTotalArea = totalAreaByTpye[type];
                var typeDemand = totalDemandByType[landuseType.IndexOf(type)];

                var ratio = CalPolygonArea(curve) / typeTotalArea;

                siteDemand[i] = typeDemand * ratio;
            }

            return siteDemand;
        }

        private void SumAreaByType(IList<Polyline> curves, IList<string> blockType, ref Dictionary<string, double> totalAreaByTpye)
        {

            for (int i = 0; i < curves.Count; i++)
            {
                var curve = curves[i];
                var type = blockType[i];

                totalAreaByTpye[type] += CalPolygonArea(curve);
            }
        }

        private double CalPolygonArea(Polyline curve)
        {
            // Polyline's orientation must be counter clock wise.
            var xlist = curve.X;
            var ylist = curve.Y;

            double area = 0;
            for (int i = 0; i < curve.Count - 1; i++)
            {
                area += (xlist[i] * ylist[i + 1] - xlist[i + 1] * ylist[i]) * 0.5;
            }

            return Math.Abs(area);
        }
    }



    internal class CaculateJunctionDemand
    {

        private readonly Dictionary<Point3d, int> _nodesToIndices;


        public LinkedList<PlanSite> PlanSites { get; private set; }

        public Polyline[] Cells { get; }

        public CaculateJunctionDemand(IList<Point3d> nodes, IList<Junctions> junctions, IList<Polyline> blocks, IList<double> siteDemands, Rectangle3d boundary, double tolerance)
        {
            PlanSites = new LinkedList<PlanSite>();
            Cells = new Polyline[nodes.Count];
            _nodesToIndices = new Dictionary<Point3d, int>(nodes.Count);

            // 0. Initialize containers, and Generate Voronoi diagram.
            Initialize(blocks, nodes, boundary, tolerance);

            // 1. Calculating Site water demand.
            GetSiteWaterDemand(siteDemands);

            // 2. Distributing water demand to each junction.
            DistributeWaterToJunction(nodes, junctions, tolerance);
        }


        private void Initialize(IList<Polyline> blocks, IList<Point3d> nodes, Rectangle3d rec, double tolerance)
        {
            // Plansites.
            for (int i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];

                var planSite = new PlanSite(block);

                PlanSites.AddLast(planSite);
            }


            // Voronoi diagram.
            List<FortuneSite> sites = new List<FortuneSite>(nodes.Count);

            var lowerLeft = rec.Corner(0);
            var lowerRight = rec.Corner(1);
            var upperRight = rec.Corner(2);
            var upperLeft = rec.Corner(3);

            for (int i = 0; i < nodes.Count; i++)
            {
                var pt = nodes[i];
                sites.Add(new FortuneSite(pt.X, pt.Y));
                _nodesToIndices.Add(pt, i);
            }


            var voronoi = new Voronoi(sites, lowerLeft.X, lowerLeft.Y, upperRight.X, upperRight.Y);


            // Find closest sites to corners.
            Point3d[] corners = { lowerLeft, lowerRight, upperRight, upperLeft };
            var cornerSites = NetworkOptimization.FindCloestPointInGraph(nodes, corners);
            for (int i = 0; i < corners.Length; i++)
            {
                sites[cornerSites[i]].Cell.Add(new VPoint(corners[i].X, corners[i].Y));
            }

            for (int i = 0; i < voronoi.Sites.Count; i++)
            {
                var cell = voronoi.Sites[i].SortCell();

                LinkedList<Point3d> _pts = new LinkedList<Point3d>();

                for (int p = 0; p < cell.Length; p++)
                {
                    if (p == cell.Length - 1)
                    {
                        var v = cell[p];
                        Point3d last = new Point3d(v.X, v.Y, 0);
                        _pts.AddLast(last);
                    }
                    else
                    {
                        var pt = cell[p];
                        Point3d tempP = new Point3d(pt.X, pt.Y, 0);
                        Point3d next = new Point3d(cell[p + 1].X, cell[p + 1].Y, 0);

                        // Handle tolerance.
                        var distance = tempP.DistanceTo(next);
                        if (distance > tolerance)
                        {
                            _pts.AddLast(tempP);
                        }
                        else
                        {
                            Point3d mid = 0.5 * (tempP + next);
                            _pts.AddLast(mid);
                            ++p;
                        }
                    }
                }

                // Close the polyline.
                var lastNode = _pts.First.Value;
                _pts.AddLast(lastNode);

                Polyline pl = new Polyline(_pts);

                Cells[i] = pl;
            }
        }

        // This part need futher development. Now we just use it for testing, in this scenario we don't consider the difference between various site types.
        private void GetSiteWaterDemand(IList<double> siteDemands)
        {
            int i = 0;

            foreach (var planSite in PlanSites)
            {
                planSite.WaterDemand = siteDemands[i];
                i++;
            }
        }


        private void DistributeWaterToJunction(IList<Point3d> nodes, IList<Junctions> junctions, double tolerance)
        {

            Parallel.ForEach(PlanSites, planSite =>
            {
                var planSiteCurve = planSite.Block.ToPolylineCurve();

                //Point3d[] needle = { planSite.Block.First };

                Point3d[] needle = { planSite.Block.CenterPoint() };
                HashSet<Point3d> tempNodes = new HashSet<Point3d>(nodes);

                // Because we only has one needle in this part of process.
                var indexOfClosetCell = NetworkOptimization.FindCloestPointInGraph(tempNodes.ToArray(), needle).First();

                var intersection = Intersection.CurveCurve(planSiteCurve, Cells[indexOfClosetCell].ToPolylineCurve(), tolerance, tolerance);


                // Have intersection or not.
                if (intersection.Count <= 1)
                {
                    var containment = Cells[indexOfClosetCell].ToPolylineCurve().Contains(planSite.Block.CenterPoint(), Plane.WorldXY, tolerance);

                    if (containment == PointContainment.Inside)
                    {
                        junctions[indexOfClosetCell].AccumulateDemand(planSite.WaterDemand);
                        //continue;
                    }
                    // block is outside of the current cell.
                }
                else
                {
                    var curveBoolean = Curve.CreateBooleanIntersection(Cells[indexOfClosetCell].ToPolylineCurve(), planSiteCurve, tolerance);

                    double sectionArea = 0;
                    foreach (var section in curveBoolean)
                    {
                        sectionArea += AreaMassProperties.Compute(section).Area;
                    }

                    var sectionDemand = (sectionArea / planSite.Area) * planSite.WaterDemand;
                    junctions[indexOfClosetCell].AccumulateDemand(sectionDemand);
                    planSite.DistributedDemand += sectionDemand;
                    tempNodes.Remove(tempNodes.ToArray()[indexOfClosetCell]);

                    // Has intersection area.
                    while (planSite.WaterDemand - planSite.DistributedDemand > 10 * tolerance)
                    {

                        var tempIndex = NetworkOptimization.FindCloestPointInGraph(tempNodes.ToArray(), needle).First();

                        var currentNode = tempNodes.ToArray()[tempIndex];
                        var tempIndexOfJunction = _nodesToIndices[currentNode];

                        var tempEvent = Intersection.CurveCurve(planSiteCurve, Cells[tempIndexOfJunction].ToPolylineCurve(), tolerance, tolerance);

                        if (tempEvent.Count == 0)
                        {
                            tempNodes.Remove(currentNode);
                            continue;
                        }

                        var tempCurveBoolean = Curve.CreateBooleanIntersection(Cells[tempIndexOfJunction].ToPolylineCurve(), planSiteCurve, tolerance);

                        double tempSectionArea = 0;
                        foreach (var section in tempCurveBoolean)
                        {
                            tempSectionArea += AreaMassProperties.Compute(section).Area;
                        }

                        var tempSectionDemand = (tempSectionArea / planSite.Area) * planSite.WaterDemand;


                        junctions[tempIndexOfJunction].AccumulateDemand(tempSectionDemand);
                        planSite.DistributedDemand += tempSectionDemand;

                        tempNodes.Remove(currentNode);

                    }

                }

            });
        }

    }
}
