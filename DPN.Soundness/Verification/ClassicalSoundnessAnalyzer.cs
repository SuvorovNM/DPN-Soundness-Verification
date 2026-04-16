using DPN.Models.DPNElements;
using DPN.Models.Enums;
using DPN.Models.Extensions;
using DPN.Soundness.TransitionSystems;
using DPN.Soundness.TransitionSystems.StateSpace;

namespace DPN.Soundness.Verification;

public static class ClassicalSoundnessAnalyzer
{
	public static SoundnessProperties CheckSoundness(StateSpaceGraph stateSpaceGraph)
	{
		var boundedness = stateSpaceGraph.IsFullGraph && !stateSpaceGraph.Nodes.Any(s => s.Marking.Any(kvp => kvp.Value == int.MaxValue));
		var stateTypes = GetStatesDividedByTypes(stateSpaceGraph);

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
			boundedness,
			deadTransitions,
			hasDeadlocks,
			isSound);

		static string[] GetDeadTransitions(StateSpaceGraph stateSpaceGraph)
		{
			var deadTransitions = stateSpaceGraph.DpnTransitions
				.Where(t => !t.IsTau)
				.Select(x => x.BaseTransitionId)
				.Except(stateSpaceGraph.Arcs.Select(y => y.BaseTransitionId))
				.ToArray();
			return deadTransitions;
		}
	}

	private static Dictionary<int, StateType> GetStatesDividedByTypes
		(StateSpaceGraph stateSpaceGraph)
	{
		var stateDictionary = stateSpaceGraph
			.Nodes.ToDictionary(x => x.Id, _ => StateType.Default);

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

		var strictlyCoveredStates = stateSpaceGraph.Nodes
			.Where(x => x.Marking.Any(kvp => kvp.Value == int.MaxValue))
			.ToArray();

		foreach (var strictlyCovered in strictlyCoveredStates)
		{
			stateDictionary[strictlyCovered.Id] |= StateType.StrictlyCovered;
		}

		if (stateSpaceGraph.Arcs.Length == 0)
		{
			return stateDictionary;
		}

		var successors = stateSpaceGraph
			.Arcs
			.GroupBy(a => a.SourceNodeId)
			.ToDictionary(a => a.Key, a => a.ToArray());

		if (stateSpaceGraph.IsFullGraph)
		{
			stateSpaceGraph.Nodes
				.Where(x => !stateDictionary[x.Id].HasFlag(StateType.Final)
				            && !stateDictionary[x.Id].HasFlag(StateType.UncleanFinal)
				            && !stateDictionary[x.Id].HasFlag(StateType.StrictlyCovered))
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
		}


		return stateDictionary;
	}
}