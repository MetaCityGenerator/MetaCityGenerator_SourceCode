using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using Grasshopper.Kernel;

using Rhino.Geometry;

using UrbanX.IO.OpenNURBS;
using UrbanX.IO.GeoJSON;

using NetTopologySuite.Geometries;
using UrbanX.Planning.UrbanDesign;
using NetTopologySuite.Features;
using UrbanX.DataProcessing;
using Grasshopper;
using Grasshopper.Kernel.Data;

namespace UrbanXTools
{
    public class UrbanDesign_Courtyard : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of UrbanXTools, used for query xml data.
        private readonly string _moduleName = "Utility";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "Utility_To3DJson";


        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override Guid ComponentGuid => new Guid("C242DFB4-8C48-4088-A879-58165C045433");
        public UrbanDesign_Courtyard() : base("", "", "", "", "")
        {
            //this._meta = SharedResources.GetXML(_moduleName, _componentId);
            //this.Name = _meta.Element("name").Value;
            //this.NickName = _meta.Element("nickname").Value;
            //this.Description = _meta.Element("description").Value;
            //this.Category = _meta.Element("category").Value;
            //this.SubCategory = _meta.Element("subCategory").Value;
            this.Name = "UrbanDesign_CourtYard_T2";
            this.NickName = "UD_CourtYard_T2";
            this.Description = "Generate CourtYardType2";
            this.Category = "UrbanX";
            this.SubCategory = "3_UrbanDesign";
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            //this._meta = SharedResources.GetXML(_moduleName, _componentId);
            //List<XElement> list = _meta.Element("inputs").Elements("input").ToList();

            pManager.AddCurveParameter("inputCurve", "inputCrv", "curves used to generate.", GH_ParamAccess.list);
            //pManager.AddNumberParameter("gridSize", "gSize", "grid size", GH_ParamAccess.item, 500d);
            //pManager.AddNumberParameter("bufferDist", "gSize", "grid size", GH_ParamAccess.item, 10d);
            //pManager.AddNumberParameter("interpolateDist", "iDist", "interpolate_dist", GH_ParamAccess.item, 10d);
            //pManager.AddNumberParameter("epsilon", "E", "epsilonSize", GH_ParamAccess.item, 10d);
            //pManager.AddNumberParameter("segmentThreshold", "segTh", "segment threshold", GH_ParamAccess.item, 10d);

            //pManager[1].Optional = false;
            //pManager[2].Optional = false;
            //pManager[3].Optional = false;
            //pManager[4].Optional = false;
            //pManager[5].Optional = false;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            //this._meta = SharedResources.GetXML(_moduleName, _componentId);
            //List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            //pManager.AddGenericParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddGenericParameter("centerLine", "CL", "centerline extraction from input crvs", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> crvs = new List<Curve>();

            //double gridSize = 500d;
            //double bufferDist = 10d;
            //double interpolateDist = 10d;
            //double epsilon = 10d;
            //double segmentThreshold = 10d;

            if (!DA.GetDataList(0, crvs))
                return;


            PrecisionModel pm = new PrecisionModel(1.0 / DocumentTolerance());
            GeometryFactory gf = new GeometryFactory(pm);
            GeometryConverter converter = new GeometryConverter(gf);

            Polygon[] ls = new Polygon[crvs.Count];
            for (int i = 0; i < crvs.Count; i++)
            {
                var c = crvs[i];
                if (!c.TryGetPolyline(out Polyline pl))
                {
                    pl = DesignToolbox.ConvertToPolyline(c, DocumentTolerance());
                }
                ls[i] = converter.ToPolygon(pl);
            }

            var siteFC = BuildFeatureCollection(ls);

            DataTree<Polyline> resultDT = new DataTree<Polyline>();

            for (int i = 0; i < siteFC.Count; i++)
            {
                Geometry g = siteFC[i].Geometry;
                Polygon p = null;
                if (g is Polygon)
                    p = (Polygon)g;
                else if (g is MultiPolygon)
                    p = (Polygon)((MultiPolygon)g).GetGeometryN(i);

                SplitSitesParallelly ssp = new SplitSitesParallelly(p);
                Polygon[] rects = ssp.Results;
                //Geometry[] rects = ssp.IntersectedSites;

                FeatureCollection res = new FeatureCollection();
                foreach (Geometry q in rects)
                    res.Add(new Feature(q, new AttributesTable()));

                //Polyline[] result = new Polyline[res.Count];
                GH_Path path = new GH_Path(i);
                
                for (int j = 0; j < res.Count; j++)
                {
                    var tempLs = (Polygon)res[j].Geometry;
                    //result[j] = converter.ToPolyline(tempLs);
                    resultDT.Add(converter.ToPolyline(tempLs), path);
                }
            }
            

            DA.SetDataTree(0, resultDT);
        }


        public static FeatureCollection BuildFeatureCollection(Geometry[] geos)
        {
            // Build feature collection with attributesTable for visulization in qgis.
            var fc = new FeatureCollection();
            for (int i = 0; i < geos.Length; i++)
            {
                AttributesTable att = new AttributesTable
                {
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
