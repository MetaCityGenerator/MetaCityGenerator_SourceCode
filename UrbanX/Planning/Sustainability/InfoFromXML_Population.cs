
using System.Collections.Generic;

using System.Xml;
using MetaCity.Planning.Utility;

namespace MetaCity.Planning.Sustainability
{
    public class InfoFromXML_Population
    {
        public double[] Layer { get; }
        public int People { get; }
        public double[] FAR { get; }
        public double MaxDensity { get; }
        public double MinGreen { get; }
        public double MaxHeight { get; }

        public InfoFromXML_Population(int populationType, string[] layer, int people, string[] fAR, double maxDensity, double minGreen, double maxHeight)
        {
            Layer = StrArray2DouArray(layer);
            People = people;
            FAR = StrArray2DouArray(fAR);
            MaxDensity = maxDensity;
            MinGreen = minGreen;
            MaxHeight = maxHeight;
        }

        #region 功能区
        public static Dictionary<int, InfoFromXML_Population> CreateDicFromXML(string xmlFileName, string level = "PopulationIndex")
        {
            Dictionary<int, InfoFromXML_Population> finalDic = new Dictionary<int, InfoFromXML_Population>();

            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(xmlFileName);
            }
            catch (System.Exception)
            {

                throw new CustomException(xmlFileName);
            }
            

            var nodeList = xmlDoc.SelectNodes($"//IndexCalculation/{level}/Population");

            for (int i = 0; i < nodeList.Count; i++)
            {
                int populationType = int.Parse(nodeList[i].Attributes["Type"].Value);
                string[] Layer = nodeList[i]["Layer"].InnerText.Split(',');
                int People = int.Parse(nodeList[i]["People"].InnerText);
                string[] FAR = nodeList[i]["FAR"].InnerText.Split(',');
                double MaxDensity = double.Parse(nodeList[i]["MaxDensity"].InnerText);
                double MinGreen = double.Parse(nodeList[i]["MinGreen"].InnerText);
                double MaxHeight = double.Parse(nodeList[i]["MaxHeight"].InnerText);

                InfoFromXML_Population populationInfo = new InfoFromXML_Population(populationType, Layer, People, FAR, MaxDensity, MinGreen, MaxHeight);
                finalDic.Add(populationType, populationInfo);
            }
            return finalDic;
        }

        public double[] StrArray2DouArray(string[] strArray)
        {
            double[] douArray = new double[strArray.Length];
            for (int i = 0; i < strArray.Length; i++)
            {
                douArray[i] = double.Parse(strArray[i]);
            }
            return douArray;
        }

        #endregion
    }
}
