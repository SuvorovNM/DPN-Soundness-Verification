using DPN.Models.DPNElements;
using DPN.Soundness.TransitionSystems.Coverability;
using DPN.Soundness.TransitionSystems.Reachability;
using DPN.Soundness.TransitionSystems.StateSpaceGraph;

namespace DPN.Visualization.Models;

public class ArcToVisualize
{
	public string TransitionName { get; set; }
	public bool IsSilent { get; set; }
	public int SourceStateId { get; set; }
	public int TargetStateId { get; set; }

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