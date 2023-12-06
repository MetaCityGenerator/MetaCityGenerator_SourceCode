using Rhino.Geometry;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MetaCity.Algorithms.Graphs;
using MetaCity.DataStructures.Graphs;
using MetaCity.Planning.SpaceSyntax;
using MetaCity.Planning.UrbanDesign;



namespace MetaCity.Planning.FacilityLocation
{
    public class Rh_CoverageComputing
    {

        private readonly double _tolerance;


        // Input curves for all roads without any cleaning work or splitting.
        public readonly Curve[] _inputAllRoads;

        // Input curves for all the sites.
        private readonly Curve[] _inputAllSites;

        private readonly HashSet<Point3d> _centroids;

        private readonly Rh_CityGraphBuilder _network;

        public Curve[] RoadsNetwork { get; }

        public Rh_CoverageComputing(Curve[] inputAllRoads, Curve[] inputAllSites, double tolerance)
        {
            _inputAllRoads = GetCleanedRoadSegments(inputAllRoads);
            _inputAllSites = inputAllSites;

            // The minimum tolerance should be 1E-8.
            //_tolerance = tolerance * 1E-3 < 1E-8 ? 1E-8 : tolerance * 1E-3;
            _tolerance = tolerance;

            Rh_CityGraphBuilder.PointEqualityComparer comparer = new Rh_CityGraphBuilder.PointEqualityComparer(_tolerance);
            _centroids = new HashSet<Point3d>(comparer);
            for (int i = 0; i < _inputAllSites.Length; i++)
            {
                var centroid = AreaMassProperties.Compute(_inputAllSites[i]).Centroid;
                _centroids.Add(centroid);
            }


            //For debug 
            var allRoadsNew = AddConnectionsFromSiteToRoad(); //TODO: for each site, should has four connections.

            RoadsNetwork = GetCleanedRoadSegments(allRoadsNew);
            _network = new Rh_CityGraphBuilder(RoadsNetwork, _tolerance);
        }


        public int[] GetCoverageIndex(Curve source, double radius, out List<Curve> coverageSites, out List<Point3d> coveragePoints)
        {
            coverageSites = new List<Curve>();
            coveragePoints = new List<Point3d>();

            var centroid = AreaMassProperties.Compute(source).Centroid;

            int sourceVertice = _network.GetPointVertice(centroid);

            Dictionary<int, int> verticesToIndices = new Dictionary<int, int>();

            var vertices = _network.NetworkofMatricWeight.Vertices.ToArray();

            for (int i = 0; i < _network.NetworkofMatricWeight.VerticesCount; i++)
            {
                verticesToIndices.Add(vertices[i], i);
            }

            var computer = new CentralitySingleSourceRadius<UndirectedWeightedSparseGraph<int>, int>(_network.NetworkofMatricWeight, sourceVertice, verticesToIndices, radius);
            var selectedInices = computer.VertexInicesWithinRadius;

            var allVerticesPts = _network._pointsToVertices.Keys.ToArray();

            for (int i = 0; i < selectedInices.Length; i++)
            {
                var pt = allVerticesPts[selectedInices[i]];

                if (_centroids.Contains(pt))
                {
                    coveragePoints.Add(pt);

                    int index = _centroids.ToList().IndexOf(pt);

                    if (index == -1)
                    {
                        // Handle exception. Find index for the closed point in centroids.
                        Point3d[] needles = { pt };
                        index = Rh_CityGraphBuilder.FindCloestPoints(_centroids.ToArray(), needles)[0];
                    }

                    coverageSites.Add(_inputAllSites[index]);
                }
            }
            return selectedInices;
        }



        private Curve[] GetCleanedRoadSegments(Curve[] allRoads)
        {
            Rh_RoadsSplitter prep = new Rh_RoadsSplitter(allRoads, _tolerance);
            return prep.Curves.ToArray();
        }


        /// <summary>
        /// Finding the centroid for all the sites, and connecting centroid to nearest road.
        /// </summary>
        /// <returns></returns>
        private Curve[] AddConnectionsFromSiteToRoad()
        {
            //List<Curve> result = new List<Curve>(_inputAllRoads.Length + 4*_inputAllSites.Length);
            //result.AddRange(_inputAllRoads);

            ConcurrentBag<Curve> result = new ConcurrentBag<Curve>(_inputAllRoads);

            //Selecting the surrounding roads by using siteparameter class.
            var allMidPts = DesignToolbox.GetMidPoints(_inputAllRoads);


            Parallel.For(0, _inputAllSites.Length, i =>
            {
                var site = _inputAllSites[i];

                var centroid = _centroids.ToArray()[i];


                var radiant = SiteParameters.GetRadiant(ref site, _tolerance);
                var needles = SiteParameters.GetEdgesMidPoints(site, radiant);
                var selectedRoadIndices = SiteParameters.FindCloestPoints(allMidPts, needles);


                HashSet<int> indices = new HashSet<int>(selectedRoadIndices);

                foreach (var index in indices)
                {
                    var road = _inputAllRoads[index];
                    var flag = road.ClosestPoint(centroid, out double t);

                    if (flag && t != road.Domain.Min && t != road.Domain.Max)
                    {
                        var endPt = road.PointAt(t);
                        PolylineCurve connection = new PolylineCurve(new Point3d[] { centroid, endPt });
                        result.Add(connection);
                    }
                }

                //List<Point3d> tempPts = new List<Point3d>(_inputAllRoads.Length);
                //List<double> tempDist = new List<double>(_inputAllRoads.Length);

                //for (int r = 0; r < _inputAllRoads.Length; r++)
                //{
                //    var road = _inputAllRoads[r];
                //    var flag = road.ClosestPoint(centroid, out double t, _radius);
                //    if (flag)
                //    {
                //        var ptGet = road.PointAt(t);
                //        tempPts.Add(ptGet);
                //        tempDist.Add(centroid.DistanceTo(ptGet));
                //    }
                //}
                //int minIndex = tempDist.IndexOf(tempDist.Min());

                //PolylineCurve connection = new PolylineCurve(new Point3d[] { centroid, tempPts[minIndex] });
                //result.Add(connection);
            });

            return result.ToArray();
        }

    }
}
