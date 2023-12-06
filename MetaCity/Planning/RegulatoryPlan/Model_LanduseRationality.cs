using System;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;


namespace MetaCity.Planning.RegulatoryPlan
{
    public class Model_LanduseRationality
    {
        private const string RESIDENCE = "R";
        private static readonly HashSet<string> badlanduse = new HashSet<string>() { "M", "W" };
        private Dictionary<string, double> landuseStructure = new Dictionary<string, double>();
        
        public Model_LanduseRationality() { }
        public Model_LanduseRationality(Polygon[] blocks, double[,] nach, int[][][] clusters, string[] landusePriority, double[] landusePct)
        {
            // Process data into the required format
            LivingRadius level = (LivingRadius)(nach.GetLength(1) - 1);
            Dictionary<string, double> landPct = GetLandusePct(landusePriority, landusePct);
            List<LandStatus> landStatuses = GetLandStatusList(blocks, nach);
            SplitIntoGoodBadLanduseArray(landusePriority, out string[] goodLandusePriority, out string[] badLandusePriority);
            double totalArea = GetTotalLandArea(landStatuses, GetIntSequence(blocks.Length));
            Console.WriteLine("Total area: {0}m2", totalArea);

            // Allocate landuse to each blocks
            for (int j = 0; j <= (int)level; j++)
            {
                LivingRadius this_level = level - j;
                double totalServiceArea = NonResidentialAreaOfLevel(totalArea, landPct, this_level, level);
                ICollection<int> availableClusterIds = IdListOfAvailableClustersOfLevel(landStatuses, clusters, this_level);
                Dictionary<int, double> clusterAreasPct = GetClusterAreasPct(blocks, clusters, availableClusterIds, this_level);

                Console.WriteLine("Total service area of Level {0}: {1}m2", this_level, totalServiceArea);

                foreach (int k in availableClusterIds)
                {
                    double totalServiceAreaOfOneCluster = totalServiceArea * clusterAreasPct[k];
                    Dictionary<string, double> allServiceTotalLandArea = GetAllLanduseAreaOfLevelOfOneCluster(totalServiceAreaOfOneCluster,
                        landPct, this_level, level);
                    LanduseAllocation la = new LanduseAllocation(this_level, level, goodLandusePriority, badLandusePriority,
                        k, clusters, landStatuses, allServiceTotalLandArea);
                }
            }

            // Split Blocks
            List<LandStatus> splitted = new List<LandStatus>();
            foreach (LandStatus ls in landStatuses)
            {
                List<LandStatus> temp = ls.Split();
                foreach (LandStatus newls in temp) splitted.Add(newls);
            }

            // Retrieve results
            GetFinalLanduseStructure(landStatuses, landusePriority);
            SplittedBlocks = GetBlocks(splitted);
            Landuses = GetLanduses(splitted);
            NACH = GetNACH(splitted);
            Levels = GetLevels(splitted);
        }

        // Output
        public Dictionary<string, double> LanduseStructure => landuseStructure;

        public Polygon[] SplittedBlocks { get; set; }

        public string[] Landuses { get; set; }

        public double[,] NACH { get; set; }

        public int[][] Levels { get; set; }

        private static Dictionary<int, double> GetClusterAreasPct(Polygon[] blocks, int[][][] clusters,
            IEnumerable<int> clusterIds, LivingRadius level)
        {
            double total = 0;
            int[][] this_level_cluster = clusters[clusters.GetLength(0) - (int)level - 1];
            Dictionary<int, double> res = new Dictionary<int, double>();
            foreach (int k in clusterIds)
            {
                double subtotal = 0;
                foreach (int j in this_level_cluster[k])
                {
                    subtotal += blocks[j].Area;
                    total += blocks[j].Area;
                }
                res.Add(k, subtotal);
            }
            foreach (var k in res.Keys.ToArray()) res[k] /= total;

            return res;
        }

        private static int[][] GetLevels(List<LandStatus> fc)
        {
            int[][] res = new int[fc.Count][];
            for (int i = 0; i<fc.Count; i++)
            {
                int levelNum = fc[i].AllLevels.Count;
                res[i] = new int[levelNum];
                for (int j = 0; j < levelNum; j++) res[i][j] = (int)fc[i].AllLevels[j];
            }
            return res;
        }

        private static double[,] GetNACH(List<LandStatus> fc)
        {
            int levelNum = fc[0].Acc.Count;
            double[,] res = new double[fc.Count, levelNum];
            for (int i = 0; i < fc.Count; i++)
                foreach (var kvp in fc[i].Acc)
                    res[i, levelNum - (int)kvp.Key - 1] = kvp.Value;
            return res;
        }

        private static IEnumerable<int> GetIntSequence(int n)
        {
            int[] res = new int[n];
            for (int i = 0; i < n; i++) res[i] = i;
            return res;
        }

        private static double GetTotalLandArea(List<LandStatus> fc, IEnumerable<int> landIds)
        {
            double total = 0;
            foreach (int i in landIds) total += fc[i].Area;
            return total;
        }

        private static void SplitIntoGoodBadLanduseArray(string[] landusePriority,
            out string[] goodLandusePriority, out string[] badLandusePriority)
        {
            List<string> goodLanduseList = new List<string>();
            List<string> badLanduseList = new List<string>();
            for (int i = 0; i < landusePriority.Length; i++)
            {
                string this_landuse = landusePriority[i];
                if (this_landuse == RESIDENCE) continue;
                if (badlanduse.Contains(this_landuse)) badLanduseList.Add(this_landuse);
                else goodLanduseList.Add(this_landuse);
            }
            goodLandusePriority = goodLanduseList.ToArray();
            badLanduseList.Reverse();
            badLandusePriority = badLanduseList.ToArray();
        }

        private static Dictionary<string, double> GetLandusePct(string[] landusePriority, double[] landusePct)
        {
            Dictionary<string, double> landPct = new Dictionary<string, double>();
            for (int i = 0; i < landusePct.Length; i++) landPct.Add(landusePriority[i], landusePct[i]);
            return landPct;
        }

        private static List<LandStatus> GetLandStatusList(Polygon[] blocks, double[,] nach)
        {
            List<LandStatus> res = new List<LandStatus>();
            int levelNum = nach.GetLength(1);
            for (int i = 0; i < blocks.Length; i++)
            {
                Feature f = new Feature(blocks[i], new AttributesTable());
                LandStatus ls = new LandStatus(f);
                for (int j = 0; j < levelNum; j++)
                {
                    LivingRadius lr = (LivingRadius)(levelNum - j - 1);
                    ls.Acc[lr] = nach[i, j];
                }
                res.Add(ls);
            }
            return res;
        }

        private static Polygon[] GetBlocks(List<LandStatus> landStatuses)
        {
            Polygon[] res = new Polygon[landStatuses.Count];
            for (int i = 0; i < res.Length; i++) res[i] = (Polygon)landStatuses[i].GetFeature().Geometry;
            return res;
        }

        private static string[] GetLanduses(List<LandStatus> landStatuses)
        {
            string[] res = new string[landStatuses.Count];
            for (int i = 0; i < res.Length; i++) res[i] = landStatuses[i].LandUse;
            return res;
        }

        private void GetFinalLanduseStructure(IEnumerable<LandStatus> landStatuses, string[] landusePriority)
        {
            foreach (LandStatus ls in landStatuses)
            {
                foreach (var kvp in ls.LanduseAreas)
                {
                    if (landuseStructure.ContainsKey(kvp.Key)) landuseStructure[kvp.Key] += kvp.Value;
                    else landuseStructure.Add(kvp.Key, kvp.Value);
                }
            };
            double totalArea = 0;
            foreach (double v in landuseStructure.Values) totalArea += v;
            foreach (string k in landuseStructure.Keys.ToArray<string>()) landuseStructure[k] /= totalArea;

            // Ensure that the sequence of the landuses is the same as the input
            Dictionary<string, double> res = new Dictionary<string, double>();
            foreach (string s in landusePriority) res.Add(s, landuseStructure[s]);
            landuseStructure = res;
        }

        private static double GetLevelPct(LivingRadius level, LivingRadius highestLevel)
        {
            Dictionary<LivingRadius, double> landRatio = new Dictionary<LivingRadius, double>() {
                { LivingRadius.City, 12 },
                { LivingRadius.District, 11 },
                { LivingRadius.T15, 10 },
                { LivingRadius.T10, 9 },
                { LivingRadius.T5, 8 }
            };
            LivingRadius lowestLevel = (LivingRadius)Enum.GetValues(typeof(LivingRadius)).GetValue(0);
            double numer = landRatio[level];
            double denom = 0;
            for (int j = (int)lowestLevel; j <= (int)highestLevel; j++) denom += landRatio[(LivingRadius)j];
            double levelPct = numer * 1.0 / (denom * 1.0);
            return levelPct;
        }

        private static double NonResidentialAreaOfLevel(double totalArea, Dictionary<string, double> landuseStructure,
            LivingRadius level, LivingRadius highestLevel)
        {
            double levelPct = GetLevelPct(level, highestLevel);
            double res = 0;
            if (level == highestLevel)
                foreach (var k in badlanduse)
                    if (landuseStructure.ContainsKey(k)) res += totalArea * landuseStructure[k];
            foreach (var k in landuseStructure.Keys)
                if (k != RESIDENCE && !badlanduse.Contains(k)) res += totalArea * landuseStructure[k] * levelPct;
            return res;
        }

        private static ICollection<int> IdListOfAvailableClustersOfLevel(List<LandStatus> landStatuses, int[][][] clusters, LivingRadius level)
        {
            const double THRESHOLD = 0.6;
            int this_level_id = clusters.GetLength(0) - (int)level - 1;
            List<int> res = new List<int>();

            for (int j = 0; j < clusters[this_level_id].GetLength(0); j++)
            {
                double minClusterArea = GetTotalLandArea(landStatuses, clusters[this_level_id][j]) * THRESHOLD;
                double total = 0;
                foreach (int k in clusters[this_level_id][j]) total += landStatuses[k].RemainArea;
                if (total >= minClusterArea) res.Add(j);
            }
            Console.WriteLine("{0} of the {1} clusters of Level {2} are available.", res.Count, clusters[this_level_id].GetLength(0), level);
            return res;
        }

        private static Dictionary<string, double> GetAllLanduseAreaOfLevelOfOneCluster(double totalServiceArea,
            Dictionary<string, double> landuseStructure, LivingRadius level, LivingRadius highestLevel)
        {
            HashSet<string> badlanduse = new HashSet<string>() { "M", "W" };
            Dictionary<string, double> res = new Dictionary<string, double>();

            if (level == highestLevel) SetBadLanduseAreaOfOneCluster(landuseStructure, highestLevel,
                ref res, ref totalServiceArea);

            double totalPct = 0;
            foreach (var kvp in landuseStructure)
                if (!badlanduse.Contains(kvp.Key) && kvp.Key != RESIDENCE) totalPct += kvp.Value;

            foreach (var kvp in landuseStructure)
                if (!badlanduse.Contains(kvp.Key) && kvp.Key != RESIDENCE) res.Add(kvp.Key, kvp.Value / totalPct * totalServiceArea);

            return res;
        }

        private static void SetBadLanduseAreaOfOneCluster(Dictionary<string, double> landuseStructure,
            LivingRadius highestLevel, ref Dictionary<string, double> res, ref double totalServiceArea)
        {
            double totalBadPct = 0;
            foreach (var k in badlanduse) totalBadPct += landuseStructure[k];
            double totalGoodPct = 0;
            foreach (var k in landuseStructure.Keys)
                if (k != RESIDENCE && !badlanduse.Contains(k)) totalGoodPct += landuseStructure[k];
            double levelPct = GetLevelPct(highestLevel, highestLevel);
            double totalBadArea = totalServiceArea / (1 + 1 / totalBadPct * totalGoodPct * levelPct);
            foreach (var k in badlanduse) res.Add(k, landuseStructure[k] / totalBadPct * totalBadArea);
            totalServiceArea -= totalBadArea;
        }
    }
}
