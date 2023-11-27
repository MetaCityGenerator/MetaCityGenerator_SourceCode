using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Rhino.Geometry;
using MetaCity.DataProcessing;
using MetaCity.IO.OpenNURBS;
using MetaCity.Planning.UrbanDesign;
using MetaCity.Planning.Utility;
using System.Linq;
using MetaCityWrapper;
using MetaCityGenerator.Properties;

namespace MetaCityGenerator
{
    public class Utility_ExtractCenterLineFromBuffer : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override Guid ComponentGuid => new Guid("C978B47E-4234-4910-965C-89B7CD738D8D");

        /// <summary>
        /// Initializes a new instance of the Debug_ExtractCenterLine class.
        /// </summary>
        public Utility_ExtractCenterLineFromBuffer()
          : base("", "", "", "", "")
        {
            this.Name = "CenterLineExtractionFromBuffer";
            this.NickName = "CLExtractFrom";
            this.Description = "Extract center lines from complex curves";
            this.Category = "MetaCity";
            this.SubCategory = "7_Utility";
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("inputCurve", "inputCrv", "curves that need to be cleaned.", GH_ParamAccess.list);
            pManager.AddNumberParameter("interpolateDist", "iDist", "interpolate_dist", GH_ParamAccess.item, 10d);
            pManager.AddNumberParameter("epsilon", "E", "epsilonSize", GH_ParamAccess.item, 10d);
            pManager.AddNumberParameter("segmentThreshold", "segTh", "segment threshold", GH_ParamAccess.item, 10d);

            pManager[1].Optional = false;
            pManager[2].Optional = false;
            pManager[3].Optional = false;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("centerLine", "CL", "centerline extraction from input crvs", GH_ParamAccess.list);
            //pManager.AddGenericParameter("time", "T", "calculation time", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            List<Curve> crvs = new List<Curve>();

            double interpolateDist = 10d;
            double epsilon = 10d;
            double segmentThreshold = 10d;
            int PRECISION = 3;

            if (!DA.GetDataList(0, crvs) ||
                !DA.GetData(1, ref interpolateDist) || !DA.GetData(2, ref epsilon) || !DA.GetData(3, ref segmentThreshold))
                return;

            //List<string> record=new List<string>();
            //System.DateTime start = System.DateTime.Now;

            PrecisionModel pm = new PrecisionModel(1.0 / DocumentTolerance());
            GeometryFactory gf = new GeometryFactory(pm);
            GeometryConverter converter = new GeometryConverter(gf);

            SortedList<double, LinearRing> ls = new SortedList<double, LinearRing>(crvs.Count);
            for (int i = 0; i < crvs.Count; i++)
            {
                var c = crvs[i];
                if (!c.TryGetPolyline(out Polyline pl))
                {
                    pl = DesignToolbox.ConvertToPolyline(c, DocumentTolerance());
                }
                var tempLr = ToLinearRing(pl);
                ls.Add(GetArea(tempLr)+0.0001*i, tempLr);
            }

            var lrList = ls.Values.ToList();
            LinearRing linearRing = lrList[ls.Count - 1];
            LinearRing[] interior=new LinearRing[ls.Count-1];
            for (int i = 0; i < ls.Count-1; i++)
            {
                interior[i]= lrList[i];
            }
            Polygon ply = new Polygon(linearRing, interior, gf);
            

            //record.Add(Raytracer.TimeCalculation(start, "开始提取中心线\n"));
            FeatureCollection extractResults = CenterLineExtraction.Debug_Extract(ply, out List<string> time,
                            interpolate_dist: interpolateDist, epsilon: epsilon, segment_threshold: segmentThreshold, PRECISION: 3);
            //record.AddRange(time);

            Polyline[] result = new Polyline[extractResults.Count];
            for (int i = 0; i < extractResults.Count; i++)
            {
                var tempLs = (LineString)extractResults[i].Geometry;
                result[i] = converter.ToPolyline(tempLs);
            }
            //record.Add(Raytracer.TimeCalculation(start, "转换结束\n"));

            DA.SetDataList(0, result);
            //DA.SetDataList(1, record);
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

        public static double GetArea(LinearRing linearRing)
        {
            return Area.OfRing(linearRing.Coordinates);
        }

        public LinearRing ToLinearRing(Polyline polyline)
        {
            int count = polyline.Count;
            var cs = new Coordinate[count];
            for (int i = 0; i < count; i++)
            {
                cs[i] = ToNTSPoint(polyline[i]);
            }
            return new LinearRing(cs);
        }

        public Coordinate ToNTSPoint(Point3d vector)
        {
            var c = new Coordinate(vector.X, vector.Y);
            return c;
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
                return Resources.UT_CentroidLineExtractionFromBuffer;
            }
        }

    }
}