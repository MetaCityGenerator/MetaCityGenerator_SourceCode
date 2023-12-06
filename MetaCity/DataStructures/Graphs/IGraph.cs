using System;
using System.Collections.Generic;


namespace MetaCity.DataStructures.Graphs
{
    public interface IGraph<T> where T : IComparable<T>
    {
        /// <summary>
        /// Returns true if graph is directed ; false undirected.
        /// </summary>
        bool IsDirected { get; }

        /// <summary>
        /// Returns true if graph is weighted ; false unweighted.
        /// </summary>
        bool IsWeighted { get; }

        /// <summary>
        /// Gets the count of vetices.
        /// </summary>
        int VerticesCount { get; }

        /// <summary>
        /// Gets the count of edges.
        /// </summary>
        int EdgesCount { get; }

        /// <summary>
        /// Returns the list of vertices.
        /// </summary>
        IEnumerable<T> Vertices { get; }

        /// <summary>
        /// Returns an enumerable collection of edges.
        /// </summary>
        IEnumerable<IEdge<T>> Edges { get; }

        /// <summary>
        /// Get all incoming edges from vertex.
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        IEnumerable<IEdge<T>> IncomingEdges(T vertex);

        /// <summary>
        /// Get all outgoind edges from vertex.
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        IEnumerable<IEdge<T>> OutgoingEdges(T vertex);

        /// <summary>
        /// Connects two vertices together.
        /// </summary>
        /// <param name="firstVertex"></param>
        /// <param name="secondVertex"></param>
        /// <returns></returns>
        bool AddEdge(T firstVertex, T secondVertex);

        /// <summary>
        /// Deletes an edge, if exists an edge between two vertices.
        /// </summary>
        /// <param name="firstVertex"></param>
        /// <param name="secondVertex"></param>
        /// <returns></returns>
        bool RemoveEdge(T firstVertex, T secondVertex);

        /// <summary>
        /// Adds a list of all the vertices to the graph ( A graph can use this method multiply times).
        /// </summary>
        /// <param name="collection"></param>
        void AddVertices(IList<T> collection);

        /// <summary>
        /// Adds a new vertex to graph.
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        bool AddVertex(T vertex);

        /// <summary>
        /// Deletes a specified vertex from graph.
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        bool RemoveVertex(T vertex);

        /// <summary>
        /// Checks whether two vertices are connected.
        /// </summary>
        /// <param name="firstVertex"></param>
        /// <param name="secondVertex"></param>
        /// <returns></returns>
        bool HasEdge(T firstVertex, T secondVertex);

        /// <summary>
        /// Determines whether this graph contains a specified vertex.
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        bool HasVertex(T vertex);

        /// <summary>
        /// Returns the neighbours doubly-linked list for the specified vertex.
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        LinkedList<T> Neighbours(T vertex);

        /// <summary>
        /// Returns the degree of the specified vertex.
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        int Degree(T vertex);

        /// <summary>
        /// Returns a human-readable string of the graph.
        /// </summary>
        /// <returns></returns>
        string ToReadable();

        /// <summary>
        /// A depth first search traversal of the graph in a LIFO manner. 
        /// It considers the first inserted vertex as the start-vertex for the walk.
        /// </summary>
        /// <returns></returns>
        IEnumerable<T> DepthFirstWalk();

        /// <summary>
        /// A depth first search traversal of the graph, starting from a specified vertex in a LIFO manner.
        /// </summary>
        /// <param name="startingVertex"></param>
        /// <returns></returns>
        IEnumerable<T> DepthFirstWalk(T startingVertex);

        /// <summary>
        /// A breadth first search traversal of the graph in a FIFO manner.
        /// It considers the first inserted vertex as the start-vertex for the walk.
        /// </summary>
        /// <returns></returns>
        IEnumerable<T> BreadthFirstWalk();

        /// <summary>
        /// A breadth first search traversal of the graph, starting from a specified vertex in a FIFO manner.
        /// </summary>
        /// <param name="startingVertex"></param>
        /// <returns></returns>
        IEnumerable<T> BreadthFirstWalk(T startingVertex);


        /// <summary>
        /// Clear this graph.
        /// </summary>
        void ClearGraph();
    }
}
