using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using NetTopologySuite.Geometries;
using Rhino.Geometry;
using UrbanX.Assessment.SpatialAnalysis;
using UrbanX.IO.OpenNURBS;
using UrbanX.Planning.UrbanDesign;

namespace UrbanXTools
{
    public class Debug_VisualSyntaxComputing : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Debug_VisualSyntaxComputing class.
        /// </summary>
        public Debug_VisualSyntaxComputing()
          : base("Debug_VisualComputing", "VSComputing",
                "Calculating visual syntax",
              "UrbanX", "7_Utility")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Roads", "R", "roads networks", GH_ParamAccess.list);//roads
            pManager.AddNumberParameter("Scores", "S", "scores from visual graph", GH_ParamAccess.list);//score
            pManager.AddNumberParameter("Radius", "R", "radius of calculation, -1 means all", GH_ParamAccess.item, -1d);//radius
            pManager.AddNumberParameter("Weights", "W", "weights for calculation.[0] for visual, [1] for angular. Total value is 1", GH_ParamAccess.list);//weights
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("visualTotalDepth", "vTD", "data of visualTotalDepth", GH_ParamAccess.list);//visualTotalDepth
            pManager.AddGenericParameter("visualMeanDepth", "vMD", "data of visualMeanDepth", GH_ParamAccess.list);//visualMeanDepth
            pManager.AddGenericParameter("visualIntegration", "vIn", "data of visualIntegration", GH_ParamAccess.list);//visualIntegration
            pManager.AddGenericParameter("visualChoice", "vC", "data of visualChoice", GH_ParamAccess.list);//visualChoice
            pManager.AddGenericParameter("NAIN", "NAIN", "data of NAIN", GH_ParamAccess.list);//NAIN
            pManager.AddGenericParameter("NACH", "NACH", "data of NACH", GH_ParamAccess.list);//NACH
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> allRoads = new List<Curve>();
            List<double> allScores = new List<double>();
            List<double> allWeights = new List<double>();
            double radius = -1;

            if (!DA.GetDataList(0, allRoads) || !DA.GetDataList(1, allScores) || !DA.GetData(2, ref radius) || !DA.GetDataList(3, allWeights))
                return;

            PrecisionModel pm = new PrecisionModel(1.0 / DocumentTolerance());
            GeometryFactory gf = new GeometryFactory(pm);
            GeometryConverter converter = new GeometryConverter(gf);

            //过滤路网
            LineString[] ls = new LineString[allRoads.Count];
            for (int i = 0; i < allRoads.Count; i++)
            {
                var c = allRoads[i];
                if (!c.TryGetPolyline(out Polyline pl))
                {
                    pl = DesignToolbox.ConvertToPolyline(c, DocumentTolerance());
                }
                ls[i] = converter.ToLineString(pl);
            }

            //开始建图
            var RoadGeosAsMultiLS = new MultiLineString(ls);
            VisualGraphBuilderWithAngular visGraphBuilt = new VisualGraphBuilderWithAngular(RoadGeosAsMultiLS, allScores.ToArray(), allWeights.ToArray());
            visGraphBuilt.Build();

            //开始计算
            VisualSyntaxComputing visCalc = new VisualSyntaxComputing(visGraphBuilt.MetricGraph, visGraphBuilt.VisualGraph, radius);

            //输出数据
            DA.SetDataList(0, visCalc.VisualTotalDepth.Values);
            DA.SetDataList(1, visCalc.VisualMeanDepth.Values);
            DA.SetDataList(2, visCalc.VisualIntegration.Values);
            DA.SetDataList(3, visCalc.VisualChoice.Values);
            DA.SetDataList(4, visCalc.NormalisedVisualIntegration.Values);
            DA.SetDataList(5, visCalc.NormalisedVisualChoice.Values);
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
            get { return new Guid("FEF78DA0-F056-431D-BC87-4D46057654DE"); }
        }
    }
}