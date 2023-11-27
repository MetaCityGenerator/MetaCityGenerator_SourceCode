
using Grasshopper.Kernel;

using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.Union;

using Rhino.Geometry;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using MetaCity.IO.OpenNURBS;
using MetaCity.Planning.UrbanDesign;

using MetaCityGenerator.Properties;


// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace MetaCityGenerator
{
    public class RegulatoryPlanning_GeneratingSites : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of MetaCityGenerator, used for query xml data.
        private readonly string _moduleName = "RegulatoryPlanning";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "RegulatoryPlanning_GeneratingSites";



        public override GH_Exposure Exposure => GH_Exposure.secondary;
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public RegulatoryPlanning_GeneratingSites() : base("", "", "", "", "")
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            this.Name = _meta.Element("name").Value;
            this.NickName = _meta.Element("nickname").Value;
            this.Description = _meta.Element("description").Value;
            this.Category = _meta.Element("category").Value;
            this.SubCategory = _meta.Element("subCategory").Value;
        }


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("inputs").Elements("input").ToList();

            pManager.AddCurveParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddCurveParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
            pManager.AddCurveParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.list);
            pManager.AddCurveParameter((string)list[3].Attribute("name"), (string)list[3].Attribute("nickname"), (string)list[3].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[4].Attribute("name"), (string)list[4].Attribute("nickname"), (string)list[4].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[5].Attribute("name"), (string)list[5].Attribute("nickname"), (string)list[5].Attribute("description"), GH_ParamAccess.item);
            pManager.AddCurveParameter((string)list[6].Attribute("name"), (string)list[6].Attribute("nickname"), (string)list[6].Attribute("description"), GH_ParamAccess.item);
            //pManager.AddNumberParameter("GreenDistances", "gDists", "Thickness for the green buffer along main roads.", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();


            pManager.AddCurveParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            //pManager.AddCurveParameter("GreenBuffer", "green", "The result of generated green buffer.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> mRoads = new List<Curve>();
            List<Curve> sRoads = new List<Curve>();
            List<Curve> tRoads = new List<Curve>();
            List<Curve> branches = new List<Curve>();
            List<double> dists = new List<double>();
            double radius = 0;
            Curve boundary = null;

            // Main roads and offset distancec are mandatory.
            if (!DA.GetDataList(0, mRoads) || !DA.GetDataList(4, dists) || !DA.GetData(5, ref radius) || !DA.GetData(6, ref boundary))
            {
                return;
            }

            PrecisionModel pm = new PrecisionModel(1.0 / DocumentTolerance());
            GeometryFactory gf = new GeometryFactory(pm);
            GeometryConverter converter = new GeometryConverter(gf);

            // take all the dists into half for buffer operation.
            for (int i = 0; i < dists.Count; i++)
            {
                dists[i] *= 0.5;
            }


            // The rest are optional parameters.
            var flag1 = DA.GetDataList(1, sRoads);
            var flag2 = DA.GetDataList(2, tRoads);
            var flag3 = DA.GetDataList(3, branches);

            // Find geometies within boundary.
            var boundPolygon = GetBoundaryPolygon(boundary,converter);

            // BufferCollection. Main roads buffer.
            List<Geometry> allBuffers = new List<Geometry>();
            List<Geometry> greenBuffers = new List<Geometry>();
            allBuffers.AddRange(GetLinesBuffer(mRoads, dists[0], boundPolygon, converter));
            //greenBuffers.AddRange(GetLinesBuffer(mRoads, dists[0] + 20, boundPolygon));

            if (flag1)
            {
                double dist;
                if (dists.Count < 2)
                {
                    dist = dists[dists.Count - 1];
                }
                else
                {
                    dist = dists[1];
                }
                allBuffers.AddRange(GetLinesBuffer(sRoads, dist, boundPolygon,converter));
                //greenBuffers.AddRange(GetLinesBuffer(sRoads, dist + 10, boundPolygon));
            }

            if (flag2)
            {
                double dist;
                if (dists.Count < 3)
                {
                    dist = dists[dists.Count - 1];
                }
                else
                {
                    dist = dists[2];
                }
                allBuffers.AddRange(GetLinesBuffer(tRoads, dist, boundPolygon,converter));
            }

            if (flag3)
            {
                double dist;
                if (dists.Count < 4)
                {
                    dist = dists[dists.Count - 1];
                }
                else
                {
                    dist = dists[3];
                }
                allBuffers.AddRange(GetLinesBuffer(branches, dist, boundPolygon, converter));
            }




            // Union.
            //var roadsBuffers =(MultiPolygon)CascadedPolygonUnion.Union(allBuffers);
            //var greenAreaBuffer = (MultiPolygon)CascadedPolygonUnion.Union(greenBuffers);
            var r = CascadedPolygonUnion.Union(allBuffers);
            MultiPolygon roadsBuffers;
            if (r.OgcGeometryType == OgcGeometryType.Polygon)
            {
                Polygon[] pols = { (Polygon)r };
                roadsBuffers = new MultiPolygon(pols);
            }
            else
            {
                roadsBuffers = (MultiPolygon)r;
            }


            BufferParameters bpR = new BufferParameters(18, EndCapStyle.Round, JoinStyle.Round, 0.6);
            //BufferParameters bpB = new BufferParameters(18, EndCapStyle.Round, JoinStyle.Bevel, 0.6);


            // Output
            //List<Polyline> blocks = new List<Polyline>();

            ConcurrentBag<Polyline> blocksBag = new ConcurrentBag<Polyline>();

            // Using parallel
            for (int m = 0; m < roadsBuffers.Count; m++)
            {
                var roadsBuffer = (Polygon)roadsBuffers[m];


                if (roadsBuffer.Holes.Length < 30)
                {
                    for (int i = 0; i < roadsBuffer.Holes.Length; i++)
                    {
                        // Current polygon.
                        var p = new Polygon(roadsBuffer.Holes[i]);
                        var shrink1 = p.Buffer(-radius);
                        var block = shrink1.Buffer(radius, bpR);

                        if (!shrink1.IsEmpty)
                        {
                            if (block.OgcGeometryType == OgcGeometryType.MultiPolygon)
                            {
                                var geoms = (MultiPolygon)block;
                                foreach (var geom in geoms.Geometries)
                                {
                                    blocksBag.Add(converter.ToPolyline((Polygon)geom));
                                }
                            }
                            else
                            {
                                blocksBag.Add(converter.ToPolyline((Polygon)block));
                            }
                        }
                    }
                }
                else
                {
                    Parallel.For(0, roadsBuffer.Holes.Length, i =>
                    {

                        var p = new Polygon(roadsBuffer.Holes[i]);


                        var shrink1 = p.Buffer(-radius);

                        var block = shrink1.Buffer(radius, bpR);


                        if (!shrink1.IsEmpty)
                        {
                            if (block.OgcGeometryType == OgcGeometryType.MultiPolygon)
                            {
                                var geoms = (MultiPolygon)block;
                                foreach (var geom in geoms.Geometries)
                                {
                                    blocksBag.Add(converter.ToPolyline((Polygon)geom));
                                }
                            }
                            else
                            {
                                blocksBag.Add(converter.ToPolyline((Polygon)block));
                            }
                        }

                    });
                }
            }

            DA.SetDataList(0, blocksBag.ToArray());
        }


        private List<Geometry> GetLinesBuffer(List<Curve> curves, double dist, Polygon boundary , GeometryConverter converter)
        {
            List<Geometry> result = new List<Geometry>();

            BufferParameters bp = new BufferParameters(18, EndCapStyle.Flat);

            foreach (var c in curves)
            {
                if (!c.TryGetPolyline(out Polyline pl))
                {
                    pl = DesignToolbox.ConvertToPolyline(c, DocumentTolerance());
                }
                var insideGeom = boundary.Intersection(converter.ToLineString(pl));
                result.Add(insideGeom.Buffer(dist, bp));
            }

            return result;
        }

        private Polygon GetBoundaryPolygon(Curve c, GeometryConverter converter)
        {
            if (!c.TryGetPolyline(out Polyline pl))
            {
                pl = DesignToolbox.ConvertToPolyline(c, DocumentTolerance());
            }

            var ls = converter.ToPolygon(pl);

            return ls;
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
                return Resources.NA_GeneratingSites;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("B17B40B3-D71E-4A2E-8D25-7E88E92300AE"); }
        }
    }
}