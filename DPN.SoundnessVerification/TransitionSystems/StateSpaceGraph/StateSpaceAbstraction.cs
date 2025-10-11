using DPN.Models.DPNElements;
using DPN.Models.Enums;

namespace DPN.SoundnessVerification.TransitionSystems;

public class StateSpaceAbstraction
{
    public StateSpaceNode[] Nodes { get; set; }
    public StateSpaceArc[] Arcs { get; set; }
    public bool IsFullGraph { get; set; }
    public TransitionSystemType StateSpaceType { get; set; }
    
    public Dictionary<string, int> FinalDpnMarking { get; set; }
    public Transition[] DpnTransitions { get; set; }
    public Dictionary<string, DomainType> TypedVariables { get; set; }

    public StateSpaceAbstraction(
        StateSpaceNode[] nodes,
        StateSpaceArc[] arcs,
        bool isFullGraph,
        TransitionSystemType stateSpaceType,
        Dictionary<string, int> finalDpnMarking,
        Transition[] dpnTransitions,
        Dictionary<string, DomainType> typedVariables)
    {
        Nodes = nodes;
        Arcs = arcs;
        IsFullGraph = isFullGraph;
        StateSpaceType = stateSpaceType;
        FinalDpnMarking = finalDpnMarking;
        DpnTransitions = dpnTransitions;
        TypedVariables = typedVariables;
    }
}