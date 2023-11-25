using Grasshopper.Kernel;

using NetTopologySuite.Geometries;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using UrbanX.DataStructures.Geometry3D;
using UrbanX.IO.OpenNURBS;
using UrbanX.Planning.SpaceSyntax;
using UrbanX.Planning.UrbanDesign;
using UrbanX.Planning.Utility;

using UrbanXTools.Properties;

namespace UrbanXTools
{
    public class NetworkStructure_Computing3DBasedOnRadius : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of UrbanXTools, used for query xml data.
        private readonly string _moduleName = "NetworkStructure";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "NetworkStructure_Computing3D";


        public override GH_Exposure Exposure => GH_Exposure.primary;
        public NetworkStructure_Computing3DBasedOnRadius() : base("", "", "", "", "")
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            this.Name = _meta.Element("name").Value;
            this.NickName = _meta.Element("nickname").Value;
            this.Description = _meta.Element("description").Value;
            this.Category = _meta.Element("category").Value;
            this.SubCategory = _meta.Element("subCategory").Value;
        }


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("inputs").Elements("input").ToList();

            pManager.AddCurveParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddBooleanParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.item);
            pManager.AddNumberParameter("Radii", "R", "input radii", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            pManager.AddNumberParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[3].Attribute("name"), (string)list[3].Attribute("nickname"), (string)list[3].Attribute("description"), GH_ParamAccess.list);

            pManager.AddNumberParameter((string)list[4].Attribute("name"), (string)list[4].Attribute("nickname"), (string)list[4].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[5].Attribute("name"), (string)list[5].Attribute("nickname"), (string)list[5].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[6].Attribute("name"), (string)list[6].Attribute("nickname"), (string)list[6].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[7].Attribute("name"), (string)list[7].Attribute("nickname"), (string)list[7].Attribute("description"), GH_ParamAccess.list);

            pManager.AddNumberParameter((string)list[8].Attribute("name"), (string)list[8].Attribute("nickname"), (string)list[8].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[9].Attribute("name"), (string)list[9].Attribute("nickname"), (string)list[9].Attribute("description"), GH_ParamAccess.list);

            pManager.AddCurveParameter((string)list[10].Attribute("name"), (string)list[10].Attribute("nickname"), (string)list[10].Attribute("description"), GH_ParamAccess.list);

            pManager.AddNumberParameter("MetricNodeCount", "MNodeCount", "node count based on metric", GH_ParamAccess.list);
            pManager.AddNumberParameter("AngularNodeCount", "ANodeCount", "node count based on angular", GH_ParamAccess.list);
            //pManager.AddCurveParameter("Debug", "d", "", GH_ParamAccess.list);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> curves = new List<Curve>();
            bool merge = true;
            List<double> radii = new List<double>();

            if (!DA.GetDataList(0, curves) || !DA.GetData(1, ref merge) || !DA.GetDataList(2, radii))
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
            var computing = new SpaceSyntaxComputing3D(graph.Graph, radii.ToArray());


            Polyline[] result = new Polyline[graph.Roads.Length];
            for (int i = 0; i < graph.Roads.Length; i++)
            {
                var seg = graph.Roads[i];
                result[i] = converter.ToPolyline(seg);
            }

            //Polyline[] debug = new Polyline[segs.Length];
            //for (int i = 0; i < segs.Length; i++)
            //{
            //    debug[i] = converter.ToPolyline(segs[i]);
            //}

            DA.SetDataList(0, computing.MetricChoice);
            DA.SetDataList(1, computing.MetricIntegration);
            DA.SetDataList(2, computing.MetricMeanDepth);
            DA.SetDataList(3, computing.MetricTotalDepth);

            // adding a splitter here.

            DA.SetDataList(4, computing.AngularChoice);
            DA.SetDataList(5, computing.AngularIntegration);
            DA.SetDataList(6, computing.AngularMeanDepth);
            DA.SetDataList(7, computing.AngularTotalDepth);

            // adding a splitter here.
            DA.SetDataList(8, computing.NormalisedAngularChoice);
            DA.SetDataList(9, computing.NormalisedAngularIntegration);

            DA.SetDataList(10, result);

            DA.SetDataList(11, computing.MetricNodeCount);
            DA.SetDataList(12, computing.AngularNodeCount);

            //DA.SetDataList(11, debug);
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
                return Resources.NA_Computing;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("75D33911-A283-4699-8E8A-EE94EF9AFD19"); }
        }
    }

}
