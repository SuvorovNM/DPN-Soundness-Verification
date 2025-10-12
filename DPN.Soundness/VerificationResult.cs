using DPN.Soundness.TransitionSystems.StateSpaceGraph;

namespace DPN.Soundness;

public class VerificationResult
{
    public StateSpaceAbstraction StateSpaceAbstraction { get; }
    public SoundnessProperties SoundnessProperties { get; }
    public TimeSpan? VerificationTime { get; }

    public VerificationResult(
	    StateSpaceAbstraction stateSpaceAbstraction, 
	    SoundnessProperties soundnessProperties,
	    TimeSpan? verificationTime = null)
    {
        StateSpaceAbstraction = stateSpaceAbstraction;
        SoundnessProperties = soundnessProperties;
        VerificationTime = verificationTime;
    }
}