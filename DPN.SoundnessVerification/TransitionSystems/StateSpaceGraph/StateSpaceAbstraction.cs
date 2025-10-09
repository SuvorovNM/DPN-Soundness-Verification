namespace DPN.SoundnessVerification.TransitionSystems;

public class StateSpaceAbstraction
{
    public StateSpaceNode[] Nodes { get; set; }
    public StateSpaceArc[] Arcs { get; set; }
    public bool IsFullGraph { get; set; }
    public TransitionSystemType StateSpaceType { get; set; }

    public StateSpaceAbstraction(
        StateSpaceNode[] nodes,
        StateSpaceArc[] arcs,
        bool isFullGraph,
        TransitionSystemType stateSpaceType)
    {
        Nodes = nodes;
        Arcs = arcs;
        IsFullGraph = isFullGraph;
        StateSpaceType = stateSpaceType;
    }
}