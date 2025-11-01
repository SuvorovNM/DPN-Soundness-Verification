using DPN.Soundness.TransitionSystems.Reachability;

namespace DPN.Soundness.Repair.Cycles;

internal class LtsCycle(HashSet<LtsArc> cycleArcs, HashSet<LtsArc> outputArcs)
{
	public HashSet<LtsArc> CycleArcs { get; init; } = cycleArcs;
	public HashSet<LtsArc> OutputArcs { get; init; } = outputArcs;

	private HashSet<LtsArc>? cycleArcsWithAdjacent;

	public HashSet<LtsArc> CycleArcsWithAdjacent => cycleArcsWithAdjacent ??= CycleArcs.Union(OutputArcs).ToHashSet();
}