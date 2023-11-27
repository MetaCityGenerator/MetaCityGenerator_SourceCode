using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using Grasshopper.Kernel;

using Rhino.Geometry;



namespace MetaCityGenerator
{
    public class NetworkStructure_GravityModel : GH_Component
    {
        private XElement _meta;

        // Module name is the subcatagory of MetaCityGenerator, used for query xml data.
        private readonly string _moduleName = "NetworkStructure";
        // componentId is used for querying xml data in current module.
        private readonly string _componentId = "NetworkStructure_GravityModel";


        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public override Guid ComponentGuid => new Guid("9163DD06-49D5-41E9-8BEF-9BE65909CEBE");


        public NetworkStructure_GravityModel() : base("", "", "", "", "")
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

            pManager.AddPointParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
            pManager.AddPointParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);
            pManager.AddNumberParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            this._meta = SharedResources.GetXML(_moduleName, _componentId);
            List<XElement> list = _meta.Element("outputs").Elements("output").ToList();

            pManager.AddNumberParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> sites = new List<Point3d>();
            List<Point3d> tods = new List<Point3d>();
            double e = 0;

            if (!DA.GetDataList(0, sites) || !DA.GetDataList(1, tods) || !DA.GetData(2, ref e))
                return;


            double[] scores = new double[sites.Count];
            for (int i = 0; i < sites.Count; i++)
            {
                var s = sites[i];
                foreach (var tod in tods)
                {
                    var dist = s.DistanceTo(tod);

                    if (dist == 0.0)
                        dist += DocumentTolerance();

                    scores[i] += F(dist, e);
                }

                scores[i] /= tods.Count;
            }

            DA.SetDataList(0, Normalization(scores));
        }

        private double F(double dist, double e) => 1.0 / Math.Pow(dist, e);

        private double[] Normalization(double[] scores)
        {
            double[] result = new double[scores.Length];
            var min = scores.Min();
            var max = scores.Max();

            for (int i = 0; i < scores.Length; i++)
            {
                var score = scores[i];
                result[i] = (score - min) / (max - min) * 1.0 ;
            }
            return result;
        }

    }
}
