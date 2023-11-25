using System;

namespace UrbanX.Planning.UrbanDesign
{
    public struct BuildingParameters : IEquatable<BuildingParameters>
    {
        public string _name;

        public double Area { get; }

        public int[] FloorRange { get; }

        public double[] Depth { get; }

        public int Priority { get; }

        public double FloorHeight { get; set; }

        public string Function { get; }

        public BuildingParameters(string name, double area, int[] floorRange, double[] depth, int priority, double height, string type)
        {
            _name = name;
            Area = area;
            FloorRange = floorRange;

            Depth = depth;
            Priority = priority;
            FloorHeight = height;
            Function = type;
        }

        public bool Equals(BuildingParameters other)
        {
            if (this._name == other._name)
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return this._name.GetHashCode();
        }
    }
}
