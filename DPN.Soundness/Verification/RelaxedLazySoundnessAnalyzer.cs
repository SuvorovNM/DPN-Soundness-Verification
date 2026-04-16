using DPN.Models.Extensions;
using DPN.Soundness.TransitionSystems;
using DPN.Soundness.TransitionSystems.StateSpace;

namespace DPN.Soundness.Verification;

public static class RelaxedLazySoundnessAnalyzer
{
	public static SoundnessProperties CheckSoundness(StateSpaceGraph stateSpaceGraph)
	{
		var stateDictionary = stateSpaceGraph
			.Nodes.ToDictionary(x => x.Id, x => StateType.Default);

		var initialNodeKey = stateDictionary.Keys.Min();
		stateDictionary[initialNodeKey] |= StateType.Initial;

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
				.Where(x => !successors.ContainsKey(x.Id) && !x.IsCovered)
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

		var hasDeadlocks = stateDictionary.Any(kvp => kvp.Value.HasFlag(StateType.Deadlock));

		var isSound = unfeasibleTransitions.Length == 0 && uncleanFinals.Length == 0;

		return new SoundnessProperties(
			SoundnessType.RelaxedLazy,
			stateDictionary,
			!stateSpaceGraph.Nodes.Any(cs => cs.Marking.ContainsValue(int.MaxValue)),
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
}