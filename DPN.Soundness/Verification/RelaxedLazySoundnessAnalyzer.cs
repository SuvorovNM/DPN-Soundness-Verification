using DPN.Models;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using DPN.Models.Extensions;
using DPN.Soundness.TransitionSystems;
using DPN.Soundness.TransitionSystems.Coverability;
using DPN.Soundness.TransitionSystems.Reachability;
using DPN.Soundness.TransitionSystems.StateSpace;
using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;

namespace DPN.Soundness.Verification;

public static class RelaxedLazySoundnessAnalyzer
{
	public static SoundnessProperties CheckSoundness(StateSpaceGraph stateSpaceGraph)
	{
		var stateDictionary = stateSpaceGraph
			.Nodes.ToDictionary(x => x.Id, x => StateType.Default);

		var initialNodeKey = stateDictionary.Keys.Min();
		stateDictionary[initialNodeKey] |= StateType.Initial;
		
		var finalMarking = Marking.FromDictionary(stateSpaceGraph.FinalDpnMarking);

		var finalStates = stateSpaceGraph.Nodes
			.Where(x => x.Marking.All(y =>
				stateSpaceGraph.FinalDpnMarking[y.Key] == 0 ||
				stateSpaceGraph.FinalDpnMarking[y.Key] != 0 && y.Value == stateSpaceGraph.FinalDpnMarking[y.Key]))
			.ToArray();

		foreach (var finalState in finalStates)
		{
			stateDictionary[finalState.Id] |= StateType.Final;
		}
		
		var uncleanFinals = stateSpaceGraph.Nodes
			.Where(x => x.Marking.Any(y =>
				stateSpaceGraph.FinalDpnMarking[y.Key] != 0 && y.Value > stateSpaceGraph.FinalDpnMarking[y.Key]))
			.ToArray();

		foreach (var uncleanFinal in uncleanFinals)
		{
			stateDictionary[uncleanFinal.Id] |= StateType.UncleanFinal;
		}

		if (stateSpaceGraph.IsFullGraph)
		{
			var successors = stateSpaceGraph
				.Arcs
				.GroupBy(a => a.SourceNodeId)
				.ToDictionary(a => a.Key, a => a.ToArray());

			stateSpaceGraph.Nodes
				.Where(x => !stateDictionary[x.Id].HasFlag(StateType.Final) && !stateDictionary[x.Id].HasFlag(StateType.UncleanFinal))
				.Where(x => !successors.ContainsKey(x.Id))
				.ToList()
				.ForEach(x => stateDictionary[x.Id] |= StateType.Deadlock);

			var predecessors = stateSpaceGraph
				.Arcs
				.GroupBy(a => a.TargetNodeId)
				.ToDictionary(g => g.Key, g => g.Select(a => a.SourceNodeId).ToArray());

			var statesLeadingToFinals = new HashSet<int>(finalStates.Select(x => x.Id));
			var intermediateStates = new HashSet<int>(statesLeadingToFinals);
			do
			{
				intermediateStates = intermediateStates
					.Where(x => predecessors.ContainsKey(x))
					.SelectMany(x => predecessors[x])
					.Where(x => !statesLeadingToFinals.Contains(x))
					.ToHashSet();
				statesLeadingToFinals.AddRange(intermediateStates);
			} while (intermediateStates.Count > 0);
		}

		var unfeasibleTransitions = stateSpaceGraph.Arcs
			.GroupBy(a => a.BaseTransitionId)
			.ToDictionary(
				arcsGroup => arcsGroup.Key,
				arcsGroup =>
					arcsGroup.All(a => stateDictionary[a.TargetNodeId].HasFlag(StateType.NoWayToFinalMarking)))
			.Where(a => a.Value)
			.Select(a => a.Key)
			.Union(GetDeadTransitions(stateSpaceGraph))
			.ToArray();

		var hasDeadlocks = stateDictionary.Any(kvp=>kvp.Value.HasFlag(StateType.Deadlock));

		var isSound = unfeasibleTransitions.Length == 0;

		return new SoundnessProperties(
			SoundnessType.RelaxedLazy,
			stateDictionary,
			stateSpaceGraph.IsFullGraph,
			unfeasibleTransitions,
			hasDeadlocks,
			isSound);
		
		static string[] GetDeadTransitions(StateSpaceGraph stateSpaceGraph)
		{
			var deadTransitions = stateSpaceGraph.DpnTransitions
				.Select(x => x.BaseTransitionId)
				.Except(stateSpaceGraph.Arcs.Select(y => y.BaseTransitionId))
				.ToArray();
			return deadTransitions;
		}
	}

	internal static SoundnessProperties CheckSoundness(DataPetriNet dpn, CoverabilityGraph cg)
	{
		var stateDictionary =
			cg.ConstraintStates.ToDictionary(x => x as AbstractState, _ => StateType.Default);

		DefineInitialState(cg, stateDictionary);
		var finalMarking = dpn.Places.Where(x => x.IsFinal).Select(p=>p.Id).ToArray();

		var finalStates = cg.ConstraintStates
			.Where(x => x.Marking.Keys.Intersect(finalMarking).All(y => x.Marking[y] == 1))
			.ToArray();

		DefineFinals(stateDictionary, finalStates);
		DefineUncleanFinals(finalMarking, stateDictionary);

		if (cg.IsFullGraph)
		{
			DefineDeadlocks(cg, stateDictionary);
			DefineStatesWithNoWayToFinals(cg, stateDictionary, finalStates);
		}

		var unfeasibleTransitions = cg.ConstraintArcs
			.GroupBy(a => a.Transition.NonRefinedTransitionId)
			.ToDictionary(
				arcsGroup => arcsGroup.Key,
				arcsGroup =>
					arcsGroup.All(a => stateDictionary[a.TargetState].HasFlag(StateType.NoWayToFinalMarking)))
			.Where(a => a.Value)
			.Select(a => a.Key)
			.Union(GetDeadTransitions(dpn, cg))
			.ToArray();

		var hasDeadlocks = cg.ConstraintStates.Aggregate(false,
			(current, constraintState) =>
				current | stateDictionary[constraintState].HasFlag(StateType.Deadlock));

		var isSound = unfeasibleTransitions.Length == 0;
		
		return new SoundnessProperties(
			SoundnessType.RelaxedLazy,
			stateDictionary.ToDictionary(x => x.Key.Id, x => x.Value),
			cg.IsFullGraph,
			unfeasibleTransitions,
			hasDeadlocks,
			isSound);
	}

	internal static SoundnessProperties CheckSoundness(DataPetriNet dpn, CoverabilityTree ct)
	{
		var stateDictionary =
			ct.ConstraintStates.ToDictionary(x => x as AbstractState, _ => StateType.Default);

		DefineInitialState(ct, stateDictionary);
		var finalMarking = dpn.Places.Where(x => x.IsFinal).Select(p=>p.Id).ToArray();

		var finalStates = ct.ConstraintStates
			.Where(x => x.Marking.Keys.Intersect(finalMarking).All(y => x.Marking[y] == 1))
			.ToArray();

		DefineFinals(stateDictionary, finalStates);
		DefineUncleanFinals(finalMarking, stateDictionary);


		DefineDeadlocks(ct, stateDictionary);
		DefineStatesWithNoWayToFinals(ct, stateDictionary, finalStates);


		var unfeasibleTransitions = ct.ConstraintArcs
			.GroupBy(a => a.Transition.NonRefinedTransitionId)
			.ToDictionary(
				arcsGroup => arcsGroup.Key,
				arcsGroup =>
					arcsGroup.All(a => stateDictionary[a.TargetState].HasFlag(StateType.NoWayToFinalMarking)))
			.Where(a => a.Value)
			.Select(a => a.Key)
			.Union(GetDeadTransitions(dpn, ct))
			.ToArray();

		var hasDeadlocks = ct.ConstraintStates.Aggregate(false,
			(current, constraintState) =>
				current | stateDictionary[constraintState].HasFlag(StateType.Deadlock));

		var isSound = unfeasibleTransitions.Length == 0;

		var isBounded = ct.ConstraintStates.All(s => s.StateType != CtStateType.StrictlyCovered);

		return new SoundnessProperties(
			SoundnessType.RelaxedLazy,
			stateDictionary.ToDictionary(x => x.Key.Id, x => x.Value),
			isBounded,
			unfeasibleTransitions,
			hasDeadlocks,
			isSound);
	}

	private static string[] GetDeadTransitions<TAbsState, TAbsTransition, TAbsArc>(DataPetriNet dpn,
		AbstractStateSpaceStructure<TAbsState, TAbsTransition, TAbsArc> cg)
		where TAbsState : AbstractState, new()
		where TAbsTransition : AbstractTransition
		where TAbsArc : AbstractArc<TAbsState, TAbsTransition>
	{
		var deadTransitions = dpn.Transitions
			.Select(x => x.BaseTransitionId)
			.Except(cg.ConstraintArcs.Select(y => y.Transition.NonRefinedTransitionId))
			.ToArray();

		return deadTransitions;
	}


	private static void DefineDeadlocks(CoverabilityGraph cg,
		Dictionary<AbstractState, StateType> stateDictionary)
	{
		cg.ConstraintStates
			.Where(x => !stateDictionary[x].HasFlag(StateType.Final) &&
			            !stateDictionary[x].HasFlag(StateType.UncleanFinal))
			.Where(x => cg.ConstraintArcs.All(y => y.SourceState != x))
			.ToList()
			.ForEach(x => stateDictionary[x] |= StateType.Deadlock);
	}

	private static void DefineDeadlocks(CoverabilityTree ct,
		Dictionary<AbstractState, StateType> stateDictionary)
	{
		ct.ConstraintStates
			.Where(x => !stateDictionary[x].HasFlag(StateType.Final) &&
			            !stateDictionary[x].HasFlag(StateType.UncleanFinal))
			.Where(x => x.StateType == CtStateType.NonCovered && ct.ConstraintArcs.All(y => y.SourceState != x))
			.ToList()
			.ForEach(x => stateDictionary[x] |= StateType.Deadlock);
	}

	// Доработать
	private static void DefineStatesWithNoWayToFinals(
		CoverabilityGraph cg,
		Dictionary<AbstractState, StateType> stateDictionary,
		LtsState[] finalStates)

	{
		var statesLeadingToFinals = new List<LtsState>(finalStates);
		var intermediateStates = new List<LtsState>(finalStates);
		var stateIncidenceDict = cg.ConstraintArcs
			.GroupBy(x => x.TargetState)
			.ToDictionary(x => x.Key, y => y.Select(x => x.SourceState).ToList());

		do
		{
			var nextStates = intermediateStates
				.Where(x => stateIncidenceDict.ContainsKey(x))
				.SelectMany(x => stateIncidenceDict[x])
				.Where(x => !statesLeadingToFinals.Contains(x))
				.Distinct();
			statesLeadingToFinals.AddRange(intermediateStates);
			intermediateStates = new List<LtsState>(nextStates);
		} while (intermediateStates.Count > 0);

		cg.ConstraintStates
			.Except(statesLeadingToFinals)
			.ToList()
			.ForEach(x => stateDictionary[x] |= StateType.NoWayToFinalMarking);
	}

	private static void DefineStatesWithNoWayToFinals(
		CoverabilityTree ct,
		Dictionary<AbstractState, StateType> stateDictionary,
		CtState[] finalStates)

	{
		var statesLeadingToFinals = new HashSet<CtState>(finalStates);
		var intermediateStates = new HashSet<CtState>(finalStates);
		var stateIncidenceDict = ct.ConstraintArcs
			.GroupBy(x => x.TargetState)
			.ToDictionary(x => x.Key, y => y.Select(x => x.SourceState).ToList());

		do
		{
			var nextStates = intermediateStates
				.Where(x => stateIncidenceDict.ContainsKey(x))
				.SelectMany(x => stateIncidenceDict[x])
				.Where(x => !statesLeadingToFinals.Contains(x))
				.ToHashSet();

			var covered = ct
				.ConstraintStates
				.Where(s => s.CoveredNode != null && nextStates.Contains(s.CoveredNode) &&
				            !statesLeadingToFinals.Contains(s.CoveredNode));

			statesLeadingToFinals.AddRange(intermediateStates);
			intermediateStates = nextStates.Union(covered).ToHashSet();
		} while (intermediateStates.Count > 0);

		ct.ConstraintStates
			.Except(statesLeadingToFinals)
			.ToList()
			.ForEach(x => stateDictionary[x] |= StateType.NoWayToFinalMarking);
	}

	private static void DefineUncleanFinals(
		string[] terminalNodesIds,
		Dictionary<AbstractState, StateType> stateDictionary)
	{
		var uncleanFinalNodes = stateDictionary
			.Where(x => x.Key.Marking.Keys.Intersect(terminalNodesIds).Any(y => x.Key.Marking[y] > 1))
			.Select(x => x.Key);

		foreach (var cgNode in uncleanFinalNodes)
		{
			if (cgNode.Marking.Keys.Intersect(terminalNodesIds).Any(y => cgNode.Marking[y] > 1))
			{
				stateDictionary[cgNode] |= StateType.UncleanFinal;
			}
		}
	}

	private static void DefineFinals(Dictionary<AbstractState, StateType> stateDictionary,
		LtsState[] finalStates)
	{
		Array.ForEach(finalStates, x => stateDictionary[x] |= StateType.Final);
	}

	private static void DefineInitialState(CoverabilityGraph cg,
		Dictionary<AbstractState, StateType> stateDictionary)
	{
		stateDictionary[cg.InitialState] |= StateType.Initial;
	}

	private static void DefineInitialState(CoverabilityTree ct,
		Dictionary<AbstractState, StateType> stateDictionary)
	{
		stateDictionary[ct.InitialState] |= StateType.Initial;
	}

	private static void DefineFinals(Dictionary<AbstractState, StateType> stateDictionary,
		CtState[] finalStates)
	{
		Array.ForEach(finalStates, x => stateDictionary[x] |= StateType.Final);
	}
}