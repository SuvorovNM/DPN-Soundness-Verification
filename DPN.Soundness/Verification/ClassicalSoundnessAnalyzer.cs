using DPN.Models;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using DPN.Models.Extensions;
using DPN.Soundness.TransitionSystems;
using DPN.Soundness.TransitionSystems.Reachability;
using DPN.Soundness.TransitionSystems.StateSpace;
using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;

namespace DPN.Soundness.Verification;

public static class ClassicalSoundnessAnalyzer
{
	public static SoundnessProperties CheckSoundness(StateSpaceGraph stateSpaceGraph)
	{
		var boundedness = stateSpaceGraph.IsFullGraph;
		var stateTypes = boundedness
			? GetStatesDividedByTypes(stateSpaceGraph)
			: stateSpaceGraph.Nodes.ToDictionary(x => x.Id, y => StateType.Default);

		var deadTransitions = GetDeadTransitions(stateSpaceGraph);

		var hasDeadlocks = false;
		var isFinalMarkingAlwaysReachable = true;
		var isFinalMarkingClean = true;

		foreach (var state in stateSpaceGraph.Nodes)
		{
			hasDeadlocks |= stateTypes[state.Id].HasFlag(StateType.Deadlock);
			isFinalMarkingAlwaysReachable &=
				!stateTypes[state.Id].HasFlag(StateType.NoWayToFinalMarking);
			isFinalMarkingClean &= !stateTypes[state.Id].HasFlag(StateType.UncleanFinal);
		}

		var isSound = boundedness
		              && !hasDeadlocks
		              && isFinalMarkingAlwaysReachable
		              && isFinalMarkingClean
		              && deadTransitions.Length == 0;

		return new SoundnessProperties(
			SoundnessType.Classical,
			stateTypes,
			stateSpaceGraph.IsFullGraph,
			deadTransitions,
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

	internal static SoundnessProperties CheckSoundness(DataPetriNet dpn, LabeledTransitionSystem cg)
	{
		var boundedness = cg.IsFullGraph;
		var stateTypes = boundedness
			? GetStatesDividedByTypesNew(cg, dpn.FinalMarking.AsDictionary())
			: cg.ConstraintStates.ToDictionary(x => (AbstractState)x, y => StateType.Default);

		var deadTransitions = GetDeadTransitions(dpn, cg);

		var hasDeadlocks = false;
		var isFinalMarkingAlwaysReachable = true;
		var isFinalMarkingClean = true;

		foreach (var constraintState in cg.ConstraintStates)
		{
			hasDeadlocks |= stateTypes[constraintState].HasFlag(StateType.Deadlock);
			isFinalMarkingAlwaysReachable &=
				!stateTypes[constraintState].HasFlag(StateType.NoWayToFinalMarking);
			isFinalMarkingClean &= !stateTypes[constraintState].HasFlag(StateType.UncleanFinal);
		}

		var isSound = boundedness
		              && !hasDeadlocks
		              && isFinalMarkingAlwaysReachable
		              && isFinalMarkingClean
		              && deadTransitions.Length == 0;

		return new SoundnessProperties(
			SoundnessType.Classical,
			stateTypes.ToDictionary(x => x.Key.Id, x => x.Value),
			cg.IsFullGraph,
			deadTransitions,
			hasDeadlocks,
			isSound);
	}

	internal static Dictionary<AbstractState, StateType> GetStatesDividedByTypesNew
		(LabeledTransitionSystem graph, Dictionary<string, int> finalMarking)
	{
		var stateDictionary =
			graph.ConstraintStates.ToDictionary(x => (AbstractState)x, y => StateType.Default);

		DefineInitialState(stateDictionary);

		var finalStates = graph.ConstraintStates
			.Where(x => x.Marking.AsDictionary()
				.All(y => y.Value == finalMarking[y.Key]))
			.ToArray();


		DefineFinals(stateDictionary, finalStates);
		DefineUncleanFinals(finalMarking, stateDictionary);

		DefineDeadlocks(stateDictionary);
		DefineStatesWithNoWayToFinals(stateDictionary, finalStates);

		return stateDictionary;

		void DefineDeadlocks(Dictionary<AbstractState, StateType> stateDictionary)
		{
			graph.ConstraintStates
				.Where(x => !stateDictionary[x].HasFlag(StateType.Final) && !stateDictionary[x].HasFlag(StateType.UncleanFinal))
				.Where(x => graph.ConstraintArcs.All(y => y.SourceState != x))
				.ToList()
				.ForEach(x => stateDictionary[x] |= StateType.Deadlock);
		}

		void DefineStatesWithNoWayToFinals(Dictionary<AbstractState, StateType> stateDictionary,
			IEnumerable<LtsState> finalStates)

		{
			var statesLeadingToFinals = new List<LtsState>(finalStates);
			var intermediateStates = new List<LtsState>(finalStates);
			var stateIncidenceDict = graph.ConstraintArcs
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

			graph.ConstraintStates
				.Except(statesLeadingToFinals)
				.ToList()
				.ForEach(x => stateDictionary[x] |= StateType.NoWayToFinalMarking);
		}

		static void DefineUncleanFinals(Dictionary<string, int> finalMarking, Dictionary<AbstractState, StateType> stateDictionary)
		{
			var uncleanFinals = stateDictionary.Keys
				.Where(x => x.Marking.AsDictionary().All(y => y.Value >= finalMarking[y.Key]) &&
				            x.Marking.AsDictionary().Any(y => y.Value > finalMarking[y.Key]))
				.ToArray();

			foreach (var uncleanFinal in uncleanFinals)
			{
				stateDictionary[uncleanFinal] |= StateType.UncleanFinal;
			}
		}

		void DefineFinals(Dictionary<AbstractState, StateType> stateDictionary,
			LtsState[] finalStates)
		{
			finalStates
				.ToList()
				.ForEach(x => stateDictionary[x] |= StateType.Final);
		}

		void DefineInitialState(Dictionary<AbstractState, StateType> stateDictionary)
		{
			stateDictionary[graph.InitialState] |= StateType.Initial;
		}
	}

	private static string[] GetDeadTransitions(DataPetriNet dpn, LabeledTransitionSystem cg)
	{
		var deadTransitions = dpn.Transitions
			.Select(x => x.BaseTransitionId)
			.Except(cg.ConstraintArcs.Select(y => y.Transition.NonRefinedTransitionId))
			.ToArray();
		return deadTransitions;
	}

	private static Dictionary<int, StateType> GetStatesDividedByTypes
		(StateSpaceGraph stateSpaceGraph)
	{
		var stateDictionary = stateSpaceGraph
			.Nodes.ToDictionary(x => x.Id, x => StateType.Default);

		var initialNodeKey = stateDictionary.Keys.Min();
		stateDictionary[initialNodeKey] |= StateType.Initial;

		var finalMarking = Marking.FromDictionary(stateSpaceGraph.FinalDpnMarking);

		var finalStates = stateSpaceGraph.Nodes
			.Where(x => Marking.FromDictionary(x.Marking).CompareTo(finalMarking) == MarkingComparisonResult.Equal)
			.ToArray();

		foreach (var finalState in finalStates)
		{
			stateDictionary[finalState.Id] |= StateType.Final;
		}

		var uncleanFinals = stateSpaceGraph.Nodes
			.Where(x => Marking.FromDictionary(x.Marking).CompareTo(finalMarking) == MarkingComparisonResult.GreaterThan)
			.ToArray();

		foreach (var uncleanFinal in uncleanFinals)
		{
			stateDictionary[uncleanFinal.Id] |= StateType.UncleanFinal;
		}

		if (stateSpaceGraph.Arcs.Length == 0)
		{
			return stateDictionary;
		}

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

		stateDictionary.Keys
			.Except(statesLeadingToFinals)
			.ToList()
			.ForEach(x => stateDictionary[x] |= StateType.NoWayToFinalMarking);

		return stateDictionary;
	}
}