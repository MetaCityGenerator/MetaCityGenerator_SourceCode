using System;
using System.Collections.Generic;

namespace UrbanX.DataStructures.Graphs
{
    /// <summary>
    /// This interface should be implemented adoubleside the IGraph interface.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IWeightedGraph<T> where T : IComparable<T>
    {
        /// <summary>
        /// Connects two vertices together with a weight, in the direction from first vertex to second vertex.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="weight"></param>
        /// <returns></returns>
        bool AddEdge(T source, T destination, double weight);

        /// <summary>
        /// Updates the edge weight from source to destinatoin.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="weight"></param>
        /// <returns></returns>
        bool UpdateEdgeWeight(T source, T destination, double weight);

        /// <summary>
        /// Get edge object from source to destination.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        WeightedEdge<T> GetEdge(T source, T destination);

        /// <summary>
        /// Returns the edge weight from source to detination.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        double GetEdgeWeight(T source, T destination);

        /// <summary>
        /// Returns the neighbours of a vertex as a dictionary of nodes-to-weights.
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        Dictionary<T, double> NeighboursMap(T vertex);
    }
}
