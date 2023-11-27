using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using Grasshopper.Kernel;

using Rhino.Geometry;

using MetaCity.Planning.Sustainability;

using MetaCityGenerator.Properties;

namespace MetaCityGenerator
{
    public class SustainabilityAnalysis_EnergyCustom : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of MetaCityGenerator, used for query xml data.
        private readonly string _moduleName = "SustainabilityAnalysis";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "SustainabilityAnalysis_EnergyCustom";


        public override GH_Exposure Exposure => GH_Exposure.secondary;

        public SustainabilityAnalysis_EnergyCustom() : base("", "", "", "", "")
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

            pManager.AddBrepParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddTextParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
            pManager.AddTextParameter("XMLPath(Optional)", "Path", "If this component does not work, Please specify the path manually.\n the file is located in [AppData/Roaming/Grasshopper/Libraries/MetaCityGenerator/data/indexCalculation.xml]", GH_ParamAccess.item, "1");
            pManager[2].Optional = false;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();
            pManager.AddIntervalParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
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

            var heightEachLayer = 3d;
            string xmlPath = "";

            List<Brep> siteBreps = new List<Brep>();
            List<string> siteFunctions = new List<string>();

            if (!DA.GetDataList(0, siteBreps)) { return; }
            if (!DA.GetDataList(1, siteFunctions)) { return; }
            if (!DA.GetData(2, ref xmlPath)) { return; }

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
            List<Interval> outputEC = new List<Interval>();

            //read height
            for (int i = 0; i < siteBreps.Count; i++)
            {
                var ptList = siteBreps[i].Vertices;
                var tempMax = 0d;
                var tempMin = ptList[0].Location.Z;
                var faceBottomIndex = 0;

                for (int ptID = 0; ptID < ptList.Count; ptID++)
                {
                    tempMax = (ptList[ptID].Location.Z > tempMax) ? ptList[ptID].Location.Z : tempMax;
                    tempMin = (ptList[ptID].Location.Z < tempMin) ? ptList[ptID].Location.Z : tempMin;
                }
                //层数
                int layer = (int)Math.Ceiling((tempMax - tempMin) / heightEachLayer);

                //底面线
                for (int faceID = 0; faceID < siteBreps[i].Faces.Count; faceID++)
                {
                    var facePtZValue = siteBreps[i].Faces[faceID].PointAt(0.5, 0.5).Z;
                    if (facePtZValue == tempMin) { faceBottomIndex = faceID; break; }
                }

                var baseCrvLoops = siteBreps[i].Faces[faceBottomIndex];
                var loops = baseCrvLoops.Loops;
                var baseCrv = loops[0].To3dCurve();
                var baseCrvArea = AreaMassProperties.Compute(baseCrv).Area;
     

                var tempECBuilding = indexCalc.EnergyConsumption_Building(siteFunctions[i], baseCrvArea * layer);

                Interval energy = new Interval(tempECBuilding[0], tempECBuilding[1]);
                outputEC.Add(energy);
            }
            #endregion

            #region 输出内容

            DA.SetDataList(0, outputEC);

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
                return Resources.RD_EnergyCustom;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("4DA0A1F9-C6B8-4952-A3A6-B9A65B21AD22"); }
        }
    }
}
