using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;

namespace UrbanX.Planning.Sustainability
{
    public class IndexCalculation
    {
        //private const int roundNum = 1;
        public Dictionary<string, InfoFromXML_BB> Con_Buildings { get; }
        public Dictionary<string, InfoFromXML_BB> Con_Blocks { get; }
        public Dictionary<int, InfoFromXML_Population> PopulationType { get; }

        public IndexCalculation() { }

        public IndexCalculation(IndexCalculation data) {
            Con_Buildings = data.Con_Buildings;
            Con_Blocks = data.Con_Blocks;
            PopulationType = data.PopulationType;
        }

        public IndexCalculation(string xmlPath)
        {
            Con_Buildings = ReadXML_BB(xmlPath, "Buildings");
            Con_Blocks = ReadXML_BB(xmlPath, "Blocks");
            PopulationType = ReadXML_PP(xmlPath);
        }

        #region 功能区

        #region 人口计算
        public int SpecifyPopulationType(int layer)
        {
            int type = -1;
            var ppList = PopulationType.Values.ToList();
            List<double[]> intervalLayer = new List<double[]>();
            for (int i = 0; i < ppList.Count; i++)
                intervalLayer.Add(ppList[i].Layer);
            for (int i = 0; i < ppList.Count; i++)
            {
                if (layer >= intervalLayer[i][0] && layer <= intervalLayer[i][1]) { type = i; break; }
                else if (i == 4 && layer > intervalLayer[i][1])
                    type = 4;
                else
                    continue;
            }
            return type;
        }

        public double CalculatePopulation(int layer, Curve crv)
        {
            var type = SpecifyPopulationType(layer);
            var peopleUnitValue = PopulationType[type].People;
            var area = Area_Building(crv, layer);
            var peopleNum = area / peopleUnitValue;
            return peopleNum;
        }

        public double[] CalculatePopulation(int[] layer, double[] area)
        {
            var len = layer.Count();
            double[] result = new double[layer.Count()];
            for (int i = 0; i < len; i++)
            {
                var type = SpecifyPopulationType(layer[i]);
                var peopleUnitValue = PopulationType[type].People;
                result[i] = area[i] / peopleUnitValue;
            }
            return result;
        }

        public double CalculatePopulation(int layer, double area)
        {
            var type = SpecifyPopulationType(layer);
            var peopleUnitValue = PopulationType[type].People;
            var result = area / peopleUnitValue;
            return result;
        }
        #endregion

        #region 读取XML

        /// <summary>
        /// 读取XML文件
        /// </summary>
        /// <param name="path">输入 xml 路径</param>
        /// <param name="level">输入 Buildings 或者 Blocks</param>
        /// <returns></returns>
        public Dictionary<string, InfoFromXML_BB> ReadXML_BB(string path, string level)
        {
            Dictionary<string, InfoFromXML_BB> DicXML = InfoFromXML_BB.CreateDicFromXML(path, level);
            return DicXML;
        }
        public Dictionary<int, InfoFromXML_Population> ReadXML_PP(string path)
        {
            Dictionary<int, InfoFromXML_Population> DicXML = InfoFromXML_Population.CreateDicFromXML(path);
            return DicXML;
        }
        #endregion

        #region 道路向指标计算
        public double[] SpaceSyntax_Road(IEnumerable<Curve> roadList, IEnumerable<double> valueList)
        {
            double[] ssValue = valueList.ToArray();
            return ssValue;
        }

        #endregion

        #region 地块向指标计算
        public double AreaTotal_Block(Curve crv)
        {
            double areaBD = -1d;
            if (crv.IsClosed)
                areaBD = AreaMassProperties.Compute(crv).Area;
            return areaBD;
        }

        public double FAR_Block(double FAR)
        {
            double FARBLK = FAR;
            return FARBLK;
        }

        public double Density_Block(double density)
        {
            double DensityBLK = density;
            return DensityBLK;
        }

        public double[] EnergyConsumption_Block(string function, Curve crv)
        {
            double[] EConsumption = new double[2];
            double area = AreaTotal_Block(crv);
            //单位：吨/平方米
            double[] EConNum = Con_Buildings[function].EConsumption;

            EConsumption[0] = area * EConNum[0];
            EConsumption[1] = area * EConNum[1];

            return EConsumption;
        }

        public double[] WaterConsumption_Block(string function, Curve crv)
        {
            double[] WConsumption = new double[2];
            double area = AreaTotal_Block(crv);
            //单位：吨/平方米
            double[] WConNum = Con_Buildings[function].WConsumption;

            WConsumption[0] = area * WConNum[0];
            WConsumption[1] = area * WConNum[1];

            return WConsumption;
        }

        public int[] Consumption_Block(string function, IEnumerable<int[]> Consumption_Building)
        {
            int[] Consumption = new int[3];
            var length = Consumption_Building.Count();
            int[] minGC = new int[length];
            int[] maxGC = new int[length];
            for (int i = 0; i < length; i++)
            {
                minGC[i] = Convert.ToInt32(Consumption_Building.ElementAt(i)[0]);
                maxGC[i] = Convert.ToInt32(Consumption_Building.ElementAt(i)[1]);
            }
            Consumption[0] = Sum(minGC);
            Consumption[1] = Sum(maxGC);
            Consumption[2] = (Consumption[0] + Consumption[1]) / 2;

            return Consumption;
        }
        #endregion

        #region 建筑向指标计算
        public double Area_Building(Curve crv, int layer)
        {
            double areaBD = -1d;
            if (crv.IsClosed)
                areaBD = AreaMassProperties.Compute(crv).Area * layer;
            return areaBD;
        }
        public int Layers_Building(int layer)
        {
            int layerBD = layer;
            return layerBD;
        }
        public string Function_Building(string function)
        {
            string functionBD = function;
            return functionBD;
        }
        public double[] EnergyConsumption_Building(string function, Curve crv, int layer)
        {
            double[] EConsumption = new double[2];
            double area = Area_Building(crv, layer);
            //单位：千瓦/平方米
            double[] EConNum = Con_Buildings[function].EConsumption;

            EConsumption[0] = area * EConNum[0];
            EConsumption[1] = area * EConNum[1];

            return EConsumption;
        }

        public int[] EnergyConsumption_Building(string function, double area)
        {
            int[] EConsumption = new int[3];
            //单位：千瓦/平方米
            double[] EConNum = Con_Buildings[function].EConsumption;

            EConsumption[0] = Convert.ToInt32(area * EConNum[0]);
            EConsumption[1] = Convert.ToInt32(area * EConNum[1]);
            EConsumption[2] = (EConsumption[0] + EConsumption[1]) / 2;

            return EConsumption;
        }

        public double[] WaterConsumption_Building(string function, Curve crv, int layer)
        {
            double[] WConsumption = new double[2];
            double area = Area_Building(crv, layer);
            //单位：吨/平方米
            double[] WConNum = Con_Buildings[function].WConsumption;

            WConsumption[0] = area * WConNum[0];
            WConsumption[1] = area * WConNum[1];


            return WConsumption;
        }
        public int[] WaterConsumption_Building(string function, double area)
        {
            int[] WConsumption = new int[3];
            //单位：吨/平方米
            double[] WConNum = Con_Buildings[function].WConsumption;

            WConsumption[0] = Convert.ToInt32(area * WConNum[0]);
            WConsumption[1] = Convert.ToInt32(area * WConNum[1]);
            WConsumption[2] = (WConsumption[0] + WConsumption[1]) / 2;

            return WConsumption;
        }
        public double[] GarbageConsumption_Building(string function, Curve crv, int layer)
        {
            double[] GConsumption = new double[2];
            var populationCount = CalculatePopulation(layer, crv);
            //单位：千克/人
            var GConNum = Con_Buildings[function].GConsumption;

            GConsumption[0] = populationCount * GConNum[0];
            GConsumption[1] = populationCount * GConNum[1];

            return GConsumption;
        }
        public int[] GarbageConsumption_Building(string function, double area, int layer)
        {
            int[] GConsumption = new int[3];
            var populationCount = CalculatePopulation(layer, area);
            //单位：千克/人
            var GConNum = Con_Buildings[function].GConsumption;

            GConsumption[0] = Convert.ToInt32(populationCount * GConNum[0]);
            GConsumption[1] = Convert.ToInt32(populationCount * GConNum[1]);
            GConsumption[2] = (GConsumption[0] + GConsumption[1]) / 2;

            return GConsumption;
        }
        public double[] GarbageConsumption_BuildingSum(int[] layer, string[] function, double[] area)
        {
            //double[] GConsumption = new double[2];
            var len = layer.Count();
            double[] GConsumption = new double[2];
            List<double> minList = new List<double>(len);
            List<double> maxList = new List<double>(len);
            for (int i = 0; i < len; i++)
            {
                double populationCount = CalculatePopulation(layer[i], area[i]);
                //单位：千克/人
                var GConNum = Con_Buildings[function[i]].GConsumption;

                minList.Add(populationCount * GConNum[0]);
                maxList.Add(populationCount * GConNum[1]);
            }
            var minResult = Sum(minList);
            var maxResult = Sum(maxList);

            GConsumption[0] = minResult;
            GConsumption[1] = maxResult;

            return GConsumption;
        }
        public int[] Consumption_BuildingSum(List<int[]> IList)
        {
            int[] GConsumption = new int[2];
            var len = IList.Count();
            var minList = new List<int>(len);
            var maxList = new List<int>(len);
            for (int i = 0; i < len; i++)
            {
                int[] valueCount = IList[i];
                minList.Add(valueCount[0]);
                maxList.Add(valueCount[1]);
            }
            var minResult = Sum(minList);
            var maxResult = Sum(maxList);

            GConsumption[0] = minResult;
            GConsumption[1] = maxResult;

            return GConsumption;
        }
        public double GetAverageValue(double[] arrayValue)
        {
            var average = (arrayValue[0] + arrayValue[1]) / 2;
            return average;
        }

        #endregion

        #region 基础运算
        private double Sum(IEnumerable<double> num)
        {
            double sum = 0d;
            foreach (var item in num) { sum += item; }
            return sum;
        }

        private int Sum(IEnumerable<int> num)
        {
            int sum = 0;
            foreach (var item in num) { sum += item; }
            return sum;
        }
        #endregion
        #endregion
    }
}
