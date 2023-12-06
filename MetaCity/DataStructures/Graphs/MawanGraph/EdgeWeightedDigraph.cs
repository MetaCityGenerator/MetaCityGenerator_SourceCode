using System;
using System.Collections.Generic;


namespace Mawan.DataStructures
{
    public class EdgeWeightedDigraph
    {
        private int v = 0;
        private int e = 0;
        private readonly List<DirectedEdge> all_edges = new List<DirectedEdge>();
        private readonly List<List<DirectedEdge>> adj = new List<List<DirectedEdge>>();

        public EdgeWeightedDigraph(int v = 0)
        {
            this.v = v;
            for (int i = 0; i < v; i++) adj.Add(new List<DirectedEdge>());
        }

        public int V => v;

        public int E => e;

        public void AddVertice()
        {
            v++;
            adj.Add(new List<DirectedEdge>());
        }

        public void AddEdge(DirectedEdge e)
        {
            int origin = e.From;
            all_edges.Add(e);
            adj[origin].Add(e);
            this.e++;
        }

        public ICollection<DirectedEdge> Adj(int v) => adj[v];

        public ICollection<DirectedEdge> Edges => all_edges;

        public EdgeWeightedDigraph Copy()
        {
            EdgeWeightedDigraph g = new EdgeWeightedDigraph(v);
            foreach (DirectedEdge de in all_edges) g.AddEdge(de);
            return g;
        }
    }
}
