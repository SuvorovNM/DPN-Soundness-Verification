using DPN.Soundness.TransitionSystems.StateSpace;

namespace DPN.Soundness;

public class VerificationResult(
	StateSpaceGraph stateSpaceGraph,
	SoundnessProperties soundnessProperties,
	TimeSpan? verificationTime = null)
{
    public StateSpaceGraph StateSpaceGraph { get; } = stateSpaceGraph;
    public SoundnessProperties SoundnessProperties { get; } = soundnessProperties;
    public TimeSpan? VerificationTime { get; } = verificationTime;
}