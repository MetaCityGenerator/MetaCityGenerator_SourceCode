using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetaCity.DataStructures.Geometry3D;
using MetaCity.DataStructures.Graphs;
using MetaCity.Planning.SpaceSyntax;

namespace MetaCity.Planning.NetworkAnalysis
{
    public sealed class NetworkComputing
    {
        private List<int> _componentSize;
        private int[] _componentType;

        public List<int> ComponentSize => _componentSize;
        public int[] ComponentType =>_componentType;


        public NetworkComputing(GraphBuilder3Df graphbuilder) 
        {
            var res=ComputeGiantComponentSize(graphbuilder);
            _componentSize=res.Item1;
            _componentType=res.Item2;
        }

        private (List<int>, int[]) ComputeGiantComponentSize(GraphBuilder3Df graphbuilder)
        {
            var graph = graphbuilder.Graph;
            var adj = graph.Edges;
            var vCount = graph.VerticesCount;
            int[] visited = new int[vCount];                                                                                                                         
            int[] type=new int[vCount];
            //UPolyline[] roadsOut = new UPolyline[vCount];
            
            List<int> componetSizeList=new List<int>();

            var adjList = Edge2Node(graphbuilder);
            int increment = 1;
            for (int i = 0; i < vCount; i++)
            {
                increment++;
                int componentSize = DFS(i, adjList, visited, increment);
                
                if (componentSize != 0)
                    componetSizeList.Add(componentSize);
            }
            return (componetSizeList, visited);
        }

        private int DFS(int v, List<int>[] adj, int[] visited, int increment)
        {
            if (visited[v] != 0)
            {
                return 0;
            }
            visited[v] = increment;
            int size = 1;
            foreach (int u in adj[v])
            {
                size += DFS(u, adj, visited, increment);
            }
            return size;
        }


        private List<int>[] Edge2Node(GraphBuilder3Df graphbuilder)
        {
            var graph = graphbuilder.Graph;
            var adj = graph.Edges;
            
            SortedDictionary<int, List<int>> edgesDic = new SortedDictionary<int, List<int>>();
            for (int i = 0; i < graph.VerticesCount; i++)
                edgesDic.Add(i, new List<int>());

            foreach (var item in adj)
            {
                edgesDic[item.U].Add(item.V);
                edgesDic[item.V].Add(item.U); 
            }
            return edgesDic.Values.ToArray();
        }
    }
}
