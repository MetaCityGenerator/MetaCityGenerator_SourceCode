using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using UrbanX.Planning.SpatialAnalysis;

namespace UrbanXTools
{
    public class Debug_InitiateMesh : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Debug_InitiateMesh class.
        /// </summary>
        public Debug_InitiateMesh()
          : base("Debug_InitiateMesh", "IMesh",
                "Initiate mesh for 3d visual syntax calculation",
              "UrbanX", "7_Utility")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("BuildingBrep/Mesh", "BBrep/Mesh", "Building breps as input", GH_ParamAccess.list);
            pManager.AddNumberParameter("SubdivisionSize", "SubSize", "Subdivision size.", GH_ParamAccess.item, -1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("OutputMesh", "OMesh", "output mesh class for 3d visual syntax", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Object> objList = new List<object>();
            List<Brep> brepInList = new List<Brep>();
            List<Mesh> meshInList = new List<Mesh>();
            double gridSize = -1;
            
            if (!DA.GetDataList<Object>(0, objList)) { return; } 
            if (!DA.GetData(1, ref gridSize)) { return; }

            int count = objList.Count;
            var topBtnMeshArray = new Mesh[count];
            var sideMeshArray = new Mesh[count];
            var sizeList = new double[count];
            var cenPtList = new Point3d[count];
            Mesh topBtnMesh=new Mesh();
            Mesh sideMesh=new Mesh();
            Debug_GeneratedMeshClass result;

            if (!(objList[0] is Grasshopper.Kernel.Types.GH_Mesh))
            {
                Brep[] brepIn=new Brep[count];
                for (int i = 0; i < count; i++)
                {
                    var temp = (Grasshopper.Kernel.Types.GH_Brep)objList[i];
                    brepIn[i] = temp.Value.DuplicateBrep();
                }
                    
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
                topBtnMesh = RhinoToolManager.JoinedMesh(topBtnMeshArray);
                sideMesh = RhinoToolManager.JoinedMesh(sideMeshArray);
                result = new Debug_GeneratedMeshClass(topBtnMesh, sideMesh, sizeList, cenPtList);

            }
            else
            {
                Mesh[] meshIn = new Mesh[count];
                for (int i = 0; i < count; i++) {
                    var temp = (Grasshopper.Kernel.Types.GH_Mesh)objList[i];
                    meshIn[i] = temp.Value;
                }
                    

                for (int i = 0; i < meshInList.Count; i++)
                {
                    var temp = meshInList[i];
                    temp.Faces.ConvertQuadsToTriangles();
                }

                System.Threading.Tasks.Parallel.For(0, meshIn.Length, i =>
                {
                    MeshCreation.CreateMeshFromWholeMesh(meshIn[i], out double size, out Point3d cenPt);

                    sizeList[i] = size;
                    cenPtList[i] = cenPt;
                });

                //创建meshClass，包含topBtn, Sides, Area, CentPt
                var meshCollection=RhinoToolManager.JoinedMesh(meshIn);
                result = new Debug_GeneratedMeshClass(meshCollection, sizeList, cenPtList);

            }

            
            #region 输出内容
            DA.SetData(0, result);
            #endregion
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("DE8086FF-2E15-408B-9D64-8E6360F45551"); }
        }
    }
}