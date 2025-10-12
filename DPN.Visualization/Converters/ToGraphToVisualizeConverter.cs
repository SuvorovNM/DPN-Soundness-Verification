using DPN.Models.Enums;
using DPN.Soundness;
using DPN.Soundness.TransitionSystems;
using DPN.Visualization.Models;

namespace DPN.Visualization.Converters;

public static class ToGraphToVisualizeConverter
{
	public static GraphToVisualize Convert(VerificationResult verificationResult)
	{
		var soundnessProperties = verificationResult.SoundnessProperties;
		var soundnessType = soundnessProperties.SoundnessType;
		bool? classicalSoundness = soundnessType == SoundnessType.Classical ? soundnessProperties.Soundness : null;
		bool? relaxedLazySoundness = soundnessType == SoundnessType.RelaxedLazy ? soundnessProperties.Soundness : null;
		
		var stateSpaceAbstraction = verificationResult.StateSpaceAbstraction;
		
		return new GraphToVisualize
		{
			States = stateSpaceAbstraction.Nodes
				.Select(x => StateToVisualize.FromNode(x,
					soundnessProperties.StateTypes.GetValueOrDefault(x.Id, ConstraintStateType.Default)))
				.ToArray(),

			Arcs = stateSpaceAbstraction.Arcs.Select(ArcToVisualize.FromArc).ToArray(),

			SoundnessProperties = new SoundnessPropertiesToVisualize(
				soundnessProperties.Boundedness,
				soundnessProperties.DeadTransitions,
				classicalSoundness,
				relaxedLazySoundness),

			IsFull = stateSpaceAbstraction.IsFullGraph,

			GraphType = ToGraphType(stateSpaceAbstraction.StateSpaceType)
		};
	}

	private static GraphType ToGraphType(TransitionSystemType transitionSystemType)
	{
		return transitionSystemType switch
		{
			TransitionSystemType.AbstractReachabilityGraph => GraphType.Lts,
			TransitionSystemType.AbstractCoverabilityGraph => GraphType.CoverabilityGraph,
			TransitionSystemType.AbstractCoverabilityTree => GraphType.CoverabilityTree,
			_ => throw new ArgumentOutOfRangeException(nameof(transitionSystemType), transitionSystemType, "Unsupported transition system type")
		};
	}
}