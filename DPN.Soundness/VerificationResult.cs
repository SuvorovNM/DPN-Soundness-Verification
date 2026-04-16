using DPN.Soundness.TransitionSystems.StateSpace;

namespace DPN.Soundness;

public record VerificationResult(
	StateSpaceGraph StateSpaceGraph,
	SoundnessProperties SoundnessProperties,
	int TotalStatesConsidered,
	int TotalRefinementsDone = 0,
	TimeSpan? VerificationTime = null);