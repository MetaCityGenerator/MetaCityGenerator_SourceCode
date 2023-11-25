using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using UrbanX.Traffic;
using UrbanXTools.Properties;

namespace UrbanXTools
{
    public class Traffic_LinkStatsComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Traffic_LinkStatsComponent class.
        /// </summary>
        public Traffic_LinkStatsComponent()
          : base("Traffic_LinkStats", "TR_Links",
                "Extract link data from linkstats.txt.gz",
              "UrbanX", "8_Traffic")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "Run", "Run the component", GH_ParamAccess.item);
            pManager.AddTextParameter("InputFile", "In", "This is the input txt.gz path", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("LinkID", "ID", "Link id of links", GH_ParamAccess.list);
            pManager.AddIntegerParameter("FromNode", "From", "From node of links", GH_ParamAccess.list);
            pManager.AddIntegerParameter("ToNode", "To", "To node of links", GH_ParamAccess.list);
            pManager.AddNumberParameter("FreeSpeed", "FreeSpeed", "FreeSpeed of links", GH_ParamAccess.list);
            //pManager.AddNumberParameter("Capacity", "Capacity", "Capacity of links", GH_ParamAccess.list);
            pManager.AddNumberParameter("TravelVolume", "T_Volume", "Travel volume of links", GH_ParamAccess.tree);
            //pManager.AddNumberParameter("TravelTime", "T_Time", "Travel time of links", GH_ParamAccess.tree);
            pManager.AddTextParameter("AttributeName", "Name", "Name of each attribute", GH_ParamAccess.list);
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

            var (headers, extractedData) = StatsAnalysis.LinkStatsAnalysis(inputPath);
            
            int count = extractedData.Count;

            List<int> linkIDs = new List<int>(count);
            List<int> fromNodes = new List<int>(count);
            List<int> toNodes = new List<int>(count);
            List<double> freeSpeeds = new List<double>(count);
            //List<double> capacities = new List<double>(count);
            DataTree<double> travelVolumes = new DataTree<double>();
            DataTree<double> travelTimes = new DataTree<double>();
            if (run)
            {
                for (int i = 0; i < count; i++)
                {
                    var row = extractedData[i];
                    linkIDs.Add(int.Parse(row[0]));
                    fromNodes.Add(int.Parse(row[1]));
                    toNodes.Add(int.Parse(row[2]));
                    freeSpeeds.Add(double.Parse(row[3]));
                    //capacities.Add(double.Parse(row[4]));

                    List<string> hrsvalues = new List<string>();
                    for (int j = 5; j < 29; j++)
                        hrsvalues.Add(row[j]);
                    //List<string> travelTimevalues = new List<string>();
                    //for (int j = 30; j < 54; j++)
                    //    travelTimevalues.Add(row[j]);

                    for (int j = 0; j < hrsvalues.Count; j++)
                        travelVolumes.Add(double.Parse(hrsvalues[j]), new GH_Path(i));

                    //for (int j = 0; j < travelTimevalues.Count; j++)
                    //    travelTimes.Add(double.Parse(travelTimevalues[j]), new GH_Path(i));
                }
            }

            DA.SetDataList(0, linkIDs);
            DA.SetDataList(1, fromNodes);
            DA.SetDataList(2, toNodes);
            DA.SetDataList(3, freeSpeeds);
            //DA.SetDataList(4, capacities);
            DA.SetDataTree(4, travelVolumes);
            //DA.SetDataTree(5, travelTimes);
            DA.SetDataList(5, headers);
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
                return Resources.TR_LinkStat;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("4CD88A8C-20BD-4992-9D6A-04BB617DB983"); }
        }
    }
}