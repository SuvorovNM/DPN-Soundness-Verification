using DPN.SoundnessVerification.TransitionSystems;

namespace DPN.SoundnessVerification;

public class VerificationResult
{
    public StateSpaceAbstraction StateSpaceAbstraction { get; }
    public SoundnessProperties SoundnessProperties { get; }

    public VerificationResult(StateSpaceAbstraction stateSpaceAbstraction, SoundnessProperties soundnessProperties)
    {
        StateSpaceAbstraction = stateSpaceAbstraction;
        SoundnessProperties = soundnessProperties;
    }
}