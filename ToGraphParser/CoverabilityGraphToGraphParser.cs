using DataPetriNetOnSmt.Enums;
using DataPetriNetVerificationDomain;
using DataPetriNetVerificationDomain.CoverabilityGraphVisualized;
using DataPetriNetVerificationDomain.GraphVisualized;
using Microsoft.Msagl.Drawing;

namespace DataPetriNetParsers;

public class CoverabilityGraphToGraphParser
{
    public Graph FormGraphBasedOnCg(CoverabilityGraphToVisualize coverabilityGraph, SoundnessType soundnessType)
    {
        var graph = new Graph();

        var states = AddStatesToGraph(coverabilityGraph.CgStates, graph, soundnessType);
        AddArcsToGraph(coverabilityGraph, graph, states);

        return graph;
    }

    private Dictionary<int, string> AddStatesToGraph(
        List<StateToVisualize> states, 
        Graph graph,
        SoundnessType soundnessType)
    {
        var addedStates = new Dictionary<int, string>();
        foreach (var state in states)
        {
            var tokens = string.Join(", ", state.Tokens
                .Where(x => x.Value > 0)
                .Select(GetVisualizedPlaceMarking));

            var constraintFormula = state.ConstraintFormula.Length > 500
                ? state.ConstraintFormula.Substring(0, 500) + "..."
                : state.ConstraintFormula;

            var nodeToAdd = TransitionSystemNodeFormer.FormNode(state, tokens, constraintFormula, soundnessType);

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