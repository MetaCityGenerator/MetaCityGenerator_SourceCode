using System;
using System.Collections.Generic;
using System.Linq;

using UrbanX.Algorithms.Mathematics;
using UrbanX.DataStructures.Trees;

namespace UrbanX.DataStructures.Geometry
{
    // internal point2d class
    public class VPoint : IComparable<VPoint>
    {
        public double X { get; }
        public double Y { get; }

        public FortuneSite Site { get; internal set; }

        /// <summary>
        /// Constructor of VPoint.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public VPoint(double x, double y) => (X, Y) = (x, y);

        public int CompareTo(VPoint other)
        {
            var tanThis = Polar(Site, this);
            var tanOther = Polar(Site, other);
            return tanThis.CompareTo(tanOther);
        }


        private double Polar(FortuneSite site, VPoint point)
        {
            var tanX = point.X - site.X;
            var tanY = point.Y - site.Y;

            var atan2 = Math.Atan2(tanY, tanX);
            if (atan2 < 0)
            {
                atan2 += 2 * Math.PI;
            }

            return atan2;
        }
    }


    public class VEdge
    {
        public VPoint Start { get; internal set; }

        public VPoint End { get; internal set; }

        public VEdge Neighbor { get; internal set; }



        /// <summary>
        /// Left focus in site.
        /// </summary>
        public FortuneSite Left { get; }

        /// <summary>
        /// Right focus in site.
        /// </summary>
        public FortuneSite Right { get; }


        internal double SlopeRise { get; }
        internal double SlopeRun { get; }
        internal double? Slope { get; }
        internal double? Intercept { get; }


        public VEdge(VPoint start, FortuneSite left, FortuneSite right)
        {
            // For bounding box edges
            if (left == null || right == null) return;

            Start = start;
            Left = left;
            Right = right;

            // From negative reciprocal of slope of line from left to right.
            // m = (left.y - right.y)/(left.x-right.x).
            SlopeRise = left.X - right.X;
            SlopeRun = -(left.Y - right.Y);
            Intercept = null;

            if (SlopeRise.ApproxEqual(0) || SlopeRun.ApproxEqual(0)) return;

            Slope = SlopeRise / SlopeRun;
            Intercept = start.Y - Slope * start.X;
        }

    }

    /// <summary>
    /// FortuneSite class. Representing the site in voronoi diagram, point(x,y) is the focus of parabola.
    /// </summary>
    public class FortuneSite
    {
        public double X { get; }
        public double Y { get; }

        // list of points would be better
        //public List<VEdge> Cell { get; private set; }

        public HashSet<VPoint> Cell { get; private set; }

        public List<FortuneSite> Neighbors { get; private set; }

        public FortuneSite(double x, double y)
        {
            X = x;
            Y = y;
            Cell = new HashSet<VPoint>();
            Neighbors = new List<FortuneSite>();
        }

        public VPoint[] SortCell()
        {
            foreach (var pt in Cell)
            {
                pt.Site = this;
            }

            RBTree<VPoint> cellTree = new RBTree<VPoint>(false, TraversalMode.InOrder);
            cellTree.Insert(Cell.ToArray());

            return cellTree.ToArray();

        }

    }

}
