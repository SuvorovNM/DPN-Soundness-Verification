using DPN.Soundness.TransitionSystems.StateSpaceGraph;

namespace DPN.Soundness;

public class VerificationResult(
	StateSpaceAbstraction stateSpaceAbstraction,
	SoundnessProperties soundnessProperties,
	TimeSpan? verificationTime = null)
{
    public StateSpaceAbstraction StateSpaceAbstraction { get; } = stateSpaceAbstraction;
    public SoundnessProperties SoundnessProperties { get; } = soundnessProperties;
    public TimeSpan? VerificationTime { get; } = verificationTime;
}