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
    public class VisibilityAnalysis_GenerateMesh : GH_Component 
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
        public static string c_id = "VisibilityAnalysis_GenerateMesh";

        #region 备用
        //public Urban_SustainabilityComponent()
        //  : base("IndexCalculation", "IndexCalc",
        //      "index calculation, included EC,WC, GC, Population Amount",
        //      "MetaCityFireFly", "AutoGenerator")
        //{
        //}
        #endregion
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public VisibilityAnalysis_GenerateMesh() : base("", "", "", "", "")
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
            pManager.AddMeshParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddBooleanParameter("VisualGraphCalc","VGraph","Toogle true for calculating visual graph", GH_ParamAccess.item,false);
            //pManager.AddIntegerParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.item, 1);
            //pManager[1].Optional = false;
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
            List<Mesh> meshInList = new List<Mesh>();
            Boolean flag = false;
            if (!DA.GetDataList<Mesh>(0, meshInList)) { return; }
            if (!DA.GetData(1, ref flag)) { return; }

            #region 细分mesh

            for (int i = 0; i < meshInList.Count; i++)
            {
                var temp = meshInList[i];
                temp.Faces.ConvertQuadsToTriangles();
            }

            GeneratedMeshClass result;
            if (flag)
            {
                Mesh[] meshIn = meshInList.ToArray();
                //var topBtnMeshArray = new Mesh[meshIn.Length];
                var sideMeshArray = new Mesh[meshIn.Length];
                var sizeList = new double[meshIn.Length];
                var cenPtList = new Point3d[meshIn.Length];

                System.Threading.Tasks.Parallel.For(0, meshIn.Length, i =>
                {
                    MeshCreation.CreateMeshFromWholeMesh(meshIn[i], out double size, out Point3d cenPt);

                    sizeList[i] = size;
                    cenPtList[i] = cenPt;
                });

                //创建meshClass，包含topBtn, Sides, Area, CentPt
                DMesh3 meshCollection = RhinoToolManager.ConvertFromRhMesh(meshInList);
                result = new GeneratedMeshClass(meshCollection, sizeList, cenPtList);
            }
            else
            {
                DMesh3 meshCollection = RhinoToolManager.ConvertFromRhMesh(meshInList);
                result = new GeneratedMeshClass(meshCollection);
            }
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
                return Resources.SA_GenerateMesh_Mesh;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("292D88E1-10C4-4AFB-9B37-3A14337323F2"); }
        }
    }
}
