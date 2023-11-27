using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaCity.Traffic
{
    public class StatsAnalysis
    {
        public static (List<string>, List<List<string>>) LinkStatsAnalysis(string filePath)
        {
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
            //Console.WriteLine(string.Join("\t", requiredColumns));
            //foreach (var row in extractedData)
            //{
            //    Console.WriteLine(string.Join("\t", row));
            //}

            return (requiredColumns, extractedData);
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
    }
}
