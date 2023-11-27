
using System;



namespace MetaCity.Planning.UrbanDesign
{

    public struct BuildingType : IComparable<BuildingType>, IEquatable<BuildingType>
    {
        public readonly double _siteArea;

        public string TypeName { get; }

        public double Area { get; }

        // Calculated result of floor number.
        public int[] Floors { get; }

        public double Ratio { get; private set; }

        public double Priority { get; private set; }

        public BuildingParameters Parameters { get; }


        // Already know the fitted parameters for current building type.
        public BuildingType(string buildingType, int[] floors, double siteArea)
        {
            _siteArea = siteArea;

            TypeName = buildingType;
            Parameters = BuildingDataset.GetBuildingParameters(TypeName);
            Area = Parameters.Area;
            Floors = floors;


            Ratio = 0;
            Priority = 0;

            CorrectFloors();
            GetRatio();
            GetPriority();
        }

        /// <summary>
        /// To make sure all the floor count is larger or equal to zero.
        /// </summary>
        private void CorrectFloors()
        {
            for (int i = 0; i < Floors.Length; i++)
            {
                Floors[i] = Floors[i] < 0 ? 0 : Floors[i];
            }
        }

        private void GetRatio()
        {
            Ratio = Parameters.Area / _siteArea;
        }

        private void GetPriority()
        {
            // computing priority based on floor area and function.

            Priority = Parameters.Priority;
        }

        public int CompareTo(BuildingType other)
        {
            var c = this.Ratio.CompareTo(other.Ratio);
            return c == 0 ? this.Priority.CompareTo(other.Priority) : c;
        }

        public bool Equals(BuildingType other)
        {
            if (this.Parameters.Equals(other.Parameters) && this.Floors == other.Floors)
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return this.Parameters.GetHashCode() ^ this.Floors.GetHashCode();
        }
    }







}
