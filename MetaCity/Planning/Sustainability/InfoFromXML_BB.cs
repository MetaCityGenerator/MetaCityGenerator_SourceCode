using System.Collections.Generic;
using System.Xml;
using MetaCity.Planning.Utility;

namespace MetaCity.Planning.Sustainability
{
    public class InfoFromXML_BB
    {
        public string _type;

        public double[] EConsumption { get; }
        public double[] WConsumption { get; }
        public double[] GConsumption { get; }

        public InfoFromXML_BB(string type, string[] EConsumption, string[] WConsumption, string[] GConsumption)
        {
            _type = type;
            this.EConsumption = StrArray2DouArray(EConsumption);
            this.WConsumption = StrArray2DouArray(WConsumption);
            this.GConsumption = StrArray2DouArray(GConsumption);
        }


        #region 功能区
        public static Dictionary<string, InfoFromXML_BB> CreateDicFromXML(string xmlFileName, string level = "Buildings")
        {
            Dictionary<string, InfoFromXML_BB> finalDic = new Dictionary<string, InfoFromXML_BB>();

            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(xmlFileName);
            }
            catch (System.Exception)
            {

                throw new CustomException(xmlFileName);
            }

            var nodeList = xmlDoc.SelectNodes($"//IndexCalculation/{level}/{level.Remove(level.Length - 1, 1)}");
            for (int i = 0; i < nodeList.Count; i++)
            {
                string BdType = nodeList[i].Attributes["Type"].Value;
                string[] BdECon = nodeList[i]["EConsumption"].InnerText.Split(',');
                string[] BdWCon = nodeList[i]["WConsumption"].InnerText.Split(',');
                string[] BdGCon = nodeList[i]["GConsumption"].InnerText.Split(',');

                InfoFromXML_BB BdInfo = new InfoFromXML_BB(BdType, BdECon, BdWCon, BdGCon);
                finalDic.Add(BdType, BdInfo);
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
