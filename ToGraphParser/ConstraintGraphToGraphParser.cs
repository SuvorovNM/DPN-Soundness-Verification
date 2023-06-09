using DataPetriNetOnSmt.Enums;
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
        public Graph FormGraphBasedOnCG(LtsToVisualize constraintGraph)
        {
            Graph graph = new Graph();

            var states = AddStatesToGraph(constraintGraph, graph);
            AddArcsToGraph(constraintGraph, graph, states);

            return graph;
        }

        private Dictionary<int, string> AddStatesToGraph
            (LtsToVisualize constraintGraph, 
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

                var nodeToAdd = new Node($"[{tokens}] ({constraintFormula})");
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
                addedStates.Add(state.Id, nodeToAdd.LabelText);
                graph.AddNode(nodeToAdd);
            }

            return addedStates;
        }

        private static void AddArcsToGraph(LtsToVisualize constraintGraph, Graph graph, Dictionary<int, string> addedStates)
        {
            foreach (var transition in constraintGraph.ConstraintArcs)
            {
                var transitionLabel = transition.IsSilent
                    ? $"τ({transition.TransitionName})"
                    : transition.TransitionName;
                graph.AddEdge(addedStates[transition.SourceStateId], transitionLabel, addedStates[transition.TargetStateId]);
            }
        }

        public Graph FormGraphBasedOnCG(ConstraintGraph constraintGraph, Dictionary<StateType, List<LtsState>> typedStates)
        {
            Graph graph = new Graph();

            var correctedTypedStates = SetSingleStateTypeForState(typedStates);

            var addedStates = AddStatesToGraph(constraintGraph, graph, correctedTypedStates);
            AddArcsToGraph(constraintGraph, graph, addedStates);

            return graph;
        }

        private static void AddArcsToGraph(ConstraintGraph constraintGraph, Graph graph, Dictionary<LtsState, string> addedStates)
        {
            foreach (var transition in constraintGraph.ConstraintArcs)
            {
                var transitionLabel = transition.Transition.IsSilent
                    ? $"τ({transition.Transition.Label})"
                    : transition.Transition.Label;
                graph.AddEdge(addedStates[transition.SourceState], transitionLabel, addedStates[transition.TargetState]);
            }
        }

        private Dictionary<LtsState, string> AddStatesToGraph(ConstraintGraph constraintGraph, Graph graph, Dictionary<LtsState, StateType> correctedTypedStates)
        {
            var addedStates = new Dictionary<LtsState, string>();
            foreach (var state in constraintGraph.ConstraintStates)
            {
                var tokens = state.Marking.ToString();

                var formulaString = state.Constraints.ToString();

                var constraintFormula = formulaString.Length > 500
                    ? formulaString.Substring(0, 200) + "..."
                    : formulaString;

                var nodeToAdd = new Node($"[{tokens}] ({constraintFormula})");// FormStringRepresentationOfBoolExpr(state.Constraints)
                nodeToAdd.Attr.Shape = Shape.Box;

                nodeToAdd.Attr.FillColor = correctedTypedStates[state] switch
                {
                    StateType.Initial => Color.LightGray,
                    StateType.Deadlock => Color.Pink,
                    StateType.CleanFinal => Color.LightGreen,
                    StateType.UncleanFinal => Color.LightBlue,
                    _ => Color.White
                };
                if (correctedTypedStates[state] == StateType.NoWayToFinalMarking)
                {
                    nodeToAdd.Attr.Color = Color.Red;
                }
                addedStates.Add(state, nodeToAdd.LabelText);
                graph.AddNode(nodeToAdd);
            }

            return addedStates;
        }

        private static Dictionary<LtsState, StateType> SetSingleStateTypeForState(Dictionary<StateType, List<LtsState>> typedStates)
        {
            var correctedTypedStates = new Dictionary<LtsState, StateType>();

            foreach (var statesOfType in typedStates.OrderBy(x => x.Key))
            {
                correctedTypedStates.AddStatesForType(statesOfType);
            }

            return correctedTypedStates;
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

        private string GetExpressionString(BoolExpr expression) =>
            expression.GetBinaryPredicate() switch
            {
                BinaryPredicate.Unequal => expression.Args[0].Args[0].ToString() + "!=" + expression.Args[0].Args[1].ToString(),
                BinaryPredicate.Equal => expression.Args[0].ToString() + "=" + expression.Args[1].ToString(),
                BinaryPredicate.LessThan => expression.Args[0].ToString() + "<" + expression.Args[1].ToString(),
                BinaryPredicate.GreaterThan => expression.Args[0].ToString() + ">" + expression.Args[1].ToString(),
                BinaryPredicate.LessThanOrEqual => expression.Args[0].ToString() + "<=" + expression.Args[1].ToString(),
                BinaryPredicate.GreaterThanOrEqual => expression.Args[0].ToString() + ">=" + expression.Args[1].ToString(),
                _ => throw new Exception("The predicate is not supported for getting string representation")
            };
    }
}
