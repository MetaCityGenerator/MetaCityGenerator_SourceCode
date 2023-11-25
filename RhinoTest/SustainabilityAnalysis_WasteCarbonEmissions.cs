using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using UrbanXTools.Properties;

namespace UrbanXTools
{
    public class SustainabilityAnalysis_WasteCarbonEmissions : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of UrbanXTools, used for query xml data.
        private readonly string _moduleName = "SustainabilityAnalysis";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "SustainabilityAnalysis_WasteCarbonEmissions";


        public override GH_Exposure Exposure => GH_Exposure.tertiary;


        public SustainabilityAnalysis_WasteCarbonEmissions() : base("", "", "", "", "")
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
            pManager.AddNumberParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.tree);
            pManager.AddIntegerParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();
            pManager.AddNumberParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Dataset

            /// <summary>
            /// 2.1tCO2/t
            /// </summary>
            double _waste_landfill = 2.13;

            /// <summary>
            /// 0.32tCO2/t
            /// </summary>
            double _waste_incineration = 0.321;

            /// <summary>
            /// 0.1tCO2/t
            /// </summary>
            double _waste_composting = 0.12;


            int method = 0;
            if (!DA.GetDataTree(0, out GH_Structure<GH_Number> inputWaste) || !DA.GetData(1, ref method))
                return;

            #region 层级数据输入
            DataTree<double> emissions = new DataTree<double>();

            double k = 0;
            switch (method)
            {
                case 0:
                    k = _waste_landfill;
                    break;
                case 1:
                    k = _waste_incineration;
                    break;
                case 2:
                    k = _waste_composting;
                    break;
            }


            foreach (var path in inputWaste.Paths)
            {

                var branch = inputWaste.get_Branch(path);

                for (int i = 0; i < branch.Count; i++)
                {
                    double n = inputWaste.get_DataItem(path, i).Value;
                    double emiss = k * n;
                    emissions.Add(emiss, path);
                }
            }


            #endregion

            #region 输出内容

            DA.SetDataTree(0, emissions);

            #endregion
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
                return Resources.RD_G_CarbonEmissions;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("E6CD01DF-A1F8-4394-B395-9968BB521680"); }
        }
    }
}
