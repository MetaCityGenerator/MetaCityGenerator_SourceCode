using g3;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;

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
    public class VisibilityAnalysis_ExposureRate3DMesh : GH_Component
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
        public static string c_id = "VisibilityAnalysis_ExposureRate3DMesh";

        #region 备用
        //public Urban_SustainabilityComponent()
        //  : base("IndexCalculation", "IndexCalc",
        //      "index calculation, included EC,WC, GC, Population Amount",
        //      "UrbanXFireFly", "AutoGenerator")
        //{
        //}
        #endregion
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public VisibilityAnalysis_ExposureRate3DMesh() : base("", "", "", "", "")
        {
            //ToDo 完善这部分内容
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
            //ToDo 完善这部分内容
            this.meta = SharedResources.GetXML(c_moduleName, c_id);
            List<XElement> list = this.meta.Element("inputs").Elements("input").ToList<XElement>();
            pManager.AddPointParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddGenericParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.item);
            pManager.AddNumberParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.item, 300d);
            pManager[2].Optional = false;
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
            pManager.AddGenericParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
            pManager.AddGenericParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.tree);
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

            double viewRangeRadius = 300d;
            Colorf basedColor = Colorf.LightGrey;

            if (!DA.GetDataList(0, inputPtList)) { return; }
            if (!DA.GetData(1, ref inputGMClass)) { return; }
            if (!DA.GetData(2, ref viewRangeRadius)) { return; }

            DMesh3 wholeMesh = new DMesh3(inputGMClass.WholeMesh);

            ////选择需要计算的点
            var ptLargeList = MeshCreation.ConvertFromRh_Point(inputPtList);

            ////初始化颜色
            MeshCreation.InitiateColor(wholeMesh, basedColor);

            //开始相切
            MeshCreation.CalcRaysThroughTriParallel(wholeMesh, ptLargeList.ToArray(), viewRangeRadius,
                out ConcurrentDictionary<int, int> meshIntrDic, out List<double> TotalVisArea, out List<double> NormalizedVisArea);

            ////上颜色
            var meshFromRays = MeshCreation.ApplyColorsBasedOnRays(wholeMesh, meshIntrDic, Colorf.Yellow, Colorf.Red);

            //转为RhinoMesh 进行输出
            var rhWholeMesh = MeshCreation.ConvertFromDMesh3(meshFromRays);

            //Rhino数据输出
            DataTree<double> visResult = new DataTree<double>();
            for (int i = 0; i < inputPtList.Count; i++)
            {
                GH_Path path = new GH_Path(i);
                visResult.Add(TotalVisArea[i], path);
                visResult.Add(NormalizedVisArea[i], path);
            }

            int[] meshIntrIndex = new int[wholeMesh.VertexCount];
            for (int i = 0; i < wholeMesh.VertexCount; i++)
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

            DA.SetData(0, rhWholeMesh);
            DA.SetDataList(1, meshIntrDic.Values.ToArray());
            DA.SetDataTree(2, visResult);
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
                return Resources.SA_ExposureRate3D_Mesh;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("29039672-83A3-4977-B4AC-883EB0E345D6"); }
        }
    }
}
