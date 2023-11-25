using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.IO;

namespace UrbanX.Planning.Water
{
    public class ReadInpFile
    {
        private readonly List<string> _pipeLines = new List<string>();
        private readonly List<string> _junctionLines = new List<string>();
        private readonly List<string> _coordinateLines = new List<string>();
        private readonly List<string> _reserviorLines = new List<string>();
        private readonly List<string> _verticeLines = new List<string>();

        private readonly string _readPath;

        private readonly Dictionary<string, Point3d> _nodesDiction = new Dictionary<string, Point3d>();
        private readonly Dictionary<string, Polyline> _pipesDiction = new Dictionary<string, Polyline>(); // Should include pumps

        public List<Point3d> Junctions { get; }

        public List<Point3d> Reserviors { get; }

        public List<Polyline> Pipes { get; }


        public ReadInpFile(string path, double tolerance)
        {
            _readPath = path;

            ReadFile(_readPath);
            Junctions = new List<Point3d>(_junctionLines.Count);
            Reserviors = new List<Point3d>(_reserviorLines.Count);
            Pipes = new List<Polyline>(_pipeLines.Count);
            DataToGeometry(tolerance);
        }



        #region Private methods for getting data.
        private void ReadFile(string path)
        {
            string[] textLines = File.ReadAllLines(path);

            GetDataFromInp("PIPES", _pipeLines, textLines);

            GetDataFromInp("JUNCTIONS", _junctionLines, textLines);

            GetDataFromInp("RESERVOIRS", _reserviorLines, textLines);

            GetDataFromInp("COORDINATES", _coordinateLines, textLines);

            GetDataFromInp("VERTICES", _verticeLines, textLines);

        }

        private void DataToGeometry(double tolerance)
        {

            // Nodes
            Dictionary<string, double[]> junctions = new Dictionary<string, double[]>();
            GetNodesCoords(junctions, _junctionLines, _coordinateLines, "JUNCTIONS");

            Dictionary<string, double[]> reservoirs = new Dictionary<string, double[]>();
            GetNodesCoords(reservoirs, _reserviorLines, _coordinateLines, "RESERVOIRS");


            foreach (var item in junctions)
            {
                Point3d node = new Point3d(item.Value[0], item.Value[1], item.Value[2]);
                Junctions.Add(node);
                _nodesDiction.Add(item.Key, node);
            }

            foreach (var item in reservoirs)
            {
                Point3d node = new Point3d(item.Value[0], item.Value[1], item.Value[2]);
                Reserviors.Add(node);
                _nodesDiction.Add(item.Key, node);
            }


            // Links

            // Need to have _nodeDiction first, then we can create the polyline.

            Dictionary<string, List<Point3d>> pipes = new Dictionary<string, List<Point3d>>();
            GetPipePolylines(pipes, _pipeLines, _verticeLines);

            foreach (var item in pipes)
            {
                var tempPl = new Polyline(item.Value);
                tempPl.ReduceSegments(tolerance * 1000);

                Pipes.Add(tempPl);
                _pipesDiction.Add(item.Key, tempPl);
            }

        }


        private void GetNodesCoords(Dictionary<string, double[]> dict, IList<string> container, IList<string> coordinates, string header)
        {
            foreach (var line in container)
            {
                if (!line.Contains($"[{header}]") && line[0] != ';')
                {
                    var temp = line.Split('\t');

                    var elevation = temp[1].Trim();
                    var id = temp[0].Trim();

                    double[] coords = new double[3] { 0, 0, Convert.ToDouble(elevation) };
                    dict.Add(id, coords);
                }
            }

            foreach (var line in coordinates)
            {
                if (!line.Contains("[COORDINATES]") && line[0] != ';')
                {
                    var temp = line.Split('\t');
                    var id = temp[0].Trim();

                    if (dict.ContainsKey(id))
                    {
                        var x = temp[1].Trim();
                        var y = temp[2].Trim();

                        dict[id][0] = Convert.ToDouble(x);
                        dict[id][1] = Convert.ToDouble(y);
                    }
                }
            }
        }

        private void GetDataFromInp(string header, IList<string> container, string[] readAllLines)
        {
            for (int i = 0; i < readAllLines.Length; i++)
            {
                if (readAllLines[i].Contains($"[{header}]"))
                {

                    while (readAllLines[i] != "")
                    {
                        var temp = readAllLines[i];
                        container.Add(temp);
                        i++;
                    }
                    break;
                }
            }
        }


        private void GetPipePolylines(Dictionary<string, List<Point3d>> dict, IList<string> pipelines, IList<string> vertices)
        {
            foreach (var line in pipelines)
            {
                if (!line.Contains("[PIPES]") && line[0] != ';')
                {
                    var temp = line.Split('\t');

                    string start = temp[1].Trim();
                    string end = temp[2].Trim();

                    List<Point3d> nodes = new List<Point3d>() { _nodesDiction[start], _nodesDiction[end] };

                    var id = temp[0].Trim();
                    dict.Add(id, nodes);
                }
            }

            foreach (var line in vertices)
            {
                if (!line.Contains($"[VERTICES]") && line[0] != ';')
                {
                    var temp = line.Trim().Split('\t');

                    var id = temp[0].Trim();
                    var x = temp[1].Trim();
                    var y = temp[2].Trim();

                    double[] coords = new double[3] { Convert.ToDouble(x), Convert.ToDouble(y), 0 };

                    if (dict.ContainsKey(id))
                    {
                        var vnode = new Point3d(coords[0], coords[1], coords[2]);
                        var tempPts = dict[id];

                        dict[id].Insert(tempPts.Count - 1, vnode);
                    }
                }
            }
        }
        #endregion
    }
}
