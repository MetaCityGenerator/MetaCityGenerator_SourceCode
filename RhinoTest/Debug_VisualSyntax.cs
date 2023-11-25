using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using NetTopologySuite.Geometries;
using Rhino.Geometry;
using UrbanX.IO.OpenNURBS;
using UrbanX.Planning.SpatialAnalysis;
using UrbanX.Planning.UrbanDesign;
using UrbanX.Planning.Utility;

namespace UrbanXTools
{
    public class Debug_VisualSyntax : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Debug_VisualSyntax class.
        /// </summary>
        public Debug_VisualSyntax()
          : base("Debug_VisualSyntax", "VSyntax",
                "Calculate visual syntax",
              "UrbanX", "7_Utility")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Road", "R", "Input curves for road networks", GH_ParamAccess.list);//road
            pManager.AddGenericParameter("GM Class", "GMC", "Input GM class", GH_ParamAccess.item);//generated mesh
            pManager.AddNumberParameter("SegmentLength", "SegL", "Input crv as segment length", GH_ParamAccess.item, 500d);//divide length
            pManager.AddNumberParameter("ViewRangeRadius", "R", "Input view range", GH_ParamAccess.item, 300d);//view range
            pManager.AddBooleanParameter("ExportMeshFlag", "E", "Export mesh or not", GH_ParamAccess.item, false);//export mesh
            pManager[2].Optional = false;
            pManager[3].Optional = false;
            pManager[4].Optional = false;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Curves","Crvs","", GH_ParamAccess.list);//road
            pManager.AddGenericParameter("Points", "Pts", "", GH_ParamAccess.tree);//pt
            pManager.AddGenericParameter("VisResult", "VisR", "output data of each road. " +
                "[0] total vis area, " +
                "[1] transfered normalized vis area(each vis area/total vis area). " +
                "[2] transfered vis ratio(each vis area/area should be seen).", GH_ParamAccess.tree);//ratio
            pManager.AddGenericParameter("ColorMesh", "CMesh", "", GH_ParamAccess.item);//mesh
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> inputRoadList = new List<Curve>();
            Debug_GeneratedMeshClass inputGMClass = new Debug_GeneratedMeshClass();

            double viewRangeRadius = 300d;
            double segmentLength = 500d;
            var exportMeshFlag = false;

            if (!DA.GetDataList(0, inputRoadList)) { return; }
            if (!DA.GetData(1, ref inputGMClass)) { return; }
            if (!DA.GetData(2, ref segmentLength)) { return; }
            if (!DA.GetData(3, ref viewRangeRadius)) { return; }
            if (!DA.GetData(4, ref exportMeshFlag)) { return; }

            PrecisionModel pm = new PrecisionModel(1.0 / DocumentTolerance());
            GeometryFactory gf = new GeometryFactory(pm);
            GeometryConverter converter = new GeometryConverter(gf);

            Mesh topBtnMesh = inputGMClass.TopBtnList;
            Mesh sidesMesh = inputGMClass.SideList;
            double[] inputAreaList = inputGMClass.SideAreaList;
            Point3d[] inputCentPtList = inputGMClass.CenPtList;

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
            var temp_segs = DataCleaning.CleanMultiLineString(geoms, gf);
            var segs = GeometryFactory.ToLineStringArray(temp_segs.Geometries);


            //准备承载数据
            List<NetTopologySuite.Geometries.Point[]> ptOut = new List<NetTopologySuite.Geometries.Point[]>(segs.Length);
            Roads result;
            Mesh rhResultMesh = new Mesh();

            //开始相切
            if (exportMeshFlag == false)
            {
                result = RoadIntrBuildings.Build(segs, sidesMesh, secPtDic, segmentLength, viewRangeRadius, out ptOut, out _, false);
            }
            else
            {
                result = RoadIntrBuildings.Build(segs, sidesMesh, secPtDic, segmentLength, viewRangeRadius, out ptOut, out Mesh sideMesh, true);
                rhResultMesh.Append(sideMesh);
                if (topBtnMesh.Vertices.Count!=0)
                {
                    rhResultMesh.Append(topBtnMesh);
                }
                
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
                //visResult.Add(result.Score_transfer_totalVisArea[i], path);
                visResult.Add(result.Score_transfer_normalizedVisArea[i], path);
                visResult.Add(result.Score_transfer_visRatio[i], path);
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
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("D3223CD9-39FE-4AA4-95C9-8805241B72D5"); }
        }
    }
}