using Grasshopper.Kernel;

using Rhino.Geometry;
using Rhino.Geometry.Intersect;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using MetaCity.Planning.UrbanDesign;

using MetaCityGenerator.Properties;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace MetaCityGenerator
{
    public class NetworkStructure_SiteAccessibility : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of MetaCityGenerator, used for query xml data.
        private readonly string _moduleName = "NetworkStructure";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "NetworkStructure_SiteAccessibility";


        public override GH_Exposure Exposure => GH_Exposure.primary;
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public NetworkStructure_SiteAccessibility() : base("", "", "", "", "")
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
            pManager.AddNumberParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
            pManager.AddCurveParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            pManager.AddCurveParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> allRoads = new List<Curve>();
            List<Curve> allSites = new List<Curve>();
            List<double> allScores = new List<double>();

            if (!DA.GetDataList(0, allRoads) || !DA.GetDataList(1, allScores) || !DA.GetDataList(2, allSites))
                return;

            var cleanedSites = CleanInputSites(allSites);

            var sitesParameters = DesignToolbox.ComputeParameters(cleanedSites, allRoads.ToArray(), allScores.ToArray(), DocumentTolerance());

            Curve[] sites = new Curve[sitesParameters.Length];
            double[] scores = new double[sitesParameters.Length];

            for (int i = 0; i < sitesParameters.Length; i++)
            {
                sites[i] = sitesParameters[i].Site;
                scores[i] = sitesParameters[i].Scores.Max();
            }


            DA.SetDataList(0, sites);
            DA.SetDataList(1, scores);
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
                return Resources.NA_SiteAccessibility;

            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9E7599B6-85AA-44A4-ADEA-17D0621D6FDA"); }
        }
    }
}

