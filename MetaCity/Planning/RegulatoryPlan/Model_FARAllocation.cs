using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;


namespace MetaCity.Planning.RegulatoryPlan
{
    public class Model_FARAllocation
    {
        private static readonly HashSet<string> badlanduse = new HashSet<string>() { "M", "W" };
        private static readonly Dictionary<string, double> maxFARDict = new Dictionary<string, double>()
            {
                { "R", 5.5 },
                { "B", 5.85 },
                { "A", 2.5 },
                { "M", 5.2 },
                { "W", 5.2 },
                { "G", 0 }
            };

        /// <summary>
        /// Allocation building areas to each block
        /// </summary>
        /// <param name="blocks">Geometric information of blocks, which should be the splitted version
        /// after the LanduseAllocation implementation</param>
        /// <param name="buildingAreaOfFunctions">The absolute building area of each landuse category</param>
        /// <param name="nach">The composite accessibility of each block in each level.
        /// The 1st dimension of this array has the same length as <c>blocks</c>.</param>
        /// <param name="landuses">Landuse of each of the <c>blocks</c>, with the same length as <c>blocks</c></param>
        /// <param name="serviceLevels">Record lists of neighbourhood-levels that each of the <c>blocks</c> contains.
        /// The 1st dimension of this array has the same length as <c>blocks</c>.</param>
        public Model_FARAllocation(Polygon[] blocks, Dictionary<string, double> buildingAreaOfFunctions,
            double[,] nach, string[] landuses, int[][] serviceLevels)
        {
            BuildingStatus[] buildingStatuses = GetBuildingStatuses(blocks, nach, landuses);
            int levelNum = nach.GetLength(1);
            LivingRadius lowestLevel = (LivingRadius)Enum.GetValues(typeof(LivingRadius)).GetValue(0);
            string[] allLanduses = GetAllLanduses(buildingStatuses);
            for (int i = levelNum - 1; i>=0; i--)
            {
                LivingRadius lr = (LivingRadius)i;
                double[] this_level_acc = GetLevelAcc(buildingStatuses, lr);
                for (int j = 0; j<allLanduses.Length; j++)
                {
                    string this_landuse = allLanduses[j];
                    if (this_landuse == "R" && lr != lowestLevel) continue;
                    List<int> this_block_list_id = GetBlockListOfLevelLanduse(landuses, serviceLevels, lr, this_landuse);
                    double totalAreaOfThisLevel = buildingAreaOfFunctions[this_landuse];
                    if (this_landuse != "R" && !badlanduse.Contains(this_landuse))
                        totalAreaOfThisLevel *= GetLevelPct(lr, (LivingRadius)(levelNum - 1));
                    SetBuildingAreaToBlocks(buildingStatuses, this_level_acc, this_block_list_id, totalAreaOfThisLevel, maxFARDict[this_landuse]);
                }
            }
            FAR = GetFARValues(buildingStatuses);
        }

        public Model_FARAllocation(Polygon[] blocks, Dictionary<string, double> buildingAreaOfFunctions, double[] nach, string[] landuses)
        {
            BuildingStatus[] buildingStatuses = GetBuildingStatuses(blocks, landuses);
            string[] allLanduses = GetAllLanduses(buildingStatuses);
            for (int j = 0; j < allLanduses.Length; j++)
            {
                string this_landuse = allLanduses[j];
                List<int> this_block_list_id = GetBlockListOfLanduse(landuses, this_landuse);
                SetBuildingAreaToBlocks(buildingStatuses, nach, this_block_list_id, buildingAreaOfFunctions[this_landuse], maxFARDict[this_landuse]);
            }
            FAR = GetFARValues(buildingStatuses);
        }

        public double[] FAR { get; set; }

        private static List<int> GetBlockListOfLanduse(string[] landuses, string target)
        {
            List<int> res = new List<int>();
            for (int i = 0; i < landuses.Length; i++)
                if (landuses[i] == target) res.Add(i);
            return res;
        }

        private static BuildingStatus[] GetBuildingStatuses(Polygon[] blocks, double[,] nach, string[] landuses)
        {
            BuildingStatus[] res = new BuildingStatus[blocks.Length];
            int levelNum = nach.GetLength(1);
            for (int i = 0; i<blocks.Length; i++)
            {
                BuildingStatus bs = new BuildingStatus
                {
                    Landuse = landuses[i],
                    NACH = new Dictionary<LivingRadius, double>(),
                    BuildingArea = 0,
                    Geometry = blocks[i]
                };
                for (int j = 0; j<levelNum; j++) bs.NACH.Add((LivingRadius)j, nach[i, levelNum - j - 1]);
                res[i] = bs;
            }

            return res;
        }

        private static BuildingStatus[] GetBuildingStatuses(Polygon[] blocks, string[] landuses)
        {
            BuildingStatus[] res = new BuildingStatus[blocks.Length];
            for (int i = 0; i<blocks.Length; i++)
            {
                BuildingStatus bs = new BuildingStatus
                {
                    Landuse = landuses[i],
                    NACH = new Dictionary<LivingRadius, double>(),
                    BuildingArea = 0,
                    Geometry = blocks[i]
                };
                res[i] = bs;
            }

            return res;
        }

        private static double[] GetLevelAcc(BuildingStatus[] buildingStatuses, LivingRadius level)
        {
            int total = buildingStatuses.Length;
            double[] nach = new double[total];
            for (int i = 0; i < total; i++) nach[i] = buildingStatuses[i].NACH[level];
            return nach;
        }

        private static string[] GetAllLanduses(IEnumerable<BuildingStatus> buildingStatuses)
        {
            HashSet<string> set = new HashSet<string>();
            foreach (BuildingStatus bs in buildingStatuses)
            {
                if (!set.Contains(bs.Landuse)) set.Add(bs.Landuse);
            }
            string[] res = set.ToArray<string>();
            return res;
        }

        private static List<int> GetBlockListOfLevelLanduse(string[] allLanduse, int[][] serviceLevels,
            LivingRadius lr, string landuse)
        {
            List<int> res = new List<int>();
            for (int i = 0; i < allLanduse.Length; i++)
                if (landuse != "R")
                {
                    if (allLanduse[i] == landuse && serviceLevels[i].Contains((int)lr)) res.Add(i);
                } else
                {
                    if (allLanduse[i] == landuse) res.Add(i);
                }
            return res;
        }

        private static double GetLevelPct(LivingRadius level, LivingRadius highestLevel)
        {
            LivingRadius lowestLevel = (LivingRadius)Enum.GetValues(typeof(LivingRadius)).GetValue(0);
            Dictionary<LivingRadius, double> buildingRatio = new Dictionary<LivingRadius, double>() {
                { LivingRadius.City, 10 },
                { LivingRadius.District, 9 },
                { LivingRadius.T15, 8 },
                { LivingRadius.T10, 6 },
                { LivingRadius.T5, 7 }
            };

            double numer = buildingRatio[level];
            double denom = 0;
            for (int j = (int)lowestLevel; j <= (int)highestLevel; j++) denom += buildingRatio[(LivingRadius)j];
            double levelPct = numer * 1.0 / (denom * 1.0);
            return levelPct;
        }

        private static void SetBuildingAreaToBlocks(BuildingStatus[] buildingStatuses, double[] acc, List<int> block_id_list,
            double totalArea, double maxFAR)
        {
            // Assume that the relationship between accessibility and FAR is linear
            if (totalArea <= 0) return;
            double A = 0;
            double B = 0;
            double S = totalArea;
            double minFAR = 0.8;
            double minAcc = double.PositiveInfinity;
            double maxAcc = 0;
            foreach (int i in block_id_list)
            {
                A += buildingStatuses[i].Geometry.Area * acc[i];
                B += buildingStatuses[i].Geometry.Area;
                if (acc[i] < minAcc) minAcc = acc[i];
                if (acc[i] > maxAcc) maxAcc = acc[i];
            }

            //double MAX = (maxFAR - S / B) / (maxAcc - A / B);
            double MIN = (minFAR - S / B) / (minAcc - A / B);

            double x;
            //if (A / B < minAcc)
            //{
            //    if (MAX >= MIN) x = MAX;
            //    else x = Math.Max(MIN, 0);
            //} else if (minAcc < A / B && A / B < maxAcc)
            //{
            //    if (MIN <= 0) x = 0;
            //    else if (MAX <= 0) x = MIN;
            //    else x = Math.Min(MIN, MAX);
            //}
            //else
            //{
            //    if (MIN <= 0) x = 0;
            //    else if (MAX > MIN) x = MIN;
            //    else x = Math.Max(MAX, 0);
            //}

            x = Math.Max(MIN, 0);

            double b = S / B - A / B * x;

            foreach (int i in block_id_list) {
                double this_far = acc[i] * x + b;
                buildingStatuses[i].AddBuildingArea(buildingStatuses[i].Geometry.Area * this_far);
            }
        }

        private static double[] GetFARValues(BuildingStatus[] buildingStatuses)
        {
            int total = buildingStatuses.Length;
            double[] res = new double[total];
            for (int i = 0; i < total; i++) res[i] = buildingStatuses[i].FAR;
            return res;
        }
    }
}
