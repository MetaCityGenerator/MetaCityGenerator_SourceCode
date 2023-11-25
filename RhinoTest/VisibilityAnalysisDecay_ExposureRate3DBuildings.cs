using g3;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using UrbanX.Planning.SpatialAnalysis;

using UrbanXTools.Properties;

using Rh = Rhino.Geometry;


// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace UrbanXTools
{
    public class VisibilityAnalysisDecay_ExposureRate3DBuildings : GH_Component
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
        public static string c_id = "VisibilityAnalysis_ExposureRate3DBuildings";

        #region 备用
        //public Urban_SustainabilityComponent()
        //  : base("IndexCalculation", "IndexCalc",
        //      "index calculation, included EC,WC, GC, Population Amount",
        //      "UrbanXFireFly", "AutoGenerator")
        //{
        //}
        #endregion
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public VisibilityAnalysisDecay_ExposureRate3DBuildings() : base("", "", "", "", "")
        {
            //ToDo 完善这部分内容
            //AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(SharedUtils.Resolve);

            //this.Name = this.meta.Element("name").Value;

            //this.NickName = this.meta.Element("nickname").Value;
            this.meta = SharedResources.GetXML(c_moduleName, c_id);
            this.Name = "VisibilityAnalysisDecay_ExposureRate3DBuildings";
            this.NickName = "Decay_Exposure";
            this.Description = this.meta.Element("description").Value + "\nv.2.2";
            this.Category = this.meta.Element("category").Value;
            this.SubCategory = this.meta.Element("subCategory").Value;
        }


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //ToDo 完善这部分内容
            this.meta = SharedResources.GetXML(c_moduleName, c_id);
            List<XElement> list = this.meta.Element("inputs").Elements("input").ToList<XElement>();
            pManager.AddPointParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddGenericParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.item);
            pManager.AddNumberParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.item, 300d);
            //pManager.AddCurveParameter((string)list[3].Attribute("name"), (string)list[3].Attribute("nickname"), (string)list[3].Attribute("description"), GH_ParamAccess.list);
            pManager.AddGenericParameter("curveRange", "crvR", "input crv that included target mesh", GH_ParamAccess.list);
            pManager.AddNumberParameter("beta", "β", "beta for vis decay", GH_ParamAccess.item, 0);
            pManager[2].Optional = true;
            //pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //ToDo 完善这部分内容
            this.meta = SharedResources.GetXML(c_moduleName, c_id);
            List<XElement> list = this.meta.Element("outputs").Elements("output").ToList<XElement>();
            pManager.AddGenericParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.item);
            pManager.AddGenericParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.item);
            pManager.AddGenericParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.list);
            pManager.AddGenericParameter((string)list[3].Attribute("name"), (string)list[3].Attribute("nickname"), (string)list[3].Attribute("description"), GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Rh.Point3d> inputPtList = new List<Rh.Point3d>();
            GeneratedMeshClass inputGMClass = new GeneratedMeshClass();

            Rh.Mesh topBtnMesh = new Rh.Mesh();
            DMesh3 sidesMesh = new DMesh3();
            List<Rh.Curve> crvList = new List<Rh.Curve>();

            double viewRangeRadius = 300d;
            double beta = 0;
            Colorf basedColor = Colorf.LightGrey;

            if (!DA.GetDataList(0, inputPtList)) { return; }
            if (!DA.GetData(1, ref inputGMClass)) { return; }
            if (!DA.GetData(2, ref viewRangeRadius)) { return; }
            if (!DA.GetDataList(3, crvList)) { return; }
            if (!DA.GetData(4, ref beta)) { return; }

            topBtnMesh = inputGMClass.TopBtnList;
            sidesMesh = inputGMClass.SideList;
            double[] inputAreaList = inputGMClass.SideAreaList;
            Rh.Point3d[] inputCentPtList = inputGMClass.CenPtList;
            var spatial = inputGMClass.SpatialTree;

            ////选择需要计算的点
            var secPtDic = MeshCreation.GenerateDic(inputAreaList, inputCentPtList);
            var ptLargeList = MeshCreation.ConvertFromRh_Point(inputPtList);

            ////初始化颜色
            List<double> TotalVisArea = new List<double>();
            List<double> VisRatio = new List<double>();
            List<double> NormalizedVisArea = new List<double>();
            ConcurrentDictionary<int, int> meshIntrDic = new ConcurrentDictionary<int, int>();
            DMesh3 meshFromRays;

            if (crvList.Count == 0)
            {
                //开始相切
                MeshCreation.CalcRaysThroughTriParallel(sidesMesh, ptLargeList.ToArray(), viewRangeRadius, beta, secPtDic,
                    out meshIntrDic, out TotalVisArea, out VisRatio, out NormalizedVisArea);

                ////上颜色
                MeshCreation.InitiateColor(sidesMesh, basedColor);
                meshFromRays = new DMesh3(MeshCreation.ApplyColorsBasedOnRays(sidesMesh, meshIntrDic, Colorf.Yellow, Colorf.Red));
            }
            else
            {
                //构建RTree
                Rh.RTree rTree = new Rh.RTree();
                for (int i = 0; i < sidesMesh.TriangleCount; i++)
                {
                    var tempCentroid = sidesMesh.GetTriCentroid(i);
                    rTree.Insert(new Rh.Point2d(tempCentroid.x, tempCentroid.y), i);
                }

                List<int> selectedMeshIndices = new List<int>();
                //线选择内部的点，返回DMesh
                for (int i = 0; i < crvList.Count; i++)
                {
                    var tempCrv = crvList[i];
                    DistanceSearchData distanceSearchData = new DistanceSearchData();
                    rTree.Search(tempCrv.GetBoundingBox(false), new EventHandler<RTreeEventArgs>(DistanceCallback), distanceSearchData);
                    selectedMeshIndices.AddRange(distanceSearchData.Ids);
                }

                //开始相切
                MeshCreation.CalcRaysThroughTriParallelDecay(sidesMesh, selectedMeshIndices.ToArray(), spatial, ptLargeList.ToArray(), viewRangeRadius, beta, secPtDic,
                    out meshIntrDic, out TotalVisArea, out VisRatio, out NormalizedVisArea);

                ////上颜色
                MeshCreation.InitiateColor(sidesMesh, basedColor);
                //MeshCreation.InitiateColor(tempMesh, basedColor);
                var coloredMesh = MeshCreation.ApplyColorsBasedOnRays(sidesMesh, meshIntrDic, Colorf.Yellow, Colorf.Red);
                meshFromRays = coloredMesh;
            }

            //转为RhinoMesh 进行输出
            var selectedMesh = MeshCreation.ConvertFromDMesh3(meshFromRays);

            //Rhino数据输出
            DataTree<double> visResult = new DataTree<double>();
            for (int i = 0; i < inputPtList.Count; i++)
            {
                GH_Path path = new GH_Path(i);
                visResult.Add(TotalVisArea[i], path);
                visResult.Add(NormalizedVisArea[i], path);
                visResult.Add(VisRatio[i], path);
            }

            int[] meshIntrIndex = new int[sidesMesh.VertexCount];
            for (int i = 0; i < sidesMesh.VertexCount; i++)
            {
                if (meshIntrDic.ContainsKey(i))
                {
                    meshIntrIndex[i] = meshIntrDic[i];
                }
                else
                {
                    meshIntrDic[i] = 0;
                }
            }

            DA.SetData(0, selectedMesh);
            DA.SetData(1, topBtnMesh);
            DA.SetDataList(2, meshIntrDic.Values.ToArray());
            DA.SetDataTree(3, visResult);
        }

        private static void DistanceCallback(object sender, RTreeEventArgs e)
        {
            DistanceSearchData distanceSearchData = e.Tag as DistanceSearchData;
            distanceSearchData.Ids.Add(e.Id);
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
                return Resources.SA_ExposureRate3D;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("18B0C47E-19A7-4894-9B3C-6BF7605942C9"); }


        }
    }
}
