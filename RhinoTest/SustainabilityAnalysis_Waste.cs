using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using UrbanX.Planning.Sustainability;
using UrbanX.Planning.UrbanDesign;

using UrbanXTools.Properties;

namespace UrbanXTools
{
    public class SustainabilityAnalysis_Waste : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of UrbanXTools, used for query xml data.
        private readonly string _moduleName = "SustainabilityAnalysis";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "SustainabilityAnalysis_Waste";


        public override GH_Exposure Exposure => GH_Exposure.primary;

        public SustainabilityAnalysis_Waste() : base("", "", "", "", "")
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
            pManager.AddTextParameter("XMLPath(Optional)", "Path", "If this component does not work, Please specify the path manually.\n the file is located in [AppData/Roaming/Grasshopper/Libraries/UrbanXTools/data/indexCalculation.xml]", GH_ParamAccess.item, "1");
            pManager[1].Optional = false;
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
            //var defaultPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //string xmlPath = Path.Combine(defaultPath, "data", "indexCalculation.xml");
            //var indexCalc = new IndexCalculation(xmlPath);

            string xmlPath = "";
            List<DesignResult> siteResults = new List<DesignResult>();
            if (!DA.GetDataList(0, siteResults)) { return; }
            if (!DA.GetData(1, ref xmlPath)) { return; }

            IndexCalculation indexCalc = new IndexCalculation();
            if (xmlPath == "1")
            {
                var defaultPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                xmlPath = Path.Combine(defaultPath, "data", "indexCalculation.xml");
                indexCalc = new IndexCalculation(xmlPath);
            }
            else
            {
                indexCalc = new IndexCalculation(xmlPath);
            }

            #region 层级数据输入


            //DataTree<Interval> outputWC = new DataTree<Interval>();
            DataTree<double> outputGC = new DataTree<double>();

            //Block层
            for (int blockID = 0; blockID < siteResults.Count; blockID++)
            {
                var siteResult = siteResults[blockID];

                //Subsite层
                for (int subSiteID = 0; subSiteID < siteResult.SubSites.Length; subSiteID++)
                {
                    //Building层
                    for (int buildingID = 0; buildingID < siteResults[blockID].SubSiteBuildingGeometries[subSiteID].Length; buildingID++)
                    {
                        var building = siteResults[blockID].SubSiteBuildingGeometries[subSiteID][buildingID];
                        //Brep层
                        for (int brepID = 0; brepID < building.BrepOutlines.Length; brepID++)
                        {
                            //Brep=数值归位

                            //var tempWCBuilding = indexCalc.WaterConsumption_Building(building.BrepFunctions[brepID], building.BrepAreas[brepID]);
                            var tempGCBuilding = indexCalc.GarbageConsumption_Building(building.BrepFunctions[brepID], building.BrepAreas[brepID], building.BrepFloors[brepID]);

                            //传入GH Tree 
                            GH_Path ghPath = new GH_Path(blockID, subSiteID, buildingID);

                            Interval garbage = new Interval(tempGCBuilding[0], tempGCBuilding[1]);

                            // kg/d --> t/year (365.25)
                            outputGC.Add(garbage.Mid * 365.25 * 0.001, ghPath);
                        }
                    }
                }
            }
            #endregion

            #region 输出内容

            DA.SetDataTree(0, outputGC);

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
                return Resources.RD_Garbage;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("795625EA-609D-4250-9BD9-EE60B9BC1CE3"); }
        }
    }
}

