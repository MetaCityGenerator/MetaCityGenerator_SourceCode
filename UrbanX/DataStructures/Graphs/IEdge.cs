using System;

namespace UrbanX.DataStructures.Graphs
{
    /// <summary>
    /// This interface should be implemented by all edges classes.
    /// </summary>
    public interface IEdge<TVertex> : IComparable<IEdge<TVertex>> where TVertex : IComparable<TVertex>
    {
        /// <summary>
        /// Returns true if graph is weighted ; false unweighted.
        /// </summary>
        bool IsWeighted { get; }

        /// <summary>
        /// Gets or sets the source of vertex.
        /// </summary>
        TVertex Source { get; set; }

        /// <summary>
        /// Gets or sets the destination of vertex.
        /// </summary>   
        TVertex Destination { get; set; }

        /// <summary>
        /// Gets or sets the weight of edge. 
        /// Unwighted edges can be set as edges of same weight.
        /// </summary>
        double Weight { get; set; }
    }
}
