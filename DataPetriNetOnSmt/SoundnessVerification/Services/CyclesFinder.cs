using DataPetriNetOnSmt.Extensions;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    public record Cycle(HashSet<ConstraintArc> CycleArcs, HashSet<ConstraintArc> OutputArcs);
    public class CyclesFinder
    {
        public List<Cycle> GetCycles(LabeledTransitionSystem lts)
        {
            List<ConstraintArc> initialPath = new List<ConstraintArc>();
            HashSet<ConstraintState> visitedStates = new HashSet<ConstraintState>();

            var cycles = RecursiveDFSToFindCycles(lts, initialPath, visitedStates);

            var intersectingCycles = CompoundIntersectingCycles(cycles);

            return intersectingCycles;
        }

        private List<Cycle> CompoundIntersectingCycles(List<Cycle> sourceCycles)
        {
            var newCycles = new List<Cycle>(sourceCycles);
            var previousCount = newCycles.Count;

            var i = 0;
            do
            {
                var j = 0;
                while (j < newCycles.Count)
                {
                    if ( i!= j &&
                        newCycles[i].CycleArcs.Select(x => x.SourceState).Intersect(newCycles[j].CycleArcs.Select(y => y.SourceState)).Any())
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

        private List<Cycle> RecursiveDFSToFindCycles(
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
                        arcsOutCycle.AddRange(outArcs);
                    }
                }
                isCycle |= arc.SourceState == arc.TargetState;
                if (isCycle)
                {
                    arcsInCycle.Add(arc);
                    var outArcs = lts.ConstraintArcs
                            .Where(x => (x.SourceState == arc.SourceState)
                                && x != arc);
                    arcsOutCycle.AddRange(outArcs);
                    cycles.Add(new Cycle(arcsInCycle, arcsOutCycle));
                }
                else
                {
                    currentPath.Add(arc);
                    var furtherCycles = RecursiveDFSToFindCycles(lts, currentPath, visitedStates);
                    currentPath.RemoveAt(currentPath.Count - 1);
                    cycles.AddRange(furtherCycles);
                }
            }

            //visitedStates.Add(currentPath.Count == 0 ? lts.InitialState : currentPath[^1].TargetState);
            return cycles;
        }
    }
}
