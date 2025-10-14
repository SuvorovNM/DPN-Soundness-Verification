using Microsoft.Z3;

namespace DPN.Soundness.TransitionSystems.StateSpace;

public class StateSpaceNode(Dictionary<string, int> marking, BoolExpr? stateConstraint, int id)
{ 
    public Dictionary<string, int> Marking { get; init; } = marking;
    public BoolExpr? StateConstraint { get; init; } = stateConstraint;
    public int Id { get; } = id;
}