using Grasshopper.Kernel;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using UrbanX.Planning.FacilityLocation;

using UrbanXTools.Properties;

namespace UrbanXTools
{
    public class FacilityAnalysis_ConnectToNetwork : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of UrbanXTools, used for query xml data.
        private readonly string _moduleName = "FacilityAnalysis";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "FacilityAnalysis_ConnectToNetwork";

        public FacilityAnalysis_ConnectToNetwork() : base("", "", "", "", "")
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            this.Name = _meta.Element("name").Value;
            this.NickName = _meta.Element("nickname").Value;
            this.Description = _meta.Element("description").Value;
            this.Category = _meta.Element("category").Value;
            this.SubCategory = _meta.Element("subCategory").Value;
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("inputs").Elements("input").ToList();

            pManager.AddCurveParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddCurveParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            pManager.AddGenericParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.item);
            pManager.AddCurveParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> inputRoads = new List<Curve>();
            List<Curve> inputSites = new List<Curve>();


            if (!DA.GetDataList(0, inputRoads) || !DA.GetDataList(1, inputSites))
                return;

            var coverage = new Rh_CoverageComputing(inputRoads.ToArray(), inputSites.ToArray(), DocumentTolerance());

            DA.SetData(0, coverage);
            DA.SetDataList(1, coverage.RoadsNetwork);
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
                //return Resources.IconForThisComponent;
                return Resources.FL_ConnectToNetwork;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("C5D6A90E-B3F5-4666-BA53-0704BD15AB75"); }
        }

    }

}
