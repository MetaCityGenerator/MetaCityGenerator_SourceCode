namespace UrbanX.Planning.Water
{
    internal class Junctions
    {
        // The index of this junction in the whole nodes list(include point components), which is the TVertex in the Gragh<int>.
        public int IndexOfNode { get; }
        public string JuncLabel { get; }
        public double WaterDemand { get; private set; }

        public int[] ShortestPath { get; private set; }



        public Junctions(int index)
        {

            IndexOfNode = index;
            JuncLabel = $"J{index}";
        }

        public void UpdateDemand(double demand)
        {
            WaterDemand = demand;
        }

        public void AccumulateDemand(double demand)
        {
            WaterDemand += demand;
        }

        public void UpdateShortestPath(int[] path)
        {
            ShortestPath = path;
        }

    }

    internal class Reservoir
    {
        public int IndexOfNode { get; }
        public string ReserLabel { get; }

        public Reservoir(int index)
        {
            IndexOfNode = index;
            ReserLabel = $"R{index}";
        }
    }

    internal class Pipe
    {

        public string IndexOfEndsNodes { get; }
        public double Length { get; }
        public string PipeLabel { get; }

        public double FlowRate { get; private set; }
        public double Diameter { get; private set; }

        public Pipe(string key, double length, int index)
        {

            IndexOfEndsNodes = key;
            Length = length;
            PipeLabel = $"Pipe{index}";
        }
        public void UpdateFlowRate(double flowRate)
        {
            FlowRate = flowRate;
        }

        public void AccumulateFlowRate(double flowRate)
        {
            FlowRate += flowRate;
        }

        public void UpdateDiameter(double diameter)
        {
            Diameter = diameter;
        }

    }

    public struct Coords
    {
        public int X { get; }
        public int Y { get; }

        public Coords(int x, int y) => (X, Y) = (x, y);

    }



}
