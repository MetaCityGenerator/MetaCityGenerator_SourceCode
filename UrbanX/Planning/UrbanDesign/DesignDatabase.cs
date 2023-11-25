using System;
using System.IO;
using System.Reflection;
using System.Xml;


namespace UrbanX.Planning.UrbanDesign
{
    public enum SiteTypes
    {
        R,
        C,
        GIC,
        M,
        W,
        U
    }

    public enum MixTypes
    {
        /// <summary>
        /// Mixed buildings occupy another subsite.
        /// </summary>
        Horizontal,

        /// <summary>
        /// Mixed function share the same building with different layers.
        /// </summary>
        Vertical,

        /// <summary>
        /// Site only has main type buildings.
        /// </summary>
        None
    }

    public enum NonResidentialStyles
    {
        /// <summary>
        /// Single dot building, basic style with most accurate Far and Density combination.
        /// </summary>
        Alone,

        /// <summary>
        /// Group builidngs around the boundary of subsite, with accurate Far and approximate Density.
        /// </summary>
        Group,

        /// <summary>
        /// Combining Alone and Group style, withe accurate Far and almost constant Density.
        /// </summary>
        Mixed
    }

    public enum ResidentialStyles
    {
        /// <summary>
        /// Parelle partions with certain radiance.
        /// </summary>
        RowRadiance,

        /// <summary>
        /// Dot residential style with differant building height.
        /// </summary>
        DotVariousHeight,

        /// <summary>
        /// Horizontal parellel partitions with ratating single building.
        /// </summary>
        DotRowMajor,

        /// <summary>
        /// Vertical parallel partitions with ratating single building.
        /// </summary>
        DotColumnMajor
    }


    [Obsolete]
    public enum BuildingShapeStyles
    {
        Flat,
        Square
    }



    /// <summary>
    /// A static container for storing the information about each type of site can contains which building types.
    /// </summary>
    public class SiteDataset
    {
        //private static readonly string _xml = @"D:\1_OneDriveBusiness\OneDrive - UrbanX\0_Coding\1_Working\ProjectX\UrbanX\Data\UrbanDesign.xml";

        private static readonly string _defaultPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string _xml = Path.Combine(_defaultPath, "data", "UrbanDesign.xml");


        /// <summary>
        /// Get main buildingtypes by reading xml.
        /// </summary>
        /// <param name="siteType"></param>
        /// <returns></returns>
        public static string[] GetMainBuildingTypes(SiteTypes siteType)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_xml);
            var path = "MainBuildingTypes";

            XmlNode node = xmlDoc.SelectSingleNode($"//UrbanDesign/{path}/{siteType}");
            var types = node.InnerText.Split(',');
            for (int i = 0; i < types.Length; i++)
            {
                types[i] = types[i].Trim();
            }

            return types;
        }

        /// <summary>
        /// Get mixed buildingtypes by reding xml.
        /// </summary>
        /// <param name="siteType"></param>
        /// <returns></returns>
        public static string[] GetMixedBuildingTypes(SiteTypes siteType)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_xml);
            var path = "MixedBuildingTypes";

            XmlNode node = xmlDoc.SelectSingleNode($"//UrbanDesign/{path}/{siteType}");
            var types = node.InnerText.Split(',');
            for (int i = 0; i < types.Length; i++)
            {
                types[i] = types[i].Trim();
            }

            return types;
        }

        // Corrention coefficients
        public static double GetMixedCorCoefficients(SiteTypes siteType)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_xml);
            var path = "MixedCorCoefficients";

            XmlNode node = xmlDoc.SelectSingleNode($"//UrbanDesign/{path}/{siteType}");

            return Convert.ToDouble(node.InnerText);
        }

        // Density interval by site types.
        public static double[] GetDensityInterval(SiteTypes siteType)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_xml);
            var path = "DensityInterval";

            XmlNode node = xmlDoc.SelectSingleNode($"//UrbanDesign/{path}/{siteType}");

            var numbers = node.InnerText.Split(',');
            return new double[] { Convert.ToDouble(numbers[0]), Convert.ToDouble(numbers[1]) };
        }


        // Far interval by site types.
        public static double[] GetFarInterval(SiteTypes siteType)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_xml);
            var path = "FarInterval";

            XmlNode node = xmlDoc.SelectSingleNode($"//UrbanDesign/{path}/{siteType}");

            var numbers = node.InnerText.Split(',');
            return new double[] { Convert.ToDouble(numbers[0]), Convert.ToDouble(numbers[1]) };
        }

        // Maximum area for each type of site.
        public static double GetMaxAreaByType(SiteTypes siteType)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_xml);
            XmlNode node = xmlDoc.SelectSingleNode($"//UrbanDesign/MaxAreaByType/{siteType}");

            return Convert.ToDouble(node.InnerText);
        }
    }

    /// <summary>
    /// A static container for storing all the parameters for all the typical building types.
    /// </summary>    
    public class BuildingDataset
    {
        //private static readonly string _xml = @"D:\1_OneDriveBusiness\OneDrive - UrbanX\0_Coding\1_Working\ProjectX\UrbanX\Data\UrbanDesign.xml";

        private static readonly string _defaultPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string _xml = Path.Combine(_defaultPath, "data", "UrbanDesign.xml");



        public static BuildingParameters GetBuildingParameters(string buildingType)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_xml);
            var path = "BuildingParameters";

            // Using xpath to query node
            XmlNode node = xmlDoc.SelectSingleNode($"//UrbanDesign/{path}/{buildingType}");

            // Using item to query node
            var floorNode = node["FloorRange"].InnerText.Split(',');
            var depthNode = node["DepthRange"].InnerText.Split(',');

            // Using item to query attributes' value.
            string type = node.Attributes["type"].Value;
            int priority = Convert.ToInt32(node.Attributes["priority"].Value);

            double area = Convert.ToDouble(node["Area"].InnerText);
            int[] floorRange = new int[] { Convert.ToInt32(floorNode[0]), Convert.ToInt32(floorNode[1]) };
            double[] depthRange = new double[] { Convert.ToDouble(depthNode[0]), Convert.ToDouble(depthNode[1]) };
            double floorHeight = Convert.ToDouble(node["FloorHeight"].InnerText);

            return new BuildingParameters(buildingType, area, floorRange, depthRange, priority, floorHeight, type);
        }


        public static double GetSetbackRtype(int floorCount)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_xml);
            var path = "Setback";

            var nodes = xmlDoc.SelectNodes($"//UrbanDesign/{path}/SetbackRtype/Type");
            for (int i = 0; i < nodes.Count; i++)
            {
                var lo = Convert.ToInt32(nodes[i].Attributes["lo"].Value);
                var hi = Convert.ToInt32(nodes[i].Attributes["hi"].Value);

                if (floorCount > lo && floorCount <= hi)
                {
                    return Convert.ToDouble(nodes[i].InnerText);
                }
            }

            return Convert.ToDouble(nodes[1].InnerText);
        }

        public static double GetSetbackOhterType(double height)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_xml);
            var path = "Setback";

            var nodes = xmlDoc.SelectNodes($"//UrbanDesign/{path}/SetbackOhterType/Type");
            for (int i = 0; i < nodes.Count; i++)
            {
                var lo = Convert.ToInt32(nodes[i].Attributes["lo"].Value);
                var hi = Convert.ToInt32(nodes[i].Attributes["hi"].Value);

                if (height > lo && height <= hi)
                {
                    return Convert.ToDouble(nodes[i].InnerText);
                }
            }

            return Convert.ToDouble(nodes[1].InnerText);
        }


        public static double[] GetGroupStyleParameters(out double minVoidDepth, out double floorHeight)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_xml);
            var path = "BuildingParameters";

            XmlNode node = xmlDoc.SelectSingleNode($"//UrbanDesign/{path}/GroupStyle");
            var depthNode = node["DepthRange"].InnerText.Split(',');

            minVoidDepth = Convert.ToDouble(node["MinVoidDepth"].InnerText);
            floorHeight = Convert.ToDouble(node["FloorHeight"].InnerText);

            double[] depthRange = new double[] { Convert.ToDouble(depthNode[0]), Convert.ToDouble(depthNode[1]) };

            return depthRange;
        }


        public static double GetSunlightDistance(double height, int cityIndex)
        {
            //层数为1 层至3 层的为低层住宅，4 层至6 层的为多层住宅，7 层至9 层的
            //为中高层住宅，10 层及以上但建筑高度不超过100m 的为高层住宅，建筑高
            //度超过100m 的为超高层住宅。

            // TODO: calculating the sunlight angle.

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_xml);
            var path = "CityCoordinates";


            var coordinates = xmlDoc.SelectSingleNode($"//UrbanDesign/{path}").ChildNodes[cityIndex].InnerText.Split(',');

            var latitude = Convert.ToDouble(coordinates[1]);

            SunCalculator sun = new SunCalculator(latitude, height);


            return sun.SunDistance;

        }
    }
}
