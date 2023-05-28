using DataPetriNetOnSmt.Extensions;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var cycles = FindElementaryCycles(lts);           

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

        private List<Cycle> FindElementaryCycles(LabeledTransitionSystem lts)
        {
            var invertedArcsDict = new Dictionary<ConstraintState, List<ConstraintArc>>();
            var arcsDict = new Dictionary<ConstraintState, List<ConstraintArc>>();

            foreach (var state in lts.ConstraintStates)
            {
                invertedArcsDict[state] = new List<ConstraintArc>();
                arcsDict[state] = new List<ConstraintArc>();
            }
            foreach (var arc in lts.ConstraintArcs)
            {
                invertedArcsDict[arc.TargetState].Add(arc);
                arcsDict[arc.SourceState].Add(arc);
            }

            var cycles = new List<Cycle>();

            foreach (var node in lts.ConstraintStates
                                        .Where(x => x.IsCyclic))
            {
                cycles.AddRange(RecursiveInvertedDFSToFindCycles(node, new List<ConstraintArc>(), invertedArcsDict, arcsDict));//, ref cycleCount
            }

            return cycles;
        }

        public List<Cycle> RecursiveInvertedDFSToFindCycles(
            ConstraintState basisState,
            List<ConstraintArc> currentPath,
            Dictionary<ConstraintState, List<ConstraintArc>> invertedArcsDict,
            Dictionary<ConstraintState, List<ConstraintArc>> arcsDict)
        {
            var cycles = new List<Cycle>();

            List<ConstraintArc> availableArcs;

            if (currentPath.Count == 0)
            {
                availableArcs = invertedArcsDict[basisState];
            }
            else
            {
                availableArcs = invertedArcsDict[currentPath[^1].SourceState]
                    .Where(x => x.SourceState == basisState || x.SourceState.ParentStates.Contains(basisState))
                    .Where(x => !x.IsVisited)//!currentPath.Contains(x))
                    .ToList();
            }

            foreach (var arc in availableArcs)
            {
                currentPath.Add(arc);
                arc.IsVisited = true;

                if (arc.SourceState == basisState)
                {
                    var outputArcs = new HashSet<ConstraintArc>();
                    foreach (var element in currentPath)
                    {
                        var outArcs = arcsDict[element.SourceState]
                            .Where(x => x != element);
                        outputArcs.AddRange(outArcs);
                    }
                    var inputArcs = new HashSet<ConstraintArc>(currentPath);

                    cycles.Add(new Cycle(inputArcs, outputArcs));
                }
                else
                {
                    cycles.AddRange(RecursiveInvertedDFSToFindCycles(basisState, currentPath, invertedArcsDict, arcsDict));
                }
                currentPath.RemoveAt(currentPath.Count - 1);
                arc.IsVisited = false;
            }

            return cycles;
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
            }
            else
            {
                availableArcs = arcsDict[currentPath[^1].TargetState];
            }

            foreach (var arc in availableArcs)
            {
                var isCycle = false;
                var arcsInCycle = new HashSet<ConstraintArc>();
                var arcsOutCycle = new HashSet<ConstraintArc>();

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
