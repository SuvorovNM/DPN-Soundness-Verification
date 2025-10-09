namespace DPN.SoundnessVerification.TransitionSystems.Converters;

public class LabeledTransitionSystemToStateSpaceConverter
{
    public static StateSpaceAbstraction Convert(LabeledTransitionSystem labeledTransitionSystem)
    {
        return new StateSpaceAbstraction(
            labeledTransitionSystem.ConstraintStates
                .Select(s => new StateSpaceNode(s.Marking, s.Constraints, s.Id))
                .ToArray(),
            labeledTransitionSystem.ConstraintArcs
                .Select(a => new StateSpaceArc(a.Transition.IsSilent, a.Transition.NonRefinedTransitionId,
                    a.SourceState.Id, a.TargetState.Id, a.Transition.Label)).ToArray(),
            labeledTransitionSystem.IsFullGraph,
            TransitionSystemType.AbstractReachabilityGraph);
    }
}