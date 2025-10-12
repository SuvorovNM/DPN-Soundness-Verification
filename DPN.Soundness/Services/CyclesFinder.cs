using DPN.Models.Enums;
using DPN.Models.Extensions;
using DPN.Soundness.TransitionSystems.CoverabilityTree;
using DPN.Soundness.TransitionSystems.LabeledTransitionSystems;
using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;

namespace DPN.Soundness.Services
{
	public class ArcForInvestigation<TAbsState, TAbsTransition, TAbsArc, TSelf>
		where TAbsState : AbstractState
		where TAbsTransition : AbstractTransition
		where TAbsArc : AbstractArc<TAbsState, TAbsTransition>
		where TSelf : ArcForInvestigation<TAbsState, TAbsTransition, TAbsArc, TSelf>
	{
		public TAbsArc Arc { get; set; }
		public bool IsVisited { get; set; }

		public ArcForInvestigation(TAbsArc arc)
		{
			Arc = arc;
			IsVisited = false;
		}
	}

	public class LtsArcForInvestigation : ArcForInvestigation<LtsState, LtsTransition, LtsArc, LtsArcForInvestigation>
	{
		public LtsArcForInvestigation(LtsArc arc) : base(arc)
		{
		}
	}

	public class CtArcForInvestigation : ArcForInvestigation<CtState, CtTransition, CtArc, CtArcForInvestigation>
	{
		public CtArcForInvestigation(CtArc arc) : base(arc)
		{
		}
	}

	public class Cycle<TAbsArc, TAbsState, TAbsTransition, TSelf>
		where TAbsArc : AbstractArc<TAbsState, TAbsTransition>
		where TAbsState : AbstractState
		where TAbsTransition : AbstractTransition
		where TSelf : Cycle<TAbsArc, TAbsState, TAbsTransition, TSelf>
	{
		public HashSet<TAbsArc> CycleArcs { get; init; }
		public HashSet<TAbsArc> OutputArcs { get; init; }

		public Cycle()
		{
			CycleArcs = new HashSet<TAbsArc>();
			OutputArcs = new HashSet<TAbsArc>();
		}

		public Cycle(HashSet<TAbsArc> cycleArcs, HashSet<TAbsArc> outputArcs)
		{
			CycleArcs = cycleArcs;
			OutputArcs = outputArcs;
		}
	}

	public class LtsCycle : Cycle<LtsArc, LtsState, LtsTransition, LtsCycle>
	{
		public LtsCycle(HashSet<LtsArc> cycleArcs, HashSet<LtsArc> outputArcs)
			: base(cycleArcs, outputArcs)
		{
		}
	}

	public class CtCycle : Cycle<CtArc, CtState, CtTransition, CtCycle>
	{
		public CtCycle(HashSet<CtArc> cycleArcs, HashSet<CtArc> outputArcs)
			: base(cycleArcs, outputArcs)
		{
		}
	}

	public class CyclesFinder
	{
		public List<CtCycle> GetCycles(CoverabilityTree ct)
		{
			var cycles = FindElementaryCycles(ct);

			var intersectingCycles = CompoundIntersectingCycles<CtState, CtTransition, CtArc, CtCycle>(cycles);

			return intersectingCycles;
		}

		private List<CtCycle> FindElementaryCycles(CoverabilityTree ct)
		{
			var cyclesList = new List<CtCycle>();
			var cyclicLeafNodes = ct.LeafStates.Where(x => x.StateType == CtStateType.NonstrictlyCovered);
			var arcDict = new Dictionary<CtState, List<CtArc>>();
			var invertedArcDict = new Dictionary<CtState, CtArc>(); // In CT each node has only one parent

			foreach (var state in ct.ConstraintStates)
			{
				arcDict[state] = new List<CtArc>();
			}

			foreach (var arc in ct.ConstraintArcs)
			{
				arcDict[arc.SourceState].Add(arc);
				invertedArcDict[arc.TargetState] = arc;
			}

			foreach (var cyclicNode in cyclicLeafNodes)
			{
				var coveredNode = cyclicNode.CoveredNode;
				var currentNode = cyclicNode;
				var outputArcs = new HashSet<CtArc>();
				var inputArcs = new HashSet<CtArc>();

				while (currentNode != coveredNode)
				{
					var arcToParent = invertedArcDict[currentNode];
					currentNode = currentNode.ParentNode!;
					outputArcs.AddRange(arcDict[currentNode].Except(new[] { arcToParent }));
					inputArcs.Add(arcToParent);
				} // It seems that we do not need to add anything else

				var cycle = new CtCycle(inputArcs, outputArcs);
				cyclesList.Add(cycle);
			}

			return cyclesList;
		}

		public List<LtsCycle> GetCyclesNew(LabeledTransitionSystem lts)
		{
			var reachableNodes = ComputeReachableNodesBFS(lts);


			var remainedNodes = lts.ConstraintStates.ToHashSet();
			var cycles = new List<HashSet<LtsState>>(lts.ConstraintStates.Count);

			/*var selfLoops = lts
				.ConstraintArcs
				.Where(a => a.SourceState == a.TargetState)
				.Select(a => a.TargetState)
				.ToHashSet();*/ // TODO: is needed

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
				.GroupBy(a=>a.SourceState)
				.ToDictionary(a=>a.Key, a=>a.Select(x=>x).ToArray());
			
			var incomingArcs = lts
				.ConstraintArcs
				.GroupBy(a=>a.TargetState)
				.ToDictionary(a=>a.Key, a=>a.Select(x=>x).ToArray());

			var ltsCycles = new List<LtsCycle>(cycles.Count);
			foreach (var cycle in cycles)
			{
				var outgoingFromCycleNodes = cycle
					.SelectMany(n => outgoingArcs[n])
					.ToHashSet();

				var arcsInsideLoop = outgoingFromCycleNodes
					.Where(a => cycle.Contains(a.TargetState))
					.ToHashSet();
				
				var arcsGoingOutside = outgoingFromCycleNodes.Except(arcsInsideLoop).ToHashSet();
				
				// TODO: нужен костыль - должны отдавать тут все
				ltsCycles.Add(new LtsCycle(arcsInsideLoop,outgoingFromCycleNodes));//arcsGoingOutside
			}

			return ltsCycles;
		}

		public Dictionary<LtsState, HashSet<LtsState>> ComputeReachableNodesBFS(LabeledTransitionSystem lts)
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

					// Add to reachable set (excluding self if desired)
					//if (current != startState)
					//{
					//}
					
					

					// Explore neighbors
					if (adjacencyList.ContainsKey(current))
					{
						foreach (var neighbor in adjacencyList[current])
						{
							reachable.Add(neighbor);//current
							if (!visited.Contains(neighbor))
							{
								visited.Add(neighbor);
								queue.Enqueue(neighbor);
							}
						}
					}
				}

				result[startState] = reachable;
			}

			return result;
		}

		private Dictionary<LtsState, List<LtsState>> BuildAdjacencyList(LabeledTransitionSystem lts)
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


		public List<LtsCycle> GetCycles(LabeledTransitionSystem lts)
		{
			var cycles = FindElementaryCycles(lts);

			var intersectingCycles = CompoundIntersectingCycles<LtsState, LtsTransition, LtsArc, LtsCycle>(cycles);

			return intersectingCycles;
		}

		private List<TSelf> CompoundIntersectingCycles<TAbsState, TAbsTransition, TAbsArc, TSelf>
			(List<TSelf> sourceCycles)
			where TAbsArc : AbstractArc<TAbsState, TAbsTransition>
			where TAbsState : AbstractState
			where TAbsTransition : AbstractTransition
			where TSelf : Cycle<TAbsArc, TAbsState, TAbsTransition, TSelf>
		{
			var newCycles = new List<TSelf>(sourceCycles);
			var previousCount = newCycles.Count;

			var i = 0;
			do
			{
				var j = 0;
				while (j < newCycles.Count && i < newCycles.Count)
				{
					if (i != j &&
					    newCycles[i].CycleArcs
						    .Select(x => x.SourceState)
						    .Intersect(newCycles[j].CycleArcs.Select(y => y.SourceState)).Any())
					{
						if (j < i)
						{
							newCycles[j].CycleArcs.AddRange(newCycles[i].CycleArcs);
							newCycles[j].OutputArcs.AddRange(newCycles[i].OutputArcs);
							newCycles.RemoveAt(i);
						}
						else
						{
							newCycles[i].CycleArcs.AddRange(newCycles[j].CycleArcs);
							newCycles[i].OutputArcs.AddRange(newCycles[j].OutputArcs);
							newCycles.RemoveAt(j);
						}
					}
					else
					{
						j++;
					}
				}

				i++;
			} while (i < newCycles.Count);

			return newCycles;
		}

		private List<LtsCycle> FindElementaryCycles(LabeledTransitionSystem lts)
		{
			var invertedArcsDict = new Dictionary<LtsState, List<LtsArcForInvestigation>>();
			var arcsDict = new Dictionary<LtsState, List<LtsArc>>();

			foreach (var state in lts.ConstraintStates)
			{
				invertedArcsDict[state] = new List<LtsArcForInvestigation>();
				arcsDict[state] = new List<LtsArc>();
			}

			foreach (var arc in lts.ConstraintArcs)
			{
				invertedArcsDict[arc.TargetState].Add(new LtsArcForInvestigation(arc));
				arcsDict[arc.SourceState].Add(arc);
			}

			var cycles = new List<LtsCycle>();

			foreach (var node in lts.ConstraintStates
				         .Where(x => x.IsCyclic))
			{
				cycles.AddRange(RecursiveInvertedDfsToFindCycles(node, new List<LtsArc>(), invertedArcsDict, arcsDict)); //, ref cycleCount
			}

			return cycles;
		}

		public List<LtsCycle> RecursiveInvertedDfsToFindCycles(
			LtsState basisState,
			List<LtsArc> currentPath,
			Dictionary<LtsState, List<LtsArcForInvestigation>> invertedArcsDict,
			Dictionary<LtsState, List<LtsArc>> arcsDict)
		{
			var cycles = new List<LtsCycle>();

			List<LtsArcForInvestigation> availableArcs;

			if (currentPath.Count == 0)
			{
				availableArcs = invertedArcsDict[basisState];
			}
			else
			{
				availableArcs = invertedArcsDict[currentPath[^1].SourceState]
					.Where(x => x.Arc.SourceState == basisState || x.Arc.SourceState.ParentStates.Contains(basisState))
					.Where(x => !x.IsVisited) //!currentPath.Contains(x))
					.ToList();
			}

			foreach (var arc in availableArcs)
			{
				currentPath.Add(arc.Arc);
				arc.IsVisited = true;

				if (arc.Arc.SourceState == basisState)
				{
					var outputArcs = new HashSet<LtsArc>();
					foreach (var element in currentPath)
					{
						var outArcs = arcsDict[element.SourceState]
							.Where(x => x != element);
						outputArcs.AddRange(outArcs);
					}

					var inputArcs = new HashSet<LtsArc>(currentPath);

					cycles.Add(new LtsCycle(inputArcs, outputArcs));
				}
				else
				{
					cycles.AddRange(RecursiveInvertedDfsToFindCycles(basisState, currentPath, invertedArcsDict, arcsDict));
				}

				currentPath.RemoveAt(currentPath.Count - 1);
				arc.IsVisited = false;
			}

			return cycles;
		}


		private List<LtsCycle> RecursiveDfsToFindCycles(
			LabeledTransitionSystem lts,
			List<LtsArc> currentPath,
			Dictionary<LtsState, List<LtsArc>> arcsDict)
		{
			List<LtsCycle> cycles = new List<LtsCycle>();

			List<LtsArc> availableArcs;
			if (currentPath.Count == 0)
			{
				availableArcs = arcsDict[lts.InitialState];
			}
			else
			{
				availableArcs = arcsDict[currentPath[^1].TargetState];
			}

			foreach (var arc in availableArcs)
			{
				var isCycle = false;
				var arcsInCycle = new HashSet<LtsArc>();
				var arcsOutCycle = new HashSet<LtsArc>();

				for (int i = 0; i < currentPath.Count; i++)
				{
					isCycle |= currentPath[i].SourceState == arc.TargetState;
					if (isCycle)
					{
						arcsInCycle.Add(currentPath[i]);
						var outArcs = arcsDict[currentPath[i].SourceState]
							.Where(x => x != currentPath[i]);

						arcsOutCycle.AddRange(outArcs);
					}
				}

				isCycle |= arc.SourceState == arc.TargetState;
				if (isCycle)
				{
					arcsInCycle.Add(arc);
					var outArcs = arcsDict[arc.SourceState]
						.Where(x => x != arc);

					arcsOutCycle.AddRange(outArcs);
					cycles.Add(new LtsCycle(arcsInCycle, arcsOutCycle));
				}
				else
				{
					currentPath.Add(arc);
					var furtherCycles = RecursiveDfsToFindCycles(lts, currentPath, arcsDict);
					currentPath.RemoveAt(currentPath.Count - 1);
					cycles.AddRange(furtherCycles);
				}
			}

			//visitedStates.Add(currentPath.Count == 0 ? lts.InitialState : currentPath[^1].TargetState);
			return cycles;
		}
	}
}