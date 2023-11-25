using g3;

using NetTopologySuite.Geometries;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using NTS = NetTopologySuite;

namespace UrbanX.Planning.SpatialAnalysis
{
    public class RoadIntrBuildings
    {
        //Property
        public DMesh3 IntrMesh { get; set; }
        public List<double> ViewData { get; set; }
        public Roads RoadsValue { get; set; }

        //Constructor
       
        public static Roads Build(LineString[] roads, DMesh3 buildings, Dictionary<NTS.Geometries.Point, double> ptAreaDic, double CutRoadDis, double ViewRadius, double beta, out List<Point[]> PtListOuput, out DMesh3 MeshOut, bool coloredMesh = false)
        {
            var result = new Roads();
            List<NetTopologySuite.Geometries.Point[]> ptListOuput = new List<NetTopologySuite.Geometries.Point[]>(roads.Length);
            DMesh3 meshout = new DMesh3();
            switch (coloredMesh)
            {
                case false:
                    result = CalcTriWithoutColor(roads.ToList(), buildings, CutRoadDis, ViewRadius, beta, ptAreaDic, out ptListOuput);
                    break;
                case true:
                    result = CalcTriWithColor(roads.ToList(), buildings, CutRoadDis, ViewRadius, beta, ptAreaDic, out ptListOuput, out DMesh3 mesh);
                    //MeshEditor.Append(meshout, mesh);
                    meshout = new DMesh3(mesh);
                    break;
            }
            PtListOuput = ptListOuput;
            MeshOut = meshout;
            return result;
        }

        //TODO: 改成Embree
        public static Roads Build(LineString[] roads, Rhino.Geometry.Mesh buildings, Dictionary<NTS.Geometries.Point, double> ptAreaDic, double CutRoadDis, double ViewRadius, out List<Point[]> PtListOuput, out Rhino.Geometry.Mesh MeshOut, bool coloredMesh = false)
        {
            var result = new Roads();
            List<NetTopologySuite.Geometries.Point[]> ptListOuput = new List<NetTopologySuite.Geometries.Point[]>(roads.Length);
            Rhino.Geometry.Mesh meshout = new Rhino.Geometry.Mesh();
            switch (coloredMesh)
            {
                case false:
                    result = CalcTriWithoutColor(roads.ToList(), buildings, CutRoadDis, ViewRadius, ptAreaDic, out ptListOuput);
                    break;
                case true:
                    result = CalcTriWithColor(roads.ToList(), buildings, CutRoadDis, ViewRadius, ptAreaDic, out ptListOuput, out Rhino.Geometry.Mesh mesh);
                    //MeshEditor.Append(meshout, mesh);
                    meshout = mesh;
                    break;
            }
            PtListOuput = ptListOuput;
            MeshOut = meshout;
            return result;
        }

        //private method
        private static Roads CalcTriWithColor(List<NTS.Geometries.LineString> roads, DMesh3 meshIn, double CutRoadDis, double viewRadius, double beta, Dictionary<NTS.Geometries.Point, double> ptAreaDic, out List<Point[]> ptListOuput, out DMesh3 MeshResult)
        {
            //得到道路观测点
            var ptLargeList = DivideRoads(roads, CutRoadDis, out List<int> ptCountLargeList, out List<Point[]> ptCountList);

            //初始化颜色
            MeshCreation.InitiateColor(meshIn, Colorf.LightGrey);

            //开始相切
            MeshCreation.CalcRaysThroughTriParallel(meshIn, ptLargeList.ToArray(), viewRadius, beta, ptAreaDic, out ConcurrentDictionary<int, int> meshIntrDic,
               out List<double> TotalVisArea, out List<double> VisRatio, out List<double> NormalizedVisArea);

            //上颜色
            var IntrMesh = MeshCreation.ApplyColorsBasedOnRays(meshIn, meshIntrDic, Colorf.Yellow, Colorf.Red);
            MeshResult = IntrMesh;

            //赋值
            ptListOuput = ptCountList;
            return VisDataInRoad(roads, ptCountLargeList, TotalVisArea, VisRatio, NormalizedVisArea);
        }

        //TODO: 改成Embree
        private static Roads CalcTriWithColor(List<NTS.Geometries.LineString> roads, Rhino.Geometry.Mesh meshIn, double CutRoadDis, double viewRadius, Dictionary<NTS.Geometries.Point, double> ptAreaDic, out List<Point[]> ptListOuput, out Rhino.Geometry.Mesh MeshResult)
        {
            //得到道路观测点
            var ptLargeList = DivideRoads(roads, CutRoadDis, out List<int> ptCountLargeList, out List<Point[]> ptCountList);

            //开始相切
            MeshCreation.CalcRaysThroughTriParallel(meshIn, ptLargeList.ToArray(), viewRadius, ptAreaDic, out ConcurrentDictionary<int, int> meshIntrDic,
               out List<double> TotalVisArea, out List<double> VisRatio, out List<double> NormalizedVisArea);

            //上颜色
            var IntrMesh = MeshCreation.ApplyColorsBasedOnRays(meshIn, meshIntrDic, Colorf.Yellow, Colorf.Red);
            MeshResult = IntrMesh;

            //赋值
            ptListOuput = ptCountList;
            return VisDataInRoad(roads, ptCountLargeList, TotalVisArea, VisRatio, NormalizedVisArea);
        }
        private static Roads CalcTriWithoutColor(List<NTS.Geometries.LineString> roads, DMesh3 meshIn, double CutRoadDis, double viewRadius, double beta, Dictionary<NTS.Geometries.Point, double> ptAreaDic, out List<Point[]> ptListOuput)
        {
            //得到道路观测点
            var ptLargeList = DivideRoads(roads, CutRoadDis, out List<int> ptCountLargeList, out List<Point[]> ptCountList);

            //开始相切
            MeshCreation.CalcRaysThroughTriWithoutColorParallelDecay(meshIn, ptLargeList.ToArray(), viewRadius, beta, ptAreaDic,
                out List<double> TotalVisArea, out List<double> VisRatio, out List<double> NormalizedVisArea);

            //赋值
            ptListOuput = ptCountList;
            return VisDataInRoad(roads, ptCountLargeList, TotalVisArea, VisRatio, NormalizedVisArea);
        }

        //TODO: 改成Embree
        private static Roads CalcTriWithoutColor(List<NTS.Geometries.LineString> roads, Rhino.Geometry.Mesh meshIn, double CutRoadDis, double viewRadius, Dictionary<NTS.Geometries.Point, double> ptAreaDic, out List<Point[]> ptListOuput)
        {
            //得到道路观测点
            var ptLargeList = DivideRoads(roads, CutRoadDis, out List<int> ptCountLargeList, out List<Point[]> ptCountList);

            //开始相切
            MeshCreation.CalcRaysThroughTriParallel(meshIn, ptLargeList.ToArray(), viewRadius, ptAreaDic,
                out List<double> TotalVisArea, out List<double> VisRatio, out List<double> NormalizedVisArea);

            //赋值
            ptListOuput = ptCountList;
            return Debug_VisDataInRoad(roads, ptCountLargeList, TotalVisArea, VisRatio, NormalizedVisArea);
        }

        private static List<NTS.Geometries.Point> DivideRoads(List<NTS.Geometries.LineString> _roads, double _cutRoadDis, out List<int> _ptCountLargeList, out List<Point[]> _ptGroupLargeList)
        {
            List<NTS.Geometries.Point> ptLargeList = new List<NTS.Geometries.Point>();
            List<int> ptCountList = new List<int>(_roads.Count);
            List<Point[]> ptGroupList = new List<Point[]>(_roads.Count);

            for (int i = 0; i < _roads.Count; i++)
            {
                var line = _roads[i];
                Road road = new Road(i, line, _cutRoadDis);
                ptCountList.Add(road.PtCount);
                ptLargeList.AddRange(road.RoadPts);
                ptGroupList.Add(road.RoadPts.ToArray());
            }

            _ptCountLargeList = ptCountList;
            _ptGroupLargeList = ptGroupList;
            return ptLargeList;
        }
        private static Roads VisDataInRoad(List<NTS.Geometries.LineString> _roads, List<int> _ptCountLargeList, List<double> TotalVisArea, List<double> VisRatio, List<double> NormalizedVisArea)
        {
            //double maxvalue = double.MaxValue;
            double maxvalue = 999999d;
            var scoreList = new List<double[]>(_roads.Count);
            var resultList_nByOne_original = new List<double>(_roads.Count);
            var resultList_nByOne_tv = new List<double>(_roads.Count);
            var resultList_nByOne_vr = new List<double>(_roads.Count);
            var resultList_nByOne_nv = new List<double>(_roads.Count);

            List<int> re_ptCountLargeList = new List<int>() { 0 };
            re_ptCountLargeList.AddRange(_ptCountLargeList);

            int tempCount = 0;

            for (int i = 0; i < re_ptCountLargeList.Count - 1; i++)
            {
                var before = re_ptCountLargeList[i];
                var data = re_ptCountLargeList[i + 1];

                double temp_tv = 0d;
                double temp_vr = 0d;
                double temp_nv = 0d;
                tempCount += before;
                for (int j = tempCount; j < tempCount + data; j++)
                {
                    temp_tv += TotalVisArea[j];
                    temp_vr += VisRatio[j];
                    temp_nv += NormalizedVisArea[j];
                }
                scoreList.Add(new double[] { temp_tv, temp_vr, temp_nv });
            }

            //TODO:
            for (int i = 0; i < scoreList.Count; i++)
            {
                resultList_nByOne_original.Add(scoreList[i][0]);
                resultList_nByOne_tv.Add(CalcRoadValue(scoreList[i][0], maxvalue));
                resultList_nByOne_vr.Add(CalcRoadValue(scoreList[i][1], maxvalue));
                resultList_nByOne_nv.Add(CalcRoadValue(scoreList[i][2], maxvalue));
            }
            return new Roads(_roads.ToArray(), resultList_nByOne_original.ToArray(), resultList_nByOne_tv.ToArray(), resultList_nByOne_vr.ToArray(), resultList_nByOne_nv.ToArray());
        }


        private static Roads Debug_VisDataInRoad(List<NTS.Geometries.LineString> _roads, List<int> _ptCountLargeList, List<double> TotalVisArea, List<double> VisRatio, List<double> NormalizedVisArea)
        {
            //double maxvalue = double.MaxValue;
            double maxvalue = 999999d;
            var scoreList = new List<double[]>(_roads.Count);
            var resultList_nByOne_original = new List<double>(_roads.Count);
            var resultList_nByOne_tv = new List<double>(_roads.Count);
            var resultList_nByOne_vr = new List<double>(_roads.Count);
            var resultList_nByOne_nv = new List<double>(_roads.Count);

            List<int> re_ptCountLargeList = new List<int>() { 0 };
            re_ptCountLargeList.AddRange(_ptCountLargeList);

            int tempCount = 0;

            for (int i = 0; i < re_ptCountLargeList.Count - 1; i++)
            {
                var before = re_ptCountLargeList[i];
                var data = re_ptCountLargeList[i + 1];

                double temp_tv = 0d;
                double temp_vr = 0d;
                double temp_nv = 0d;
                tempCount += before;
                for (int j = tempCount; j < tempCount + data; j++)
                {
                    temp_tv += TotalVisArea[j];
                    temp_vr += VisRatio[j];
                    temp_nv += NormalizedVisArea[j];
                }
                scoreList.Add(new double[] { temp_tv, temp_vr, temp_nv });
            }

            //TODO:
            for (int i = 0; i < scoreList.Count; i++)
            {
                resultList_nByOne_original.Add(scoreList[i][0]);
                //resultList_nByOne_tv.Add(scoreList[i][0]);
                resultList_nByOne_vr.Add(scoreList[i][1]);
                resultList_nByOne_nv.Add(scoreList[i][2]);
            }
            return new Roads(_roads.ToArray(), resultList_nByOne_original.ToArray(), resultList_nByOne_vr.ToArray(), resultList_nByOne_nv.ToArray());
        }

        private static double CalcRoadValue(double data, double maxValue)
        {
            double result;
            if (data != 0)
            {
                var score = 1 / data;
                result = score;
            }
            else
            {
                result = maxValue;
            }
            return result;
        }
    }
}


