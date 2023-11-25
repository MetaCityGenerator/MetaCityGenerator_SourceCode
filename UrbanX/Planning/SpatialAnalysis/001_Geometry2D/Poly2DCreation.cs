using g3;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

using System;
using System.Collections.Generic;
using System.Linq;

using NTS = NetTopologySuite;

namespace UrbanX.Planning.SpatialAnalysis
{
    public enum JSONGeo
    {
        Point,
        LineString,
        Polygon
    }
    public class Poly2DCreation
    {
        public static NTS.Geometries.Geometry[] CreateCircle(Vector2d[] origins, double radius)
        {
            var count = origins.Length;
            var circleList = new NTS.Geometries.Geometry[count];
            for (int i = 0; i < count; i++)
            {
                NTS.Geometries.Point temp = new NTS.Geometries.Point(origins[i].x, origins[i].y);
                var circle = temp.Buffer(radius, NetTopologySuite.Operation.Buffer.EndCapStyle.Round);
                circleList[i] = circle;
            }
            return circleList;
        }

        public static NTS.Geometries.Geometry[] CreateCircle(Vector2d[] origins, double[] radius)
        {
            var count = origins.Length;
            var circleList = new NTS.Geometries.Geometry[count];
            for (int i = 0; i < count; i++)
            {
                NTS.Geometries.Point temp = new NTS.Geometries.Point(origins[i].x, origins[i].y);
                var circle = temp.Buffer(radius[i], NetTopologySuite.Operation.Buffer.EndCapStyle.Round);
                circleList[i] = circle;
            }
            return circleList;
        }

        public static Polygon[] CreatePolygon(Coordinate[][] polygonVertices)
        {
            var count = polygonVertices.Length;
            var polygonList = new Polygon[count];
            for (int i = 0; i < count; i++)
            {
                Polygon polygon = new Polygon(new LinearRing(polygonVertices[i]));
                polygonList[i] = polygon;
            }
            return polygonList;

        }

        //public static Coordinate[][] ReadJsonData2D(string jsonFilePath)
        //{
        //    GeoJsonReader geoReader = new GeoJsonReader();

        //    StreamReader sr = File.OpenText(jsonFilePath);
        //    var feactureCollection = geoReader.Read<FeatureCollection>(sr.ReadToEnd());
        //    Coordinate[][] polygonResult = new Coordinate[feactureCollection.Count][];
        //    for (int i = 0; i < feactureCollection.Count; i++)
        //    {
        //        //读取数据
        //        var jsonDic = feactureCollection[i].Geometry;
        //        polygonResult[i] = jsonDic.Coordinates;
        //    }
        //    return polygonResult;
        //}

        //public static T[] ReadJsonData2D<T>(string jsonFilePath, JSONGeo geotype)
        //{
        //    GeoJsonReader geoReader = new GeoJsonReader();

        //    StreamReader sr = File.OpenText(jsonFilePath);
        //    var feactureCollection = geoReader.Read<FeatureCollection>(sr.ReadToEnd());
        //    T[] result = new T[feactureCollection.Count];

        //    switch (geotype)
        //    {
        //        case JSONGeo.Point:
        //            for (int i = 0; i < feactureCollection.Count; i++)
        //            {
        //                var jsonDic = feactureCollection[i].Geometry;
        //                NTS.Geometries.Point pt = new NTS.Geometries.Point(jsonDic.Coordinates[0]);
        //                result[i] = (T)(object)pt;
        //            }
        //            break;
        //        case JSONGeo.LineString:
        //            for (int i = 0; i < feactureCollection.Count; i++)
        //            {
        //                var jsonDic = feactureCollection[i].Geometry;
        //                NTS.Geometries.LineString line = new NTS.Geometries.LineString(jsonDic.Coordinates);
        //                result[i] = (T)(object)line;
        //            }
        //            break;
        //        case JSONGeo.Polygon:
        //            break;
        //        default:
        //            break;
        //    }

        //    return result;
        //}

        public static FeatureCollection BuildFeatureCollection(NTS.Geometries.Geometry[] geosInfo)
        {
            var fc = new FeatureCollection();
            for (int i = 0; i < geosInfo.Length; i++)
            {
                AttributesTable att = new AttributesTable
                {
                };
                Feature f = new Feature(geosInfo[i], att);
                fc.Add(f);
            }
            return fc;
        }

        public static FeatureCollection BuildFeatureCollection(NTS.Geometries.Geometry[] geosInfo, double[] data)
        {
            var fc = new FeatureCollection();
            for (int i = 0; i < geosInfo.Length; i++)
            {
                AttributesTable att = new AttributesTable
                {
                    { "data", data[i]}
                };
                Feature f = new Feature(geosInfo[i], att);
                fc.Add(f);
            }
            return fc;
        }

        /// <summary>
        /// 计算被包含的点
        /// </summary>
        /// <param name="mainPtList"></param>
        /// <param name="secPtList"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static List<List<NTS.Geometries.Point>> ContainsInPts(NTS.Geometries.Point[] mainPtList, NTS.Geometries.Point[] secPtList, double[] radius)
        {
            NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point> quadTree = new NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point>();
            for (int i = 0; i < secPtList.Length; i++)
                quadTree.Insert(secPtList[i].EnvelopeInternal, secPtList[i]);

            List<List<NTS.Geometries.Point>> secPtListCollection = new List<List<NTS.Geometries.Point>>(mainPtList.Length);
            for (int i = 0; i < mainPtList.Length; i++)
            {
                var mainCoor = new Coordinate(mainPtList[i].X, mainPtList[i].Y);
                var tempEnv = CreateEnvelopeFromPt(mainPtList[i], radius[i]);
                var secPtListQuery = quadTree.Query(tempEnv);

                List<NTS.Geometries.Point> secPtContain = new List<NTS.Geometries.Point>();
                for (int j = 0; j < secPtListQuery.Count; j++)
                {
                    var secPt = secPtListQuery[j];

                    Coordinate secCoor = new Coordinate(secPt.X, secPt.Y);
                    double dis = mainCoor.Distance(secCoor);
                    if (dis < radius[i])
                        secPtContain.Add(secPt);
                }
                secPtListCollection.Add(secPtContain);
            }
            return secPtListCollection;
        }

        public static List<List<NTS.Geometries.Point>> ContainsInPts(NTS.Geometries.Point[] mainPtList, NTS.Geometries.Point[] secPtList, double radius = 300)
        {
            NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point> quadTree = new NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point>();
            for (int i = 0; i < secPtList.Length; i++)
                quadTree.Insert(secPtList[i].EnvelopeInternal, secPtList[i]);

            List<List<NTS.Geometries.Point>> secPtListCollection = new List<List<NTS.Geometries.Point>>(mainPtList.Length);
            for (int i = 0; i < mainPtList.Length; i++)
            {
                var mainCoor = new Coordinate(mainPtList[i].X, mainPtList[i].Y);
                var tempEnv = CreateEnvelopeFromPt(mainPtList[i], radius);
                var secPtListQuery = quadTree.Query(tempEnv);

                List<NTS.Geometries.Point> secPtContain = new List<NTS.Geometries.Point>();
                for (int j = 0; j < secPtListQuery.Count; j++)
                {
                    var secPt = secPtListQuery[j];

                    Coordinate secCoor = new Coordinate(secPt.X, secPt.Y);
                    double dis = mainCoor.Distance(secCoor);
                    if (dis < radius)
                        secPtContain.Add(secPt);
                }
                secPtListCollection.Add(secPtContain);
            }
            return secPtListCollection;
        }

        public static List<List<NTS.Geometries.Point>> ContainsInPtsParallel(NTS.Geometries.Point[] mainPtList, NTS.Geometries.Point[] secPtList, double radius = 300)
        {
            NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point> quadTree = new NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point>();
            for (int i = 0; i < secPtList.Length; i++)
                quadTree.Insert(secPtList[i].EnvelopeInternal, secPtList[i]);

            List<List<NTS.Geometries.Point>> secPtListCollection = new List<List<NTS.Geometries.Point>>(mainPtList.Length);
            for (int i = 0; i < mainPtList.Length; i++)
            {
                var mainCoor = new Coordinate(mainPtList[i].X, mainPtList[i].Y);
                var tempEnv = CreateEnvelopeFromPt(mainPtList[i], radius);
                var secPtListQuery = quadTree.Query(tempEnv);

                List<NTS.Geometries.Point> secPtContain = new List<NTS.Geometries.Point>();
                for (int j = 0; j < secPtListQuery.Count; j++)
                {
                    var secPt = secPtListQuery[j];

                    Coordinate secCoor = new Coordinate(secPt.X, secPt.Y);
                    double dis = mainCoor.Distance(secCoor);
                    if (dis < radius)
                        secPtContain.Add(secPt);
                }
                secPtListCollection.Add(secPtContain);
            }
            return secPtListCollection;
        }

        public static Dictionary<int, double> ContainsAreaInPts(NTS.Geometries.Point[] mainPtList, Dictionary<NTS.Geometries.Point, double> ptAreaDic, double radius = 300)
        {
            NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point> quadTree = new NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point>();
            var secPtList = ptAreaDic.Keys.ToArray();
            for (int i = 0; i < secPtList.Length; i++)
                quadTree.Insert(secPtList[i].EnvelopeInternal, secPtList[i]);

            List<List<NTS.Geometries.Point>> secPtListCollection = new List<List<NTS.Geometries.Point>>(mainPtList.Length);
            for (int i = 0; i < mainPtList.Length; i++)
            {
                var mainCoor = new Coordinate(mainPtList[i].X, mainPtList[i].Y);
                var tempEnv = CreateEnvelopeFromPt(mainPtList[i], radius);
                var secPtListQuery = quadTree.Query(tempEnv);

                List<NTS.Geometries.Point> secPtContain = new List<NTS.Geometries.Point>();
                for (int j = 0; j < secPtListQuery.Count; j++)
                {
                    var secPt = secPtListQuery[j];

                    Coordinate secCoor = new Coordinate(secPt.X, secPt.Y);
                    double dis = mainCoor.Distance(secCoor);
                    if (dis < radius)
                        secPtContain.Add(secPt);
                }
                secPtListCollection.Add(secPtContain);
            }

            var wholeArea = ContainsAreaInPts(secPtListCollection, ptAreaDic);
            Dictionary<int, double> ptWholeAreaDic = new Dictionary<int, double>();
            for (int i = 0; i < mainPtList.Length; i++)
            {
                ptWholeAreaDic.Add(i, wholeArea[i]);
            }

            return ptWholeAreaDic;
        }

        //TODO 划分点，输出mainPtList
        //public static NTS.Geometries.Point[] DividePolyline()
        //{
        //    var dividedPtList=
        //}

        /// <summary>
        /// 计算点内所包含的所有面积
        /// </summary>
        /// <param name="containsInPtsData"></param>
        /// <param name="areaDic"> point is 2d point</param>
        /// <returns></returns>
        public static double[] ContainsAreaInPts(List<List<NTS.Geometries.Point>> containsInPtsData, Dictionary<NTS.Geometries.Point, double> areaDic)
        {
            double[] areaResult = new double[containsInPtsData.Count];
            for (int i = 0; i < containsInPtsData.Count; i++)
            {
                var ptListInMainPt = containsInPtsData[i];
                List<double> areaList = new List<double>(ptListInMainPt.Count);
                for (int j = 0; j < ptListInMainPt.Count; j++)
                {
                    var single = areaDic[ptListInMainPt[j]];
                    areaList.Add(single);
                }
                areaResult[i] = areaList.Sum();
            }
            return areaResult;
        }



        public static Envelope CreateEnvelopeFromPt(NTS.Geometries.Point origin, double radius)
        {
            var ptLeftDown = new NTS.Geometries.Coordinate(origin.X - radius, origin.Y - radius);
            var ptRightUp = new NTS.Geometries.Coordinate(origin.X + radius, origin.Y + radius);
            return new Envelope(ptLeftDown, ptRightUp);
        }

        public static List<NTS.Geometries.Point> DivideLineString(NTS.Geometries.LineString line, double dis, out int ptCount)
        {
            NTS.LinearReferencing.LengthIndexedLine index = new NTS.LinearReferencing.LengthIndexedLine(line);
            double temp = index.EndIndex;
            var _ptCount = Convert.ToInt32(Math.Ceiling(temp / dis)) - 1;
            var ptList = new List<NTS.Geometries.Point>(_ptCount);
            //var ptList = new List<NTS.Geometries.Point>();
            if (line.Length < dis)
            {
                var pt = index.ExtractPoint(line.Length / 2);
                ptList.Add(new NTS.Geometries.Point(pt.X, pt.Y, 0));
            }
            else
            {
                for (double j = dis; j < temp; j += dis)
                {
                    var pt = index.ExtractPoint(j);
                    ptList.Add(new NTS.Geometries.Point(pt.X, pt.Y, 0));
                }
            }


            ptCount = ptList.Count;
            return ptList;
        }
    }
}
