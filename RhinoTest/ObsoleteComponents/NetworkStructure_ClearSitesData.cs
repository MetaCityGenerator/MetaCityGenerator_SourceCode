using Grasshopper.Kernel;

using Rhino.Geometry;
using Rhino.Geometry.Intersect;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using UrbanX.Planning.UrbanDesign;

using UrbanXTools.Properties;

namespace UrbanXTools
{
    [Obsolete("Have been embeded into SiteAccessibility component.")]
    public class NetworkStructure_ClearSitesData : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of UrbanXTools, used for query xml data.
        private readonly string _moduleName = "NetworkStructure";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "NetworkStructure_ClearSitesData";


        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public NetworkStructure_ClearSitesData() : base("", "", "", "", "")
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
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            pManager.AddCurveParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> allSites = new List<Curve>();


            if (!DA.GetDataList(0, allSites))
                return;


            Curve[] planarSites = new Curve[allSites.Count];
            var tol = DocumentTolerance();

            Parallel.For(0, allSites.Count, i =>
            {
                var c = allSites[i];

                var pl = DesignToolbox.ConvertToPolyline(c, tol);
                pl.SetAllZ(0);
                if (pl.First != pl.Last)
                    pl.Add(pl.First);

                var plc = pl.ToPolylineCurve();

                var sects = Intersection.CurveSelf(plc, tol);

                if (!plc.IsValid || sects.Count > 0)
                {
                    planarSites[i] = null;
                }
                else
                {
                    planarSites[i] = plc;
                }
            });


            List<Curve> result = new List<Curve>();
            foreach (var s in planarSites)
            {
                if (s != null)
                    result.Add(s);
            }

            DA.SetDataList(0, result);
        }


        private Curve[] CleanInputSites(IList<Curve> allSites)
        {
            Curve[] planarSites = new Curve[allSites.Count];
            var tol = DocumentTolerance();

            Parallel.For(0, allSites.Count, i =>
            {
                var c = allSites[i];

                var pl = DesignToolbox.ConvertToPolyline(c, tol);
                pl.SetAllZ(0);
                if (pl.First != pl.Last)
                    pl.Add(pl.First);

                var plc = pl.ToPolylineCurve();

                var sects = Intersection.CurveSelf(plc, tol);

                if (!plc.IsValid || sects.Count > 0)
                {
                    planarSites[i] = null;
                }
                else
                {
                    planarSites[i] = plc;
                }
            });


            List<Curve> result = new List<Curve>();
            foreach (var s in planarSites)
            {
                if (s != null)
                    result.Add(s);
            }

            return result.ToArray();
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
                return Resources.NA_ClearSiteData;

            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("1507AB92-4108-46CA-9680-E3505CF7B2EC"); }
        }
    }

}
