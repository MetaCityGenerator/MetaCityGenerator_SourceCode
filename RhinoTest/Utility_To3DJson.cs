using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using Grasshopper.Kernel;

using Rhino.Geometry;

using MetaCity.IO.OpenNURBS;
using MetaCity.IO.GeoJSON;

using NetTopologySuite.Geometries;
using MetaCity.Planning.UrbanDesign;
using NetTopologySuite.Features;


namespace MetaCityGenerator
{
    public class Utility_To3DJson : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of MetaCityGenerator, used for query xml data.
        private readonly string _moduleName = "Utility";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "Utility_To3DJson";


        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public override Guid ComponentGuid => new Guid("9E332FDA-7C90-434B-84E9-85987DDCCE6B");

        public Utility_To3DJson() : base("", "", "", "", "")
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
            pManager.AddTextParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            pManager.AddBooleanParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> crvs = new List<Curve>();

            string p = null;
            
            if (!DA.GetDataList(0, crvs) || !DA.GetData(1,ref p))
                return;

            PrecisionModel pm = new PrecisionModel(1.0 / DocumentTolerance());
            GeometryFactory gf = new GeometryFactory(pm);
            GeometryConverter converter = new GeometryConverter(gf);


            LineString[] ls = new LineString[crvs.Count];
            for (int i = 0; i < crvs.Count; i++)
            {
                var c = crvs[i];
                if (!c.TryGetPolyline(out Polyline pl))
                {
                    pl = DesignToolbox.ConvertToPolyline(c, DocumentTolerance());
                }

                ls[i] = converter.ToLineString3D(pl);
            }


            var fc = BuildFeatureCollection(ls);
            var flag = WriteFeatureCollection(fc, p);

            DA.SetData(0, flag);
        }


        public static FeatureCollection BuildFeatureCollection(Geometry[] geos)
        {
            // Build feature collection with attributesTable for visulization in qgis.
            var fc = new FeatureCollection();
            for (int i = 0; i < geos.Length; i++)
            {
                AttributesTable att = new AttributesTable
                {
                    { "CurveId", i},
                    {"SRID",geos[i].SRID },
                    {"NumPoints",geos[i].NumPoints },
                    {"IsSimple",geos[i].IsSimple },
                    {"Dimension",geos[i].Dimension },
                };

                Feature f = new Feature(geos[i], att);
                fc.Add(f);
            }

            return fc;
        }


        public static bool WriteFeatureCollection(FeatureCollection fc, string p)
        {
            var writer = new GeoJsonWriter();

            var s = writer.Write(fc);

            //string path = Path.Combine(p, fileName);

            File.WriteAllText(p, s);


            if (s != null)
                return true;
            else
                return false;
        }
    }
}
