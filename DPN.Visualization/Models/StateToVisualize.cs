using DPN.Models.Enums;
using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;
using DPN.Soundness.TransitionSystems.StateSpaceGraph;

namespace DPN.Visualization.Models;

public class StateToVisualize
{
    public int Id { get; private init; }
    public Dictionary<string, int> Tokens { get; private init; }
    public string ConstraintFormula { get; private init; }
    public ConstraintStateType StateType { get; private init; }
    
    public static StateToVisualize FromNode(StateSpaceNode node, ConstraintStateType stateType)
    {
	    return new StateToVisualize
	    {
		    Id = node.Id,
		    ConstraintFormula = node.StateConstraint?.ToString() ?? string.Empty,
		    Tokens = node.Marking,
		    StateType = stateType
	    };
    }
}