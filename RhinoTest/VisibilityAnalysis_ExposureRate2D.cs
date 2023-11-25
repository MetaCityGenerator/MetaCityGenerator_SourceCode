using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using UrbanX.Planning.SpatialAnalysis;

using UrbanXTools.Properties;



// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace UrbanXTools
{
    public class VisibilityAnalysis_ExposureRate2D : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public XElement meta;
        public static string c_moduleName = "VisibilityAnalysis";
        public static string c_id = "VisibilityAnalysis_ExposureRate2D";

        #region 备用
        //public Urban_SustainabilityComponent()
        //  : base("IndexCalculation", "IndexCalc",
        //      "index calculation, included EC,WC, GC, Population Amount",
        //      "UrbanXFireFly", "AutoGenerator")
        //{
        //}
        #endregion
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public VisibilityAnalysis_ExposureRate2D() : base("", "", "", "", "")
        {
            //AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(SharedUtils.Resolve);
            this.meta = SharedResources.GetXML(c_moduleName, c_id);
            this.Name = this.meta.Element("name").Value;
            this.NickName = this.meta.Element("nickname").Value;
            this.Description = this.meta.Element("description").Value + "\nv.2.2";
            this.Category = this.meta.Element("category").Value;
            this.SubCategory = this.meta.Element("subCategory").Value;
        }


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            this.meta = SharedResources.GetXML(c_moduleName, c_id);
            List<XElement> list = this.meta.Element("inputs").Elements("input").ToList<XElement>();
            pManager.AddBrepParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddPointParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
            pManager.AddBooleanParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.item, true);
            pManager.AddNumberParameter((string)list[3].Attribute("name"), (string)list[3].Attribute("nickname"), (string)list[3].Attribute("description"), GH_ParamAccess.item, 500.00);
            pManager.AddIntegerParameter((string)list[4].Attribute("name"), (string)list[4].Attribute("nickname"), (string)list[4].Attribute("description"), GH_ParamAccess.item, 20);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            this.meta = SharedResources.GetXML(c_moduleName, c_id);
            List<XElement> list = this.meta.Element("outputs").Elements("output").ToList<XElement>();
            pManager.AddGenericParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.tree);
            pManager.AddGenericParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Brep> siteBreps = new List<Brep>();
            List<Point3d> sitePtLists = new List<Point3d>();
            bool normalizedFlag = true;
            double radius = 500;
            int segment = 20;
            if (!DA.GetDataList(0, siteBreps)) { return; }
            if (!DA.GetDataList(1, sitePtLists)) { return; }
            if (!DA.GetData(2, ref normalizedFlag)) { return; }
            if (!DA.GetData(3, ref radius)) { return; }
            if (!DA.GetData(4, ref segment)) { return; }

            List<Line> LineList = new List<Line>();
            List<int> countListTemp = new List<int>();

            DataTree<Line> outLineTree = new DataTree<Line>();
            DataTree<double> exposureRateTree = new DataTree<double>();

            #region 层级数据输入
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

                //底面线
                for (int faceID = 0; faceID < siteBreps[i].Faces.Count; faceID++)
                {
                    var facePtZValue = siteBreps[i].Faces[faceID].PointAt(0.5, 0.5).Z;
                    if (facePtZValue == tempMin) { faceBottomIndex = faceID; break; }
                }
                var loops = siteBreps[i].Faces[faceBottomIndex].Loops;
                for (int edgeId = 0; edgeId < loops.Count; edgeId++)
                {
                    var singleLoops = loops[edgeId].To3dCurve();
                    singleLoops.TryGetPolyline(out Polyline pl);
                    var loopList = pl.GetSegments();
                    LineList.AddRange(loopList);
                }
                countListTemp.Add(siteBreps[i].Faces[faceBottomIndex].AdjacentEdges().Length);
            }

            CuttingPreparation cuttingPreparation = new CuttingPreparation(LineList, sitePtLists, normalizedFlag, radius, segment);
            int countNumber = 0;
            var countList = CountAdd(countListTemp);
            var resultLineList = cuttingPreparation.Outlines.Keys.ToList();
            List<double> resultValueList;
            if (normalizedFlag == true) { resultValueList = cuttingPreparation.OutlinesNormilized.Values.ToList(); }
            else { resultValueList = cuttingPreparation.Outlines.Values.ToList(); }

            for (int i = 0; i < resultLineList.Count; i++)
            {
                bool flag = (i >= countList[countNumber]);
                if (flag) { countNumber += 1; }
                GH_Path ghPath = new GH_Path(countNumber);
                outLineTree.Add(resultLineList[i], ghPath);
                exposureRateTree.Add(resultValueList[i], ghPath);
            }

            #endregion

            #region 输出内容
            DA.SetDataTree(0, outLineTree);
            DA.SetDataTree(1, exposureRateTree);

            #endregion
        }

        private List<int> CountAdd(List<int> countListTemp)
        {
            List<int> result = new List<int>();
            for (int i = 0; i < countListTemp.Count; i++)
            {
                if (i == 0) { result.Add(countListTemp[i]); }
                else
                {
                    var temp = countListTemp[i] + result[i - 1];
                    result.Add(temp);
                }
            }
            return result;
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
                return Resources.SA_ExposureRate2D;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("5A4B162A-46DF-45A8-AE38-ECEE6B687B9E"); }
        }
    }
}
