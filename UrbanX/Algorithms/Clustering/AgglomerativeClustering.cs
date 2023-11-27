using System.Collections.Generic;
using System.Linq;

namespace MetaCity.Algorithms.Clustering
{
    /// <summary>
    /// A hierarchical agglomerative clustering (HAC) algorithm based on complete linkage to ensure all the clusters are with more or less the same diameter.
    /// </summary>
    public class AgglomerativeClustering
    {
        /// <summary>
        /// Method for running the hierarchical agglomerative clustering algorithm based on a given diameter.
        /// </summary>
        /// <param name="distanceMatrix">Distance matrix for all the singleton clusters.</param>
        /// <param name="diameter">The maximum diameter for all the clusters.</param>
        /// <returns></returns>
        public static MultiClusters Run(double[,] distanceMatrix, double diameter, double[,] coordinates)
        {
            // Step 1: Build Singleton Cluster.
            int singletonCount = distanceMatrix.GetLength(0);
            Cluster[] singletons = new Cluster[singletonCount];
            for (int i = 0; i < singletonCount; i++)
            {
                singletons[i] = new Cluster(i, coordinates[i, 0], coordinates[i, 1], coordinates[i, 2]);
            }

            // Step 2: Build MultiClusters to store all the clusters while running this algorithm.
            MultiClusters clusters = new MultiClusters(singletons);

            // Step 3: Build Dissimilarity Matrix
            DissimilarityMatrix matrix = new DissimilarityMatrix(clusters, distanceMatrix, diameter);


            // Step 4: Build Hierarchical Clustering
            BuildHierarchicalClustering(ref singletonCount, diameter, clusters, matrix);

            return clusters;
        }


        /// <summary>
        /// Method for running the hierarchical agglomerative clustering algorithm based on a given array of diameter.
        /// </summary>
        /// <param name="distanceMatrix"></param>
        /// <param name="diameters"></param>
        /// <returns></returns>
        public static MultiClusters[] Run(double[,] distanceMatrix, double[] diameters, double[,] coordinates)
        {
            // Step 0: Sorting diameters.
            SortedSet<double> diams = new SortedSet<double>(diameters);// default comparer.
            diameters = diams.ToArray();


            // Step 1: Build Singleton Cluster.
            int singletonCount = distanceMatrix.GetLength(0);
            Cluster[] singletons = new Cluster[singletonCount];
            for (int i = 0; i < singletonCount; i++)
            {
                singletons[i] = new Cluster(i, coordinates[i, 0], coordinates[i, 1], coordinates[i, 2]);
            }

            // Step 2: Build MultiClusters to store all the clusters while running this algorithm.
            MultiClusters clusters = new MultiClusters(singletons);

            // Step 3: Build Dissimilarity Matrix
            DissimilarityMatrix matrix = new DissimilarityMatrix(clusters, distanceMatrix, diameters[diameters.Length - 1]);


            // Step 4: Build Hierarchical Clustering recursively.
            MultiClusters[] HAclusters = new MultiClusters[diameters.Length];
            for (int i = 0; i < diameters.Length; i++)
            {
                var diameter = diameters[i];
                BuildHierarchicalClustering(ref singletonCount, diameter, clusters, matrix);

                HAclusters[i] = new MultiClusters(clusters);
            }

            return HAclusters;
        }


        /// <summary>
        /// Method for build clusters based on a given number of clusters.
        /// </summary>
        /// <param name="newClusterIndex">Index of new cluster.</param>
        /// <param name="k">Count of target clusters.</param>
        /// <param name="clusters">Current clusters collection.</param>
        /// <param name="matrix">Dissimilarity matrix storing all the cluster-pairs.</param>
        private static void BuildHierarchicalClustering(int newClusterIndex, int k, MultiClusters clusters, DissimilarityMatrix matrix)
        {
            var closestPair = matrix.GetClosestClusterPair(out _);
            matrix.UpdateMatrix(closestPair, newClusterIndex, clusters);

            if (clusters.Count > k)
                BuildHierarchicalClustering(newClusterIndex + 1, k, clusters, matrix);
        }


        /// <summary>
        /// Method for build clusters based on a given diameter of clusters.
        /// The largest cluster's diameter should be smaller than this input diameter.
        /// </summary>
        /// <param name="newClusterIndex">Index of new cluster.</param>
        /// <param name="diameter">The maximum diameter for all the clusters.</param>
        /// <param name="clusters">Current clusters collection.</param>
        /// <param name="matrix">Dissimilarity matrix storing all the cluster-pairs.</param>
        private static void BuildHierarchicalClustering(ref int newClusterIndex, double diameter, MultiClusters clusters, DissimilarityMatrix matrix)
        {
            // Because MultiClusters and DissimilarityMatrix are reference type, we don't need ref key word before those parameters.
            if (matrix.PairsCount > 0)
            {
                var closestPair = matrix.GetClosestClusterPair(out double dist);
                if (dist < diameter)
                {
                    matrix.UpdateMatrix(closestPair, newClusterIndex, clusters);
                    newClusterIndex++;
                    BuildHierarchicalClustering(ref newClusterIndex, diameter, clusters, matrix);
                }
            }
        }
    }
}
