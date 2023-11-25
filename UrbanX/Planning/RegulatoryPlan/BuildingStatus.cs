using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using UrbanX.Planning.RegulatoryPlan;


namespace UrbanX.Planning.RegulatoryPlan
{
    public class BuildingStatus
    {
        public Polygon Geometry { get; set; }

        public string Landuse { get; set; }

        public Dictionary<LivingRadius, double> NACH { get; set; }

        public double BuildingArea { get; set; }

        public double FAR => Math.Round(BuildingArea / Geometry.Area, 1);

        public void AddBuildingArea(double area)
        {
            BuildingArea += area;
        }
    }
}
