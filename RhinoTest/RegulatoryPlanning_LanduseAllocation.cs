
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using NetTopologySuite.Geometries;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using MetaCity.IO.OpenNURBS;
using MetaCity.Planning.RegulatoryPlan;
using MetaCity.Planning.UrbanDesign;

using MetaCityGenerator.Properties;


// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace MetaCityGenerator
{
    public class RegulatoryPlanning_LanduseAllocation : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of MetaCityGenerator, used for query xml data.
        private readonly string _moduleName = "RegulatoryPlanning";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "RegulatoryPlanning_LanduseAllocation";


        public override GH_Exposure Exposure => GH_Exposure.primary;
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public RegulatoryPlanning_LanduseAllocation() : base("", "", "", "", "")
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
            pManager.AddNumberParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.tree);
            pManager.AddGenericParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.item);
            pManager.AddTextParameter((string)list[3].Attribute("name"), (string)list[3].Attribute("nickname"), (string)list[3].Attribute("description"), GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            pManager.AddCurveParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddTextParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
            pManager.AddTextParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.list);
            pManager.AddGenericParameter((string)list[3].Attribute("name"), (string)list[3].Attribute("nickname"), (string)list[3].Attribute("description"), GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> sitesCrv = new List<Curve>();
            List<string> sitePerc = new List<string>();
            RegulatoryPlanning_ClusteringBlocks.ClusterIds ids = null;


            if (!DA.GetDataList(0, sitesCrv) || !DA.GetDataTree(1, out GH_Structure<GH_Number> siteScoresDT)
                || !DA.GetData(2, ref ids)
                || !DA.GetDataList(3, sitePerc))
                return;

            PrecisionModel pm = new PrecisionModel(1.0 / DocumentTolerance());
            GeometryFactory gf = new GeometryFactory(pm);
            GeometryConverter converter = new GeometryConverter(gf);

            //landusePrority + landuse pct
            string[] landusePriority = new string[sitePerc.Count];
            double[] landusePct = new double[sitePerc.Count];
            for (int i = 0; i < sitePerc.Count; i++)
            {
                string[] temp = sitePerc[i].Split(':');
                landusePriority[i] = temp[0];
                landusePct[i] = double.Parse(temp[1]);
            }

            
            //组织数据
            int sitesCount = sitesCrv.Count;
            int radiusCount = siteScoresDT.get_Branch(0).Count;  // TODO: check.
            double[,] nachValue = new double[sitesCount, radiusCount];
            int[][][] clusterValue = ids.Ids;


            //过滤地块
            Polygon[] polygons = new Polygon[sitesCount];
            for (int i = 0; i < sitesCrv.Count; i++)
            {
                var c = sitesCrv[i];
                if (!c.TryGetPolyline(out Polyline pl))
                {
                    pl = DesignToolbox.ConvertToPolyline(c, DocumentTolerance());
                }
                polygons[i] = converter.ToPolygon(pl); 
            }


            //extract nach value in each site
            for (int i = 0; i < siteScoresDT.Branches.Count; i++)
            {
                int nachCount = siteScoresDT.Branches[i].Count;
                for (int j = 0; j < nachCount; j++)
                {
                    nachValue[i, j] = double.Parse(siteScoresDT.get_Branch(i)[j].ToString());
                }
            }


            //calculation
            Model_LanduseRationality model = new Model_LanduseRationality(polygons, nachValue, clusterValue, landusePriority, landusePct);
            string[] landuses = model.Landuses;
            Polygon[] sitesPolygon = model.SplittedBlocks;
            Polyline[] sites = new Polyline[sitesPolygon.Length];
            for (int i = 0; i < sitesPolygon.Length; i++)
            {
                sites[i] = converter.ToPolyline(sitesPolygon[i]);
            }

            Dictionary<string, double> structure = model.LanduseStructure;


            DA.SetDataList(0, sites);
            DA.SetDataList(1, landuses);
            DA.SetDataList(2, ToString(structure));
            DA.SetData(3, model);
        }

        private static List<string> ToString(Dictionary<string, double> structure)
        {
            List<string> result = new List<string>(structure.Count);
            foreach (var item in structure)
            {
                string temp = $"{item.Key}:{item.Value}";
                result.Add(temp);
            }
            return result;
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
                return Resources.RP_LanduseAllocation;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6A51DD54-0F5C-478A-AFF1-A2D359336F72"); }
        }
    }
}