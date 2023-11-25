using Grasshopper.Kernel;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using UrbanX.DataStructures.Trees;
using UrbanX.DataStructures.Utility;
using UrbanX.Planning.UrbanDesign;

using UrbanXTools.Properties;

namespace UrbanXTools
{
    //[Obsolete("Rarely use this method.")]
    public class Urban_DynamicSplit : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of UrbanXTools, used for query xml data.
        private readonly string _moduleName = "UrbanDesign";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "Urban_DynamicSplit";


        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public Urban_DynamicSplit() : base("", "", "", "", "")
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
            pManager.AddNumberParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[3].Attribute("name"), (string)list[3].Attribute("nickname"), (string)list[3].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[4].Attribute("name"), (string)list[4].Attribute("nickname"), (string)list[4].Attribute("description"), GH_ParamAccess.item);
            pManager.AddBooleanParameter((string)list[5].Attribute("name"), (string)list[5].Attribute("nickname"), (string)list[5].Attribute("description"), GH_ParamAccess.item);
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            pManager.AddCurveParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddCurveParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[3].Attribute("name"), (string)list[3].Attribute("nickname"), (string)list[3].Attribute("description"), GH_ParamAccess.list);
            pManager.AddIntegerParameter((string)list[4].Attribute("name"), (string)list[4].Attribute("nickname"), (string)list[4].Attribute("description"), GH_ParamAccess.list);
            pManager.AddTextParameter((string)list[5].Attribute("name"), (string)list[5].Attribute("nickname"), (string)list[5].Attribute("description"), GH_ParamAccess.item);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve curve = null;
            List<double> ratios = new List<double>();
            List<double> priorities = new List<double>();
            List<double> scores = new List<double>();

            // polyline as the boundingbox.
            List<Polyline> edges = new List<Polyline>();
            List<double> scoresOutput = new List<double>();

            double radiant = 0;
            bool renewRadiant = true;
            var tolerance = DocumentTolerance();

            if (!DA.GetData(0, ref curve) || !DA.GetDataList(1, ratios) || !DA.GetDataList(2, priorities) || !DA.GetDataList(3, scores) || !DA.GetData(4, ref radiant) || !DA.GetData(5, ref renewRadiant))
                return;

            var nodes = new BSPTreeNode[ratios.Count];
            for (int c = 0; c < ratios.Count; c++)
            {
                // Create new node by using value and priority from building type.
                nodes[c] = new BSPTreeNode(c, ratios[c], priorities[c]);
            }

            BSPTree bspTree = new BSPTree(nodes);
            LinkedList<Curve> curvesResult = new LinkedList<Curve>();
            LinkedList<double> radiantsResult = new LinkedList<double>();
            LinkedList<double[]> scoresResult = new LinkedList<double[]>();
            LinkedList<int> nodeKeys = new LinkedList<int>();


            DesignToolbox.SplitRecursive(curve, bspTree.Root, scores.ToArray(), radiant, renewRadiant, tolerance, ref curvesResult, ref radiantsResult, ref scoresResult, ref nodeKeys);

            for (int i = 0; i < curvesResult.Count; i++)
            {
                var edgeLines = SiteBoundingRect.GetEdges(curvesResult.ToArray()[i], radiantsResult.ToArray()[i]);

                HashSet<Point3d> visited = new HashSet<Point3d>();
                List<Point3d> pts = new List<Point3d>();
                foreach (var line in edgeLines)
                {
                    if (!visited.Contains(line.From))
                    {
                        visited.Add(line.From);
                        pts.Add(line.From);
                    }

                    if (!visited.Contains(line.To))
                    {
                        visited.Add(line.To);
                        pts.Add(line.To);
                    }
                }
                pts.Add(pts[0]);

                //edges.AddRange(SiteBoundingRect.GetEdges(curvesResult.ToArray()[i], radiantsResult.ToArray()[i]));
                edges.Add(new Polyline(pts));
                scoresOutput.AddRange(scoresResult.ToArray()[i]);
            }


            DA.SetDataList(0, curvesResult);
            DA.SetDataList(1, edges);
            DA.SetDataList(2, scoresOutput);
            DA.SetDataList(3, radiantsResult);
            DA.SetDataList(4, nodeKeys);
            DA.SetData(5, bspTree.DrawTree());
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
                return Resources.UD_DynamicSplit;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("5483E605-BC61-45C2-AE44-C8AEF276F678"); }
        }
    }

}
