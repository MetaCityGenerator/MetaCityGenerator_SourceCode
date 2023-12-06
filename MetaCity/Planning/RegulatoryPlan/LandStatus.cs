using System;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using MetaCity.Planning.UrbanDesign;
using MetaCity.Planning.Utility;


namespace MetaCity.Planning.RegulatoryPlan
{
    public class LandStatus
    {
        private readonly Dictionary<string, double> landusePct = new Dictionary<string, double>();
        private readonly Dictionary<string, List<LivingRadius>> landuseLevelInfo = new Dictionary<string, List<LivingRadius>>();
        private readonly Dictionary<LivingRadius, double> acc = new Dictionary<LivingRadius, double>();
        private readonly Feature feature;
        private readonly double area;
        private readonly string[] landuses = { "R", "A", "G", "B", "M", "W" };

        private double remainArea;
        private string landuse;

        public Dictionary<LivingRadius, double> Acc => acc;

        public Dictionary<string, double> LanduseAreas => landusePct;

        public double RemainArea => remainArea;

        public double Area => area;

        public List<LivingRadius> AllLevels { get; set; }

        public string LandUse
        {
            get => landuse;
            set => landuse = value;
        }

        public bool HasRemainArea => remainArea > 0;

        public LandStatus(Feature f)
        {
            feature = f;
            area = f.Geometry.Area;
            remainArea = f.Geometry.Area;
            foreach (string s in landuses) landusePct.Add(s, 0);
        }

        public void ConvertRemainToR()
        {
            landusePct["R"] += remainArea;
            remainArea = 0;
        }

        public void AssignFunction(string lu, LivingRadius level, double targetArea, double minLandArea = 2500)
        {
            landusePct[lu] += targetArea;
            remainArea -= targetArea;
            if (remainArea < 0) throw new Exception("Negative land remaining area error!");
            if (remainArea < minLandArea)
            {
                landusePct[lu] += remainArea;
                remainArea = 0;
            }
            if (!landuseLevelInfo.ContainsKey(lu)) landuseLevelInfo.Add(lu, new List<LivingRadius>());
            if (!landuseLevelInfo[lu].Contains(level)) landuseLevelInfo[lu].Add(level);
        }

        public int GetLanduseNum()
        {
            int total = 0;
            foreach (string s in landusePct.Keys) if (landusePct[s] > 0) total++;
            return total;
        }

        public string GetMainLanduse()
        {
            double pct = -1;
            string res = "R";
            foreach (string s in landusePct.Keys)
            {
                if (landusePct[s] > pct)
                {
                    pct = landusePct[s];
                    res = s;
                }
            }
            return res;
        }

        public Feature GetFeature()
        {
            Feature f = new Feature(this.feature.Geometry, this.feature.Attributes);
            f.Geometry.SRID = 32650;
            f.Attributes.Add("landuse", landuse);
            return f;
        }

        /// <summary>
        /// Convert the Landuse Structure of this block into two arrays
        /// </summary>
        /// <param name="res_s">Array of the landuses of this block</param>
        /// <param name="res_d">Array of the landuse-pct of this block, each of which corresponding to the elements of <c>res_s</c></param>
        public void LandUsePctToArrays(out string[] res_s, out double[] res_d)
        {
            Dictionary<string, double> d = GetLanduseStructure();
            int landuseNum = d.Count;
            foreach (var v in d.Values) if (v <= 0) landuseNum--;
            res_s = new string[landuseNum];
            res_d = new double[landuseNum];
            int j = 0;
            foreach (var k in d.Keys)
            {
                if (d[k] <= 0) continue;
                res_s[j] = k;
                res_d[j] = d[k];
                j++;
            }
        }

        private Dictionary<string, double> GetLanduseStructure()
        {
            Dictionary<string, double> res = new Dictionary<string, double>();
            foreach (var kvp in landusePct) res.Add(kvp.Key, kvp.Value / Area);
            return res;
        }

        private List<LivingRadius> GetAllLevels()
        {
            List<LivingRadius> res = new List<LivingRadius>();
            foreach (var k in landuseLevelInfo.Keys)
                foreach (var lr in landuseLevelInfo[k])
                    if (!res.Contains(lr)) res.Add(lr);
            return res;
        }

        /// <summary>
        /// Split this blocks according to the landuses and their percentages in this block
        /// </summary>
        /// <returns></returns>
        public List<LandStatus> Split()
        {
            List<LandStatus> res = new List<LandStatus>();
            ConvertRemainToR();
            AllLevels = GetAllLevels();
            int landusenum = GetLanduseNum();
            if (landusenum <= 1)
            {
                LandUse = GetMainLanduse();
                res.Add(this);
            }
            else
            {
                LandUsePctToArrays(out string[] landuseSeq, out double[] pctSeq);
                Feature[] this_splitted = DividePolygonFeature(feature, pctSeq);
                for (int j = 0; j < landuseSeq.Length; j++)
                {
                    LandStatus newls = new LandStatus(this_splitted[j]);
                    string this_landuse = landuseSeq[j];
                    newls.LandUse = this_landuse;
                    if (this_landuse != "R") newls.AllLevels = landuseLevelInfo[this_landuse];
                    else newls.AllLevels = new List<LivingRadius>();
                    foreach (var kvp in acc) newls.Acc.Add(kvp.Key, kvp.Value);
                    res.Add(newls);
                }
            }

            return res;
        }

        private static Feature[] DividePolygonFeature(Feature f, double[] ratios)
        {
            int parts = ratios.Length;
            double[] priorities = new double[parts];
            for (int j = 0; j < parts; j++) priorities[j] = 1;
            double[] scores = { 1, 1, 1, 1 };
            Polygon[] splittedPolygon = Toolbox.SplitSiteByRatiosAccuratly((Polygon)f.Geometry, ratios, priorities, scores,
                ((Polygon)f.Geometry).GetPolygonRadiant(), true);
            Feature[] newFeatures = new Feature[parts];
            for (int j = 0; j < ratios.Length; j++)
            {
                newFeatures[j] = new Feature(splittedPolygon[j], new AttributesTable());
                CopyAttribute(f, newFeatures[j]);
            }
            return newFeatures;
        }

        private static void CopyAttribute(Feature f1, Feature f2)
        {
            foreach (var kvp in (AttributesTable)f1.Attributes) f2.Attributes.Add(kvp.Key, kvp.Value);
        }
    }
}
