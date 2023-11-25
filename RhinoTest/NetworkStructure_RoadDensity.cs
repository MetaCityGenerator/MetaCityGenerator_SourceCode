using Grasshopper.Kernel;

using Rhino.Geometry;
using Rhino.Geometry.Intersect;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using UrbanX.Planning.UrbanDesign;

using UrbanXTools.Properties;

namespace UrbanXTools
{
    public class NetworkStructure_RoadDensity : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of UrbanXTools, used for query xml data.
        private readonly string _moduleName = "NetworkStructure";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "NetworkStructure_RoadDensity";



        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public NetworkStructure_RoadDensity() : base("", "", "", "", "")
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
            pManager.AddCurveParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.item);
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            pManager.AddNumberParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.item);
            pManager.AddTextParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.item);
            pManager.AddCurveParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.list);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> allRoads = new List<Curve>();
            Curve region = null;

            var tolerance = DocumentTolerance();

            if (!DA.GetDataList(0, allRoads) || !DA.GetData(1, ref region))
                return;

            // Check the validation of inputs.
            var regionPl = DesignToolbox.ConvertToPolyline(region, tolerance);
            if (!regionPl.IsClosed)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Calculating region curve must be closed.");
                return;
            }
            regionPl.SetAllZ(0);

            var brep = Brep.CreatePlanarBreps(regionPl.ToPolylineCurve(), tolerance)[0];

            double length = 0;

            List<Curve> withinCurves = new List<Curve>();
            for (int i = 0; i < allRoads.Count; i++)
            {
                var c = allRoads[i];
                var pl = DesignToolbox.ConvertToPolyline(c, tolerance);
                pl.SetAllZ(0);

                var curve = pl.ToPolylineCurve();

                Intersection.CurveBrep(curve, brep, tolerance, out Curve[] overlape, out _);
                withinCurves.AddRange(overlape);

                for (int s = 0; s < overlape.Length; s++)
                {
                    length += overlape[s].GetLength();
                }
            }

            var area = AreaMassProperties.Compute(brep).Area;
            var density = Math.Round(length / area, 6);

            DA.SetData(0, density);
            DA.SetData(1, $"{density * 1000} km/km^2");
            DA.SetDataList(2, withinCurves);
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
                return Resources.NA_RoadDensity;

            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9B38805C-CA1C-476B-83D9-64A19CDAC8EB"); }
        }

    }

}
