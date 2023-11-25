using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;

using UrbanX.Algorithms.Graphs;
using UrbanX.Algorithms.Utility;
using UrbanX.DataStructures.Graphs;

namespace UrbanX.Planning.Water
{
    public class NetworkOptimization
    {
        /// <summary>
        /// Internal dictionary for finding the index of a specified point. Point3d can be used as key
        /// </summary>
        private readonly Dictionary<Point3d, int> _pointsToIndices;
        /// <summary>
        /// Internal dictionary for finding the polyline of a specified link. key is a string combined firstnode index and endnode index. eg"36" means the firstnode of this line's index is 3.
        /// </summary>
        private readonly Dictionary<string, Polyline> _linksToGeometry;

        /// <summary>
        /// Internal dictionary for quering pipe by using key. key is a string combined firstnode index and endnode index.
        /// </summary>
        private readonly Dictionary<string, Pipe> _linksToPipe;
        /// <summary>
        /// Internal graph for water network.
        /// </summary>
        private readonly UndirectedWeightedSparseGraph<int> _graph;



        private readonly List<Junctions> _junctions;

        private readonly List<Reservoir> _reservoirs;
        private readonly Dictionary<int, DijkstraShortestPaths<UndirectedWeightedSparseGraph<int>, int>> _allShortestPaths;

        public List<double> LinksFlowRate { get; }
        public List<double> LinksDiameter { get; }
        public List<Curve> Curves { get; }

        public Polyline[] VoronoiCells { get; private set; }

        public double[] WaterDemands
        {
            get
            {
                double[] demands = new double[_junctions.Count];
                for (int i = 0; i < _junctions.Count; i++)
                {
                    demands[i] = _junctions[i].WaterDemand;
                }
                return demands;
            }
        }
        /// <summary>
        /// Constructors: nodes list include sources.    
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="links"></param>
        /// <param name="source"></param>
        public NetworkOptimization(IList<Point3d> nodes, IList<Polyline> links, IList<Point3d> sources, SortedDictionary<double, double> pipeData, IList<Polyline> blocks, IList<double> siteDemands, Rectangle3d boundary, double tolerance = 0.001)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }
            if (links == null)
            {
                throw new ArgumentNullException(nameof(links));
            }
            if (sources.Count == 0)
            {
                throw new ArgumentException("The source vertex doesn't exist in this graph.");
            }

            // 0. Instiantiating all the fields.
            _pointsToIndices = new Dictionary<Point3d, int>(nodes.Count + sources.Count);
            _linksToGeometry = new Dictionary<string, Polyline>(links.Count);
            _linksToPipe = new Dictionary<string, Pipe>(links.Count);

            _graph = new UndirectedWeightedSparseGraph<int>(nodes.Count + sources.Count);
            _junctions = new List<Junctions>(nodes.Count);
            _reservoirs = new List<Reservoir>(sources.Count);

            _allShortestPaths = new Dictionary<int, DijkstraShortestPaths<UndirectedWeightedSparseGraph<int>, int>>(sources.Count);


            // 1. Generate graph, add items in all the lists.
            Initialize(nodes, links, sources);

            // 2. Calculating the shortest path for all the junctions.
            GetShortestPathVertex(_graph, _reservoirs);

            // 3. Calculating the water demand for all the junctions.
            GetDemand(nodes, blocks, siteDemands, boundary, tolerance);

            // 4. Calculating the flowrate for all the pipes.
            GetFlowRate();

            // 5. Calculating the best diameters for all the pipes.
            GetDiameter(pipeData);

            LinksFlowRate = new List<double>(links.Count);
            LinksDiameter = new List<double>(links.Count);
            foreach (var item in _linksToPipe.Values)
            {
                LinksFlowRate.Add(item.FlowRate);
                LinksDiameter.Add(item.Diameter);
            }

            Curves = new List<Curve>(_junctions.Count);
            foreach (var junction in _junctions)
            {
                var curve = GetPathCurve(junction.ShortestPath);
                Curves.Add(curve);
            }
        }

        /// <summary>
        /// Internal method for initialization all the graph, lists and dictionaries.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="links"></param>
        /// <param name="sources"></param>
        private void Initialize(IList<Point3d> nodes, IList<Polyline> links, IList<Point3d> sources)
        {

            // Pointy elements.
            for (int i = 0; i < sources.Count; i++)
            {
                _pointsToIndices.Add(sources[i], i);
                _reservoirs.Add(new Reservoir(i));
            }
            for (int i = 0; i < nodes.Count; i++)
            {
                var junctionVertex = i + sources.Count;
                _pointsToIndices.Add(nodes[i], junctionVertex);
                _junctions.Add(new Junctions(junctionVertex));
            }


            // Generate graph and add link item to pipe list and linksToGeometry dictionary.
            // Use point3d's index representing the TVertex in graph.
            _graph.AddVertices(_pointsToIndices.Values.ToArray());

            // Add edge to this graph.
            for (int i = 0; i < links.Count; i++)
            {
                var link = links[i];

                Point3d startNode, endNode;

                // In case the toolerance error occured during export dxf from epanet that there are some margin between nodes and polyline's ends.
                if (_pointsToIndices.ContainsKey(link.First) && _pointsToIndices.ContainsKey(link.Last))
                {
                    startNode = link.First;
                    endNode = link.Last;
                }
                else
                {
                    Point3d[] needle = { link.First, link.Last };
                    var closestPoints = FindCloestPointInGraph(nodes, needle);

                    startNode = nodes[closestPoints[0]];
                    endNode = nodes[closestPoints[1]];
                }


                _graph.AddEdge(_pointsToIndices[startNode], _pointsToIndices[endNode], link.Length);

                // For digraph, change SortedSet to HashSet.
                var set = new SortedSet<int> { _pointsToIndices[startNode], _pointsToIndices[endNode] };
                string key = $"{set.First()} + {set.Last()}";
                _linksToGeometry.Add(key, link);
                _linksToPipe.Add(key, new Pipe(key, link.Length, i));
            }

        }

        /// <summary>
        /// Internal method for calculating the shortest path in a graph and storing all the path int[] in each junction object.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="sources"></param>
        private void GetShortestPathVertex(UndirectedWeightedSparseGraph<int> graph, IList<Reservoir> sources) // change to reservior
        {

            foreach (var source in sources)
            {

                var tempSpt = new DijkstraShortestPaths<UndirectedWeightedSparseGraph<int>, int>(graph, source.IndexOfNode);
                _allShortestPaths.Add(source.IndexOfNode, tempSpt);
            }

            // Now juncitons list only has the demand junctions' index(or vertex).
            foreach (var junction in _junctions)
            {
                List<double> tempDist = new List<double>(sources.Count);
                foreach (var spt in _allShortestPaths)
                {
                    tempDist.Add(spt.Value.DistanceTo(junction.IndexOfNode));
                }

                var minIndex = tempDist.IndexOf(tempDist.Min());
                var key = _allShortestPaths.Keys.ToArray()[minIndex];

                //junction.ShortestPath = _allShortestPaths[key].ShortestPathTo(junction.IndexOfNode).ToArray();
                junction.UpdateShortestPath(_allShortestPaths[key].ShortestPathTo(junction.IndexOfNode).ToArray());


            }
        }

        /// <summary>
        /// Internal method for calculating the water demand for each junctions. This method relays on another class.
        /// </summary>
        private void GetDemand(IList<Point3d> nodes, IList<Polyline> blocks, IList<double> siteDemands, Rectangle3d boundary, double tolerance)
        {
            var calWaterDemand = new CaculateJunctionDemand(nodes, _junctions, blocks, siteDemands, boundary, tolerance);

            VoronoiCells = calWaterDemand.Cells;
        }

        /// <summary>
        /// Internal method for calculating the flow rate for each pipes in the whole water network.
        /// </summary>
        private void GetFlowRate()
        {
            foreach (var junction in _junctions)
            {
                var pathIndices = junction.ShortestPath;
                for (int i = 0; i < pathIndices.Length - 1; i++)
                {
                    var set = new SortedSet<int> { pathIndices[i], pathIndices[i + 1] };
                    string key = $"{set.First()} + {set.Last()}";

                    //_linksToPipe[key].FlowRate += junction.WaterDemand;
                    _linksToPipe[key].AccumulateFlowRate(junction.WaterDemand);
                }
            }
        }

        /// <summary>
        /// Internal metod for determine the most economic diameter for each pipes in the whole water network.
        /// </summary>
        private void GetDiameter(SortedDictionary<double, double> pipeData)
        {
            // The unit of pipe's diameter should be m. eg.0.2m
            var x = pipeData.Keys.ToArray();
            var y = pipeData.Values.ToArray();

            Statistics.PowerRegression(x, y, out double R2, out double a, out double b);
            if (R2 < 0.5 || a == 0 || b == 0)
            {
                throw new Exception("pipe data is invalid.");
            }

            // The core algorithm of optimization.
            SortedSet<double> threshold = new SortedSet<double>();
            for (int i = 0; i < x.Length - 1; i++)
            {
                double d1 = x[i];
                double d2 = x[i + 1];
                const double m = 5.33d;
                const double f = 1d;
                const double e = 1 / 3d;

                var coe1 = Math.Pow(m / (f * b), e);
                var coe2 = Math.Pow((Math.Pow(d2, b) - Math.Pow(d1, b)) / (Math.Pow(d1, -m) - Math.Pow(d2, -m)), e);
                // *1000 because we need to convert m3 to L.
                threshold.Add(coe1 * coe2 * 1000);
            }

            foreach (var pipe in _linksToPipe.Values)
            {
                var tempSet = new SortedSet<double>(threshold);
                if (pipe.FlowRate == 0)
                {
                    //pipe.Diameter = x.First();
                    pipe.UpdateDiameter(x.First());
                }

                tempSet.Add(pipe.FlowRate);
                var index = tempSet.ToList().IndexOf(pipe.FlowRate);
                //pipe.Diameter = x[index];
                pipe.UpdateDiameter(x[index]);
            }
        }

        private Curve GetPathCurve(int[] pathIndex)
        {
            List<PolylineCurve> result = new List<PolylineCurve>();
            for (int i = 0; i < pathIndex.Count() - 1; i++)
            {
                var set = new SortedSet<int> { pathIndex[i], pathIndex[i + 1] };
                string key = $"{set.First()} + {set.Last()}";
                result.Add(_linksToGeometry[key].ToPolylineCurve());
            }
            var jointCurve = Curve.JoinCurves(result);

            return jointCurve.First();
        }

        public static int[] FindCloestPointInGraph(IList<Point3d> allNodes, IList<Point3d> needles)
        {
            var indicesArray = RTree.Point3dClosestPoints(allNodes, needles, double.MaxValue).ToArray();

            int[] result = new int[indicesArray.Length];
            for (int i = 0; i < indicesArray.Length; i++)
            {
                result[i] = indicesArray[i].First();
            }

            return result;
        }
    }
}
