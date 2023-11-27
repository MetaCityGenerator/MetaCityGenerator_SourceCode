using System;
using System.Collections.Generic;
using System.Numerics;
using Grasshopper.Kernel;
using Rhino.Geometry;
using MetaCityWrapper;

namespace MetaCityGenerator
{
    public class Debug_RayTracingComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Debug_RayTracingComponent class.
        /// </summary>
        public Debug_RayTracingComponent()
          : base("EmbreeRayHit", "RayHit",
                "Test rayhit in rhino gh",
              "MetaCity", "7_Utility")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "The input for ray hit is a mesh.", GH_ParamAccess.item);
            pManager.AddLineParameter("Line", "L", "The input for ray hit is a ray.", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("TriID", "ID", "hitId based on mesh id", GH_ParamAccess.list);
            pManager.AddTextParameter("TimeSeries", "Time", "CalculationTime", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            System.DateTime start = System.DateTime.Now;
            Mesh mesh = new Mesh();
            List<Line> rays = new List<Line>();
            if (!DA.GetData(0, ref mesh) || !DA.GetDataList<Line>(1, rays))
            {
                return;
            }
            List<string> result = new List<string>(5);
            result.Add(Raytracer.TimeCalculation(start, "得到GH数据\n"));

            Mesh m = mesh.DuplicateMesh();
            //m.Vertices.UseDoublePrecisionVertices = true;
            //m.Faces.ConvertQuadsToTriangles();
            //m.Vertices.CombineIdentical(true, true);
            //m.Vertices.CullUnused();
            //m.Weld(Math.PI);
            //m.FillHoles();
            //m.RebuildNormals();
            result.Add(Raytracer.TimeCalculation(start, "mesh数据处理\n"));

            Vector3[] vertXyzArray = new Vector3[m.Vertices.Count];
            for (int i = 0; i < m.Vertices.Count; i++)
            {
                var tempPt = m.Vertices.Point3dAt(i);
                vertXyzArray[i] = new Vector3((float)tempPt.X, (float)tempPt.Y, (float)tempPt.Z);
            }

            var vertCount = m.Vertices.Count;

            // info of faces
            // the face indices of a mesh
            int[] faceIndexArray = m.Faces.ToIntArray(true);
            //var facesCount = m.Faces.Count;

            var rt = new Raytracer();
            rt.AddMesh(vertXyzArray, vertCount, faceIndexArray, faceIndexArray.Length);
            result.Add(Raytracer.TimeCalculation(start, "mesh转换\n"));
            

            rt.CommitScene();
            int[] hitResult=new int[rays.Count];
            System.Threading.Tasks.Parallel.For(0, rays.Count, i =>
            {
                var ray = rays[i];
                Ray singleRay = new Ray
                {
                    Origin = new Vector3((float)ray.FromX, (float)ray.FromY, (float)ray.FromZ),
                    Direction = new Vector3((float)ray.Direction.X, (float)ray.Direction.Y, (float)ray.Direction.Z),
                    MinDistance = 0f
                };
                rt.Trace(singleRay, out Hit hit);
                hitResult[i] = hit.primId;
            });
            //    for (int i = 0; i < rays.Count; i++)
            //{
            //    var ray = rays[i];
            //    Ray singleRay = new Ray
            //    {
            //        Origin = new Vector3((float)ray.FromX, (float)ray.FromY, (float)ray.FromZ),
            //        Direction = new Vector3((float)ray.Direction.X, (float)ray.Direction.Y, (float)ray.Direction.Z),
            //        MinDistance = 0f
            //    };
            //    rt.Trace(singleRay, out Hit hit);
            //    hitResult[i] = hit.primId;
            //}
            result.Add(Raytracer.TimeCalculation(start, "rayHit计算\n"));
            
            rt.Dispose();
            result.Add(Raytracer.TimeCalculation(start, "释放\n"));

            DA.SetDataList(0, hitResult);
            DA.SetDataList(1, result);
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
            get { return new Guid("D524B3F2-4C3B-4554-AEF3-968586297324"); }
        }
    }
}