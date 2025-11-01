using DPN.Models.DPNElements;
using DPN.Models.Enums;

namespace DPN.Soundness.TransitionSystems.StateSpace;

public class StateSpaceGraph(
	StateSpaceNode[] nodes,
	StateSpaceArc[] arcs,
	bool isFullGraph,
	TransitionSystemType stateSpaceType,
	Dictionary<string, int> finalDpnMarking,
	Transition[] dpnTransitions,
	Dictionary<string, DomainType> typedVariables)
{
    public StateSpaceNode[] Nodes { get; set; } = nodes;
    public StateSpaceArc[] Arcs { get; set; } = arcs;
    public bool IsFullGraph { get; set; } = isFullGraph;
    public TransitionSystemType StateSpaceType { get; set; } = stateSpaceType;

    public Dictionary<string, int> FinalDpnMarking { get; set; } = finalDpnMarking;
    public Transition[] DpnTransitions { get; set; } = dpnTransitions;
    public Dictionary<string, DomainType> TypedVariables { get; set; } = typedVariables;
}