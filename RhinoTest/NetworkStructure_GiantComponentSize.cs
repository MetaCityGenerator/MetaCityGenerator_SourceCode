using Grasshopper.Kernel;

using NetTopologySuite.Geometries;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using MetaCity.DataStructures.Geometry3D;
using MetaCity.IO.OpenNURBS;
using MetaCity.Planning.NetworkAnalysis;
using MetaCity.Planning.SpaceSyntax;
using MetaCity.Planning.UrbanDesign;
using MetaCity.Planning.Utility;

using MetaCityGenerator.Properties;

namespace MetaCityGenerator
{
    public class NetworkStructure_GiantComponentSize : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of MetaCityGenerator, used for query xml data.
        private readonly string _moduleName = "NetworkStructure";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "NetworkStructure_Computing3D";


        public override GH_Exposure Exposure => GH_Exposure.primary;
        public NetworkStructure_GiantComponentSize() : base("", "", "", "", "")
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            this.Name = "NetworkStructure_GiantComponentSize";
            this.NickName = "NS_GCSize";
            //this.Name = _meta.Element("name").Value;
            //this.NickName = _meta.Element("nickname").Value;
            this.Description = _meta.Element("description").Value;
            this.Category = _meta.Element("category").Value;
            this.SubCategory = _meta.Element("subCategory").Value;
        }


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("inputs").Elements("input").ToList();

            pManager.AddCurveParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            //pManager.AddNumberParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.item);
            pManager.AddBooleanParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            //pManager.AddNumberParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter("CompoonetSize", "CSize", "A list of compoonet size", GH_ParamAccess.list);
            pManager.AddNumberParameter("CompoonetTypeIndices", "CTypes", "A list of compoonet type", GH_ParamAccess.list);
            pManager.AddCurveParameter("CleanedCurves", "CleanedCrv", "A list of clean curves", GH_ParamAccess.list);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> curves = new List<Curve>();
            bool merge = true;

            if (!DA.GetDataList(0, curves) || !DA.GetData(1, ref merge))
                return;

            //var tol = DocumentTolerance();
            var tol = 0.0001d;
            GeometryConverter converter = new GeometryConverter();

            List<UPolyline> pl3s = new List<UPolyline>(curves.Count);
            for (int i = 0; i < curves.Count; i++)
            {
                var c = curves[i];
                if (!c.TryGetPolyline(out Polyline pl))
                {
                    pl = DesignToolbox.ConvertToPolyline(c, tol);
                }

                var pl3 = converter.ToPolyline3D(pl);
                pl3s.Add(pl3);
            }


            var segs3 = DataCleaning.CleanPolylines(pl3s.ToArray(), tol);
            GraphBuilder3Df graph = new GraphBuilder3Df(segs3, merge);
            graph.Build();

            NetworkComputing networkComputing=new NetworkComputing(graph);

            Polyline[] result = new Polyline[graph.Roads.Length];
            for (int i = 0; i < graph.Roads.Length; i++)
            {
                var seg = graph.Roads[i];
                result[i] = converter.ToPolyline(seg);
            }

            DA.SetDataList(0, networkComputing.ComponentSize);
            DA.SetDataList(1, networkComputing.ComponentType);
            DA.SetDataList(2, result);
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
                return Resources.NA_Component;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("53BCB59C-47FE-41B7-BFE5-063B16E38155"); }
        }
}

}
