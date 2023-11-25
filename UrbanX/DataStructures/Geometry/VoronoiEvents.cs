using System;

namespace UrbanX.DataStructures.Geometry
{
    interface IFortuneEvent : IComparable<IFortuneEvent>
    {
        double X { get; }
        double Y { get; }
    }


    internal class FortuneSiteEvent : IFortuneEvent
    {
        internal FortuneSite Site { get; }
        public double X => Site.X;
        public double Y => Site.Y;

        internal FortuneSiteEvent(FortuneSite site)
        {
            Site = site;
        }

        public int CompareTo(IFortuneEvent other)
        {
            var c = Y.CompareTo(other.Y);
            return c == 0 ? X.CompareTo(other.X) : c;
        }
    }

    internal class FortuneCircleEvent : IFortuneEvent
    {
        internal VPoint Lowest { get; }
        internal double YCenter { get; }
        internal VRBTreeNode<BeachSection> ToDelete { get; }

        internal FortuneCircleEvent(VPoint lowest, double yCenter, VRBTreeNode<BeachSection> toDelete)
        {
            Lowest = lowest;
            YCenter = yCenter;
            ToDelete = toDelete;
        }


        public double X => Lowest.X;
        public double Y => Lowest.Y;


        public int CompareTo(IFortuneEvent other)
        {
            var c = Y.CompareTo(other.Y);
            return c == 0 ? X.CompareTo(other.X) : c;
        }
    }
}
