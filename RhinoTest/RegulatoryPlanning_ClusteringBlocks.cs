using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;

using NetTopologySuite.Geometries;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using MetaCity.Algorithms.Clustering;
using MetaCity.IO.OpenNURBS;
using MetaCity.Planning.FacilityLocation;
using MetaCity.Planning.UrbanDesign;
using MetaCity.Planning.Utility;

using MetaCityGenerator.Properties;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace MetaCityGenerator
{
    public class RegulatoryPlanning_ClusteringBlocks : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of MetaCityGenerator, used for query xml data.
        private readonly string _moduleName = "RegulatoryPlanning";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "RegulatoryPlanning_ClusteringBlocks";



        public override GH_Exposure Exposure => GH_Exposure.secondary;
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public RegulatoryPlanning_ClusteringBlocks() : base("", "", "", "", "")
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            this.Name = _meta.Element("name").Value;
            this.NickName = _meta.Element("nickname").Value;
            this.Description = _meta.Element("description").Value;
            this.Category = _meta.Element("category").Value;
            this.SubCategory = _meta.Element("subCategory").Value;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("inputs").Elements("input").ToList();

            pManager.AddCurveParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddCurveParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            pManager.AddLineParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.tree);
            pManager.AddIntegerParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.tree);
            pManager.AddGenericParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.item);
        }


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> roads = new List<Curve>();
            List<Curve> sites = new List<Curve>();
            List<double> diams = new List<double>();

            if (!DA.GetDataList(0, roads) || !DA.GetDataList(1, sites) || !DA.GetDataList(2, diams))
            {
                return;
            }
            PrecisionModel pm = new PrecisionModel(1.0 / DocumentTolerance());
            GeometryFactory gf = new GeometryFactory(pm);

            var roadGc = GetGeometryCollection(roads,gf);
            var siteGc = GetGeometryCollection(sites,gf);

            var cleanedRoads = DataCleaning.CleanMultiLineString(roadGc, gf);
            var cleanedSites = DataCleaning.CleanMultiPolygon(siteGc, gf);

            // Get centroids.
            List<Point3d> pts = new List<Point3d>(cleanedSites.Count);
            double[,] coordinates = new double[cleanedSites.Count, 3];
            for (int i = 0; i < cleanedSites.Count; i++)
            {
                var center = cleanedSites[i].Centroid;
                pts.Add(new Point3d(center.X, center.Y, 0));
                coordinates[i, 0] = center.X;
                coordinates[i, 1] = center.Y;
                coordinates[i, 2] = 0;
            }

            Nts_CoverageComputing coverage = new Nts_CoverageComputing(cleanedRoads, cleanedSites);

            var distanceMatrix = coverage.GetDistanceMatrix();

            // Run HAC algorithm.
            var clusters = AgglomerativeClustering.Run(distanceMatrix, diams.ToArray(), coordinates);


            DataTree<Line> links = new DataTree<Line>();
            DataTree<int> clusterIds = new DataTree<int>();
            int[][][] ids = new int[clusters.Length][][];


            int cId = clusters.Length - 1;
            foreach (var cls in clusters)
            {
                int sId = 0;
                ids[cId] = new int[cls.Count][];

                foreach (var cs in cls.SubClusters)
                {
                    GH_Path gpth = new GH_Path(cId, sId);
                    clusterIds.AddRange(cs.Children, gpth);
                    ids[cId][sId] = cs.Children.ToArray();

                    sId++;
                }

                cId--;
            }


            for (int i = 0; i < clusters.Length; i++)
            {
                if (i > 0)
                {
                    // Remove all clusters which haven't been merged in this iteration.
                    clusters[i].SubClusters.ExceptWith(clusters[i - 1].SubClusters);
                }

                for (int j = 0; j < clusters[i].Count; j++)
                {
                    List<Line> lines = new List<Line>();
                    GH_Path path = new GH_Path(i, j);
                    var c = clusters[i][j];
                    var centroid = new Point3d(c.Centroid.X, c.Centroid.Y, c.Centroid.Z);
                    foreach (var child in c.Children)
                    {
                        var pt = pts[child];
                        //points.Add(pt, path);
                        lines.Add(new Line(centroid, pt));
                    }

                    links.AddRange(lines, path);
                }
            }

            DA.SetDataTree(0, links);
            DA.SetDataTree(1, clusterIds);
            DA.SetData(2, new ClusterIds(ids));

        }


        public GeometryCollection GetGeometryCollection(List<Curve> curves , GeometryFactory gf)
        {
            GeometryConverter converter = new GeometryConverter(gf);

            Geometry[] geometries = new Geometry[curves.Count];
            for (int i = 0; i < curves.Count; i++)
            {
                var c = curves[i];
                var pl = DesignToolbox.ConvertToPolyline(c, DocumentTolerance());
                if (pl.IsClosed)
                {

                    geometries[i] = converter.ToPolygon(pl);
                }
                else
                {
                    geometries[i] = converter.ToLineString(pl);
                }
            }

            return new GeometryCollection(geometries);
        }


        public class ClusterIds
        {
            public int[][][] Ids { get; }

            public ClusterIds(int[][][] _ids)
            {
                Ids = _ids;
            }
        }



        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                return Resources.NA_ClusteringBlocks;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("8ABD6474-5F00-473D-82CB-A39916D61D6F"); }
        }
    }
}
