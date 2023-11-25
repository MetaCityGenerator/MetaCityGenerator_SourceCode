using System;
using System.Linq;
using System.Collections.Generic;


namespace UrbanX.Planning.RegulatoryPlan
{
    public enum LivingRadius
    {
        T5,
        T10,
        T15,
        District,
        City
    }

    public class LanduseAllocation
    {
        public LanduseAllocation(LivingRadius level, LivingRadius highestLevel,
            string[] landusePriority, string[] badLandusePriority, int thisLevelClusterId, int[][][] landIds,
            List<LandStatus> landStatuses, Dictionary<string, double> allServiceTotalLandArea)
        {
            // Initialization
            List<int> featureList = landIds[(int)highestLevel - (int)level][thisLevelClusterId].ToList<int>();

            // Assign service functions to some of the blocks
            IndexPQ<double> pq = new IndexPQ<double>("max");
            foreach (int i in featureList)
                if (landStatuses[i].HasRemainArea) pq.Insert(i, landStatuses[i].Acc[level]);

            foreach (string landuse in landusePriority)
                AssignServiceFunctionToBlocksAccurately(landuse, level, allServiceTotalLandArea[landuse], landStatuses, pq);

            // Assign special landuse to some of the blocks (e.g. industrial blocks), only in the highest level
            if (level != highestLevel) return;
            IndexPQ<double> minpq = pq.MinMaxConversion();
            foreach (string landuse in badLandusePriority)
                AssignServiceFunctionToBlocksAccurately(landuse, level, allServiceTotalLandArea[landuse], landStatuses, minpq);
        }

        private static void AssignServiceFunctionToBlocksAccurately(string landuse, LivingRadius level, double landAreaOfThisLevel,
            List<LandStatus> landList, IndexPQ<double> pq)
        {
            Dictionary<LivingRadius, double> allMinArea = new Dictionary<LivingRadius, double>()
            {
                { LivingRadius.City, 2500 },
                { LivingRadius.District, 2500 },
                { LivingRadius.T15, 2000 },
                { LivingRadius.T10, 1000 },
                { LivingRadius.T5, 500 }
            };

            // Accumulate the land area for the service function
            double currentTotalLandAreaA = 0;
            double minArea = allMinArea[level];
            if (pq.IsEmpty) Console.WriteLine("PQ Empty!!!");
            while (currentTotalLandAreaA + minArea < landAreaOfThisLevel && !pq.IsEmpty)
            {
                int currentId = pq.DelMin();
                LandStatus ls = landList[currentId];
                double assignedArea = GetAssignedArea(ls, landAreaOfThisLevel - currentTotalLandAreaA, minArea);
                if (currentTotalLandAreaA + assignedArea <= landAreaOfThisLevel + minArea)
                {
                    ls.AssignFunction(landuse, level, assignedArea, minArea);
                    currentTotalLandAreaA += assignedArea;
                }
                if (ls.HasRemainArea) pq.Insert(currentId, landList[currentId].Acc[level]);

                if (pq.IsEmpty) Console.WriteLine("PQ space runout!");
            }

            if (Math.Abs(currentTotalLandAreaA - landAreaOfThisLevel) >= 1)
            {
                Console.WriteLine("Minimum area of Level {0}: {1}", level, minArea);
                Console.WriteLine("Expected Area for Landuse {0} in Level {1}: {2}", landuse, level, landAreaOfThisLevel);
                Console.WriteLine("Actual Area for Landuse {0} in Level {1}: {2}", landuse, level, currentTotalLandAreaA);
                Console.WriteLine();
            }
        }

        private static double GetAssignedArea(LandStatus ls, double unassignedArea, double minArea = 2500)
        {
            if (ls.RemainArea - unassignedArea <= minArea) return ls.RemainArea;
            return unassignedArea;
        }
    }
}
