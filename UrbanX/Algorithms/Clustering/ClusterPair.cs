using System;


namespace UrbanX.Algorithms.Clustering
{
    public class ClusterPair : IEquatable<ClusterPair>, IDisposable
    {
        #region constructors
        public ClusterPair(Cluster cluster1, Cluster cluster2)
        {
            this.Cluster1 = cluster1;
            this.Cluster2 = cluster2;
        }

        #endregion

        #region class properties
        public Cluster Cluster1 { get; }

        public Cluster Cluster2 { get; }

        #endregion

        public bool Equals(ClusterPair other)
        {
            var flag1 = this.Cluster1.Equals(other.Cluster1) && this.Cluster2.Equals(other.Cluster2);
            var flag2 = this.Cluster1.Equals(other.Cluster2) && this.Cluster2.Equals(other.Cluster1);
            return flag1 | flag2;
        }


        public override int GetHashCode()
        {
            // using this hashcode as key with two entries in HashSet<ClusterPair>.
            return this.Cluster1.GetHashCode() ^ this.Cluster2.GetHashCode();
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
                var cp = (ClusterPair)obj;
                return this.Equals(cp);
            }
        }

        public override string ToString()
        {
            return $"ClusterPair:{this.Cluster1},{this.Cluster2}";
        }

        public void Dispose()
        {
            this.Cluster1.Dispose();
            this.Cluster2.Dispose();
        }
    }

    /// <summary>
    /// For minHeap to find the closest cluster-pair.
    /// </summary>
    public class PairNode : IComparable<PairNode>
    {
        public ClusterPair Pair { get; }

        public double Value { get; }

        public PairNode(ClusterPair pair, double value)
        {
            Pair = pair;
            Value = value;
        }

        public int CompareTo(PairNode other)
        {
            var c = this.Value.CompareTo(other.Value);
            return c == 0 ? this.Pair.Cluster1.Id.CompareTo(other.Pair.Cluster1.Id) : c;
        }
    }
}
