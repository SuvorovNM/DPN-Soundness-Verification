using Microsoft.Z3;

namespace DPN.Soundness.TransitionSystems.StateSpace;

public class StateSpaceNode(Dictionary<string, int> marking, BoolExpr? stateConstraint, int id, bool isCovered = false)
{ 
    public Dictionary<string, int> Marking { get; } = marking;
    public BoolExpr? StateConstraint { get; } = stateConstraint;
    public int Id { get; } = id;
    public bool IsCovered { get; } = isCovered;
}