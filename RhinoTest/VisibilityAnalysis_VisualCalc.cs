using g3;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;

using NetTopologySuite.Geometries;

using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using MetaCity.IO.OpenNURBS;
using MetaCity.Planning.SpatialAnalysis;
using MetaCity.Planning.UrbanDesign;
using MetaCity.Planning.Utility;

using MetaCityGenerator.Properties;

using Rh = Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace MetaCityGenerator
{
    public class VisibilityAnalysis_VisualCalc : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public XElement meta;
        public static string c_moduleName = "VisibilityAnalysis";
        public static string c_id = "VisibilityAnalysis_VisualCalc";

        #region 备用
        //public Urban_SustainabilityComponent()
        //  : base("IndexCalculation", "IndexCalc",
        //      "index calculation, included EC,WC, GC, Population Amount",
        //      "MetaCityFireFly", "AutoGenerator")
        //{
        //}
        #endregion
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public VisibilityAnalysis_VisualCalc() : base("", "", "", "", "")
        {
            //ToDo 完善这部分内容
            //AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(SharedUtils.Resolve);
            this.meta = SharedResources.GetXML(c_moduleName, c_id);
            this.Name = this.meta.Element("name").Value;
            this.NickName = this.meta.Element("nickname").Value;
            this.Description = this.meta.Element("description").Value + "\nv.2.2";
            this.Category = this.meta.Element("category").Value;
            this.SubCategory = this.meta.Element("subCategory").Value;
        }


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //ToDo 完善这部分内容
            this.meta = SharedResources.GetXML(c_moduleName, c_id);
            List<XElement> list = this.meta.Element("inputs").Elements("input").ToList<XElement>();
            pManager.AddCurveParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.list);//road
            pManager.AddGenericParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.item);//generated mesh
            pManager.AddNumberParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.item, 500d);//divide length
            pManager.AddNumberParameter((string)list[3].Attribute("name"), (string)list[3].Attribute("nickname"), (string)list[3].Attribute("description"), GH_ParamAccess.item, 300d);//view range
            pManager.AddBooleanParameter((string)list[4].Attribute("name"), (string)list[4].Attribute("nickname"), (string)list[4].Attribute("description"), GH_ParamAccess.item, false);//export mesh
            pManager.AddNumberParameter("beta", "β", "beta for vis decay", GH_ParamAccess.item, 0);
            pManager[2].Optional = false;
            pManager[3].Optional = false;
            pManager[4].Optional = false;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //ToDo 完善这部分内容
            this.meta = SharedResources.GetXML(c_moduleName, c_id);
            List<XElement> list = this.meta.Element("outputs").Elements("output").ToList<XElement>();
            pManager.AddGenericParameter((string)list[0].Attribute("name"), (string)list[0].Attribute("nickname"), (string)list[0].Attribute("description"), GH_ParamAccess.item);//road
            pManager.AddGenericParameter((string)list[1].Attribute("name"), (string)list[1].Attribute("nickname"), (string)list[1].Attribute("description"), GH_ParamAccess.list);//pt
            pManager.AddGenericParameter((string)list[2].Attribute("name"), (string)list[2].Attribute("nickname"), (string)list[2].Attribute("description"), GH_ParamAccess.tree);//ratio
            pManager.AddGenericParameter((string)list[3].Attribute("name"), (string)list[3].Attribute("nickname"), (string)list[3].Attribute("description"), GH_ParamAccess.tree);//mesh
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Rh.Curve> inputRoadList = new List<Rh.Curve>();
            GeneratedMeshClass inputGMClass = new GeneratedMeshClass();

            double viewRangeRadius = 300d;
            double segmentLength = 500d;
            double beta = 0d;
            var exportMeshFlag = false;

            if (!DA.GetDataList(0, inputRoadList)) { return; }
            if (!DA.GetData(1, ref inputGMClass)) { return; }
            if (!DA.GetData(2, ref segmentLength)) { return; }
            if (!DA.GetData(3, ref viewRangeRadius)) { return; }
            if (!DA.GetData(4, ref exportMeshFlag)) { return; }
            if (!DA.GetData(5, ref beta)) { return; }

            PrecisionModel pm = new PrecisionModel(1.0 / DocumentTolerance());
            GeometryFactory gf = new GeometryFactory(pm);
            GeometryConverter converter = new GeometryConverter(gf);

            Rh.Mesh topBtnMesh = inputGMClass.TopBtnList;
            DMesh3 sidesMesh = inputGMClass.SideList;
            double[] inputAreaList = inputGMClass.SideAreaList;
            Rh.Point3d[] inputCentPtList = inputGMClass.CenPtList;

            ////选择需要计算的点
            var secPtDic = MeshCreation.GenerateDic(inputAreaList, inputCentPtList);

            //将输入线转化为多段线
            LineString[] ls = new LineString[inputRoadList.Count];
            for (int i = 0; i < inputRoadList.Count; i++)
            {
                var c = inputRoadList[i];
                if (!c.TryGetPolyline(out Polyline pl))
                    pl = DesignToolbox.ConvertToPolyline(c, DocumentTolerance());
                ls[i] = converter.ToLineString(pl);
            }

            GeometryCollection geoms = new GeometryCollection(ls, gf);
            //var temp_segs = DataCleaning.CleanMultiLineString(geoms, gf);
            var segs = GeometryFactory.ToLineStringArray(geoms.Geometries);


            //准备承载数据
            List<NetTopologySuite.Geometries.Point[]> ptOut = new List<NetTopologySuite.Geometries.Point[]>(segs.Length);
            Roads result;
            Mesh rhResultMesh = new Mesh();

            //开始相切
            if (exportMeshFlag == false)
            {
                result = RoadIntrBuildings.Build(segs, sidesMesh, secPtDic, segmentLength, viewRangeRadius, beta, out ptOut, out _, false);
            }
            else
            {
                result = RoadIntrBuildings.Build(segs, sidesMesh, secPtDic, segmentLength, viewRangeRadius, beta, out ptOut, out DMesh3 sideMesh, true);
                rhResultMesh.Append(MeshCreation.ConvertFromDMesh3(sideMesh));
                rhResultMesh.Append(topBtnMesh);
            }


            //Rhino数据输出
            DataTree<Point3d> ptResult = new DataTree<Point3d>();
            for (int i = 0; i < ptOut.Count; i++)
            {
                GH_Path path = new GH_Path(i);
                var tempPts = ptOut[i];
                for (int j = 0; j < tempPts.Length; j++)
                {
                    ptResult.Add(ToRHPt(tempPts[j]), path);
                }
            }

            Polyline[] crvResult = new Polyline[segs.Length];
            for (int i = 0; i < segs.Length; i++)
            {
                var seg = segs[i];
                crvResult[i] = converter.ToPolyline(seg);
            }

            DataTree<double> visResult = new DataTree<double>();
            for (int i = 0; i < segs.Length; i++)
            {
                GH_Path path = new GH_Path(i);
                visResult.Add(result.Score_totalVisArea[i], path);
                visResult.Add(result.Score_transfer_totalVisArea[i], path);
                visResult.Add(result.Score_transfer_normalizedVisArea[i], path);
                visResult.Add(result.Score_transfer_normalizedVisArea[i], path);
            }

            DA.SetDataList(0, crvResult);
            DA.SetDataTree(1, ptResult);
            DA.SetDataTree(2, visResult);
            DA.SetData(3, rhResultMesh);
        }


        public static Point3d ToRHPt(NetTopologySuite.Geometries.Point pt)
        {
            return new Point3d(pt.X, pt.Y, pt.Z);
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
                //return Resources.IconForThisComponent;
                return Resources.SA_VisualCalc;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("A454C33C-D393-4BF3-8E31-35393D042A38"); }
        }
    }
}
