using DPN.Models.DPNElements;
using Microsoft.Z3;

namespace DPN.SoundnessVerification.TransitionSystems;

public class StateSpaceNode
{ 
    public Marking Marking { get; init; }
    public BoolExpr? StateConstraint { get; init; }
    public int Id { get; }

    public StateSpaceNode(Marking marking, BoolExpr? stateConstraint, int id)
    {
        Marking = marking;
        StateConstraint = stateConstraint;
        Id = id;
    }
}