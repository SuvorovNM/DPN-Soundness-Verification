using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetOnSmt.Visualization.Extensions;
using Microsoft.Msagl.Drawing;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.Visualization.Services
{
    public class ConstraintGraphToGraphParser
    {
        public Graph FormGraphBasedOnCG(ConstraintGraph constraintGraph, Dictionary<StateType, List<ConstraintState>> typedStates)
        {
            Graph graph = new Graph();

            var correctedTypedStates = SetSingleStateTypeForState(typedStates);

            var addedStates = AddStatesToGraph(constraintGraph, graph, correctedTypedStates);
            AddArcsToGraph(constraintGraph, graph, addedStates);

            return graph;
        }

        private static void AddArcsToGraph(ConstraintGraph constraintGraph, Graph graph, Dictionary<ConstraintState, string> addedStates)
        {
            foreach (var transition in constraintGraph.ConstraintArcs)
            {
                var transitionLabel = transition.Transition.IsSilent
                    ? $"τ({transition.Transition.Label})"
                    : transition.Transition.Label;
                graph.AddEdge(addedStates[transition.SourceState], transitionLabel, addedStates[transition.TargetState]);
            }
        }

        private Dictionary<ConstraintState, string> AddStatesToGraph(ConstraintGraph constraintGraph, Graph graph, Dictionary<ConstraintState, StateType> correctedTypedStates)
        {
            var addedStates = new Dictionary<ConstraintState, string>();
            foreach (var state in constraintGraph.ConstraintStates)
            {
                var tokens = string.Join(", ", state.PlaceTokens
                    .Where(x => x.Value > 0)
                    .Select(x => x.Value > 1
                        ? x.Value.ToString() + x.Key.Label
                        : x.Key.Label));
                var nodeToAdd = new Node($"{{{tokens}}} {{{FormStringRepresentationOfBoolExpr(state.Constraints)}}}");
                nodeToAdd.Attr.Shape = Shape.Box;

                nodeToAdd.Attr.FillColor = correctedTypedStates[state] switch
                {
                    StateType.Initial => Color.Gray,
                    StateType.Deadlock => Color.Pink,
                    StateType.CleanFinal => Color.LightGreen,
                    StateType.UncleanFinal => Color.Blue,
                    _ => Color.White
                };

                addedStates.Add(state, nodeToAdd.LabelText);
                graph.AddNode(nodeToAdd);
            }

            return addedStates;
        }

        private static Dictionary<ConstraintState, StateType> SetSingleStateTypeForState(Dictionary<StateType, List<ConstraintState>> typedStates)
        {
            var correctedTypedStates = new Dictionary<ConstraintState, StateType>();

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
                LogicalConnective.Empty => GetExpressionString(expression),
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
