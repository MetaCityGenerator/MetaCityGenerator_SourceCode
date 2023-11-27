using g3;

using Grasshopper.Kernel;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using MetaCity.Planning.SpatialAnalysis;

using MetaCityGenerator.Properties;


// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace MetaCityGenerator
{
    public class VisibilityAnalysis_GenerateMeshBuildings : GH_Component
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
        public static string c_id = "VisibilityAnalysis_GenerateMeshBuildings";

        #region 备用
        //public Urban_SustainabilityComponent()
        //  : base("IndexCalculation", "IndexCalc",
        //      "index calculation, included EC,WC, GC, Population Amount",
        //      "MetaCityFireFly", "AutoGenerator")
        //{
        //}
        #endregion
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public VisibilityAnalysis_GenerateMeshBuildings() : base("", "", "", "", "")
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
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            this.meta = SharedResources.GetXML(c_moduleName, c_id);
            List<XElement> list = this.meta.Element("inputs").Elements("input").ToList<XElement>();
            pManager.AddGeometryParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.item, -1);
            pManager[1].Optional = false;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            this.meta = SharedResources.GetXML(c_moduleName, c_id);
            List<XElement> list = this.meta.Element("outputs").Elements("output").ToList<XElement>();
            pManager.AddGenericParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Brep> brepInList = new List<Brep>();
            double gridSize = -1;
            if (!DA.GetDataList<Brep>(0, brepInList)) { return; }
            if (!DA.GetData(1, ref gridSize)) { return; }

            #region 细分mesh
            Brep[] brepIn = brepInList.ToArray();
            var topBtnMeshArray = new Mesh[brepIn.Length];
            var sideMeshArray = new Mesh[brepIn.Length];
            var sizeList = new double[brepIn.Length];
            var cenPtList = new Point3d[brepIn.Length];

            var mp = MeshingParameters.Default;
            mp.MaximumEdgeLength = gridSize;
            mp.MinimumEdgeLength = gridSize;
            mp.GridAspectRatio = 1;

            System.Threading.Tasks.Parallel.For(0, brepIn.Length, i =>
            {
                MeshCreation.CreateBrepMinusTopBtn(brepIn[i], mp, out Mesh resulTopBtn, out Mesh resultSides, out double size, out Point3d cenPt);

                topBtnMeshArray[i] = resulTopBtn;
                sideMeshArray[i] = resultSides;
                sizeList[i] = size;
                cenPtList[i] = cenPt;
            });


            //创建meshClass，包含topBtn, Sides, Area, CentPt
            var topBtnMesh = RhinoToolManager.JoinedMesh(topBtnMeshArray);
            var sideMesh = new DMesh3(RhinoToolManager.ConvertFromRhMesh(sideMeshArray), true);
            GeneratedMeshClass result = new GeneratedMeshClass(topBtnMesh, sideMesh, sizeList, cenPtList);
            #endregion

            #region 输出内容
            DA.SetData(0, result);
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
                return Resources.SA_GenerateMesh;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("8B82FF41-BFB4-4314-969A-5717E201DFB7"); }
        }
    }
}
