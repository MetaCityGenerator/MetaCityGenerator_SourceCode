using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace MetaCity.Algorithms.Clustering
{
    /// <summary>
    /// Structure for storing the clustering result.
    /// </summary>
    public class MultiClusters : ICollection<Cluster>, IDisposable
    {
        // Storing all the clusters.
        private readonly HashSet<Cluster> _clusters;

        public int Count => _clusters.Count;

        public bool IsReadOnly => false;

        public Cluster this[int index] => _clusters.ElementAt(index);

        public HashSet<Cluster> SubClusters => _clusters;

        #region constructors
        public MultiClusters()
        {
            this._clusters = new HashSet<Cluster>();
        }

        public MultiClusters(int capacity)
        {
            this._clusters = new HashSet<Cluster>(capacity);
        }

        public MultiClusters(IEnumerable<Cluster> collection)
        {
            this._clusters = new HashSet<Cluster>(collection);
        }
        #endregion

        public void Add(Cluster item)
        {
            _clusters.Add(item);
        }


        public void Clear()
        {
            _clusters.Clear();
        }

        public bool Contains(Cluster item)
        {
            return _clusters.Contains(item);
        }

        public void CopyTo(Cluster[] array, int arrayIndex)
        {
            _clusters.CopyTo(array, arrayIndex);
        }

        public bool Remove(Cluster item)
        {
            return _clusters.Remove(item);
        }

        public void RemoveClusterPair(ClusterPair clusterPair)
        {
            this.Remove(clusterPair.Cluster1);
            this.Remove(clusterPair.Cluster2);
        }

        public Cluster[] ToArray()
        {
            return _clusters.ToArray();
        }

        public void Dispose()
        {
            this.Clear();
        }

        public IEnumerator<Cluster> GetEnumerator()
        {
            return _clusters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
