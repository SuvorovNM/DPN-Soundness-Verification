namespace DPN.SoundnessVerification.TransitionSystems.Converters;

public class CoverabilityTreeToStateSpaceConverter
{
    public static StateSpaceAbstraction Convert(CoverabilityTree coverabilityTree)
    {
        return new StateSpaceAbstraction(
            coverabilityTree.ConstraintStates
                .Select(s => new StateSpaceNode(s.Marking, s.Constraints, s.Id))
                .ToArray(),
            coverabilityTree.ConstraintArcs
                .Select(a => new StateSpaceArc(a.Transition.IsSilent, a.Transition.NonRefinedTransitionId,
                    a.SourceState.Id, a.TargetState.Id, a.Transition.Label)).ToArray(),
            true,
            TransitionSystemType.AbstractCoverabilityTree);
    }
}