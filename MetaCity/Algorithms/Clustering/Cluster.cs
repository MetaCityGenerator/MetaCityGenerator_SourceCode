using System;
using System.Collections.Generic;

namespace MetaCity.Algorithms.Clustering
{
    public class Cluster : IEquatable<Cluster>, IDisposable
    {
        /// <summary>
        /// Unique id for identifing cluster.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// The largest distance between all the children clusters.
        /// </summary>
        public double Diameter { get; }

        /// <summary>
        /// All the unique ids for all the children clusters.
        /// </summary>
        public List<int> Children { get; }

        /// <summary>
        /// The number of children.
        /// </summary>
        public int Count => Children.Count;


        /// <summary>
        /// Left child cluster.
        /// </summary>
        public Cluster LeftCluster { get; }

        /// <summary>
        /// Right child cluster.
        /// </summary>
        public Cluster RightCluster { get; }

        /// <summary>
        /// Coordinates for cluster centroid.
        /// </summary>
        public (double X, double Y, double Z) Centroid => (CentroidSum.X / Count, CentroidSum.Y / Count, CentroidSum.Z / Count);

        /// <summary>
        /// Coordinates sum for all the children clusters.
        /// </summary>
        public (double X, double Y, double Z) CentroidSum { get; }
        /// <summary>
        /// Constructor for creating a single cluster.
        /// </summary>
        /// <param name="clusterId"></param>
        public Cluster(int clusterId, double x, double y, double z = 0)
        {
            this.Id = clusterId;
            this.Diameter = 0;

            // Single cluster has no child.
            this.Children = new List<int> { clusterId };
            this.CentroidSum = (x, y, z);
        }


        /// <summary>
        /// Cluster constructor for merging a <see cref="ClusterPair"/>.
        /// </summary>
        public Cluster(int clusterId, ClusterPair clusterPair, double diameter)
        {
            this.Id = clusterId;
            this.Diameter = diameter;
            this.Children = new List<int>(clusterPair.Cluster1.Count + clusterPair.Cluster2.Count);
            this.LeftCluster = clusterPair.Cluster1;
            this.RightCluster = clusterPair.Cluster2;

            // Merging all the children into this cluster.
            AddChildren(clusterPair.Cluster1);
            AddChildren(clusterPair.Cluster2);

            // Renew centroid's coordinatesSum.
            this.CentroidSum = (LeftCluster.CentroidSum.X + RightCluster.CentroidSum.X, LeftCluster.CentroidSum.Y + RightCluster.CentroidSum.Y, LeftCluster.CentroidSum.Z + RightCluster.CentroidSum.Z);
        }

        /// <summary>
        /// Method for adding all the children from a <see cref="Cluster"/>.
        /// </summary>
        /// <param name="cluster"></param>
        private void AddChildren(Cluster cluster)
        {
            this.Children.AddRange(cluster.Children);
        }


        public override int GetHashCode() => (this.Id, this.Diameter, this.Count).GetHashCode();


        public override string ToString()
        {
            return $"C_{this.Id} [Diam:{this.Diameter};Count{this.Count}]";
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || this.GetType() != obj.GetType())
            {
                //Check for null and compare run-time types.
                return false;
            }
            else
            {
                var c = (Cluster)obj;
                return this.Equals(c);
            }
        }

        public bool Equals(Cluster other)
        {
            return other.Id == this.Id && other.Diameter == this.Diameter && other.Count == this.Count;
        }


        public void Dispose()
        {
            this.Children.Clear();
        }
    }
}
