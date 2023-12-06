using System;
using System.Collections.Generic;
using System.Linq;
using MetaCity.Algorithms.Graphs;
using MetaCity.DataStructures.Graphs;


namespace MetaCity.Assessment.SpatialAnalysis
{
    /// <summary>
    /// A wrapper for calculating cenrality class in algorithms namespace.
    /// This is used for spacesyntax only by using the undirected weighted graph.
    /// Revised version based on spacetyntax computing for spatial anlysis, where using visual scores as the main weight for edges.
    /// </summary>
    public class VisualSyntaxComputing
    {

        /// <summary>
        /// Choice is calculated by counting the number of times each street segment falls on the shortest path between 
        /// all pairs of segments within a selected distance (termed ‘radius’). The ‘shortest path’ refers to the path 
        /// of least angular deviation (namely, the straightest route) through the system. 
        /// </summary>
        public Dictionary<int, double> VisualChoice { get; }


        /// <summary>
        /// Angular integration is the reciprocal of the normalised angular total depth. It can be compared across systems. 
        /// It measures how close each segment is to all others in terms of the sum of angular changes that are made on each route.
        /// </summary>
        public Dictionary<int, double> VisualIntegration { get; }


        /// <summary>
        /// Angular mean depth is the sum of the shortest angular paths over the sum of all angular intersections in the system. 
        /// In DepthMap, it is defined as the sum of the shortest angular paths over the sum of the number of segments encountered 
        /// on the paths from the root (origin) segment to all others.
        /// </summary>
        public Dictionary<int, double> VisualMeanDepth { get; }


        /// <summary>
        /// Angular total depth is the cumulative total of the shortest angular paths to a selected segment as root.
        /// </summary>
        public Dictionary<int, double> VisualTotalDepth { get; }

        public Dictionary<int, double>  VisualNodeCount { get; }
        /// <summary>
        /// Normalised choice aims to solve the paradox that segregated designs add more total (and average) choice to the system than 
        /// integrated ones. It divides total choice by total depth for each segment in the system. This adjusts choice values according
        /// to the depth of each segment in the system, since the more segregated is, the more its choice value with be reduced by being 
        /// divided by a higher total depth number. This would seem to have the effect of measuring choice in a cost-benefit way.
        /// </summary>
        public Dictionary<int, double> NormalisedVisualChoice { get; }


        /// <summary>
        /// Normalised angular integration aims to normalise angular total depth by comparing the system to the urban average.
        /// </summary>
        public Dictionary<int, double> NormalisedVisualIntegration { get; }

        public VisualSyntaxComputing(UndirectedWeightedSparseGraph<int> metricGraph, UndirectedWeightedSparseGraph<int> visualGraph, double radius = double.PositiveInfinity)
        {
            radius = radius==-1?double.PositiveInfinity:radius;
            
            if (radius == double.PositiveInfinity) // Global space syntax.
            {
                var computeVisual = new CalculateCentrality<UndirectedWeightedSparseGraph<int>, int>(visualGraph);


                VisualChoice = computeVisual.Betweenness;
                VisualIntegration = ComputeIntegration(computeVisual.TotalDepths, computeVisual.NodeCounts);
                VisualMeanDepth = ComputeMeanDepth(computeVisual.TotalDepths, computeVisual.NodeCounts);
                VisualTotalDepth = new Dictionary<int, double>(computeVisual.TotalDepths);

                VisualNodeCount = computeVisual.NodeCounts.ToDictionary(entry => entry.Key,
                                                       entry => entry.Value);
                
                NormalisedVisualChoice = ComputeNACH(computeVisual.TotalDepths, VisualChoice);
                NormalisedVisualIntegration = ComputeNAIN(computeVisual.TotalDepths, computeVisual.NodeCounts);
            }
            else
            {
                // Has radius.
                // Step1: compute metric graph.
                var computeMetric = new CalculateCentrality<UndirectedWeightedSparseGraph<int>, int>(metricGraph, radius);

                // Step2: get sub graph within radius.
                var subGraphs = computeMetric.SubGraphs;
                var computeVisual = new CalculateCentrality<UndirectedWeightedSparseGraph<int>, int>(visualGraph, subGraphs);

                VisualChoice = computeVisual.Betweenness;
                VisualIntegration = ComputeIntegration(computeVisual.TotalDepths, computeVisual.NodeCounts);
                VisualMeanDepth = ComputeMeanDepth(computeVisual.TotalDepths, computeVisual.NodeCounts);
                VisualTotalDepth = new Dictionary<int, double>(computeVisual.TotalDepths);

                VisualNodeCount = computeVisual.NodeCounts.ToDictionary(entry => entry.Key,
                                       entry => entry.Value);

                NormalisedVisualChoice = ComputeNACH(computeVisual.TotalDepths, VisualChoice);
                NormalisedVisualIntegration = ComputeNAIN(computeVisual.TotalDepths, computeVisual.NodeCounts);
            }
        }

        //TODO: 得出node 2
        private static Dictionary<int, double> ComputeIntegration(IDictionary<int, double> totalDepth, IDictionary<int, double> nodeCount)
        {
            var result = new Dictionary<int, double>(totalDepth.Count);

            foreach (var node in totalDepth.Keys)
            {
                if (totalDepth[node] == 0)
                {
                    result.Add(node, -1); // when total depth equals zero, integration is positive infinity which is invalid, therefore we using -1 to represent this result.
                    continue;
                }

                result.Add(node, nodeCount[node] * nodeCount[node]/totalDepth[node]);
            }

            return result;
        }


        private static Dictionary<int, double> ComputeMeanDepth(IDictionary<int, double> totalDepth, IDictionary<int, double> nodeCount)
        {
            var result = new Dictionary<int, double>(totalDepth);

            foreach (var i in totalDepth.Keys)
            {
                if (nodeCount[i] < 2)
                {
                    result[i] = -1;
                    continue;
                }
                result[i] = totalDepth[i] * 1.0 / (nodeCount[i] - 1);
            }

            return result;
        }


        private static Dictionary<int, double> ComputeNACH(IDictionary<int, double> angularTotalDepth, IDictionary<int, double> angularChoice)
        {
            var result = new Dictionary<int, double>(angularTotalDepth.Count);

            foreach (var node in angularTotalDepth.Keys)
            {
                result.Add(node, Math.Log(angularChoice[node] + 1) / Math.Log(angularTotalDepth[node] + 3));
            }

            return result;
        }


        private static Dictionary<int, double> ComputeNAIN(IDictionary<int, double> angularTotalDepth, IDictionary<int, double> nodeCount)
        {
            //NATD=totalDepth/((nodeCount+2)^1.2)
            //NAIN = 1/ NATD 
            //NAIN = (nodeCount +2)^1.2 / totalDepth

            var result = new Dictionary<int, double>(angularTotalDepth.Count);

            foreach (var i in angularTotalDepth.Keys)
            {
                if (angularTotalDepth[i] == 0)
                {
                    result.Add(i, -1);
                    continue;
                }

                result.Add(i, Math.Pow(nodeCount[i] + 2, 1.2) / angularTotalDepth[i]);
            }

            return result;
        }


        [Obsolete("NAIN and NACH are the specific normalization.")]
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
