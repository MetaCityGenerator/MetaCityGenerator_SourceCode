using System;
using System.Collections.Generic;

using UrbanX.DataStructures.Heaps;



namespace UrbanX.Algorithms.Clustering
{
    public class DissimilarityMatrix
    {
        private readonly Dictionary<ClusterPair, double> _distanceMatrix;

        //private readonly ConcurrentDictionary<ClusterPair, double> _distanceMatrix;

        public int PairsCount => _distanceMatrix.Count;

        public DissimilarityMatrix(MultiClusters clusters, double[,] distanceMatrix, double diameter)
        {
            // Building distance matrix by going through initial clusters.
            _distanceMatrix = new Dictionary<ClusterPair, double>(GetCapacity(clusters.Count));


            //int numProcs = Environment.ProcessorCount;
            //int concurrencyLevel = numProcs * 2;
            //_distanceMatrix = new ConcurrentDictionary<ClusterPair, double>(concurrencyLevel, GetCapacity(clusters.Count));


            for (int i = 0; i < clusters.Count; i++)
            {
                for (int j = i + 1; j < clusters.Count; j++)
                {
                    var c1 = clusters[i];
                    var c2 = clusters[j];
                    var dist = distanceMatrix[i, j];

                    if (dist > diameter)
                        continue;

                    ClusterPair cp = new ClusterPair(c1, c2);
                    AddClusterPair(cp, dist);
                }
            }

            // Parallel.For(0, clusters.Count, i =>
            //{
            //    for (int j = i + 1; j < clusters.Count; j++)
            //    {
            //        var c1 = clusters[i];
            //        var c2 = clusters[j];
            //        var dist = distanceMatrix[i, j];

            //        ClusterPair cp = new ClusterPair(c1, c2);
            //        AddClusterPair(cp, dist);
            //    }
            //});
        }


        /// <summary>
        /// Public method for finding the closest cluster-pair in distance matrix.
        /// </summary>
        /// <param name="dist">The minimum distance for closest cluster-pair.</param>
        /// <returns></returns>
        public ClusterPair GetClosestClusterPair(out double dist)
        {
            BinaryMinHeap<PairNode> minHeap = new BinaryMinHeap<PairNode>(_distanceMatrix.Count);
            foreach (var pair in _distanceMatrix)
            {
                minHeap.Add(new PairNode(pair.Key, pair.Value));
            }

            var min = minHeap.Peek();
            dist = min.Value;
            minHeap.Clear();

            return min.Pair;
        }


        /// <summary>
        /// After found the closest <see cref="ClusterPair"/>, the current matrix needs to be updated.
        /// </summary>
        /// <param name="clusterPair">The current closest cluster-pair.</param>
        /// <param name="clusters">The current clusters collection, also will be updated internally.</param>
        public void UpdateMatrix(ClusterPair clusterPair, int newClusterIndex, MultiClusters clusters)
        {
            // Create new cluster by merge cluster-pair.
            Cluster merged = new Cluster(newClusterIndex, clusterPair, _distanceMatrix[clusterPair]);

            // Remove cluster pair.
            RemoveClusterPair(clusterPair);
            clusters.Remove(clusterPair.Cluster1);
            clusters.Remove(clusterPair.Cluster2);

            // Update distance matrix. Can run parallel.
            foreach (var cluster in clusters)
            {
                var flag = GetCompleteLinkageDistance(clusterPair, cluster, out double dist);

                // Add new cluster-pair to matrix.
                if (flag)
                {
                    ClusterPair updateCurrent = new ClusterPair(cluster, merged);
                    AddClusterPair(updateCurrent, dist);
                }
            }

            //Parallel.ForEach(clusters, cluster =>
            //{
            //  var dist = GetCompleteLinkageDistance(clusterPair, cluster);

            //  ClusterPair clusterPair1 = new ClusterPair(cluster, clusterPair.Cluster1);
            //  ClusterPair clusterPair2 = new ClusterPair(cluster, clusterPair.Cluster2);

            //  RemoveClusterPair(clusterPair1);
            //  RemoveClusterPair(clusterPair2);

            //    // Add new cluster-pair to matrix.
            //    ClusterPair updateCurrent = new ClusterPair(cluster, merged);
            //  AddClusterPair(updateCurrent, dist);

            //});


            // Add new cluster to clusters.
            clusters.Add(merged);
        }

        private double GetClusterPairDistance(Cluster cluster1, Cluster cluster2)
        {
            ClusterPair entries = new ClusterPair(cluster1, cluster2);
            if (_distanceMatrix.ContainsKey(entries))
            {
                var dist = _distanceMatrix[entries];
                RemoveClusterPair(entries);
                return dist;
            }
            else
            {
                // current distance matrix doesn't have this cluster pair, because the distance between two clusters is larger than the given diameter.
                return double.PositiveInfinity;
            }
        }

        private bool GetCompleteLinkageDistance(ClusterPair clusterPair, Cluster cluster, out double completeDist)
        {
            double d1 = GetClusterPairDistance(clusterPair.Cluster1, cluster);
            double d2 = GetClusterPairDistance(clusterPair.Cluster2, cluster);

            completeDist = Math.Max(d1, d2);

            // If one distance is infinity, means we don't need to add this new cluster-pair.
            return completeDist != double.PositiveInfinity;
        }


        /// <summary>
        /// Private method for adding a cluster-pair and distance to distance matrix.
        /// </summary>
        /// <param name="clusterPair"></param>
        /// <param name="distance"></param>
        private void AddClusterPair(ClusterPair clusterPair, double distance)
        {
            //_distanceMatrix.TryAdd(clusterPair, distance);
            _distanceMatrix.Add(clusterPair, distance);
        }


        /// <summary>
        /// Private method for removing a cluster-pair.
        /// </summary>
        /// <param name="clusterPair"></param>
        private void RemoveClusterPair(ClusterPair clusterPair)
        {
            if (!_distanceMatrix.ContainsKey(clusterPair))
                throw new Exception($"Can't find {clusterPair}.");

            _distanceMatrix.Remove(clusterPair);
        }



        private int GetCapacity(int count)
        {
            count--;
            int capacity = count;
            while (count > 1)
            {
                count--;
                capacity += count;
            }

            return capacity;
        }

    }
}
