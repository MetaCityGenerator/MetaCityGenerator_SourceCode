using Rhino.Geometry;

using System;
using System.Linq;

namespace UrbanX.Planning.UrbanDesign
{
    public class DesignResult : IDisposable
    {
        /// <summary>
        /// Original site curve. 
        /// </summary>
        public Curve Site { get; }


        /// <summary>
        /// Site accessibility inherited from space syntax calculation result.
        /// </summary>
        public double SiteScore { get; }


        /// <summary>
        /// Original site's area.
        /// </summary>
        public double SiteArea { get; }


        /// <summary>
        /// Site land use type representing by function name.
        /// </summary>
        public string SiteType { get; }


        /// <summary>
        /// Sub_sites curve from site partioning.
        /// </summary>
        public Curve[] SubSites { get; private set; }


        /// <summary>
        /// Sub_sites's setbacks, setbacks count may larger than subSites' count.
        /// </summary>
        public Curve[][] SubSiteSetbacks { get; private set; }


        /// <summary>s
        /// Sub_sites' accessibility scores.
        /// </summary>
        public double[] SubSiteScores { get; private set; }


        /// <summary>
        /// Contains all the building geometries for each sub site. Every geometry has properties such as building area.
        /// For calculating each brep energy(water) consumption, need to iterate this array.
        /// </summary>
        public BuildingGeometry[][] SubSiteBuildingGeometries { get; private set; }


        // Need calculation.
        /// <summary>
        /// Total building area by summing all the floors' area.
        /// </summary>
        public double BuildingArea { get; }


        /// <summary>
        /// Site current density by calculating { totalFootPrintArea / siteArea }.
        /// <para>Reminder: current density may different than target value.</para>
        /// </summary>
        public double Density { get; }


        /// <summary>
        /// Site current FAR by calculating { BuildingArea/siteArea }.
        /// <para>Reminder: current FAR may different than target value.</para>
        /// </summary>
        public double FAR { get; }


        public DesignResult(Curve site, double[] scores, string siteType, Curve[] subSites, double[] subSiteScores, Curve[][] subSiteSetbacks, BuildingGeometry[][] buildingGeometries)
        {
            Site = site.DuplicateCurve();
            SiteScore = scores.Sum();
            SiteArea = AreaMassProperties.Compute(site).Area;
            SiteType = siteType;
            SubSites = subSites;
            SubSiteScores = subSiteScores;
            SubSiteSetbacks = subSiteSetbacks;
            SubSiteBuildingGeometries = buildingGeometries;

            BuildingArea = GetBuildingArea(out double totalFootPrintArea);
            Density = GetSiteDensity(totalFootPrintArea);
            FAR = GetSiteFAR();
        }

        private double GetBuildingArea(out double totalFootPrintArea)
        {
            double result = 0;
            totalFootPrintArea = 0;
            for (int i = 0; i < SubSiteBuildingGeometries.Length; i++)
            {
                for (int b = 0; b < SubSiteBuildingGeometries[i].Length; b++)
                {
                    var building = SubSiteBuildingGeometries[i][b];
                    result += building.BuildingArea;
                    totalFootPrintArea += building.FootPrintArea;
                }
            }
            totalFootPrintArea = Math.Round(totalFootPrintArea, 3);

            return Math.Round(result, 3);
        }

        private double GetSiteDensity(double totalFootPrintArea)
        {
            return Math.Round(totalFootPrintArea / SiteArea, 3);
        }

        private double GetSiteFAR()
        {
            return Math.Round(BuildingArea / SiteArea, 3);
        }

        public void Dispose()
        {
            Site.Dispose();
            SubSites = null;
            SubSiteScores = null;
            SubSiteBuildingGeometries = null;
        }
    }
}
