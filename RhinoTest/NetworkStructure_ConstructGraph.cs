using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using MetaCity.DataStructures.Geometry3D;
using MetaCity.IO.OpenNURBS;
using MetaCity.Planning.SpaceSyntax;
using MetaCity.Planning.UrbanDesign;
using MetaCity.Planning.Utility;
using Rhino.Geometry;

namespace MetaCityGenerator
{
    public class NetworkStructure_ConstructGraph : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the NetworkStructure_ConstructGraph class.
        /// </summary>
        public NetworkStructure_ConstructGraph()
          : base("NetworkStructure_ConstructGraph", "NS_CGraph",
                "Construct input curves into graphs",
              "MetaCity", "1_NetworkStructure")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("RoadsData", "O_RoadsData", "The original list of roads in 3D.", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Merge", "Merge", "True for merging all the roads segments when degree is two.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("ConstructedGraph", "Graph", "Generate graph data", GH_ParamAccess.item);
            pManager.AddCurveParameter("CleanedRoads", "Roads", "Cleaned curves data", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> curves = new List<Curve>();
            bool merge = true;

            if (!DA.GetDataList(0, curves) || !DA.GetData(1, ref merge))
                return;

            var tol = DocumentTolerance();
            //PrecisionModel pm = new PrecisionModel(1.0 / tol);
            //GeometryFactory gf = new GeometryFactory(pm);
            GeometryConverter converter = new GeometryConverter();

            //List<LineString> ls = new List<LineString>(curves.Count);
            List<UPolyline> pl3s = new List<UPolyline>(curves.Count);
            for (int i = 0; i < curves.Count; i++)
            {
                var c = curves[i];
                if (!c.TryGetPolyline(out Polyline pl))
                {
                    pl = DesignToolbox.ConvertToPolyline(c, tol);
                }
                //var l = converter.ToLineString3D(pl);
                //if (l != null) // Important: l may be null.
                //    ls.Add(l);

                var pl3 = converter.ToPolyline3D(pl);
                pl3s.Add(pl3);
            }

            //var segs = DataCleaning.CleanMultiLineString3D(ls.ToArray(), gf);
            //GraphBuilder3D graph = new GraphBuilder3D(segs, false);
            //graph.Build();
            //var computing = new SpaceSyntaxComputing(graph.MetricGraph, graph.AngularGraph, radius);

            //GraphBuilder3DS graph = new GraphBuilder3DS(segs, merge);
            //graph.Build();
            //var computing = new SpaceSyntaxComputingS(graph.Graph, radius);


            var segs3 = DataCleaning.CleanPolylines(pl3s.ToArray(), tol);
            GraphBuilder3Df graph = new GraphBuilder3Df(segs3, merge);
            graph.Build();

            Polyline[] result = new Polyline[graph.Roads.Length];
            for (int i = 0; i < graph.Roads.Length; i++)
            {
                var seg = graph.Roads[i];
                result[i] = converter.ToPolyline(seg);
            }

            DA.SetData(0, graph);
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
            get { return new Guid("5FF7D72E-B7E2-446E-9A6D-A3188FDD71C5"); }
        }
    }
}