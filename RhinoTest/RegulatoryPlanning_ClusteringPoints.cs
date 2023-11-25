using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using UrbanX.Algorithms.Clustering;

using UrbanXTools.Properties;



// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace UrbanXTools
{

    public class RegulatoryPlanning_ClusteringPoints : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of UrbanXTools, used for query xml data.
        private readonly string _moduleName = "RegulatoryPlanning";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "RegulatoryPlanning_ClusteringPoints";


        public override GH_Exposure Exposure => GH_Exposure.secondary;
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public RegulatoryPlanning_ClusteringPoints() : base("", "", "", "", "")
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

            pManager.AddPointParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
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
            List<Point3d> pts = new List<Point3d>();
            List<double> diams = new List<double>();

            if (!DA.GetDataList(0, pts) || !DA.GetDataList(1, diams))
            {
                return;
            }

            // Build distance matrix.
            double[,] distanceMatrix = new double[pts.Count, pts.Count];
            double[,] coordinates = new double[pts.Count, 3];
            for (int i = 0; i < pts.Count; i++)
            {
                for (int j = i + 1; j < pts.Count; j++)
                {
                    var dist = pts[i].DistanceTo(pts[j]);
                    distanceMatrix[i, j] = dist;
                    distanceMatrix[j, i] = dist;
                }

                coordinates[i, 0] = pts[i].X;
                coordinates[i, 1] = pts[i].Y;
                coordinates[i, 2] = pts[i].Z;
            }


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
                        lines.Add(new Line(centroid, pt));
                    }

                    links.AddRange(lines, path);
                }
            }

            DA.SetDataTree(0, links);
            DA.SetDataTree(1, clusterIds);
            DA.SetData(2, new RegulatoryPlanning_ClusteringBlocks.ClusterIds(ids));
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
                return Resources.NA_ClusteringPoints;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("1942F63B-CE0D-46E6-A8ED-C89266B07266"); }
        }
    }
}
