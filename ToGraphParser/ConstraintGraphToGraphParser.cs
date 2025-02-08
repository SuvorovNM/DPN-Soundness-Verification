﻿using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetVerificationDomain;
using DataPetriNetVerificationDomain.GraphVisualized;
using Microsoft.Msagl.Drawing;

namespace DataPetriNetParsers
{
    public class LtsToGraphParser
    {
        public Graph FormGraphBasedOnCG(GraphToVisualize constraintGraph)
        {
            Graph graph = new Graph();

            var states = AddStatesToGraph(constraintGraph.States, graph);
            AddArcsToGraph(constraintGraph, graph, states);

            return graph;
        }

        private Dictionary<int, string> AddStatesToGraph(List<StateToVisualize> states, Graph graph)
        {
            var addedStates = new Dictionary<int, string>();
            foreach (var state in states)
            {
                var tokens = string.Join(", ", state.Tokens
                    .Where(x => x.Value > 0)
                    .Select(x => x.Value > 1
                        ? x.Value + x.Key
                        : x.Key));

                var constraintFormula = state.ConstraintFormula.Length > 500
                    ? state.ConstraintFormula.Substring(0, 500) + "..."
                    : state.ConstraintFormula;

                var nodeToAdd = TransitionSystemNodeFormer.FormNode(
                    state, 
                    tokens, 
                    constraintFormula,
                    SoundnessType.Classical);

                addedStates.Add(state.Id, nodeToAdd.LabelText);
                graph.AddNode(nodeToAdd);
            }

            return addedStates;
        }

        private static void AddArcsToGraph(GraphToVisualize constraintGraph, Graph graph,
            Dictionary<int, string> addedStates)
        {
            foreach (var transition in constraintGraph.Arcs)
            {
                var transitionLabel = transition.IsSilent
                    ? $"τ({transition.TransitionName})"
                    : transition.TransitionName;
                graph.AddEdge(addedStates[transition.SourceStateId], transitionLabel,
                    addedStates[transition.TargetStateId]);
            }
        }
    }
}