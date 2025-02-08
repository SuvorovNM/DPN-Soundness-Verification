using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using DataPetriNetVerificationDomain.CoverabilityTreeVisualized;
using Microsoft.Msagl.Drawing;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetParsers
{
    // Refactor
    public class CoverabilityTreeToGraphParser
    {
        public Graph FormGraphBasedOnCt(CoverabilityTreeToVisualize coverabilityTree)
        {
            Graph graph = new Graph();

            var states = AddStatesToGraph(coverabilityTree, graph);
            AddArcsToGraph(coverabilityTree, graph, states);

            return graph;
        }

        private Dictionary<int, string> AddStatesToGraph
            (CoverabilityTreeToVisualize coverabilityTree,
            Graph graph)
        {
            var addedStates = new Dictionary<int, string>();
            foreach (var state in coverabilityTree.CtStates)
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
                nodeToAdd.Attr.FillColor = state.StateColor == CtStateColor.Green 
                    ? Color.LightGreen 
                    : state.StateColor == CtStateColor.Red ? Color.Pink : Color.White;

                addedStates.Add(state.Id, nodeToAdd.LabelText);
                graph.AddNode(nodeToAdd);
            }

            return addedStates;
        }

        private static void AddArcsToGraph(CoverabilityTreeToVisualize coverabilityTree, Graph graph, Dictionary<int, string> addedStates)
        {
            foreach (var transition in coverabilityTree.CtArcs)
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
