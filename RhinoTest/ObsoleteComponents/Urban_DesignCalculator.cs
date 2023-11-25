using Grasshopper.Kernel;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using UrbanX.Planning.UrbanDesign;

using UrbanXTools.Properties;

namespace UrbanXTools
{
    [Obsolete("Rarely use this method.")]
    public class Urban_DesignCalculator : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of UrbanXTools, used for query xml data.
        private readonly string _moduleName = "UrbanDesign";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "Urban_DesignCalculator";


        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public Urban_DesignCalculator() : base("", "", "", "", "")
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

            pManager.AddCurveParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.item);
            pManager.AddCurveParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
            pManager.AddIntervalParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[3].Attribute("name"), (string)list[3].Attribute("nickname"), (string)list[3].Attribute("description"), GH_ParamAccess.item);
            pManager.AddNumberParameter((string)list[4].Attribute("name"), (string)list[4].Attribute("nickname"), (string)list[4].Attribute("description"), GH_ParamAccess.item);
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            pManager.AddNumberParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.item);
            pManager.AddNumberParameter((string)list[3].Attribute("name"), (string)list[3].Attribute("nickname"), (string)list[3].Attribute("description"), GH_ParamAccess.item);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve site = null;
            List<Curve> types = new List<Curve>();
            List<Interval> ranges = new List<Interval>();
            double far = 0, density = 0;

            if (!DA.GetData(0, ref site) || !DA.GetDataList(1, types) || !DA.GetDataList(2, ranges) || !DA.GetData(3, ref far) || !DA.GetData(4, ref density))
                return;

            var siteArea = AreaMassProperties.Compute(site).Area;
            double[] buildingAreas = new double[types.Count];
            for (int i = 0; i < types.Count; i++)
            {
                buildingAreas[i] = AreaMassProperties.Compute(types[i]).Area;
            }

            int[] typeCounts = DesignCalculator.GetBuildingCounts(buildingAreas, siteArea, density);
            int[] typeFloors = DesignCalculator.GetBuildingFloors(buildingAreas, typeCounts, ranges.ToArray(), siteArea, far);

            double totalOutlinesArea = 0, totalFloorsArea = 0;
            for (int i = 0; i < buildingAreas.Length; i++)
            {
                totalOutlinesArea += buildingAreas[i] * typeCounts[i];
                totalFloorsArea += buildingAreas[i] * typeCounts[i] * typeFloors[i];
            }


            DA.SetDataList(0, typeCounts);
            DA.SetDataList(1, typeFloors);
            DA.SetData(2, Math.Round(totalFloorsArea / siteArea, 3));
            DA.SetData(3, Math.Round(totalOutlinesArea / siteArea, 3));
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
                return Resources.ICON_for_All;

            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2C3099DF-ED82-4139-B81D-AAAC92FE33A5"); }
        }
    }

}
