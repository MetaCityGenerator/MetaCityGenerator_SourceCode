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
using UrbanXTools.Properties;

namespace UrbanXTools
{
    public class Utility_CenterLineExtraction : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of UrbanXTools, used for query xml data.
        private readonly string _moduleName = "Utility";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "Utility_To3DJson";


        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override Guid ComponentGuid => new Guid("48E8EF24-F5E4-4285-8B56-8EC8B4630DA8");
        public Utility_CenterLineExtraction() : base("", "", "", "", "")
        {
            //this._meta = SharedResources.GetXML(_moduleName, _componentId);
            //this.Name = _meta.Element("name").Value;
            //this.NickName = _meta.Element("nickname").Value;
            //this.Description = _meta.Element("description").Value;
            //this.Category = _meta.Element("category").Value;
            //this.SubCategory = _meta.Element("subCategory").Value;
            this.Name = "CenterLineExtraction";
            this.NickName = "CLExtract";
            this.Description = "Extract center lines from complex curves";
            this.Category = "UrbanX";
            this.SubCategory = "7_Utility";
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            //this._meta = SharedResources.GetXML(_moduleName, _componentId);
            //List<XElement> list = _meta.Element("inputs").Elements("input").ToList();

            pManager.AddCurveParameter("inputCurve","inputCrv","curves that need to be cleaned.",GH_ParamAccess.list);
            pManager.AddNumberParameter("gridSize","gSize","grid size",GH_ParamAccess.item,500d);
            pManager.AddNumberParameter("bufferDist","gSize","grid size",GH_ParamAccess.item,10d);
            pManager.AddNumberParameter("interpolateDist", "iDist", "interpolate_dist", GH_ParamAccess.item,10d);
            pManager.AddNumberParameter("epsilon","E","epsilonSize",GH_ParamAccess.item,10d);
            pManager.AddNumberParameter("segmentThreshold","segTh","segment threshold", GH_ParamAccess.item,10d);

            pManager[1].Optional = false;
            pManager[2].Optional = false;
            pManager[3].Optional = false;
            pManager[4].Optional = false;
            pManager[5].Optional = false;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            //this._meta = SharedResources.GetXML(_moduleName, _componentId);
            //List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            //pManager.AddGenericParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddGenericParameter("centerLine","CL","centerline extraction from input crvs", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> crvs = new List<Curve>();

            double gridSize = 500d;
            double bufferDist = 10d;
            double interpolateDist = 10d;
            double epsilon = 10d;
            double segmentThreshold = 10d;

            if (!DA.GetDataList(0, crvs) || !DA.GetData(1, ref gridSize) || !DA.GetData(2, ref bufferDist) || 
                !DA.GetData(3, ref interpolateDist) || !DA.GetData(4, ref epsilon) || !DA.GetData(5, ref segmentThreshold))
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
            //var flag = WriteFeatureCollection(fc, p);

            FeatureCollection extractResults = CenterLineExtraction.Extract(fc, grid_size:gridSize, buffer_dist:bufferDist,
            interpolate_dist:interpolateDist, epsilon :epsilon, segment_threshold :segmentThreshold, PRECISION : 3);
            Polyline[] result = new Polyline[extractResults.Count];
            for (int i = 0; i < extractResults.Count; i++)
            {
                var tempLs =(LineString)extractResults[i].Geometry;
                result[i]=converter.ToPolyline(tempLs);
            }
            DA.SetDataList(0, result);
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
                Feature f = new Feature(geos[i],att);
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

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.UT_CentroidLineExtraction;
            }
        }
    }
}
