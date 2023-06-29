﻿using DataPetriNetOnSmt.Enums;
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
    public class ArcForInvestigation<AbsState, AbsTransition, AbsArc, TSelf>
        where AbsState : AbstractState
        where AbsTransition : AbstractTransition
        where AbsArc : AbstractArc<AbsState, AbsTransition>
        where TSelf : ArcForInvestigation<AbsState, AbsTransition, AbsArc, TSelf>
    {
        public AbsArc Arc { get; set; }
        public bool IsVisited { get; set; }
        public ArcForInvestigation(AbsArc arc) 
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

    public class Cycle<AbsArc, AbsState, AbsTransition, TSelf>
        where AbsArc : AbstractArc<AbsState, AbsTransition>
        where AbsState : AbstractState
        where AbsTransition : AbstractTransition
        where TSelf : Cycle<AbsArc,AbsState,AbsTransition,TSelf>
    {
        public HashSet<AbsArc> CycleArcs { get; init; }
        public HashSet<AbsArc> OutputArcs { get; init; }

        public Cycle()
        {
            CycleArcs = new HashSet<AbsArc>();
            OutputArcs = new HashSet<AbsArc>();
        }
        public Cycle(HashSet<AbsArc> cycleArcs, HashSet<AbsArc> outputArcs)
        {
            CycleArcs = cycleArcs;
            OutputArcs = outputArcs;
        }
    }

    public class LtsCycle : Cycle<LtsArc, LtsState, LtsTransition,LtsCycle>
    {
        public LtsCycle(HashSet<LtsArc> cycleArcs, HashSet<LtsArc> outputArcs)
            :base(cycleArcs, outputArcs) { }
    }
    public class CtCycle : Cycle<CtArc,CtState,CtTransition,CtCycle>
    {
        public CtCycle(HashSet<CtArc> cycleArcs, HashSet<CtArc> outputArcs)
            : base(cycleArcs, outputArcs) { }
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
                    currentNode = currentNode.ParentNode;
                    outputArcs.AddRange(arcDict[currentNode].Except(new[] { arcToParent }));
                    inputArcs.Add(arcToParent);
                } // It seems that we do not need to add anything else

                var cycle = new CtCycle(inputArcs, outputArcs);
                cyclesList.Add(cycle);
            }

            return cyclesList;
        }

        public List<LtsCycle> GetCycles(LabeledTransitionSystem lts)
        {
            var cycles = FindElementaryCycles(lts);           

            var intersectingCycles = CompoundIntersectingCycles<LtsState, LtsTransition, LtsArc, LtsCycle>(cycles);

            return intersectingCycles;
        }

        private List<TSelf> CompoundIntersectingCycles<AbsState,AbsTransition,AbsArc,TSelf>
            (List<TSelf> sourceCycles)
            where AbsArc : AbstractArc<AbsState, AbsTransition>
            where AbsState : AbstractState
            where AbsTransition : AbstractTransition
            where TSelf : Cycle<AbsArc, AbsState, AbsTransition, TSelf>
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
                cycles.AddRange(RecursiveInvertedDFSToFindCycles(node, new List<LtsArc>(), invertedArcsDict, arcsDict));//, ref cycleCount
            }

            return cycles;
        }

        public List<LtsCycle> RecursiveInvertedDFSToFindCycles(
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
                    .Where(x => !x.IsVisited)//!currentPath.Contains(x))
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
                    cycles.AddRange(RecursiveInvertedDFSToFindCycles(basisState, currentPath, invertedArcsDict, arcsDict));
                }
                currentPath.RemoveAt(currentPath.Count - 1);
                arc.IsVisited = false;
            }

            return cycles;
        }


        private List<LtsCycle> RecursiveDFSToFindCycles(
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
