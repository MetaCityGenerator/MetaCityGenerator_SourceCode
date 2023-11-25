using System;
using System.Collections.Generic;
using System.Diagnostics;

using UrbanX.Algorithms.Graphs;
using UrbanX.DataStructures.Graphs;


namespace UrbanX.Planning.SpaceSyntax
{
    /// <summary>
    /// A wrapper for calculating cenrality class in algorithms namespace.
    /// This is used for spacesyntax only by using the undirected weighted graph.
    /// </summary>
    public class SpaceSyntaxComputing
    {

        /// <summary>
        /// Metric choice is calculated by counting the number of times each segment falls on the shortest path 
        /// between all pairs of segments within a selected distance measured metrically.
        /// Equals Betweeness in graph theory.
        /// </summary>
        public Dictionary<int, double> MetricChoice { get; }


        /// <summary>
        /// Metric integration measures how close each segment is to all others under the definition of metric distance, 
        /// that is, the metric distance along the lines between the mid-points of two adjacent segments.
        /// metricIntegration=n^2/(totalDepth)
        /// </summary>
        public Dictionary<int, double> MetricIntegration { get; }


        /// <summary>
        /// Metric mean depth is the average metric distance from each space to all others.
        /// meanDepth= (totalDepth)/(n−1)  , n is the number of destinations found.
        /// </summary>
        public Dictionary<int, double> MetricMeanDepth { get; }


        /// <summary>
        /// Metric total depth is the cumulative total of the shortest metric distance paths between all pairs of nodes.
        /// </summary>
        public Dictionary<int, double> MetricTotalDepth { get; }


        /// <summary>
        /// Choice is calculated by counting the number of times each street segment falls on the shortest path between 
        /// all pairs of segments within a selected distance (termed ‘radius’). The ‘shortest path’ refers to the path 
        /// of least angular deviation (namely, the straightest route) through the system. 
        /// </summary>
        public Dictionary<int, double> AngularChoice { get; }


        /// <summary>
        /// Angular integration is the reciprocal of the normalised angular total depth. It can be compared across systems. 
        /// It measures how close each segment is to all others in terms of the sum of angular changes that are made on each route.
        /// </summary>
        public Dictionary<int, double> AngularIntegration { get; }


        /// <summary>
        /// Angular mean depth is the sum of the shortest angular paths over the sum of all angular intersections in the system. 
        /// In DepthMap, it is defined as the sum of the shortest angular paths over the sum of the number of segments encountered 
        /// on the paths from the root (origin) segment to all others.
        /// </summary>
        public Dictionary<int, double> AngularMeanDepth { get; }


        /// <summary>
        /// Angular total depth is the cumulative total of the shortest angular paths to a selected segment as root.
        /// </summary>
        public Dictionary<int, double> AngularTotalDepth { get; }


        /// <summary>
        /// Normalised choice aims to solve the paradox that segregated designs add more total (and average) choice to the system than 
        /// integrated ones. It divides total choice by total depth for each segment in the system. This adjusts choice values according
        /// to the depth of each segment in the system, since the more segregated is, the more its choice value with be reduced by being 
        /// divided by a higher total depth number. This would seem to have the effect of measuring choice in a cost-benefit way.
        /// </summary>
        public Dictionary<int, double> NormalisedAngularChoice { get; }


        /// <summary>
        /// Normalised angular integration aims to normalise angular total depth by comparing the system to the urban average.
        /// </summary>
        public Dictionary<int, double> NormalisedAngularIntegration { get; }

        public SpaceSyntaxComputing(UndirectedWeightedSparseGraph<int> metricGraph, UndirectedWeightedSparseGraph<int> angularGraph, double radius = double.PositiveInfinity)
        {
            if (radius == double.PositiveInfinity) // Global space syntax.
            {
                //Stopwatch w = new Stopwatch();
                //w.Start();

                var computeMetric = new CalculateCentrality<UndirectedWeightedSparseGraph<int>, int>(metricGraph);
                //w.Stop();
                //var s1 = w.ElapsedMilliseconds;
                //w.Reset();

                //w.Start();
                MetricChoice = computeMetric.Betweenness;
                MetricIntegration = ComputeIntegration(computeMetric.TotalDepths, computeMetric.NodeCounts);
                MetricMeanDepth = ComputeMeanDepth(computeMetric.TotalDepths, computeMetric.NodeCounts);
                MetricTotalDepth = new Dictionary<int, double>(computeMetric.TotalDepths);

               //w.Start();
                var computeAngular = new CalculateCentrality<UndirectedWeightedSparseGraph<int>, int>(angularGraph);
                //w.Stop();

               // var s2 = w.ElapsedMilliseconds;

                AngularChoice = computeAngular.Betweenness;
                AngularIntegration = ComputeIntegration(computeAngular.TotalDepths, computeAngular.NodeCounts);
                AngularMeanDepth = ComputeMeanDepth(computeAngular.TotalDepths, computeAngular.NodeCounts);
                AngularTotalDepth = new Dictionary<int, double>(computeAngular.TotalDepths);


                NormalisedAngularChoice = ComputeNACH(computeAngular.TotalDepths, AngularChoice);
                NormalisedAngularIntegration = ComputeNAIN(computeAngular.TotalDepths, computeAngular.NodeCounts);
            }
            else
            {
                // Has radius.
                // Step1: compute metric graph.
                var computeMetric = new CalculateCentrality<UndirectedWeightedSparseGraph<int>, int>(metricGraph, radius);

                MetricChoice = computeMetric.Betweenness;
                MetricIntegration = ComputeIntegration(computeMetric.TotalDepths, computeMetric.NodeCounts);
                MetricMeanDepth = ComputeMeanDepth(computeMetric.TotalDepths, computeMetric.NodeCounts);
                MetricTotalDepth = new Dictionary<int, double>(computeMetric.TotalDepths);

                // Step2: get sub graph within radius.
                var subGraphs = computeMetric.SubGraphs;
                var computeAngular = new CalculateCentrality<UndirectedWeightedSparseGraph<int>, int>(angularGraph, subGraphs);

                AngularChoice = computeAngular.Betweenness;
                AngularIntegration = ComputeIntegration(computeAngular.TotalDepths, computeAngular.NodeCounts);
                AngularMeanDepth = ComputeMeanDepth(computeAngular.TotalDepths, computeAngular.NodeCounts);
                AngularTotalDepth = new Dictionary<int, double>(computeAngular.TotalDepths);

                NormalisedAngularChoice = ComputeNACH(computeAngular.TotalDepths, AngularChoice);
                NormalisedAngularIntegration = ComputeNAIN(computeAngular.TotalDepths, computeAngular.NodeCounts);
            }
        }

        private Dictionary<int, double> ComputeIntegration(IDictionary<int, double> totalDepth, IDictionary<int, double> nodeCount)
        {
            var result = new Dictionary<int, double>(totalDepth.Count);

            foreach (var node in totalDepth.Keys)
            {
                if (totalDepth[node] == 0)
                {
                    result.Add(node, 0);
                    continue;
                }

                result.Add(node, nodeCount[node] * nodeCount[node] / totalDepth[node]);
            }

            return result;
        }


        private Dictionary<int, double> ComputeMeanDepth(IDictionary<int, double> totalDepth, IDictionary<int, double> nodeCount)
        {
            var result = new Dictionary<int, double>(totalDepth);

            foreach (var node in totalDepth.Keys)
            {
                if (totalDepth[node] == 0)
                    continue;

                result[node] = totalDepth[node] * 1.0 / (nodeCount[node] - 1);
            }

            return result;
        }


        private Dictionary<int, double> ComputeNACH(IDictionary<int, double> angularTotalDepth, IDictionary<int, double> angularChoice)
        {
            var result = new Dictionary<int, double>(angularTotalDepth.Count);

            foreach (var node in angularTotalDepth.Keys)
            {
                result.Add(node, Math.Log(angularChoice[node] + 1) / Math.Log(angularTotalDepth[node] + 3));
            }

            return result;
        }


        private Dictionary<int, double> ComputeNAIN(IDictionary<int, double> angularTotalDepth, IDictionary<int, double> nodeCount)
        {
            //NATD=totalDepth/((nodeCount+2)^1.2)
            //NAIN = 1/ NATD 
            //NAIN = (nodeCount +2)^1.2 / totalDepth

            var result = new Dictionary<int, double>(angularTotalDepth.Count);

            foreach (var node in angularTotalDepth.Keys)
            {
                if (angularTotalDepth[node] == 0)
                {
                    result.Add(node, 0);
                    continue;
                }

                result.Add(node, Math.Pow(nodeCount[node] + 2, 1.2) / angularTotalDepth[node]);
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
