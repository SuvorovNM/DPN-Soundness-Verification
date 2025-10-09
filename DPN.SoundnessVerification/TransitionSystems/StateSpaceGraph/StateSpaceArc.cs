namespace DPN.SoundnessVerification.TransitionSystems;

public class StateSpaceArc
{
    public bool IsSilent { get; init; }
    public string BaseTransitionId { get; init; }
    public int SourceNodeId { get; init; }
    public int TargetNodeId { get; init; }
    public string Label { get; init; }

    public StateSpaceArc(
        bool isSilent,
        string baseTransitionId,
        int sourceNodeId,
        int targetNodeId,
        string label)
    {
        IsSilent = isSilent;
        BaseTransitionId = baseTransitionId;
        SourceNodeId = sourceNodeId;
        TargetNodeId = targetNodeId;
        Label = label;
    }
}