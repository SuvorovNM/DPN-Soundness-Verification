namespace DPN.SoundnessVerification.TransitionSystems.Converters;

public static class CoverabilityGraphToStateSpaceConverter
{
    public static StateSpaceAbstraction Convert(CoverabilityGraph coverabilityGraph)
    {
        return new StateSpaceAbstraction(
            coverabilityGraph.ConstraintStates
                .Select(s => new StateSpaceNode(s.Marking, s.Constraints, s.Id))
                .ToArray(),
            coverabilityGraph.ConstraintArcs
                .Select(a => new StateSpaceArc(a.Transition.IsSilent, a.Transition.NonRefinedTransitionId,
                    a.SourceState.Id, a.TargetState.Id, a.Transition.Label)).ToArray(),
            coverabilityGraph.IsFullGraph,
            TransitionSystemType.AbstractCoverabilityGraph);
    }
}