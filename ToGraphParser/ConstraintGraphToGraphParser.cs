﻿using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using DataPetriNetVerificationDomain.ConstraintGraphVisualized;
using Microsoft.Msagl.Drawing;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using ToGraphParser.Extensions;

namespace ToGraphParser
{
    public class LtsToGraphParser
    {
        public Graph FormGraphBasedOnCG(ConstraintGraphToVisualize constraintGraph)
        {
            Graph graph = new Graph();

            var states = AddStatesToGraph(constraintGraph, graph);
            AddArcsToGraph(constraintGraph, graph, states);

            return graph;
        }

        private Dictionary<int, string> AddStatesToGraph
            (ConstraintGraphToVisualize constraintGraph, 
            Graph graph)
        {
            var addedStates = new Dictionary<int, string>();
            foreach (var state in constraintGraph.ConstraintStates)
            {
                var tokens = string.Join(", ", state.Tokens
                    .Where(x => x.Value > 0)
                    .Select(x => x.Value > 1
                        ? x.Value.ToString() + x.Key
                        : x.Key));

                var constraintFormula = state.ConstraintFormula.Length > 500
                    ? state.ConstraintFormula.Substring(0, 500) + "..."
                    : state.ConstraintFormula;

                var nodeToAdd = new Node($"Id:{state.Id} [{tokens}] ({constraintFormula})");
                nodeToAdd.Attr.Shape = Shape.Box;

                if (state.StateType.HasFlag(ConstraintStateType.Initial))
                {
                    nodeToAdd.Attr.FillColor = Color.LightGray;
                }
                if (state.StateType.HasFlag(ConstraintStateType.Deadlock))
                {
                    nodeToAdd.Attr.FillColor = Color.Pink;
                }
                if (state.StateType.HasFlag(ConstraintStateType.Final))
                {
                    nodeToAdd.Attr.FillColor = Color.LightGreen;
                }
                if (state.StateType.HasFlag(ConstraintStateType.UncleanFinal))
                {
                    nodeToAdd.Attr.FillColor = Color.LightBlue;
                }
                if (state.StateType.HasFlag(ConstraintStateType.NoWayToFinalMarking))
                {
                    nodeToAdd.Attr.Color = Color.Red;
                }
                if (state.StateType.HasFlag(ConstraintStateType.StrictlyCovered))
                {
                    nodeToAdd.Attr.FillColor = Color.Red;
                }
                addedStates.Add(state.Id, nodeToAdd.LabelText);
                graph.AddNode(nodeToAdd);
            }

            return addedStates;
        }

        private static void AddArcsToGraph(ConstraintGraphToVisualize constraintGraph, Graph graph, Dictionary<int, string> addedStates)
        {
            foreach (var transition in constraintGraph.ConstraintArcs)
            {
                var transitionLabel = transition.IsSilent
                    ? $"τ({transition.TransitionName})"
                    : transition.TransitionName;
                graph.AddEdge(addedStates[transition.SourceStateId], transitionLabel, addedStates[transition.TargetStateId]);
            }
        }

        private string FormStringRepresentationOfBoolExpr(BoolExpr expression)
        {
            if (expression == null)
            {
                return string.Empty;
            }

            return expression.GetLogicalConnective() switch
            {
                LogicalConnective.Or => string.Join(" ∨\n", expression.Args.Select(x => FormStringRepresentationOfBoolExpr((BoolExpr)x))),
                LogicalConnective.And => string.Join(" ∧ ", expression.Args.Select(x => FormStringRepresentationOfBoolExpr((BoolExpr)x))),
                LogicalConnective.Empty => expression.ToString(),
                _ => throw new Exception("The logical connective is not supported for getting string representation")
            };

            throw new ArgumentException("The operation is not supported for parsing to string");
        }
    }
}
