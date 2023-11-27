using Grasshopper.Kernel;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using MetaCity.Planning.UrbanDesign;

using MetaCityGenerator.Properties;

namespace MetaCityGenerator
{
    [Obsolete("Rarely use this method.")]
    public class Urban_ResidentialLimitation : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of MetaCityGenerator, used for query xml data.
        private readonly string _moduleName = "UrbanDesign";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "Urban_ResidentialLimitation";


        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public Urban_ResidentialLimitation() : base("", "", "", "", "")
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

            pManager.AddCurveParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.item);
            pManager.AddIntegerParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.item);
            pManager.AddNumberParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.item);
            pManager.AddNumberParameter((string)list[3].Attribute("name"), (string)list[3].Attribute("nickname"), (string)list[3].Attribute("description"), GH_ParamAccess.item, 3.0);
            pManager.AddNumberParameter((string)list[4].Attribute("name"), (string)list[4].Attribute("nickname"), (string)list[4].Attribute("description"), GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter((string)list[5].Attribute("name"), (string)list[5].Attribute("nickname"), (string)list[5].Attribute("description"), GH_ParamAccess.item, 26.0);

            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            pManager.AddNumberParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.item);
            pManager.AddTextParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.item);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve site = null;
            int cityIndex = 0;
            double heightLim = 0, floorHeight = 0, radians = 0, depth = 0;


            if (!DA.GetData(0, ref site) || !DA.GetData(1, ref cityIndex) || !DA.GetData(2, ref heightLim) || !DA.GetData(3, ref floorHeight) || !DA.GetData(4, ref radians) || !DA.GetData(5, ref depth))
                return;

            int floorsLimit = (int)Math.Round(heightLim / floorHeight);


            DesignCalculator.CorrectResitentialDensity(site, cityIndex, depth, floorsLimit, floorHeight, radians, out double far, out double density);

            DA.SetData(0, far);
            DA.SetData(1, $"{Math.Round(density * 100, 3)}%");
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
                return Resources.ICON_for_All;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("05B6D0F1-1AA2-4DD1-9029-28B814D1E3CD"); }
        }
    }
}
