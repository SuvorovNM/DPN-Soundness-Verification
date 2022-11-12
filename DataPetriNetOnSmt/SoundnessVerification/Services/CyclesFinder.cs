using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    public record Cycle(List<ConstraintArc> CycleArcs, List<ConstraintArc> OutputArcs);
    public class CyclesFinder
    {
        public List<Cycle> GetCycles(LabeledTransitionSystem lts)
        {
            List<ConstraintArc> initialPath = new List<ConstraintArc>();
            HashSet<ConstraintState> visitedStates = new HashSet<ConstraintState>();

            return RecursiveDFSToFindCycles(lts, initialPath, visitedStates);
        }

        public List<Cycle> RecursiveDFSToFindCycles(
            LabeledTransitionSystem lts, 
            List<ConstraintArc> currentPath,
            HashSet<ConstraintState> visitedStates)
        {
            List<Cycle> cycles = new List<Cycle>();

            List<ConstraintArc> availableArcs;
            if (currentPath.Count == 0)
            {
                availableArcs = lts.ConstraintArcs
                    .Where(x => x.SourceState == lts.InitialState)
                    .ToList();
            }
            else
            {
                availableArcs = lts.ConstraintArcs
                    .Where(x => x.SourceState == currentPath[^1].TargetState && !visitedStates.Contains(x.SourceState))
                    .ToList();
            }

            foreach (var arc in availableArcs)
            {
                var isCycle = false;
                var arcsInCycle = new HashSet<ConstraintArc>();//List
                var arcsOutCycle = new HashSet<ConstraintArc>();

                for (int i =0; i< currentPath.Count; i++)
                {
                    isCycle |= currentPath[i].SourceState == arc.TargetState;
                    if (isCycle)
                    {
                        arcsInCycle.Add(currentPath[i]);
                        var outArcs = lts.ConstraintArcs
                            .Where(x => (x.SourceState == currentPath[i].SourceState)//|| x.SourceState == currentPath[i].TargetState 
                                && x != currentPath[i]);
                        foreach (var item in outArcs)
                        {
                            arcsOutCycle.Add(item);
                        }
                    }
                }
                isCycle |= arc.SourceState == arc.TargetState;
                if (isCycle)
                {
                    arcsInCycle.Add(arc);
                    var outArcs = lts.ConstraintArcs
                            .Where(x => (x.SourceState == arc.SourceState)
                                && x != arc);
                    foreach (var item in outArcs)
                    {
                        arcsOutCycle.Add(item);
                    }
                    cycles.Add(new Cycle(arcsInCycle.ToList(), arcsOutCycle.ToList()));
                }
                else
                {
                    currentPath.Add(arc);
                    var furtherCycles = RecursiveDFSToFindCycles(lts, currentPath, visitedStates);
                    currentPath.RemoveAt(currentPath.Count - 1);
                    cycles.AddRange(furtherCycles);
                }
            }

            visitedStates.Add(currentPath.Count == 0 ? lts.InitialState : currentPath[^1].TargetState);
            return cycles;
        }
    }
}
