using Grasshopper.Kernel;

using NetTopologySuite.Geometries;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using MetaCity.IO.OpenNURBS;
using MetaCity.Planning.SpaceSyntax;
using MetaCity.Planning.UrbanDesign;
using MetaCity.Planning.Utility;

using MetaCityGenerator.Properties;

namespace MetaCityGenerator
{
    [Obsolete("Using Network_Computing3D.")]
    public class NetworkStructure_Computing : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of MetaCityGenerator, used for query xml data.
        private readonly string _moduleName = "NetworkStructure";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "NetworkStructure_Computing";


        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public NetworkStructure_Computing() : base("", "", "", "", "")
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
            pManager.AddNumberParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.item);
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
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> curves = new List<Curve>();
            double radius = 0;

            if (!DA.GetDataList(0, curves) || !DA.GetData(1, ref radius))
                return;

            PrecisionModel pm = new PrecisionModel(1.0 / DocumentTolerance());
            GeometryFactory gf = new GeometryFactory(pm);
            GeometryConverter converter = new GeometryConverter(gf);

            LineString[] ls = new LineString[curves.Count];
            for (int i = 0; i < curves.Count; i++)
            {
                var c = curves[i];
                if (!c.TryGetPolyline(out Polyline pl))
                {
                    pl = DesignToolbox.ConvertToPolyline(c, DocumentTolerance());
                }
                ls[i] = converter.ToLineString(pl);
            }



            GeometryCollection geoms = new GeometryCollection(ls, gf);
            var segs = DataCleaning.CleanMultiLineString(geoms, gf);


            // var graph = new GraphConstructor(curves.ToArray(), DocumentTolerance());
            var graph = new GraphBuilder(segs);
            graph.Build();

            radius = radius == -1 ? double.PositiveInfinity : radius;

            var computing = new SpaceSyntaxComputing(graph.MetricGraph, graph.AngularGraph, radius);


            Polyline[] result = new Polyline[segs.Count];
            for (int i = 0; i < segs.Count; i++)
            {
                var seg = segs[i];
                result[i] = converter.ToPolyline((LineString)seg);
            }


            DA.SetDataList(0, computing.MetricChoice.Values);
            DA.SetDataList(1, computing.MetricIntegration.Values);
            DA.SetDataList(2, computing.MetricMeanDepth.Values);
            DA.SetDataList(3, computing.MetricTotalDepth.Values);

            // adding a splitter here.
            DA.SetDataList(4, computing.AngularChoice.Values);
            DA.SetDataList(5, computing.AngularIntegration.Values);
            DA.SetDataList(6, computing.AngularMeanDepth.Values);
            DA.SetDataList(7, computing.AngularTotalDepth.Values);

            // adding a splitter here.
            DA.SetDataList(8, computing.NormalisedAngularChoice.Values);
            DA.SetDataList(9, computing.NormalisedAngularIntegration.Values);

            DA.SetDataList(10, result);
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
            get { return new Guid("F66EBECA-4F01-4496-ADF4-02A36A6434FA"); }
        }
    }

}
