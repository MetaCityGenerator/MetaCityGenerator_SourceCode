using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Geometries;


namespace Mawan.DataStructures
{
    public class DirectedEdge
    {
        private readonly int v;
        private readonly int w;
        private readonly Dictionary<string, double> weight = new Dictionary<string, double>();
        private readonly LineString lineString;

        public DirectedEdge(int v, int w, LineString ls = null)
        {
            this.v = v;
            this.w = w;
            this.lineString = ls;
        }

        public void SetWeight(string attr, double new_weight)
        {
            if (!weight.ContainsKey(attr)) { weight.Add(attr, new_weight); }
              else { weight[attr] = new_weight; }
        }

        public double GetWeight(string attr = null)
        {
            if (attr == null)
            {
                var temp = weight.Values.ToArray<double>();
                return temp[0];
            } else
            {
                return weight[attr];
            }
        }

        public DirectedEdge GetReversedEdge()
        {
            LineString ls = lineString;
            if (ls != null) ls = (LineString)ls.Reverse();
            DirectedEdge reversed = new DirectedEdge(w, v, ls);
            foreach (var kvp in weight) reversed.SetWeight(kvp.Key, kvp.Value);
            return reversed;
        }

        public int From => v;

        public int To => w;

        public LineString LineString => lineString;
    }
}
