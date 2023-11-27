using Grasshopper.Kernel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using MetaCity.Planning.UrbanDesign;

using MetaCityGenerator.Properties;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace MetaCityGenerator
{
    public class UrbanDesign_SiteParameterExtra : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of MetaCityGenerator, used for query xml data.
        private readonly string _moduleName = "UrbanDesign";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "UrbanDesign_SiteParameterExtra";

        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public UrbanDesign_SiteParameterExtra() : base("", "", "", "", "")
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

            pManager.AddGenericParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddIntegerParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[3].Attribute("name"), (string)list[3].Attribute("nickname"), (string)list[3].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[4].Attribute("name"), (string)list[4].Attribute("nickname"), (string)list[4].Attribute("description"), GH_ParamAccess.list);
            pManager.AddIntegerParameter((string)list[5].Attribute("name"), (string)list[5].Attribute("nickname"), (string)list[5].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[6].Attribute("name"), (string)list[6].Attribute("nickname"), (string)list[6].Attribute("description"), GH_ParamAccess.list);

            pManager[6].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            pManager.AddGenericParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<SiteParameters> siteParameters = new List<SiteParameters>();

            List<int> siteTypes = new List<int>(siteParameters.Count);
            List<double> fars = new List<double>(siteParameters.Count);
            List<double> density = new List<double>(siteParameters.Count);
            List<double> mixRatios = new List<double>(siteParameters.Count);
            List<int> buildingStyles = new List<int>(siteParameters.Count);
            List<double> radiants = new List<double>(siteParameters.Count);

            if (!DA.GetDataList(0, siteParameters))
                return;

            // Deep copy inputs.
            SiteParameters[] result = new SiteParameters[siteParameters.Count];


            var flag1 = DA.GetDataList(1, siteTypes);
            var flag2 = DA.GetDataList(2, fars);
            var flag3 = DA.GetDataList(3, density);
            var flag4 = DA.GetDataList(4, mixRatios);
            var flag5 = DA.GetDataList(5, buildingStyles);
            var flag6 = DA.GetDataList(6, radiants);

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = siteParameters[i].DeepCopy();

                if (flag1)
                    result[i].SetSiteType(siteTypes[i]);

                if (flag2)
                    result[i].SetSiteFar(fars[i]);

                if (flag3)
                    result[i].SetDensity(density[i]);

                if (flag4)
                    result[i].SetMixRatio(mixRatios[i]);

                if (flag5)
                    result[i].SetBuildingStyle(buildingStyles[i]);

                if (flag6)
                    result[i].SetRadiant(radiants[i]);

            }

            DA.SetDataList(0, result);

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
                return Resources.UD_SiteParameterExtra;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("80B06855-22D4-46A7-8D7D-9D696E98FB1E"); }
        }
    }
}
