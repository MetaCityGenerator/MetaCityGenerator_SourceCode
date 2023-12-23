using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using MetaCity.Algorithms.Graphs;
using MetaCity.Planning.SpaceSyntax;
using MetaCity.Planning.UrbanDesign;
using NetTopologySuite.Utilities;
using Rhino.Geometry;
using static System.Resources.ResXFileRef;

namespace MetaCityGenerator
{
    public class NetworkStructure_FindShortestPath : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the NetworkStructure_FindShortestPath class.
        /// </summary>
        public NetworkStructure_FindShortestPath()
          : base("NetworkStructure_FindShortestPath", "NS_FPath",
              "Find shortest paths based on constructed graph",
              "MetaCity", "1_NetworkStructure")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("ConstructedGraph", "O_RoadsData", "The original list of roads in 3D.", GH_ParamAccess.item);
            pManager.AddCurveParameter("CleanedRoads", "Roads", "Cleaned curves data", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Origin", "O_RoadsData", "The original list of roads in 3D.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Destination", "O_RoadsData", "The original list of roads in 3D.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("ShortestPath", "Graph", "Generate graph data", GH_ParamAccess.list);
            pManager.AddCurveParameter("ShortestPathCrvs", "Roads", "Shortest path curves data", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GraphBuilder3Df graph = new GraphBuilder3Df();
            int ori = -1;
            int des = -1;
            List<Curve> curves = new List<Curve>();

            if (!DA.GetData(0, ref graph)) { return; }
            if (!DA.GetDataList(1, curves)) { return; }
            if (!DA.GetData(2, ref ori)) { return; }
            if (!DA.GetData(3, ref des)) { return; }
            

            var radius = double.PositiveInfinity;
            CalculateCentrality3D calculateCentrality3D = new CalculateCentrality3D(graph.Graph, GraphType.Metric, false, radius);
            var indexList = calculateCentrality3D.FindShortestPath(ori, des);

            Curve[] result = new Curve[indexList.Length] ;
            for (int i = 0; i < indexList.Length; i++)
            {
                result[i] = curves[indexList[i]];
            }

            DA.SetDataList(0, indexList);
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
            get { return new Guid("77FDFBF5-B841-48FD-8714-B3B1B244D2D5"); }
        }
    }
}