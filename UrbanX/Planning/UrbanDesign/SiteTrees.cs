using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetTopologySuite.Geometries;
using UrbanX.Planning.Utility;

namespace UrbanX.Planning.UrbanDesign
{
    public class Tree
    {
        public Coordinate Centroid { get; private set; }

        public double Radius => Canopy * 0.5;

        public double CanopyArea => Math.PI * Radius* Radius;

        public double  Height { get;  }

        public double Canopy { get; }



        public Tree( double canopy, double height = 6)
        {
            Canopy = canopy;
            Height = height;
        }

        public void SetCoordinate(Coordinate pt) => Centroid = pt;

        public Tree DeepCopy() => new Tree(this.Canopy, this.Height);
    }


    public class SiteTrees
    {
        private static readonly Tree _tree1 = new Tree(9, 6);

        private static readonly Tree _tree2 = new Tree(6, 4);

        private static readonly Tree _tree3 = new Tree(3, 2);


        public Tree[] Trees { get;  }

        public Polygon[] Polygons { get; }



        public SiteTrees(Polygon site, double treeAreaRatio = 0.3)
        {
            double treeArea = site.Area * treeAreaRatio;

            Stack<Tree> trees = new Stack<Tree>();

            while (treeArea > 0)
            {
                trees.Push(_tree3.DeepCopy());
                treeArea -= _tree3.CanopyArea;
                if (treeArea < 0) break;

                trees.Push(_tree3.DeepCopy());
                treeArea -= _tree3.CanopyArea;
                if (treeArea < 0) break;

                trees.Push(_tree3.DeepCopy());
                treeArea -= _tree3.CanopyArea;
                if (treeArea < 0) break;


                trees.Push(_tree2.DeepCopy());
                treeArea -= _tree2.CanopyArea;
                if (treeArea < 0) break;

                trees.Push(_tree2.DeepCopy());
                treeArea -= _tree2.CanopyArea;
                if (treeArea < 0) break;

                trees.Push(_tree1.DeepCopy());
                treeArea -= _tree1.CanopyArea;
                if (treeArea < 0) break;
            }

            Trees = trees.ToArray();

            double[] areas = new double[trees.Count * 2];
            double[] priorities = new double[trees.Count * 2];


            for (int i = 0; i < areas.Length; i += 2)
            {
                var t = trees.Pop();

                areas[i] = t.CanopyArea;
                areas[i + 1] = areas[i] * ((1 - treeAreaRatio) / treeAreaRatio);

                priorities[i] = 1 / t.Radius;
                priorities[i + 1] = double.MinValue;
            }

            var scores = new double[] { 11, 11, 10, 10 };
            var radiant = site.GetPolygonRadiant();

            var result = Toolbox.SplitSiteByRatiosAccuratly(site, areas, priorities, scores, radiant, false); //TODO: rootfind : 3 iterations.

            for (int i = 0; i < Trees.Length; i++)
            {
                Trees[i].SetCoordinate(result[i * 2].Centroid.Coordinate);
            }

            Polygons = result;
        }




    }
}
