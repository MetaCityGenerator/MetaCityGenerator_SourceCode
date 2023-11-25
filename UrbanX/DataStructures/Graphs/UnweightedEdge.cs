using System;

using UrbanX.DataStructures.Utility;

namespace UrbanX.DataStructures.Graphs
{
    /// <summary>
    /// The graph edge class.
    /// </summary>
    /// <typeparam name="TVertex"></typeparam>
    public class UnweightedEdge<TVertex> : IEdge<TVertex> where TVertex : IComparable<TVertex>
    {
        /// <summary>
        /// Gets or sets the source vertex.
        /// </summary>
        public TVertex Source { get; set; }

        /// <summary>
        /// Gets or sets the destinaiton vertex.
        /// </summary>
        public TVertex Destination { get; set; }

        /// <summary>
        /// Gets or sets the weight of edge. 
        /// Unwighted edges can be set as edges of same weight.
        /// </summary>
        public double Weight
        {
            get { throw new NotImplementedException("Unweighted edges don't have weights."); }
            set { throw new NotImplementedException("Unweighted edges don't have weights."); }
            //get { return _edgeWeight; }
        }

        /// <summary>
        /// Returns true if graph is weighted ; false unweighted.
        /// </summary>
        public bool IsWeighted
        {
            get { return false; }
        }

        /// <summary>
        /// Constructor of unweightedEdge class.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        public UnweightedEdge(TVertex src, TVertex dst)
        {
            Source = src;
            Destination = dst;
        }


        #region IComparable implementation
        public int CompareTo(IEdge<TVertex> other)
        {
            if (other == null)
            {
                return -1;
            }

            bool areNodesEqual = Source.IsEqualTo(other.Source) && Destination.IsEqualTo(other.Destination);
            if (!areNodesEqual)
            {
                return -1;
            }
            return 0;
        }
        #endregion
    }
}
