using DPN.Soundness.TransitionSystems.Reachability;

namespace DPN.Soundness.Repair.Cycles
{
	internal static class CyclesFinder
	{
		public static List<LtsCycle> GetCycles(LabeledTransitionSystem lts)
		{
			var reachableNodes = ComputeReachableNodesBfs(lts);

			var remainedNodes = lts.ConstraintStates.ToHashSet();
			var cycles = new List<HashSet<LtsState>>(lts.ConstraintStates.Count);

			do
			{
				var currentNode = remainedNodes.First();

				var nodesReachableFromCurrent = reachableNodes[currentNode];
				var nodesWithPathToCurrent = nodesReachableFromCurrent
					.Where(n => reachableNodes[n].Contains(currentNode))
					.ToArray();

				if (nodesWithPathToCurrent.Length != 0)
				{
					var loop = nodesWithPathToCurrent.Union([currentNode]).ToHashSet();
					cycles.Add(loop);
					foreach (var nodeInLoop in loop)
					{
						remainedNodes.Remove(nodeInLoop);
					}
				}
				else
				{
					remainedNodes.Remove(currentNode);
				}
			} while (remainedNodes.Count != 0);

			var outgoingArcs = lts
				.ConstraintArcs
				.GroupBy(a => a.SourceState)
				.ToDictionary(a => a.Key, a => a.Select(x => x).ToArray());

			var ltsCycles = new List<LtsCycle>(cycles.Count);
			foreach (var cycle in cycles)
			{
				var outgoingFromCycleNodes = cycle
					.SelectMany(n => outgoingArcs[n])
					.ToHashSet();

				var arcsInsideLoop = outgoingFromCycleNodes
					.Where(a => cycle.Contains(a.TargetState))
					.ToHashSet();

				var arcsOutsideLoop = outgoingFromCycleNodes
					.Except(arcsInsideLoop)
					.ToHashSet();

				ltsCycles.Add(new LtsCycle(arcsInsideLoop, arcsOutsideLoop));
			}

			return ltsCycles;
		}

		private static Dictionary<LtsState, HashSet<LtsState>> ComputeReachableNodesBfs(LabeledTransitionSystem lts)
		{
			var result = new Dictionary<LtsState, HashSet<LtsState>>();
			var adjacencyList = BuildAdjacencyList(lts);

			foreach (var startState in lts.ConstraintStates)
			{
				var reachable = new HashSet<LtsState>();
				var queue = new Queue<LtsState>();
				var visited = new HashSet<LtsState>();

				queue.Enqueue(startState);
				visited.Add(startState);

				while (queue.Count > 0)
				{
					var current = queue.Dequeue();

					// Explore neighbors
					if (adjacencyList.TryGetValue(current, out var adjacentNodes))
					{
						foreach (var neighbor in adjacentNodes)
						{
							reachable.Add(neighbor);
							if (visited.Add(neighbor))
							{
								queue.Enqueue(neighbor);
							}
						}
					}
				}

				result[startState] = reachable;
			}

			return result;
		}

		private static Dictionary<LtsState, List<LtsState>> BuildAdjacencyList(LabeledTransitionSystem lts)
		{
			var adjList = new Dictionary<LtsState, List<LtsState>>();

			foreach (var state in lts.ConstraintStates)
			{
				adjList[state] = new List<LtsState>();
			}

			foreach (var arc in lts.ConstraintArcs)
			{
				adjList[arc.SourceState].Add(arc.TargetState);
			}

			return adjList;
		}
	}
}