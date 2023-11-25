using System;
using System.Collections.Generic;

using UrbanX.Algorithms.Graphs;
using UrbanX.DataStructures.Graphs;


namespace UrbanX.Planning.SpaceSyntax
{
    /// <summary>
    /// A wrapper for calculating cenrality class in algorithms namespace.
    /// This is used for spacesyntax only by using the undirected weighted graph.
    /// </summary>
    public sealed class SpaceSyntaxComputing3D
    {
        /// <summary>
        /// Metric choice is calculated by counting the number of times each segment falls on the shortest path 
        /// between all pairs of segments within a selected distance measured metrically.
        /// Equals Betweeness in graph theory.
        /// </summary>
        public double[] MetricChoice { get; }

        /// <summary>
        /// Metric integration measures how close each segment is to all others under the definition of metric distance, 
        /// that is, the metric distance along the lines between the mid-points of two adjacent segments.
        /// metricIntegration=n^2/(totalDepth)
        /// </summary>
        public double[] MetricIntegration { get; }


        /// <summary>
        /// Metric mean depth is the average metric distance from each space to all others.
        /// meanDepth= (totalDepth)/(n−1)  , n is the number of destinations found.
        /// </summary>
        public double[] MetricMeanDepth { get; }


        /// <summary>
        /// Metric total depth is the cumulative total of the shortest metric distance paths between all pairs of nodes.
        /// </summary>
        public double[] MetricTotalDepth { get; }


        public int[] MetricNodeCount { get; }

        /// <summary>
        /// Choice is calculated by counting the number of times each street segment falls on the shortest path between 
        /// all pairs of segments within a selected distance (termed ‘radius’). The ‘shortest path’ refers to the path 
        /// of least angular deviation (namely, the straightest route) through the system. 
        /// </summary>
        public double[] AngularChoice { get; }


        /// <summary>
        /// Angular integration is the reciprocal of the normalised angular total depth. It can be compared across systems. 
        /// It measures how close each segment is to all others in terms of the sum of angular changes that are made on each route.
        /// </summary>
        public double[] AngularIntegration { get; }


        /// <summary>
        /// Angular mean depth is the sum of the shortest angular paths over the sum of all angular intersections in the system. 
        /// In DepthMap, it is defined as the sum of the shortest angular paths over the sum of the number of segments encountered 
        /// on the paths from the root (origin) segment to all others.
        /// </summary>
        public double[] AngularMeanDepth { get; }


        /// <summary>
        /// Angular total depth is the cumulative total of the shortest angular paths to a selected segment as root.
        /// </summary>
        public double[] AngularTotalDepth { get; }

        public int[] AngularNodeCount { get; }

        /// <summary>
        /// Normalised choice aims to solve the paradox that segregated designs add more total (and average) choice to the system than 
        /// integrated ones. It divides total choice by total depth for each segment in the system. This adjusts choice values according
        /// to the depth of each segment in the system, since the more segregated is, the more its choice value with be reduced by being 
        /// divided by a higher total depth number. This would seem to have the effect of measuring choice in a cost-benefit way.
        /// </summary>
        public double[] NormalisedAngularChoice { get; }

        public double[] CustomChoice { get; }
        public double[] CustomIntegration { get; }
        public double[] CustomMeanDepth { get; }
        public double[] CustomTotalDepth { get; }
        public int[] CustomNodeCount { get; }

        /// <summary>
        /// Normalised angular integration aims to normalise angular total depth by comparing the system to the urban average.
        /// </summary>
        public double[] NormalisedAngularIntegration { get; }

        public SpaceSyntaxComputing3D(in SpaceSyntaxGraph graph,in double radius)
        {
            if (radius == double.PositiveInfinity) // Global space syntax.
            {
                //Stopwatch w = new Stopwatch();
                //w.Start();

                var computeMetric = new CalculateCentrality3D(in graph, GraphType.Metric);
                //w.Stop();
                //var s1 = w.ElapsedMilliseconds;
                //w.Reset();

                //w.Start();
                MetricChoice = computeMetric.Betweenness;
                MetricIntegration = ComputeIntegration(computeMetric.TotalDepths, computeMetric.NodeCounts);
                MetricMeanDepth = ComputeMeanDepth(computeMetric.TotalDepths, computeMetric.NodeCounts);
                MetricTotalDepth = computeMetric.TotalDepths;
                MetricNodeCount = computeMetric.NodeCounts;
               //w.Start();
                var computeAngular = new CalculateCentrality3D(in graph, GraphType.Angular);
                //w.Stop();

                // var s2 = w.ElapsedMilliseconds;

                AngularChoice = computeAngular.Betweenness;
                AngularIntegration = ComputeIntegration(computeAngular.TotalDepths, computeAngular.NodeCounts);
                AngularMeanDepth = ComputeMeanDepth(computeAngular.TotalDepths, computeAngular.NodeCounts);
                AngularTotalDepth =computeAngular.TotalDepths;
                AngularNodeCount = computeAngular.NodeCounts;

                NormalisedAngularChoice = ComputeNACH(computeAngular.TotalDepths, AngularChoice);
                NormalisedAngularIntegration = ComputeNAIN(computeAngular.TotalDepths, computeAngular.NodeCounts);
            }
            else
            {
                // Has radius.
                // Step1: compute metric graph.
                var computeMetric = new CalculateCentrality3D(in graph, GraphType.Metric ,in radius);

                MetricChoice = computeMetric.Betweenness;
                MetricIntegration = ComputeIntegration(computeMetric.TotalDepths, computeMetric.NodeCounts);
                MetricMeanDepth = ComputeMeanDepth(computeMetric.TotalDepths, computeMetric.NodeCounts);
                MetricTotalDepth = computeMetric.TotalDepths;
                MetricNodeCount = computeMetric.NodeCounts;

                // Step2: get sub graph within radius.
                var subGraphs = computeMetric.SubGraphs;
               
                var computeAngular = new CalculateCentrality3D(in graph, GraphType.Angular,in subGraphs);

                AngularChoice = computeAngular.Betweenness;
                AngularIntegration = ComputeIntegration(computeAngular.TotalDepths, computeAngular.NodeCounts);
                AngularMeanDepth = ComputeMeanDepth(computeAngular.TotalDepths, computeAngular.NodeCounts);
                AngularTotalDepth = computeAngular.TotalDepths;
                AngularNodeCount = computeAngular.NodeCounts;

                NormalisedAngularChoice = ComputeNACH(computeAngular.TotalDepths, AngularChoice);
                NormalisedAngularIntegration = ComputeNAIN(computeAngular.TotalDepths, computeAngular.NodeCounts);
            }
        }

        public SpaceSyntaxComputing3D(in SpaceSyntaxGraph graph, in double[] radius)
        {
            // Has radius.
            // Step1: compute metric graph.
            var computeMetric = new CalculateCentrality3D(in graph, GraphType.Metric, in radius);

            MetricChoice = computeMetric.Betweenness;
            MetricIntegration = ComputeIntegration(computeMetric.TotalDepths, computeMetric.NodeCounts);
            MetricMeanDepth = ComputeMeanDepth(computeMetric.TotalDepths, computeMetric.NodeCounts);
            MetricTotalDepth = computeMetric.TotalDepths;
            MetricNodeCount = computeMetric.NodeCounts;

            // Step2: get sub graph within radius.
            var subGraphs = computeMetric.SubGraphs;

            var computeAngular = new CalculateCentrality3D(in graph, GraphType.Angular, in subGraphs);

            AngularChoice = computeAngular.Betweenness;
            AngularIntegration = ComputeIntegration(computeAngular.TotalDepths, computeAngular.NodeCounts);
            AngularMeanDepth = ComputeMeanDepth(computeAngular.TotalDepths, computeAngular.NodeCounts);
            AngularTotalDepth = computeAngular.TotalDepths;
            AngularNodeCount = computeAngular.NodeCounts;

            NormalisedAngularChoice = ComputeNACH(computeAngular.TotalDepths, AngularChoice);
            NormalisedAngularIntegration = ComputeNAIN(computeAngular.TotalDepths, computeAngular.NodeCounts);
        }

        public SpaceSyntaxComputing3D(in SpaceSyntaxGraph graph, in double radius, GraphType gt)
        {
            if (radius == double.PositiveInfinity) // Global space syntax.
            {
                //Stopwatch w = new Stopwatch();
                //w.Start();

                //var computeMetric = new CalculateCentrality3D(in graph, GraphType.Metric);
                //w.Stop();
                //var s1 = w.ElapsedMilliseconds;
                //w.Reset();

                //w.Start();
                //MetricChoice = computeMetric.Betweenness;
                //MetricIntegration = ComputeIntegration(computeMetric.TotalDepths, computeMetric.NodeCounts);
                //MetricMeanDepth = ComputeMeanDepth(computeMetric.TotalDepths, computeMetric.NodeCounts);
                //MetricTotalDepth = computeMetric.TotalDepths;
                //MetricNodeCount = computeMetric.NodeCounts;
                //w.Start();
                var computeCustomWeight = new CalculateCentrality3D(in graph, gt);
                //w.Stop();

                // var s2 = w.ElapsedMilliseconds;

                CustomChoice = computeCustomWeight.Betweenness;
                CustomIntegration = ComputeIntegration(computeCustomWeight.TotalDepths, computeCustomWeight.NodeCounts);
                CustomMeanDepth = ComputeMeanDepth(computeCustomWeight.TotalDepths, computeCustomWeight.NodeCounts);
                CustomTotalDepth = computeCustomWeight.TotalDepths;
                CustomNodeCount = computeCustomWeight.NodeCounts;

                //NormalisedAngularChoice = ComputeNACH(computeAngular.TotalDepths, AngularChoice);
                //NormalisedAngularIntegration = ComputeNAIN(computeAngular.TotalDepths, computeAngular.NodeCounts);
            }
            else
            {
                // Has radius.
                // Step1: compute metric graph.
                var computeMetric = new CalculateCentrality3D(in graph, GraphType.Metric, in radius);

                //MetricChoice = computeMetric.Betweenness;
                //MetricIntegration = ComputeIntegration(computeMetric.TotalDepths, computeMetric.NodeCounts);
                //MetricMeanDepth = ComputeMeanDepth(computeMetric.TotalDepths, computeMetric.NodeCounts);
                //MetricTotalDepth = computeMetric.TotalDepths;
                //MetricNodeCount = computeMetric.NodeCounts;

                // Step2: get sub graph within radius.
                var subGraphs = computeMetric.SubGraphs;

                var computeCustomWeight = new CalculateCentrality3D(in graph, gt, in subGraphs);

                CustomChoice = computeCustomWeight.Betweenness;
                CustomIntegration = ComputeIntegration(computeCustomWeight.TotalDepths, computeCustomWeight.NodeCounts);
                CustomMeanDepth = ComputeMeanDepth(computeCustomWeight.TotalDepths, computeCustomWeight.NodeCounts);
                CustomTotalDepth = computeCustomWeight.TotalDepths;
                CustomNodeCount = computeCustomWeight.NodeCounts;

                //NormalisedAngularChoice = ComputeNACH(computeCustomWeight.TotalDepths, AngularChoice);
                //NormalisedAngularIntegration = ComputeNAIN(computeCustomWeight.TotalDepths, computeCustomWeight.NodeCounts);
            }
        }

        private double[] ComputeIntegration(double[] totalDepth, int[] nodeCount)
        {
            var result =new double[totalDepth.Length];

            for (int i = 0; i < totalDepth.Length; i++)
            {
                if (totalDepth[i] == 0)
                {
                    continue;
                }
                result[i] = nodeCount[i] * nodeCount[i] / totalDepth[i];
            }

            return result;
        }


        private double[] ComputeMeanDepth(double[] totalDepth, int[] nodeCount)
        {
            var result = new double[totalDepth.Length];

            for (int i = 0; i < totalDepth.Length; i++)
            {
                if (totalDepth[i] == 0)
                {
                    continue;
                }
                result[i] = totalDepth[i] / (nodeCount[i] - 1);
            }
            return result;
        }


        private double[] ComputeNACH(double[] angularTotalDepth, double[] angularChoice)
        {
            var result = new double[angularTotalDepth.Length];

            for (int i = 0; i < angularTotalDepth.Length; i++)
            {
                result[i] = Math.Log(angularChoice[i] + 1) / Math.Log(angularTotalDepth[i] + 3);
            }
            return result;
        }


        private double[] ComputeNAIN(double[] angularTotalDepth, int[] nodeCount)
        {
            //NATD=totalDepth/((nodeCount+2)^1.2)
            //NAIN = 1/ NATD 
            //NAIN = (nodeCount +2)^1.2 / totalDepth

            var result = new double[angularTotalDepth.Length];
            for (int i = 0; i < angularTotalDepth.Length; i++)
            {
                if (angularTotalDepth[i] == 0)
                    continue;

                result[i] = Math.Pow(nodeCount[i] + 2, 1.2) / angularTotalDepth[i];
            }
            return result;
        }


        [Obsolete]
        /// <summary>
        /// Normalizatoing the betweenness score by using (n - 1) * (n - 2).
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="normalize"></param>
        private void NormalizeBetweenness(IDictionary<int, double> betweeness)
        {
            double scale;

            // if use int, will occur overflow error.
            double n = betweeness.Count;

            if (n <= 2)
                return;

            scale = 1.0 / ((n - 1) * (n - 2));
            foreach (var vertex in betweeness.Keys)
            {
                betweeness[vertex] *= scale;
            }
        }
    }
}
