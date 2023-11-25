using System;

using UrbanX.DataStructures.Utility;

namespace UrbanX.DataStructures.Graphs
{
    /// <summary>
    /// The graph Weighted edge class.
    /// </summary>
    /// <typeparam name="TVertex"></typeparam>
    public class WeightedEdge<TVertex> : IEdge<TVertex> where TVertex : IComparable<TVertex>
    {
        /// <summary>
        /// Gets or sets the source
        /// </summary>
        public TVertex Source { get; set; }

        /// <summary>
        /// Gets or sets the destination.
        /// </summary>
        public TVertex Destination { get; set; }

        /// <summary>
        /// Gets or sets the weight of edge.
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// Returns true if graph is weighted ; false unweighted.
        /// </summary>
        public bool IsWeighted
        {
            get { return true; }
        }

        /// <summary>
        /// Constructor of WeightedEdge class.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="weight"></param>
        public WeightedEdge(TVertex src, TVertex dst, double weight)
        {
            Source = src;
            Destination = dst;
            Weight = weight;
        }


        #region IComparable implementation
        public int CompareTo(IEdge<TVertex> other)
        {
            if (other == null)
            {
                return -1;
            }

            // Determine if both the source and destination node are equal( first vertex ,second vertex).
            bool areNodesEqual = Source.IsEqualTo(other.Source) && Destination.IsEqualTo(other.Destination);
            if (!areNodesEqual)
            {
                return -1;
            }
            // Determine whether the weight of two edges are equal.
            return Weight.CompareTo(other.Weight);
        }
        #endregion
    }

}
