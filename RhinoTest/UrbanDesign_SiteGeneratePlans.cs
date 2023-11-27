using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using MetaCity.Planning.UrbanDesign;

using MetaCityGenerator.Properties;

namespace MetaCityGenerator
{
    public class UrbanDesign_SiteGeneratePlans : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of MetaCityGenerator, used for query xml data.
        private readonly string _moduleName = "UrbanDesign";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "UrbanDesign_SiteGeneratePlans";


        public UrbanDesign_SiteGeneratePlans() : base("", "", "", "", "")
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

            pManager.AddGenericParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddIntegerParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            pManager.AddGenericParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);

            pManager.AddCurveParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
            pManager.AddTextParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.list);
            pManager.AddTextParameter((string)list[3].Attribute("name"), (string)list[3].Attribute("nickname"), (string)list[3].Attribute("description"), GH_ParamAccess.list);

            pManager.AddCurveParameter((string)list[4].Attribute("name"), (string)list[4].Attribute("nickname"), (string)list[4].Attribute("description"), GH_ParamAccess.tree);
            pManager.AddCurveParameter((string)list[5].Attribute("name"), (string)list[5].Attribute("nickname"), (string)list[5].Attribute("description"), GH_ParamAccess.tree);

            pManager.AddCurveParameter((string)list[6].Attribute("name"), (string)list[6].Attribute("nickname"), (string)list[6].Attribute("description"), GH_ParamAccess.tree);
            pManager.AddCurveParameter((string)list[7].Attribute("name"), (string)list[7].Attribute("nickname"), (string)list[7].Attribute("description"), GH_ParamAccess.tree);
            pManager.AddIntegerParameter((string)list[8].Attribute("name"), (string)list[8].Attribute("nickname"), (string)list[8].Attribute("description"), GH_ParamAccess.tree);

            pManager.AddBrepParameter((string)list[9].Attribute("name"), (string)list[9].Attribute("nickname"), (string)list[9].Attribute("description"), GH_ParamAccess.tree);
            pManager.AddTextParameter((string)list[10].Attribute("name"), (string)list[10].Attribute("nickname"), (string)list[10].Attribute("description"), GH_ParamAccess.tree);
            pManager.AddTextParameter("ErrorLog", "Log", "Recording the failures detial", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<SiteParameters> siteParametersInput = new List<SiteParameters>();
            int cityIndex = 0;

            if (!DA.GetDataList(0, siteParametersInput) || !DA.GetData(1, ref cityIndex))
                return;

            string log = null;
            var siteResults = DesignToolbox.ComputingDesign(siteParametersInput.ToArray(), cityIndex, DocumentTolerance(), out _, out _ ,ref log);

            // Error log.
            StringBuilder builder = new StringBuilder();


            // Sites level.
            List<Curve> outputSites = new List<Curve>();
            List<string> outputSitesFAR = new List<string>();
            List<string> outputSitesDensity = new List<string>();

            // SubSites level.
            DataTree<Curve> outputSubSites = new DataTree<Curve>();
            DataTree<Curve> outputSetbacks = new DataTree<Curve>();

            // Buildings level.
            DataTree<Curve> outputLayers = new DataTree<Curve>();
            DataTree<Curve> outputRoofs = new DataTree<Curve>();
            DataTree<int> outputFloors = new DataTree<int>();

            // Breps level.
            DataTree<Brep> outputBreps = new DataTree<Brep>();
            DataTree<string> outputFunctions = new DataTree<string>();



            for (int siteId = 0; siteId < siteResults.Length; siteId++)
            {
                if (siteResults[siteId] == null)
                    continue;

                outputSites.Add(siteResults[siteId].Site);
                outputSitesFAR.Add($"FAR:{siteResults[siteId].FAR}");
                outputSitesDensity.Add($"Density:{siteResults[siteId].Density}");

                for (int subSiteId = 0; subSiteId < siteResults[siteId].SubSites.Length; subSiteId++)
                {
                    GH_Path subSitePath = new GH_Path(siteId, subSiteId);
                    outputSubSites.Add(siteResults[siteId].SubSites[subSiteId], subSitePath);
                    outputSetbacks.AddRange(siteResults[siteId].SubSiteSetbacks[subSiteId], subSitePath);

                    for (int buildingId = 0; buildingId < siteResults[siteId].SubSiteBuildingGeometries[subSiteId].Length; buildingId++)
                    {
                        var building = siteResults[siteId].SubSiteBuildingGeometries[subSiteId][buildingId];

                        if (building.BuildingArea == 0)
                        {
                            builder.AppendLine($"Site_{siteId} failed to generate buildings.");
                            builder.AppendLine($"Landuse:{(SiteTypes)siteParametersInput[siteId].SiteType}");
                            builder.AppendLine($"Far:{siteParametersInput[siteId].FAR}");
                            builder.AppendLine($"Density；{siteParametersInput[siteId].Density}\n");
                            continue;
                        }


                        GH_Path buildingPath = new GH_Path(siteId, subSiteId, buildingId);

                        outputLayers.AddRange(building.Layers, buildingPath);
                        outputRoofs.AddRange(building.RoofCurves, buildingPath);
                        outputFloors.AddRange(building.Floors, buildingPath);

                        for (int brepId = 0; brepId < building.Breps.Length; brepId++)
                        {
                            //GH_Path brepPath = new GH_Path(siteId, subSiteId, buildingId , brepId);
                            outputBreps.Add(building.Breps[brepId], buildingPath);
                            outputFunctions.Add(building.BrepFunctions[brepId], buildingPath);
                        }
                    }
                }
            }


            DA.SetDataList(0, siteResults);


            DA.SetDataList(1, outputSites);
            DA.SetDataList(2, outputSitesFAR);
            DA.SetDataList(3, outputSitesDensity);

            DA.SetDataTree(4, outputSubSites);
            DA.SetDataTree(5, outputSetbacks);

            DA.SetDataTree(6, outputLayers);
            DA.SetDataTree(7, outputRoofs);
            DA.SetDataTree(8, outputFloors);

            DA.SetDataTree(9, outputBreps);
            DA.SetDataTree(10, outputFunctions);
            DA.SetData(11, log);
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
                return Resources.UD_SiteGeneratePlans;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("657E8075-9FDC-4127-8369-7C3F946CDE01"); }
        }
    }
}
