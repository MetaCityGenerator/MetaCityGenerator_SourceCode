using System;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;


namespace Mawan.DataStructures
{
    public class Edge
    {
        private readonly int v;
        private readonly int w;
        private readonly Dictionary<string, double> weight = new Dictionary<string, double>();
        private readonly LineString lineString;

        public Edge(int v, int w, LineString ls = null)
        {
            this.v = v;
            this.w = w;
            this.lineString = ls;
        }

        public LineString LineString => lineString;

        public void SetWeight(string attr, double thisWeight)
        {
            if (weight.ContainsKey(attr)) { weight[attr] = thisWeight; }
            else weight.Add(attr, thisWeight);
        }

        public double GetWeight(string attr = null)
        {
            if (attr == null)
            {
                var temp = weight.Values.ToArray<double>();
                return temp[0];
            }
            else
            {
                return weight[attr];
            }
        }

        public int Either() => v;

        public int Other(int v)
        {
            if (this.v == v) { return w; }
            if (this.w == v) { return this.v; }
            throw new ArgumentException("Wrong vertice id!");
        }

        public override string ToString()
        {
            string res = v + "-" + w;
            return res;
        }
    }
}
