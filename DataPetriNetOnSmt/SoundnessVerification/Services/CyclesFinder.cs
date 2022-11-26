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
        private long pathsNumber = 0;
        public List<Cycle> GetCycles(LabeledTransitionSystem lts)
        {
            List<ConstraintArc> initialPath = new List<ConstraintArc>();
            HashSet<ConstraintState> visitedStates = new HashSet<ConstraintState>();

            var arcsDict = new Dictionary<ConstraintState, List<ConstraintArc>>();

            foreach(var state in lts.ConstraintStates)
            {
                arcsDict[state] = new List<ConstraintArc>();
            }
            foreach(var arc in lts.ConstraintArcs)
            {
                arcsDict[arc.SourceState].Add(arc);
            }

            var cycles = RecursiveDFSToFindCycles(lts, initialPath, arcsDict);

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

        private List<Cycle> RecursiveDFSToFindCycles(
            LabeledTransitionSystem lts, 
            List<ConstraintArc> currentPath,
            Dictionary<ConstraintState, List<ConstraintArc>> arcsDict)
        {
            List<Cycle> cycles = new List<Cycle>();

            List<ConstraintArc> availableArcs;
            if (currentPath.Count == 0)
            {

                availableArcs = arcsDict[lts.InitialState];
                    /*lts.ConstraintArcs
                    .Where(x => x.SourceState == lts.InitialState)
                    .ToList();*/
            }
            else
            {
                availableArcs = arcsDict[currentPath[^1].TargetState];
                    /*lts.ConstraintArcs
                    .Where(x => x.SourceState == currentPath[^1].TargetState)
                    .ToList();*/
            }
            if (availableArcs.Count == 0)
            {
                pathsNumber++;
            }

            foreach (var arc in availableArcs)
            {
                var isCycle = false;
                var arcsInCycle = new HashSet<ConstraintArc>();
                var arcsOutCycle = new HashSet<ConstraintArc>();

                for (int i =0; i< currentPath.Count; i++)
                {
                    isCycle |= currentPath[i].SourceState == arc.TargetState;
                    if (isCycle)
                    {
                        arcsInCycle.Add(currentPath[i]);
                        var outArcs = arcsDict[currentPath[i].SourceState]
                            .Where(x => x != currentPath[i]);

                            /*lts.ConstraintArcs
                            .Where(x => (x.SourceState == currentPath[i].SourceState)
                                && x != currentPath[i]);*/
                        arcsOutCycle.AddRange(outArcs);
                    }
                }
                isCycle |= arc.SourceState == arc.TargetState;
                if (isCycle)
                {
                    pathsNumber++;
                    arcsInCycle.Add(arc);
                    var outArcs = arcsDict[arc.SourceState]
                        .Where(x => x != arc);

                        /*lts.ConstraintArcs
                            .Where(x => (x.SourceState == arc.SourceState)
                                && x != arc);*/
                    arcsOutCycle.AddRange(outArcs);
                    cycles.Add(new Cycle(arcsInCycle, arcsOutCycle));
                }
                else
                {
                    currentPath.Add(arc);
                    var furtherCycles = RecursiveDFSToFindCycles(lts, currentPath, arcsDict);
                    currentPath.RemoveAt(currentPath.Count - 1);
                    cycles.AddRange(furtherCycles);
                }
            }

            //visitedStates.Add(currentPath.Count == 0 ? lts.InitialState : currentPath[^1].TargetState);
            return cycles;
        }
    }
}
