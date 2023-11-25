using Grasshopper.Kernel;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using UrbanX.Planning.Sustainability;

using UrbanXTools.Properties;

namespace UrbanXTools
{
    public class SustainabilityAnalysis_Population : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of UrbanXTools, used for query xml data.
        private readonly string _moduleName = "SustainabilityAnalysis";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "SustainabilityAnalysis_Population";


        public override GH_Exposure Exposure => GH_Exposure.primary;

        public SustainabilityAnalysis_Population() : base("", "", "", "", "")
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
            pManager.AddIntegerParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
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

            if (!DA.GetDataList(0, siteBreps)) { return; }
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
            //Block层
            List<int> outputPop = new List<int>();

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
                loops[0].To3dCurve().TryGetPolyline(out Polyline plTemp);
                var baseCrv = plTemp.ToPolylineCurve();

                var populationCount = (int)Math.Round(indexCalc.CalculatePopulation(layer, baseCrv));

                outputPop.Add(populationCount);
            }
            #endregion

            #region 输出内容

            DA.SetDataList(0, outputPop);

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
                return Resources.RD_Population;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("1F0022F9-32D0-42EE-91F5-52AA9881A816"); }
        }
    }
}



