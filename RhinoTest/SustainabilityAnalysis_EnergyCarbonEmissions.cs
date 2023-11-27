using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using MetaCityGenerator.Properties;

namespace MetaCityGenerator
{
    public class SustainabilityAnalysis_EnergyCarbonEmissions : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of MetaCityGenerator, used for query xml data.
        private readonly string _moduleName = "SustainabilityAnalysis";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "SustainabilityAnalysis_EnergyCarbonEmissions";


        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public SustainabilityAnalysis_EnergyCarbonEmissions() : base("", "", "", "", "")
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
            /// 0.75kg CO2/kWh
            /// </summary>
            double _electricity = 0.75;

            if (!DA.GetDataTree(0, out GH_Structure<GH_Number> inputEnergy))
                return;


            DataTree<double> emissions = new DataTree<double>();

            var k = _electricity;

            foreach (var path in inputEnergy.Paths)
            {

                var branch = inputEnergy.get_Branch(path);

                for (int i = 0; i < branch.Count; i++)
                {
                    double n = inputEnergy.get_DataItem(path, i).Value;
                    double emiss = k * n;

                    // kg --> t
                    emissions.Add(emiss * 0.001, path);
                }
            }

            DA.SetDataTree(0, emissions);


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
                return Resources.RD_E_CarbonEmissions;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("F707233E-8808-4EE2-B386-74832D745E37"); }
        }
    }
}
