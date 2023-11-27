
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
    public class RegulatoryPlanning_FARAllocation : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of MetaCityGenerator, used for query xml data.
        private readonly string _moduleName = "RegulatoryPlanning";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "RegulatoryPlanning_FarAllocation";


        public override GH_Exposure Exposure => GH_Exposure.primary;
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public RegulatoryPlanning_FARAllocation() : base("", "", "", "", "")
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

            pManager.AddGenericParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.item);
            pManager.AddNumberParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.item);
            pManager.AddTextParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter("inputNach", "NACH", "input mixed nach value", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            pManager.AddNumberParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Model_LanduseRationality model_landse = new Model_LanduseRationality();
            double totalArea = -1;
            List<string> functionRatio = new List<string>();
            List<double> nachList = new List<double>();
            

            if (!DA.GetData(0, ref model_landse) || !DA.GetData(1, ref totalArea)
                || !DA.GetDataList(2, functionRatio)||!DA.GetDataList(3, nachList))
                return;

            //convert input into Dictionary<string, double>
            Dictionary<string, double> buildingAreaOfFunctions = new Dictionary<string, double>();
            for (int i = 0; i < functionRatio.Count; i++)
            {
                var tempText = functionRatio[i].Split(':');
                var tempArea = double.Parse(tempText[1]) * totalArea;
                try
                {
                    buildingAreaOfFunctions.Add(tempText[0], tempArea);
                }
                catch (Exception e)
                {
                    throw new ArgumentOutOfRangeException(
                    "This input include duplicated keys.", e);
                }
                
            }

            //Calculation
            Model_FARAllocation model_FAR = new Model_FARAllocation(model_landse.SplittedBlocks, buildingAreaOfFunctions,
                                                                    nachList.ToArray(), model_landse.Landuses);

            double[] FARList = model_FAR.FAR;

            DA.SetDataList(0, FARList);
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
                return Resources.RP_FARAllocation;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("78126484-9EE9-4B2E-8801-3EEBEBEC72D9"); }
        }
    }
}