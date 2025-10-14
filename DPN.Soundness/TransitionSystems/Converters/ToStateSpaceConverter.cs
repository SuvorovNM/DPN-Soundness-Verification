using DPN.Models.DPNElements;
using DPN.Models.Extensions;
using DPN.Soundness.TransitionSystems.Coverability;
using DPN.Soundness.TransitionSystems.Reachability;
using DPN.Soundness.TransitionSystems.StateSpace;

namespace DPN.Soundness.TransitionSystems.Converters;

internal static class ToStateSpaceConverter
{
	internal static StateSpace.StateSpaceGraph Convert(CoverabilityGraph coverabilityGraph)
	{
		return new StateSpace.StateSpaceGraph(
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

	internal static StateSpace.StateSpaceGraph Convert(CoverabilityTree coverabilityTree)
	{
		return new StateSpace.StateSpaceGraph(
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

	internal static StateSpace.StateSpaceGraph Convert(LabeledTransitionSystem labeledTransitionSystem)
	{
		var extraTransitions = new List<Transition>(labeledTransitionSystem.DataPetriNet.Transitions);
		if (labeledTransitionSystem is ConstraintGraph)
		{
			var tauTransitions = labeledTransitionSystem.ConstraintArcs
				.Where(a => a.Transition.IsSilent)
				.Select(a => a.Transition.Id)
				.ToHashSet();

			foreach (var baseTransitionId in tauTransitions)
			{
				var baseTransition = labeledTransitionSystem.DataPetriNet.Transitions.Single(t => t.Id == baseTransitionId);

				var tauTransition = baseTransition.MakeTau()!;

				extraTransitions.Add(tauTransition);
			}
		}

		return new StateSpace.StateSpaceGraph(
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