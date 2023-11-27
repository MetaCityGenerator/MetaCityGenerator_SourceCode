using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MetaCity.IO.XML
{
    public class XMLConverter
    {
        public static void ConvertXMLtoGeojson(string xmlFilePath, string geoJsonOutputPath)
        {
            var geoJson = ConvertXmlToString(xmlFilePath);
            File.WriteAllText(geoJsonOutputPath, geoJson);
        }

        private static string ConvertXmlToString(string xmlFilePath)
        {
            XDocument doc = XDocument.Load(xmlFilePath);

            var nodes = doc.Root.Element("nodes").Elements("node").Select(n => new
            {
                Id = n.Attribute("id").Value,
                X = double.Parse(n.Attribute("x").Value),
                Y = double.Parse(n.Attribute("y").Value)
            }).ToDictionary(n => n.Id, n => new { n.X, n.Y });

            var features = new List<object>();

            //foreach (var node in nodes)
            //{
            //    features.Add(new
            //    {
            //        type = "Feature",
            //        geometry = new
            //        {
            //            type = "Point",
            //            coordinates = new[] { node.Value.X, node.Value.Y }
            //        },
            //        properties = new
            //        {
            //            id = node.Key
            //        }
            //    });
            //}

            foreach (var link in doc.Root.Element("links").Elements("link"))
            {
                var fromNode = nodes[link.Attribute("from").Value];
                var toNode = nodes[link.Attribute("to").Value];

                features.Add(new
                {
                    type = "Feature",
                    geometry = new
                    {
                        type = "LineString",
                        coordinates = new[]
                        {
                        new[] { fromNode.X, fromNode.Y },
                        new[] { toNode.X, toNode.Y }
                    }
                    },
                    properties = new
                    {
                        id = link.Attribute("id").Value,
                        modes = link.Attribute("modes").Value
                    }
                });
            }

            var geoJsonObj = new
            {
                type = "FeatureCollection",
                features = features
            };

            return JsonSerializer.Serialize(geoJsonObj);
        }
    }
}
