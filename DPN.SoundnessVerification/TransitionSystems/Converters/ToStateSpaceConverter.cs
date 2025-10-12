using DPN.Models.DPNElements;
using DPN.Models.Enums;
using DPN.Models.Extensions;

namespace DPN.SoundnessVerification.TransitionSystems.Converters;

public static class ToStateSpaceConverter
{
    public static StateSpaceAbstraction Convert(CoverabilityGraph coverabilityGraph)
    {
        return new StateSpaceAbstraction(
            coverabilityGraph.ConstraintStates
                .Select(s => new StateSpaceNode(s.Marking.AsDictionary(), s.Constraints, s.Id))
                .ToArray(),
            coverabilityGraph.ConstraintArcs
                .Select(a => new StateSpaceArc(a.Transition.IsSilent, a.Transition.NonRefinedTransitionId,
                    a.SourceState.Id, a.TargetState.Id, a.Transition.Label)).ToArray(),
            coverabilityGraph.IsFullGraph,
            TransitionSystemType.AbstractCoverabilityGraph,
            coverabilityGraph.DataPetriNet.FinalMarking.AsDictionary(),
            coverabilityGraph.DataPetriNet.Transitions.ToArray(),
            coverabilityGraph.DataPetriNet.GetVariablesDictionary());
    }
    
    public static StateSpaceAbstraction Convert(CoverabilityTree coverabilityTree)
    {
        return new StateSpaceAbstraction(
            coverabilityTree.ConstraintStates
                .Select(s => new StateSpaceNode(s.Marking.AsDictionary(), s.Constraints, s.Id))
                .ToArray(),
            coverabilityTree.ConstraintArcs
                .Select(a => new StateSpaceArc(a.Transition.IsSilent, a.Transition.NonRefinedTransitionId,
                    a.SourceState.Id, a.TargetState.Id, a.Transition.Label)).ToArray(),
            true,
            TransitionSystemType.AbstractCoverabilityTree,
            coverabilityTree.DataPetriNet.FinalMarking.AsDictionary(),
            coverabilityTree.DataPetriNet.Transitions.ToArray(),
            coverabilityTree.DataPetriNet.GetVariablesDictionary());
    }
    
    public static StateSpaceAbstraction Convert(LabeledTransitionSystem labeledTransitionSystem)
    {
	    var extraTransitions = new List<Transition>(labeledTransitionSystem.DataPetriNet.Transitions);
	    if (labeledTransitionSystem is ConstraintGraph)
	    {
		    var tauTransitions = labeledTransitionSystem.ConstraintArcs
			    .Where(a => a.Transition.IsSilent)
			    .Select(a => a.Transition.Id)
			    .ToHashSet();

		    var context = labeledTransitionSystem.DataPetriNet.Context;
		    foreach (var baseTransitionId in tauTransitions)
		    {
			    var baseTransition = labeledTransitionSystem.DataPetriNet.Transitions.Single(t => t.Id == baseTransitionId);

			    var tauTransition = baseTransition.MakeTau()!;
				    
				/*    new Transition(
				    $"τ({baseTransition.Label})", 
				    new Guard(context, context.MkNot(context.GetReadExpression(baseTransition.Guard.ActualConstraintExpression, new Dictionary<string, DomainType>()))),
				    baseTransitionId);*/
			    extraTransitions.Add(tauTransition);
		    }
	    }
	    
        return new StateSpaceAbstraction(
            labeledTransitionSystem.ConstraintStates
                .Select(s => new StateSpaceNode(s.Marking.AsDictionary(), s.Constraints, s.Id))
                .ToArray(),
            labeledTransitionSystem.ConstraintArcs
                .Select(a => new StateSpaceArc(a.Transition.IsSilent, a.Transition.NonRefinedTransitionId,
                    a.SourceState.Id, a.TargetState.Id, a.Transition.Label)).ToArray(),
            labeledTransitionSystem.IsFullGraph,
            TransitionSystemType.AbstractReachabilityGraph,
            labeledTransitionSystem.DataPetriNet.FinalMarking.AsDictionary(),
            labeledTransitionSystem.DataPetriNet.Transitions.Union(extraTransitions).ToArray(),
            labeledTransitionSystem.DataPetriNet.GetVariablesDictionary());
    }
}