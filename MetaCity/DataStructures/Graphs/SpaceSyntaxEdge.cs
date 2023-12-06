using System;
using System.Numerics;

namespace MetaCity.DataStructures.Graphs
{
    /// <summary>
    /// A light weight graph edge structure. This type is immutable.
    /// </summary>
    public readonly struct SpaceSyntaxEdge : IEquatable<SpaceSyntaxEdge> //"DO implement IEquatable<T> on value types." MS
    {
        /// <summary>
        /// One vertex of this edge. V is small than U.
        /// </summary>
        public int V { get; }

        /// <summary>
        /// Another vertex of this edge. U is bigger than V.
        /// </summary>
        public int U { get; }


        /// <summary>
        /// Using Vector can hold three different weights.
        /// When query specific weight, should use x,y,z instead index.
        /// </summary>
        public Vector3 Weights { get; }

    /// <summary>
    /// Constructor of WeightedEdge class.
    /// </summary>
    /// <param name="v"></param>
    /// <param name="u"></param>
    /// <param name="weight"></param>
    public SpaceSyntaxEdge(int v, int u, float w1, float w2 = float.NaN, float w3 = float.NaN)
        {
            if (v == u)
                throw new ArgumentException("Self-loop is not supported.");
            if (w1 < 0 || w2 < 0 || w3 < 0)
                throw new ArgumentException("Weight is invalided.");

            this.V = Math.Min(v, u);
            this.U = Math.Max(v, u);

            Weights = new Vector3(w1, w2, w3);
        }


        public override string ToString() => $"{V}-->{U}: {Weights}";

        /// <summary>
        /// Returns the endpoint of this edge that is different from the given vertex.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public int OtherVertex(int v)
        {
            if (v == this.V)
                return this.U;
            else if (v == this.U)
                return this.V;
            else
                throw new ArgumentException("Illegal input vertex.");
        }


        /// <summary>
        /// The Object.Equals method on value types causes boxing, and its default implementation is not very efficient, because it uses reflection. 
        /// Equals can have much better performance and can be implemented so that it will not cause boxing.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals( SpaceSyntaxEdge other)
        {
            // Determine if both the source and destination node are equal( first vertex ,second vertex).
            bool areNodesEqual = this.V == other.V && this.U == other.U;
            if (areNodesEqual)
            {
                return this.Weights == other.Weights;
            }
            else
                return false;
        }


        public override int GetHashCode()
        {
            return HashCode.Combine(V, U);
        }
    }
}
