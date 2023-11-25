using Grasshopper.Kernel;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using UrbanX.Planning.UrbanDesign;
using UrbanX.IO.OpenNURBS;

using UrbanXTools.Properties;
using NetTopologySuite.Geometries;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace UrbanXTools
{
    public class UrbanDesign_SiteTrees : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of UrbanXTools, used for query xml data.
        private readonly string _moduleName = "UrbanDesign";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "UrbanDesign_SiteTrees";

        public override GH_Exposure Exposure => GH_Exposure.hidden;


        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public UrbanDesign_SiteTrees() : base("", "", "", "", "")
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

            pManager.AddCurveParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.item);
            pManager.AddNumberParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            pManager.AddPointParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
            pManager.AddCurveParameter("pls","pls","", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve curve = null;
            double ratio = 0;

            if (!DA.GetData(0, ref curve) || !DA.GetData(1, ref ratio))
                return;

            var tol = DocumentTolerance();
            PrecisionModel pm = new PrecisionModel(1.0 / tol);
            GeometryFactory gf = new GeometryFactory(pm);

            GeometryConverter converter = new GeometryConverter(gf);
            var pl = DesignToolbox.ConvertToPolyline(curve, tol);
            var site = converter.ToPolygon(pl);


            SiteTrees trees = new SiteTrees(site, ratio);

            Point3d[] pts = new Point3d[trees.Trees.Length];
            double[] radius = new double[trees.Trees.Length];

            for (int i = 0; i < pts.Length; i++)
            {
                var cd = trees.Trees[i].Centroid;

                pts[i] = new Point3d(cd.X, cd.Y, 0);
                radius[i] = trees.Trees[i].Radius;
            }

            Polyline[] pls = new Polyline[trees.Polygons.Length];
            for (int i = 0; i < trees.Polygons.Length; i++)
            {
                pls[i] = converter.ToPolyline(trees.Polygons[i]);
            }


            DA.SetDataList(0, pts);
            DA.SetDataList(1, radius);
            DA.SetDataList(2, pls);
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
                return Resources.UD_SiteParameter;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("7FF9130B-A999-4957-8400-1313434FABE6"); }
        }
    }
}