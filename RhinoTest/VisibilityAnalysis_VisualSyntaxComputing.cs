using Grasshopper.Kernel;

using NetTopologySuite.Geometries;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UrbanX.Assessment.SpatialAnalysis;
using UrbanX.DataStructures.Geometry3D;
using UrbanX.IO.OpenNURBS;
using UrbanX.Planning.SpaceSyntax;
using UrbanX.Planning.SpatialAnalysis;
using UrbanX.Planning.UrbanDesign;
using UrbanX.Planning.Utility;
using UrbanXTools.Properties;


// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace UrbanXTools
{
    public class VisibilityAnalysis_VisualSyntaxComputing : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public XElement meta;
        public static string c_id = "VisibilityAnalysis_VisualSyntaxComputing";
        public static string c_moduleName = "VisibilityAnalysis";

        #region 备用
        //public Urban_SustainabilityComponent()
        //  : base("IndexCalculation", "IndexCalc",
        //      "index calculation, included EC,WC, GC, Population Amount",
        //      "UrbanXFireFly", "AutoGenerator")
        //{
        //}
        #endregion
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public VisibilityAnalysis_VisualSyntaxComputing() : base("", "", "", "", "")
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
            pManager.AddCurveParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);//roads
            pManager.AddNumberParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);//score
            pManager.AddNumberParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.item, -1d);//radius
            pManager.AddBooleanParameter((string)list[3].Attribute("name"), (string)list[3].Attribute("nickname"), (string)list[3].Attribute("description"), GH_ParamAccess.item, true);//radius
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
            pManager.AddGenericParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);//visualTotalDepth
            pManager.AddGenericParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);//visualMeanDepth
            pManager.AddGenericParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.list);//visualIntegration
            pManager.AddGenericParameter((string)list[3].Attribute("name"), (string)list[3].Attribute("nickname"), (string)list[3].Attribute("description"), GH_ParamAccess.list);//visualChoice
            //pManager.AddGenericParameter((string)list[4].Attribute("name"), (string)list[4].Attribute("nickname"), (string)list[4].Attribute("description"), GH_ParamAccess.list);//NAIN
            //pManager.AddGenericParameter((string)list[5].Attribute("name"), (string)list[5].Attribute("nickname"), (string)list[5].Attribute("description"), GH_ParamAccess.list);//NACH
            pManager.AddNumberParameter("NodeCount", "NodeCount", "node count based on visual", GH_ParamAccess.list);
            pManager.AddCurveParameter("CleanedRoad", "Roads", "Cleaned Roads", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> allRoads = new List<Curve>();
            List<double> allScores = new List<double>();
            double radius = -1;
            bool merge = true;

            if (!DA.GetDataList(0, allRoads) || !DA.GetDataList(1, allScores) || !DA.GetData(2, ref radius) || !DA.GetData(3, ref merge))
                return;

            var tol = DocumentTolerance();
            //PrecisionModel pm = new PrecisionModel(1.0 / tol);
            //GeometryFactory gf = new GeometryFactory(pm);
            //GeometryConverter converter =new GeometryConverter(gf);
            GeometryConverter converter =new GeometryConverter();

            //过滤路网
            List<UPolyline> pl3s=new List<UPolyline>(allRoads.Count);
            for (int i = 0; i < allRoads.Count; i++)
            {
                var c = allRoads[i];
                if (!c.TryGetPolyline(out Polyline pl))
                {
                    pl = DesignToolbox.ConvertToPolyline(c, tol);
                }
                var pl3=converter.ToPolyline3D(pl);
                pl3s.Add(pl3);
            }

            var segs3 = DataCleaning.CleanPolylines(pl3s.ToArray(), tol);
            GraphBuilder3Df graph = new GraphBuilder3Df(segs3, merge);
            graph.Build(allScores.ToArray());
            radius = radius == -1 ? double.PositiveInfinity : radius;
            var computing = new SpaceSyntaxComputing3D(graph.Graph, radius, UrbanX.Algorithms.Graphs.GraphType.Other);

            //LineString[] ls = new LineString[allRoads.Count];
            //for (int i = 0; i < allRoads.Count; i++)
            //{
            //    var c = allRoads[i];
            //    if (!c.TryGetPolyline(out Polyline pl))
            //    {
            //        pl = DesignToolbox.ConvertToPolyline(c, DocumentTolerance());
            //    }
            //    ls[i] = converter.ToLineString(pl);
            //}

            Polyline[] result=new Polyline[graph.Roads.Length];
            for (int i = 0; i < graph.Roads.Length; i++)
            {
                var seg=graph.Roads[i];
                result[i]=converter.ToPolyline(seg);
            }

            //输出数据
            DA.SetDataList(0, computing.CustomTotalDepth);
            DA.SetDataList(1, computing.CustomMeanDepth);
            DA.SetDataList(2, computing.CustomIntegration);
            DA.SetDataList(3, computing.CustomChoice);
            //DA.SetDataList(4, computing.NormalisedAngularChoice);
            //DA.SetDataList(5, computing.NormalisedAngularIntegration);

            DA.SetDataList(4, computing.CustomNodeCount);
            DA.SetDataList(5, result);
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
                return Resources.SA_VisualSyntaxCompute;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("8DCFB10A-EF7A-4448-82AE-C6E42E764E9A"); }
        }
    }
}
