using System.Collections.Generic;

using NTS = NetTopologySuite;

namespace UrbanX.Planning.SpatialAnalysis
{
    public class Road
    {
        public int Index { get; }
        public NTS.Geometries.LineString RoadGeo { get; }
        public List<NTS.Geometries.Point> RoadPts { get; }
        public int PtCount { get; set; }
        private double Dis { get; set; }

        public Road(string jsonPath)
        {

        }

        public Road(int Index, NTS.Geometries.LineString RoadGeo, double Dis)
        {
            this.Index = Index;
            this.Dis = Dis;
            this.RoadGeo = RoadGeo;
            RoadPts = DividedRoad();
        }

        public List<NTS.Geometries.Point> DividedRoad()
        {
            var ptList = Poly2DCreation.DivideLineString(RoadGeo, Dis, out int pointCount);
            PtCount = pointCount;
            return ptList;
        }
    }

    public class Roads
    {
        public NTS.Geometries.MultiLineString RoadGeosAsMultiLS;
        public NTS.Geometries.LineString[] RoadGeosAsLS;
        public double[] Score_totalVisArea;
        public double[] Score_transfer_visRatio;
        public double[] Score_transfer_totalVisArea;
        public double[] Score_transfer_normalizedVisArea;
        public double[] Score;

        public Roads(NTS.Geometries.LineString[] roads, double[] score)
        {
            RoadGeosAsMultiLS = new NTS.Geometries.MultiLineString(roads);
            RoadGeosAsLS = roads;
            Score = score;
        }
        public Roads(NTS.Geometries.LineString[] roads, double[] score_vr, double[] score_trans_vr, double[] score_trans_tv, double[] score_trans_nv)
        {
            RoadGeosAsMultiLS = new NTS.Geometries.MultiLineString(roads);
            RoadGeosAsLS = roads;
            Score_totalVisArea = score_vr;
            Score_transfer_visRatio = score_trans_vr;
            Score_transfer_totalVisArea = score_trans_tv;
            Score_transfer_normalizedVisArea = score_trans_nv;
        }

        public Roads(NTS.Geometries.LineString[] roads, double[] score_vr, double[] score_trans_vr, double[] score_trans_nv)
        {
            RoadGeosAsMultiLS = new NTS.Geometries.MultiLineString(roads);
            RoadGeosAsLS = roads;
            Score_totalVisArea = score_vr;
            Score_transfer_visRatio = score_trans_vr;
            Score_transfer_normalizedVisArea = score_trans_nv;
        }

        public Roads()
        {

        }

        //public static NTS.Geometries.LineString[] ReadJson(string roadPath)
        //{
        //    return Poly2DCreation.ReadJsonData2D<NTS.Geometries.LineString>(roadPath,JSONGeo.LineString);
        //}
    }
}
