using Microsoft.Z3;

namespace DPN.Soundness.TransitionSystems.StateSpaceGraph;

public class StateSpaceNode
{ 
    public Dictionary<string, int> Marking { get; init; }
    public BoolExpr? StateConstraint { get; init; }
    public int Id { get; }

    public StateSpaceNode(Dictionary<string, int> marking, BoolExpr? stateConstraint, int id)
    {
        Marking = marking;
        StateConstraint = stateConstraint;
        Id = id;
    }
}