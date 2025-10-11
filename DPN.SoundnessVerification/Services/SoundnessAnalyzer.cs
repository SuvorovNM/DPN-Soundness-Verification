using DPN.Models;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using DPN.Models.Extensions;
using DPN.SoundnessVerification.TransitionSystems;

namespace DPN.SoundnessVerification.Services;

public static class SoundnessAnalyzer
{
	public static SoundnessProperties CheckSoundness(StateSpaceAbstraction stateSpaceAbstraction)
	{
		var boundedness = stateSpaceAbstraction.IsFullGraph;
		var stateTypes = boundedness
			? GetStatesDividedByTypesNew(stateSpaceAbstraction)
			: stateSpaceAbstraction.Nodes.ToDictionary(x => x.Id, y => ConstraintStateType.Default);

		var deadTransitions = GetDeadTransitions(stateSpaceAbstraction);

		var hasDeadlocks = false;
		var isFinalMarkingAlwaysReachable = true;
		var isFinalMarkingClean = true;

		foreach (var state in stateSpaceAbstraction.Nodes)
		{
			hasDeadlocks |= stateTypes[state.Id].HasFlag(ConstraintStateType.Deadlock);
			isFinalMarkingAlwaysReachable &=
				!stateTypes[state.Id].HasFlag(ConstraintStateType.NoWayToFinalMarking);
			isFinalMarkingClean &= !stateTypes[state.Id].HasFlag(ConstraintStateType.UncleanFinal);
		}

		var isSound = boundedness
		              && !hasDeadlocks
		              && isFinalMarkingAlwaysReachable
		              && isFinalMarkingClean
		              && deadTransitions.Length == 0;

		return new SoundnessProperties(
			SoundnessType.Classical,
			stateTypes,
			stateSpaceAbstraction.IsFullGraph,
			deadTransitions,
			hasDeadlocks,
			isSound);

		static string[] GetDeadTransitions(StateSpaceAbstraction stateSpaceAbstraction)
		{
			var deadTransitions = stateSpaceAbstraction.DpnTransitions
				.Select(x => x.BaseTransitionId)
				.Except(stateSpaceAbstraction.Arcs.Select(y => y.BaseTransitionId))
				.ToArray();
			return deadTransitions;
		}
	}

	public static SoundnessProperties CheckSoundness(DataPetriNet dpn, LabeledTransitionSystem cg)
	{
		var boundedness = cg.IsFullGraph;
		var stateTypes = boundedness
			? GetStatesDividedByTypesNew(cg, dpn.FinalMarking.AsDictionary())
			: cg.ConstraintStates.ToDictionary(x => (AbstractState)x, y => ConstraintStateType.Default);

		var deadTransitions = GetDeadTransitions(dpn, cg);

		var hasDeadlocks = false;
		var isFinalMarkingAlwaysReachable = true;
		var isFinalMarkingClean = true;

		foreach (var constraintState in cg.ConstraintStates)
		{
			hasDeadlocks |= stateTypes[constraintState].HasFlag(ConstraintStateType.Deadlock);
			isFinalMarkingAlwaysReachable &=
				!stateTypes[constraintState].HasFlag(ConstraintStateType.NoWayToFinalMarking);
			isFinalMarkingClean &= !stateTypes[constraintState].HasFlag(ConstraintStateType.UncleanFinal);
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

	public static Dictionary<AbstractState, ConstraintStateType> GetStatesDividedByTypesNew
		(LabeledTransitionSystem graph, Dictionary<string, int> finalMarking)
	{
		var stateDictionary =
			graph.ConstraintStates.ToDictionary(x => (AbstractState)x, y => ConstraintStateType.Default);

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

		void DefineDeadlocks(Dictionary<AbstractState, ConstraintStateType> stateDictionary)
		{
			graph.ConstraintStates
				.Where(x => !stateDictionary[x].HasFlag(ConstraintStateType.Final) && !stateDictionary[x].HasFlag(ConstraintStateType.UncleanFinal))
				.Where(x => graph.ConstraintArcs.All(y => y.SourceState != x))
				.ToList()
				.ForEach(x => stateDictionary[x] |= ConstraintStateType.Deadlock);
		}

		// Доработать
		void DefineStatesWithNoWayToFinals(Dictionary<AbstractState, ConstraintStateType> stateDictionary,
			IEnumerable<LtsState> finalStates)

		{
			var statesLeadingToFinals = new List<LtsState>(finalStates);
			var intermediateStates = new List<LtsState>(finalStates);
			var stateIncidenceDict = graph.ConstraintArcs
				.GroupBy(x => x.TargetState)
				.ToDictionary(x => x.Key, y => y.Select(x => x.SourceState).ToList());

			do //check for covered
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
				.ForEach(x => stateDictionary[x] |= ConstraintStateType.NoWayToFinalMarking);
		}

		static void DefineUncleanFinals(Dictionary<string, int> finalMarking, Dictionary<AbstractState, ConstraintStateType> stateDictionary)
		{
			// TODO: refactor
			var uncleanFinals = stateDictionary.Keys
				.Where(x => x.Marking.AsDictionary().All(y => y.Value >= finalMarking[y.Key]) &&
				            x.Marking.AsDictionary().Any(y => y.Value > finalMarking[y.Key]))
				.ToArray();

			foreach (var uncleanFinal in uncleanFinals)
			{
				stateDictionary[uncleanFinal] |= ConstraintStateType.UncleanFinal;
			}
		}

		void DefineFinals(Dictionary<AbstractState, ConstraintStateType> stateDictionary,
			LtsState[] finalStates)
		{
			finalStates
				.ToList()
				.ForEach(x => stateDictionary[x] |= ConstraintStateType.Final);
		}

		void DefineInitialState(Dictionary<AbstractState, ConstraintStateType> stateDictionary)
		{
			stateDictionary[graph.InitialState] |= ConstraintStateType.Initial;
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

	private static Dictionary<int, ConstraintStateType> GetStatesDividedByTypesNew
		(StateSpaceAbstraction stateSpaceAbstraction)
	{
		var stateDictionary = stateSpaceAbstraction
			.Nodes.ToDictionary(x => x.Id, x => ConstraintStateType.Default);

		var initialNodeKey = stateDictionary.Keys.Min();
		stateDictionary[initialNodeKey] |= ConstraintStateType.Initial;

		var finalStates = stateSpaceAbstraction.Nodes
			.Where(x => x.Marking.All(y => y.Value == stateSpaceAbstraction.FinalDpnMarking[y.Key]))
			.ToArray();

		foreach (var finalState in finalStates)
		{
			stateDictionary[finalState.Id] |= ConstraintStateType.Final;
		}

		var uncleanFinals = stateSpaceAbstraction.Nodes
			.Where(x => x.Marking.All(y => y.Value >= stateSpaceAbstraction.FinalDpnMarking[y.Key]) &&
			            x.Marking.Any(y => y.Value > stateSpaceAbstraction.FinalDpnMarking[y.Key]))
			.ToArray();

		foreach (var uncleanFinal in uncleanFinals)
		{
			stateDictionary[uncleanFinal.Id] |= ConstraintStateType.UncleanFinal;
		}

		if (stateSpaceAbstraction.Arcs.Length == 0)
		{
			return stateDictionary;
		}

		var successors = stateSpaceAbstraction
			.Arcs
			.GroupBy(a => a.SourceNodeId)
			.ToDictionary(a => a.Key, a => a.ToArray());

		stateSpaceAbstraction.Nodes
			.Where(x => !stateDictionary[x.Id].HasFlag(ConstraintStateType.Final) && !stateDictionary[x.Id].HasFlag(ConstraintStateType.UncleanFinal))
			.Where(x => successors.ContainsKey(x.Id))
			.ToList()
			.ForEach(x => stateDictionary[x.Id] |= ConstraintStateType.Deadlock);

		var predecessors = stateSpaceAbstraction
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
			.ForEach(x => stateDictionary[x] |= ConstraintStateType.NoWayToFinalMarking);

		return stateDictionary;
	}
}