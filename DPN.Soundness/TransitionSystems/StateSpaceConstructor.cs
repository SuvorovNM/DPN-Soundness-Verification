using DPN.Models;
using DPN.Soundness.TransitionSystems.Converters;
using DPN.Soundness.TransitionSystems.Coverability;
using DPN.Soundness.TransitionSystems.Reachability;

namespace DPN.Soundness.TransitionSystems;

public static class StateSpaceConstructor
{
	public static StateSpace.StateSpaceGraph ConstructCoverabilityGraph(DataPetriNet dpn, bool stopOnCoveringFinalPosition)
	{
		var cg = new CoverabilityGraph(dpn, stopOnCoveringFinalPosition: stopOnCoveringFinalPosition);
		cg.GenerateGraph();

		return ToStateSpaceConverter.Convert(cg);
	}
	
	public static StateSpace.StateSpaceGraph ConstructCoverabilityTree(DataPetriNet dpn, bool stopOnCoveringFinalPosition)
	{
		var ct = new CoverabilityTree(dpn, stopOnCoveringFinalPosition);
		ct.GenerateGraph();

		return ToStateSpaceConverter.Convert(ct);
	}
	
	public static StateSpace.StateSpaceGraph ConstructReachabilityGraph(DataPetriNet dpn)
	{
		var lts = new ReachabilityGraph(dpn);
		lts.GenerateGraph();

		return ToStateSpaceConverter.Convert(lts);
	}
	
	public static StateSpace.StateSpaceGraph ConstructConstraintGraph(DataPetriNet dpn)
	{
		var cg = new ConstraintGraph(dpn);
		cg.GenerateGraph();

		return ToStateSpaceConverter.Convert(cg);
	}
}