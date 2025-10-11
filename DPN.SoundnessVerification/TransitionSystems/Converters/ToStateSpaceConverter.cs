using DPN.Models.Extensions;

namespace DPN.SoundnessVerification.TransitionSystems.Converters;

public static class ToStateSpaceConverter
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
            TransitionSystemType.AbstractCoverabilityGraph,
            coverabilityGraph.DataPetriNet.FinalMarking,
            coverabilityGraph.DataPetriNet.Transitions.ToArray(),
            coverabilityGraph.DataPetriNet.GetVariablesDictionary());
    }
    
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
            TransitionSystemType.AbstractCoverabilityTree,
            coverabilityTree.DataPetriNet.FinalMarking,
            coverabilityTree.DataPetriNet.Transitions.ToArray(),
            coverabilityTree.DataPetriNet.GetVariablesDictionary());
    }
    
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
            TransitionSystemType.AbstractReachabilityGraph,
            labeledTransitionSystem.DataPetriNet.FinalMarking,
            labeledTransitionSystem.DataPetriNet.Transitions.ToArray(),
            labeledTransitionSystem.DataPetriNet.GetVariablesDictionary());
    }
}