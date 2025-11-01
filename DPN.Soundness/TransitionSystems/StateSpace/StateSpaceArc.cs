namespace DPN.Soundness.TransitionSystems.StateSpace;

public class StateSpaceArc(
	bool isSilent,
	string baseTransitionId,
	int sourceNodeId,
	int targetNodeId,
	string label)
{
    public bool IsSilent { get; init; } = isSilent;
    public string BaseTransitionId { get; init; } = baseTransitionId;
    public int SourceNodeId { get; init; } = sourceNodeId;
    public int TargetNodeId { get; init; } = targetNodeId;
    public string Label { get; init; } = label;
}