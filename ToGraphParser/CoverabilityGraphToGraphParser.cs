using DataPetriNetOnSmt.Enums;
using DataPetriNetVerificationDomain.CoverabilityGraphVisualized;
using Microsoft.Msagl.Drawing;

namespace DataPetriNetParsers;

public class CoverabilityGraphToGraphParser
{
    public Graph FormGraphBasedOnCg(CoverabilityGraphToVisualize coverabilityGraph)
    {
        var graph = new Graph();

        var states = AddStatesToGraph(coverabilityGraph, graph);
        AddArcsToGraph(coverabilityGraph, graph, states);

        return graph;
    }

    private Dictionary<int, string> AddStatesToGraph(CoverabilityGraphToVisualize coverabilityGraph, Graph graph)
    {
        var addedStates = new Dictionary<int, string>();
        foreach (var state in coverabilityGraph.CgStates)
        {
            var tokens = string.Join(", ", state.Tokens
                .Where(x => x.Value > 0)
                .Select(GetVisualizedPlaceMarking));

            var constraintFormula = state.ConstraintFormula.Length > 500
                ? state.ConstraintFormula.Substring(0, 500) + "..."
                : state.ConstraintFormula;

            var nodeToAdd = new Node($"Id:{state.Id} [{tokens}] ({constraintFormula})");
            nodeToAdd.Attr.Shape = Shape.Box;
            nodeToAdd.Attr.FillColor = state.StateColor == CtStateColor.Green
                ? Color.LightGreen
                : state.StateColor == CtStateColor.Red
                    ? Color.Pink
                    : Color.White;

            addedStates.Add(state.Id, nodeToAdd.LabelText);
            graph.AddNode(nodeToAdd);
        }

        return addedStates;
    }

    private static string GetVisualizedPlaceMarking(KeyValuePair<string, int> x)
    {
        return x.Value switch
        {
            1 => x.Key,
            int.MaxValue => "ω" + x.Key,
            _ => x.Value + x.Key
        };
    }

    private static void AddArcsToGraph(CoverabilityGraphToVisualize coverabilityGraph, Graph graph,
        Dictionary<int, string> addedStates)
    {
        foreach (var transition in coverabilityGraph.CgArcs)
        {
            var transitionLabel = transition.IsSilent
                ? $"τ({transition.TransitionName})"
                : transition.TransitionName;
            graph.AddEdge(addedStates[transition.SourceStateId], transitionLabel,
                addedStates[transition.TargetStateId]);
        }
    }
}