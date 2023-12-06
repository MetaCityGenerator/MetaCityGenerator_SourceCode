using NetTopologySuite.Algorithm;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.Operation.Distance;
using NetTopologySuite.Precision;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MetaCity.Algorithms.Graphs;
using MetaCity.DataStructures.Graphs;
using MetaCity.Planning.Utility;

namespace MetaCity.Planning.FacilityLocation
{
    public class Nts_CoverageComputing
    {
        private readonly GeometryFactory _geometryFactory;

        // Input curves for all roads without any cleaning work or splitting.
        private readonly MultiLineString _inputAllRoads;

        // Input curves for all the sites.
        private readonly MultiPolygon _inputAllSites;


        private readonly int[] _connectionsForEachSite;


        private readonly Dictionary<LineString, string> _belongToSite;


        private readonly Nts_CityGraphBuilder _cityGraphBuilder;



        /// <summary>
        /// Cleaned all roads including newly added linestring from site to roads.
        /// </summary>
        public MultiLineString RoadsLineString { get; }



        /// <summary>
        /// Cleaned all sites polygons.
        /// </summary>
        public MultiPolygon SitesPolygon => _inputAllSites;



        /// <summary>
        /// City graph builder contains internal graph.
        /// </summary>
        public Nts_CityGraphBuilder CityGraphBuilder => _cityGraphBuilder;



        /// <summary>
        /// Constructor for CoverageComputing class.
        /// </summary>
        /// <param name="inputRoads">Input data should have been cleaned before by using <see cref="DataCleaning.CleanMultiLineString(GeometryCollection, GeometryFactory)"/>.</param>
        /// <param name="inputSites">Input data should have been cleaned before by using <see cref="DataCleaning.CleanMultiPolygon(GeometryCollection, GeometryFactory)"/>.</param>
        public Nts_CoverageComputing(MultiLineString inputRoads, MultiPolygon inputSites)
        {
            #region Step1: Preparation.
            // Getting the geometry factory by reading input.
            var geomFact = inputRoads.Factory;

            // Check geometry factory. Only accept fixed-type precision model.
            if (geomFact.PrecisionModel.IsFloating)
                PrecisionSetting.ChangePrecision(ref geomFact);

            _geometryFactory = geomFact;

            // Preparing input data.
            _inputAllRoads = inputRoads;
            _inputAllSites = inputSites;

            _connectionsForEachSite = new int[_inputAllSites.Count];
            _belongToSite = new Dictionary<LineString, string>(_inputAllSites.Count * 6);

            var _tempSites = _inputAllSites.Buffer(-0.1).IsEmpty ? _inputAllSites : _inputAllSites.Buffer(-0.1);

            PreparedPolygon preparedPolygon = new PreparedPolygon((IPolygonal)_tempSites);
            #endregion Step1 End.


            #region Step2: Add entries from sites to roads. Constructing RoadsNetwork in MultiLineString type.
            // Go througn site polygons. Can run in multipel threads.
            for (int i = 0; i < _inputAllSites.Count; i++)
            {
                // Getting minimumRectangle. Polygon rectangle coordinates should have 5 items. 
                // Offset site polygon towards inside to handle tolrence error.
                var minRectPts = ((Polygon)MinimumDiameter.GetMinimumRectangle(_inputAllSites[i])).Coordinates;

                for (int m = 0; m < minRectPts.Length - 1; m++)
                {
                    var midPt = new Point(new Coordinate(0.5 * (minRectPts[m].X + minRectPts[m + 1].X), 0.5 * (minRectPts[m].Y + minRectPts[m + 1].Y)));
                    var entryPt = new Point(DistanceOp.NearestPoints(midPt, _inputAllSites[i])[1]);


                    // Consider all the roads linestring as a single geometry.
                    var connectPt = new Point(DistanceOp.NearestPoints(entryPt, _inputAllRoads)[1]);
                    var ls = GeometryPrecisionReducer.Reduce(_geometryFactory.CreateLineString(new Coordinate[]
                    { entryPt.Coordinate, connectPt.Coordinate }), _geometryFactory.PrecisionModel);


                    // Check if ls cross with site polygons by using centroid, otherwise will occur error.  
                    var flag = preparedPolygon.Intersects(ls);


                    // Add to BelongToSite is not crosses.
                    if (!flag)
                    {
                        _connectionsForEachSite[i]++;
                        _belongToSite.Add((LineString)ls, $"Site_{i}");
                    }
                }

                // If current site has zero entry connection. Create a connection from centroid.
                // This is a rare case.
                if (_connectionsForEachSite[i] == 0)
                {
                    var connectPt = new Point(DistanceOp.NearestPoints(_inputAllSites[i].Centroid, _inputAllRoads)[1]);
                    var ls = GeometryPrecisionReducer.Reduce(_geometryFactory.CreateLineString(new Coordinate[]
                    { _inputAllSites[i].Centroid.Coordinate, connectPt.Coordinate }), _geometryFactory.PrecisionModel);


                    _connectionsForEachSite[i]++;
                    _belongToSite.Add((LineString)ls, $"Site_{i}");
                }
            }

            var roadsNetwork = new List<Geometry>(_inputAllRoads.Count + _belongToSite.Count);
            roadsNetwork.AddRange(_belongToSite.Keys);
            roadsNetwork.AddRange(_inputAllRoads.Geometries);


            RoadsLineString = DataCleaning.CleanMultiLineString(new GeometryCollection(roadsNetwork.ToArray(), _geometryFactory), _geometryFactory);

            #endregion Step2 End.

            #region Step3: Create city graph.
            _cityGraphBuilder = new Nts_CityGraphBuilder(RoadsLineString);
            _cityGraphBuilder.Build(_belongToSite);

            #endregion Step3 End.
        }



        /// <summary>
        /// Current source point must belong to sites.
        /// </summary>
        /// <param name="source">Centroid point of a source site.</param>
        /// <param name="radius"></param>
        /// <param name="coveragedPts"></param>
        /// <returns></returns>
        public MultiPolygon GetCoveragedSites(Point source, double radius, out MultiPoint coveragedPts)
        {
            //coverageSites = new List<Curve>();
            //coveragePoints = new List<Point3d>();
            //int sourceVertice = _network.GetPointVertice(source);

            HashSet<Polygon> coverageSites = new HashSet<Polygon>();
            HashSet<Point> coveragePoints = new HashSet<Point>();

            // Get entry points in graph.
            var entryPts = _cityGraphBuilder.GetEntryPointVertices(source);

            // Using virtual point as source to connect all the entries for current site.
            // Add source point to current graph.
            _cityGraphBuilder.CityGraph.AddVertex("Source");

            // Add edges to current graph.
            foreach (var entryPt in entryPts)
            {
                _cityGraphBuilder.CityGraph.AddEdge("Source", entryPt, 1.0);
            }

            Dictionary<string, int> verticesToIndices = new Dictionary<string, int>();

            var vertices = _cityGraphBuilder.CityGraph.Vertices.ToArray();

            for (int i = 0; i < vertices.Length; i++)
            {
                verticesToIndices.Add(vertices[i], i);
            }


            var computer = new CentralitySingleSourceRadius<UndirectedWeightedSparseGraph<string>, string>(_cityGraphBuilder.CityGraph, "Source", verticesToIndices, radius);
            var selectedInices = computer.VertexInicesWithinRadius;


            for (int i = 0; i < selectedInices.Length; i++)
            {
                var pt = vertices[selectedInices[i]];

                if (pt.StartsWith("N") || pt == "Source")
                    continue;

                var siteIndex = int.Parse(pt.Split('_').Last());
                coverageSites.Add((Polygon)_inputAllSites[siteIndex]);
                coveragePoints.Add(_inputAllSites[siteIndex].Centroid);

            }

            // To keep the cityGraph as the orignial version.
            _cityGraphBuilder.CityGraph.RemoveVertex("Source");
            coveragedPts = new MultiPoint(coveragePoints.ToArray(), _geometryFactory);
            return new MultiPolygon(coverageSites.ToArray(), _geometryFactory);
        }



        /// <summary>
        /// Computing the shortest distance between all the site-pairs, and storing the result in distance matrix.
        /// </summary>
        /// <returns></returns>
        public double[,] GetDistanceMatrix()
        {
            double[,] distancMatrix = new double[_inputAllSites.Count, _inputAllSites.Count];

            if (_inputAllSites.Count > 30)
            {
                // Run parallel.

                // Partitioning the vertices collection.
                int taskNumber = 6;
                List<Task> tasks = new List<Task>(taskNumber);

                List<int>[] verticesPartition = new List<int>[taskNumber];
                for (int i = 0; i < taskNumber; i++)
                {
                    var rangeCount = _inputAllSites.Count / taskNumber;
                    var rangeInmut = rangeCount;

                    if (i == taskNumber - 1)
                    {
                        rangeCount = _inputAllSites.Count - i * rangeCount;
                    }

                    List<int> indices = new List<int>(rangeCount);
                    for (int r = 0; r < rangeCount; r++)
                    {
                        indices.Add(i * rangeInmut + r);
                    }

                    verticesPartition[i] = indices;
                }

                foreach (var partition in verticesPartition)
                {
                    var t = Task.Run(() =>
                    {
                        var graphCopy = _cityGraphBuilder.CityGraph.DeepCopy();

                        for (int i = 0; i < partition.Count; i++)
                        {
                            var sourceIndex = partition[i];
                            var sourceSite = _inputAllSites[sourceIndex];
                            var sourcePt = sourceSite.Centroid;

                            // Get entry points in graph.
                            var entryPts = _cityGraphBuilder.GetEntryPointVertices(sourcePt);

                            // Add source point to current graph.
                            graphCopy.AddVertex("Source");

                            // Add edges to current graph. Using source centroid to connect all the entries of current site.
                            foreach (var entryPt in entryPts)
                            {
                                // Add virtual distance 1 for building weighted graph. For actuall distance, need to substract 1.
                                graphCopy.AddEdge("Source", entryPt, 1.0);
                            }

                            // Dijkstra.
                            DijkstraShortestPaths<UndirectedWeightedSparseGraph<string>, string> shortestPaths = new DijkstraShortestPaths<UndirectedWeightedSparseGraph<string>, string>(graphCopy, "Source");

                            foreach (var vertex in graphCopy.Vertices)
                            {
                                if (vertex.StartsWith("N") || vertex == "Source")
                                    continue;

                                var dist = shortestPaths.DistanceTo(vertex) - 1.0;
                                var siteIndex = int.Parse(vertex.Split('_').Last());
                                if (distancMatrix[sourceIndex, siteIndex] == 0)
                                {
                                    distancMatrix[sourceIndex, siteIndex] = dist;
                                    distancMatrix[siteIndex, sourceIndex] = dist;
                                }
                                else
                                {
                                    if (dist < distancMatrix[sourceIndex, siteIndex])
                                    {
                                        distancMatrix[sourceIndex, siteIndex] = dist;
                                        distancMatrix[siteIndex, sourceIndex] = dist;
                                    }
                                }
                            }
                            graphCopy.RemoveVertex("Source");
                        }
                    });

                    tasks.Add(t);
                }
                Task.WaitAll(tasks.ToArray());
            }
            else
            {
                // Single thread.

                for (int i = 0; i < _inputAllSites.Count; i++)
                {
                    var site = _inputAllSites[i];
                    var sourcePt = site.Centroid;

                    // Get entry points in graph.
                    var entryPts = _cityGraphBuilder.GetEntryPointVertices(sourcePt);

                    // Add source point to current graph.
                    _cityGraphBuilder.CityGraph.AddVertex("Source");

                    // Add edges to current graph. Using source centroid to connect all the entries of current site.
                    foreach (var entryPt in entryPts)
                    {
                        // Add virtual distance 1 for building weighted graph. For actuall distance, need to substract 1.
                        _cityGraphBuilder.CityGraph.AddEdge("Source", entryPt, 1.0);
                    }

                    // Dijkstra.
                    DijkstraShortestPaths<UndirectedWeightedSparseGraph<string>, string> shortestPaths = new DijkstraShortestPaths<UndirectedWeightedSparseGraph<string>, string>(_cityGraphBuilder.CityGraph, "Source");

                    foreach (var vertex in _cityGraphBuilder.CityGraph.Vertices)
                    {
                        if (vertex.StartsWith("N") || vertex == "Source")
                            continue;

                        var dist = shortestPaths.DistanceTo(vertex) - 1.0;
                        var siteIndex = int.Parse(vertex.Split('_').Last());
                        if (distancMatrix[i, siteIndex] == 0)
                        {
                            distancMatrix[i, siteIndex] = dist;
                            distancMatrix[siteIndex, i] = dist;
                        }
                        else
                        {
                            if (dist < distancMatrix[i, siteIndex])
                            {
                                distancMatrix[i, siteIndex] = dist;
                                distancMatrix[siteIndex, i] = dist;
                            }
                        }
                    }
                    _cityGraphBuilder.CityGraph.RemoveVertex("Source");
                }
            }

            return distancMatrix;
        }


        /// <summary>
        /// Static method for adding entry-segments from sites to original roads network.
        /// </summary>
        /// <param name="inputRoads"></param>
        /// <param name="inputSites"></param>
        /// <returns></returns>
        public static List<Geometry> AddEntriesToRoads(MultiLineString inputRoads, MultiPolygon inputSites, out GeometryFactory gf, out Dictionary<LineString, string> belongToSite)
        {
            #region Step1: Preparation.

            // Getting the geometry factory by reading input.
            var geomFact = inputRoads.Factory;
            if (geomFact.PrecisionModel.IsFloating)
                PrecisionSetting.ChangePrecision(ref geomFact);

            // Check geometry factory. Only accept fixed-type precision model.
            gf = geomFact;


            // Preparing input data.
            var connectionsForEachSite = new int[inputSites.Count];
            belongToSite = new Dictionary<LineString, string>(inputSites.Count * 6);
            var tempSites = inputSites.Buffer(-0.1).IsEmpty ? inputSites : inputSites.Buffer(-0.1);

            PreparedPolygon preparedPolygon = new PreparedPolygon((IPolygonal)tempSites);

            #endregion Step1 End.



            #region Step2: Add entries from sites to roads. Constructing RoadsNetwork in MultiLineString type.
            // Go througn site polygons. Can run in multipel threads.
            for (int i = 0; i < inputSites.Count; i++)
            {
                // Getting minimumRectangle. Polygon rectangle coordinates should have 5 items. 
                // Offset site polygon towards inside to handle tolrence error.
                var minRectPts = ((Polygon)MinimumDiameter.GetMinimumRectangle(inputSites[i])).Coordinates;

                for (int m = 0; m < minRectPts.Length - 1; m++)
                {
                    var midPt = new Point(new Coordinate(0.5 * (minRectPts[m].X + minRectPts[m + 1].X), 0.5 * (minRectPts[m].Y + minRectPts[m + 1].Y)));
                    var entryPt = new Point(DistanceOp.NearestPoints(midPt, inputSites[i])[1]);
                    //var debug = DistanceOp.NearestPoints(midPt, inputSites[i]);

                    // Consider all the roads linestring as a single geometry.
                    //var dubug = DistanceOp.NearestPoints(entryPt, inputRoads); 
                    var connectPt = new Point(DistanceOp.NearestPoints(entryPt, inputRoads)[1]);

                    var ls = GeometryPrecisionReducer.Reduce(gf.CreateLineString(new Coordinate[]
                    { entryPt.Coordinate, connectPt.Coordinate }), gf.PrecisionModel);

                    if (ls.IsEmpty)
                        continue;

                    // Check if ls cross with site polygons by using centroid, otherwise will occur error.  
                    var flag = preparedPolygon.Intersects(ls);


                    // Add to BelongToSite if not crosses.
                    if (!flag)
                    {
                        connectionsForEachSite[i]++;
                        belongToSite.Add((LineString)ls, $"Site_{i}");
                    }
                }

                // If current site has zero entry connection. Create a connection from "one point" on the current site.
                // This is a rare case.
                if (connectionsForEachSite[i] == 0)
                {
                    var connectPt = new Point(DistanceOp.NearestPoints(inputSites[i].InteriorPoint, inputRoads)[1]);
                    var ls = GeometryPrecisionReducer.Reduce(gf.CreateLineString(new Coordinate[]
                    { inputSites[i].InteriorPoint.Coordinate, connectPt.Coordinate }), gf.PrecisionModel);


                    connectionsForEachSite[i]++;

                    belongToSite.Add((LineString)ls, $"Site_{i}");
                }
            }

            var roadsNetwork = new List<Geometry>(inputRoads.Count + belongToSite.Count);
            roadsNetwork.AddRange(belongToSite.Keys);
            roadsNetwork.AddRange(inputRoads.Geometries);

            #endregion Step2 End.

            return roadsNetwork;
        }



        /// <summary>
        /// Static method for getting the coveraged sites based on a given array of source points.
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="radius"></param>
        /// <param name="coveragedPts"></param>
        /// <returns></returns>
        public static MultiPolygon[] GetCoveragedSitesFromPts(ref List<Geometry> roads, MultiPolygon inputSites, AttributesTable[] tables, GeometryFactory gf, Dictionary<LineString, string> belongToSite, MultiPoint sourcePts, double radius,
            out MultiPoint[] coveragedPts, out double[] coverageRatios, out double totalRatio, out AttributesTable[][] sitesTables)
        {
            // Step1: connecting all the source to roads.
            var roadsGeoms = new GeometryCollection(roads.ToArray(), gf);

            // for toPointArray method, must using GeometryCollection.Geometries property.
            var sources = GeometryFactory.ToPointArray(sourcePts.Geometries);

            sitesTables = new AttributesTable[sources.Length][];

            foreach (var source in sources)
            {
                //var debug = DistanceOp.NearestPoints(source, roadsGeoms);

                var connectCoord = DistanceOp.NearestPoints(source, roadsGeoms)[1];

                var ls = GeometryPrecisionReducer.Reduce(gf.CreateLineString(new Coordinate[]
                    { source.Coordinate, connectCoord }), gf.PrecisionModel);

                roads.Add(ls);
            }

            // extracting linestring.
            var roadsLinestring = DataCleaning.CleanMultiLineString(new GeometryCollection(roads.ToArray(), gf), gf);


            // step2: building graph.
            var cityGraphBuilder = new Nts_CityGraphBuilder(roadsLinestring);
            cityGraphBuilder.Build(belongToSite);


            // step3: get coveraged sites.
            MultiPolygon[] multiPolygons = new MultiPolygon[sources.Length];
            coveragedPts = new MultiPoint[sources.Length];
            coverageRatios = new double[sources.Length];

            Dictionary<string, int> verticesToIndices = new Dictionary<string, int>();

            var vertices = cityGraphBuilder.CityGraph.Vertices.ToArray();

            for (int v = 0; v < vertices.Length; v++)
            {
                verticesToIndices.Add(vertices[v], v);
            }



            HashSet<Polygon> total_coverageSites = new HashSet<Polygon>();
            double totalArea = 0;

            for (int i = 0; i < sources.Length; i++)
            {
                // Finding the selected indices within the given radius.
                var s = sources[i];
                var sourceVertex = cityGraphBuilder.GetPointVertex(s);
                var computer = new CentralitySingleSourceRadius<UndirectedWeightedSparseGraph<string>, string>(cityGraphBuilder.CityGraph, sourceVertex, verticesToIndices, radius);
                var selectedInices = computer.VertexInicesWithinRadius;


                // Create collections for storing results.
                HashSet<Polygon> coverageSites = new HashSet<Polygon>();
                HashSet<Point> coveragePoints = new HashSet<Point>();
                List<AttributesTable> coverageTables = new List<AttributesTable>();
                double area = 0;


                // Adding valid result to collections.
                for (int t = 0; t < selectedInices.Length; t++)
                {
                    var pt = vertices[selectedInices[t]];

                    if (pt.StartsWith("N"))
                        continue;

                    var siteIndex = int.Parse(pt.Split('_').Last());
                    //var siteIndex = int.Parse(pt.Split("Site_".)[1]);
                    var coveredSite = (Polygon)inputSites[siteIndex];

                    if (!coverageSites.Contains(coveredSite))
                    {
                        coverageSites.Add(coveredSite);
                        coveragePoints.Add(coveredSite.Centroid);
                        coverageTables.Add(tables[siteIndex]);
                        area += coveredSite.Area;
                    }


                    if (!total_coverageSites.Contains(coveredSite))
                    {
                        totalArea += coveredSite.Area;
                        total_coverageSites.Add(coveredSite);
                    }
                }


                // add all the results to output collections.
                multiPolygons[i] = new MultiPolygon(coverageSites.ToArray(), gf);
                coveragedPts[i] = new MultiPoint(coveragePoints.ToArray(), gf);
                coverageRatios[i] = area / inputSites.Area;
                sitesTables[i] = coverageTables.ToArray();
            }

            totalRatio = totalArea / inputSites.Area;

            return multiPolygons;
        }
    }
}