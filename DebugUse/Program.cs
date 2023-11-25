using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace DebugUse
{
    public class OutputForm : Form
    {
        public TextBox OutputBox { get; private set; } = new TextBox();

        public OutputForm()
        {
            OutputBox.Multiline = true;
            OutputBox.Dock = DockStyle.Fill;
            Controls.Add(OutputBox);
        }
    }

    public class Program
    {
        [STAThread]
        public static void Main()
        {
            #region Textbox output
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);

            //OutputForm form = new OutputForm();

            //// 启动.jar文件并重定向输出
            //string jarPath = @"C:\Users\AlphonsePC\AppData\Roaming\Grasshopper\Libraries\UrbanXTools\matsim_preparation_jar\generate_demand\matsim_preparation.jar"; // 替换为你的.jar文件的路径
            //string inputPath = @"D:\Code\114_temp\008_CodeCollection\005_java\matsim_data_backup\debug\tq38_london_original\001_input\"; // 替换为你的输入文件的路径
            //string outputPath = @"D:\Code\114_temp\008_CodeCollection\005_java\matsim_data_backup\debug\tq38_london_strategy_urbanxtools\"; // 替换为你的输出文件的路径
            //string indexPath = @"C:\Users\AlphonsePC\AppData\Roaming\Grasshopper\Libraries\UrbanXTools\data\indexCalculation.xml"; // 替换为你的索引文件的路径

            //Process process = new Process();
            //process.StartInfo.FileName = "java";
            //process.StartInfo.Arguments = $"-jar {jarPath} {inputPath} {outputPath} {indexPath}"; ;
            //process.StartInfo.RedirectStandardOutput = true;
            //process.StartInfo.UseShellExecute = false;
            //process.StartInfo.CreateNoWindow = true;
            //process.StartInfo.RedirectStandardError = true;


            //process.Start();
            //process.BeginOutputReadLine();
            //// 在process.Start()之后添加以下行来开始读取错误输出
            //process.BeginErrorReadLine();

            //process.OutputDataReceived += (s, eventData) =>
            //{
            //    if (!string.IsNullOrEmpty(eventData.Data))
            //    {
            //        // 使用Invoke确保线程安全
            //        form.Invoke(new Action(() => form.OutputBox.AppendText(eventData.Data + Environment.NewLine)));
            //    }
            //};


            //Application.Run(form);
            #endregion

            #region Convert XML to GeoJSON
            //string xmlFilePath = @"C:\Users\AlphonsePC\Downloads\temp\network.xml";
            //string geoJsonOutputPath = @"C:\Users\AlphonsePC\Downloads\temp\network.geojson";
            //var geoJson = ConvertXmlToGeoJson(xmlFilePath);
            //File.WriteAllText(geoJsonOutputPath, geoJson);
            //Console.WriteLine(geoJson);
            #endregion

            string filePath = @"C:\Users\AlphonsePC\Downloads\temp\output_traffic\ITERS\it.5\5.linkstats.txt.gz";
            string data = ReadGZipFile(filePath);

            // Split the data into rows
            var rows = data.Split('\n').Select(r => r.Trim()).Where(r => !string.IsNullOrWhiteSpace(r)).ToList();

            // Extract headers
            var headers = rows[0].Split('\t');

            // Extracting the required columns based on headers
            var requiredColumns = new List<string> { "LINK", "FROM", "TO", "FREESPEED", "CAPACITY" };
            requiredColumns.AddRange(headers.Where(h => h.Contains("HRS") && h.Contains("avg")));
            requiredColumns.AddRange(headers.Where(h => h.Contains("TRAVELTIME") && h.Contains("avg")));

            var columnIndex = requiredColumns.Select(rc => Array.IndexOf(headers, rc)).ToList();

            // Extracting the required data
            var extractedData = new List<List<string>>();
            foreach (var row in rows.Skip(1))  // Skipping header row
            {
                var rowData = row.Split('\t');
                var extractedRowData = columnIndex.Select(ci => rowData[ci]).ToList();
                extractedData.Add(extractedRowData);
            }

            // Displaying the extracted data
            Console.WriteLine(string.Join("\t", requiredColumns));
            for (int i = 0; i < 2; i++)
            {
                Console.WriteLine(string.Join("\t", extractedData[i]));
            }
            //foreach (var row in extractedData)
            //{
            //    Console.WriteLine(string.Join("\t", row));
            //}

            Console.ReadLine();
        }

        public static string ReadGZipFile(string filePath)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
            using (StreamReader reader = new StreamReader(gzipStream, Encoding.UTF8)) // assuming the text is in UTF-8
            {
                return reader.ReadToEnd();
            }
        }

        public static string ConvertXmlToGeoJson(string xmlFilePath)
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
                        modes = link.Attribute("modes").Value.Split(',').ToList()
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
