using DPN.Models.DPNElements;
using DPN.Soundness.TransitionSystems.CoverabilityTree;
using DPN.Soundness.TransitionSystems.LabeledTransitionSystems;
using DPN.Soundness.TransitionSystems.StateSpaceGraph;

namespace DPN.Visualization.Models;

public class ArcToVisualize
{
    public string TransitionName { get; set; }
    public bool IsSilent { get; set; }
    public int SourceStateId { get; set; }
    public int TargetStateId { get; set; }

    public static ArcToVisualize FromArc(LtsArc arc)
    {
        return new ArcToVisualize
        {
            TransitionName = arc.Transition.Label,
            IsSilent = arc.Transition.IsSilent,
            SourceStateId = arc.SourceState.Id,
            TargetStateId = arc.TargetState.Id
        };
    }
    
    public static ArcToVisualize FromArc(CtArc arc)
    {
        return new ArcToVisualize
        {
            TransitionName = arc.Transition.Label,
            IsSilent = arc.Transition.IsSilent,
            SourceStateId = arc.SourceState.Id,
            TargetStateId = arc.TargetState.Id
        };
    }

    public static ArcToVisualize FromArc(StateSpaceArc arc)
    {
	    return new ArcToVisualize
	    {
		    TransitionName = arc.Label,
		    IsSilent = arc.IsSilent,
		    SourceStateId = arc.SourceNodeId,
		    TargetStateId = arc.TargetNodeId
	    };
    }
}