using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using UrbanX.IO.XML;
using UrbanXTools.Properties;

namespace UrbanXTools
{
    public class Traffic_XML2GeojsonComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Traffic_XML2GeojsonComponent class.
        /// </summary>
        public Traffic_XML2GeojsonComponent()
          : base("Traffic_ConvertToGeojson", "TR_XML2GeoJSON",
                "Convert xml file to geojson",
              "UrbanX", "8_Traffic")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "Run", "Run the component", GH_ParamAccess.item);
            pManager.AddTextParameter("InputFile", "In", "This is the input xml path", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("GeneratedResult", "Res", "GenerateResult", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string inputPath = "";
            bool run = false;
            if (!DA.GetData(1, ref inputPath) || !DA.GetData(0, ref run))
            {
                return;
            }
            string geoJsonOutputPath= inputPath.Replace(".xml", ".geojson");
            XMLConverter.ConvertXMLtoGeojson(inputPath, geoJsonOutputPath);

            DA.SetData(0, geoJsonOutputPath);
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
                return Resources.TR_Convert;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9EEC2602-51E7-4043-AC7D-D9EAC56419C6"); }
        }
    }
}