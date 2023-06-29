using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    public class TransformerToRefined
    {
        private CyclesFinder cyclesFinder;
        public TransformerToRefined()
        {
            cyclesFinder = new CyclesFinder();
        }

        private DataPetriNet PerformTransformationStep<AbsState, AbsTransition, AbsArc, TSelf>
            (DataPetriNet sourceDpn, List<TSelf> cycles)
            where AbsArc : AbstractArc<AbsState, AbsTransition>
            where AbsState : AbstractState
            where AbsTransition : AbstractTransition
            where TSelf : Cycle<AbsArc, AbsState, AbsTransition, TSelf>

        {
            var newDPN = (DataPetriNet)sourceDpn.Clone();
            var context = sourceDpn.Context;

            var transitionsPreset = new Dictionary<Transition, List<(Place place, int weight)>>();
            var transitionsPostset = new Dictionary<Transition, List<(Place place, int weight)>>();
            FillTransitionsArcs(newDPN, transitionsPreset, transitionsPostset);

            //var cycles = cyclesFinder.GetCycles(lts);

            var refinedTransitions = new List<Transition>();
            var refinedArcs = new List<Arc>();

            var transitionsDict = sourceDpn
                .Transitions
                .ToDictionary(x => x.Id);

            foreach (var sourceTransition in newDPN.Transitions)
            {
                var updatedTransitions = new List<Transition> { sourceTransition };

                var writeVarsInSourceTransition = sourceTransition.Guard.WriteVars;

                if (writeVarsInSourceTransition.Count > 0)
                {
                    var cyclesWithTransition = cycles
                        .Where(x => x.CycleArcs.Any(y => y.Transition.Id == sourceTransition.Id));

                    var outputTransitions = cyclesWithTransition
                        .SelectMany(x => x.OutputArcs)
                        .Where(x => transitionsDict[x.Transition.Id].Guard.ReadVars
                            .Intersect(writeVarsInSourceTransition).Any())
                        .Select(x => transitionsDict[x.Transition.Id])
                        .Distinct();

                    foreach (var outputTransition in outputTransitions)
                    {
                        var updatedTransitionsBasis = new List<Transition>(updatedTransitions);

                        var overwrittenVarsInOutTransition = outputTransition.Guard.WriteVars;
                        var readFormula = context.GetReadExpression(outputTransition.Guard.ActualConstraintExpression, overwrittenVarsInOutTransition);


                        var formulaToConjunct = readFormula;
                        foreach (var overwrittenVar in writeVarsInSourceTransition)
                        {
                            var readVar = context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Read);
                            var writeVar = context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Written);

                            formulaToConjunct = (BoolExpr)formulaToConjunct.Substitute(readVar, writeVar);
                        }

                        foreach (var baseTransition in updatedTransitionsBasis)
                        {
                            (var positiveTransition, var negativeTransition) = baseTransition
                                .Split(formulaToConjunct, outputTransition.Id);
                            if (positiveTransition != null && negativeTransition != null)
                            {
                                updatedTransitions.Add(positiveTransition);
                                updatedTransitions.Add(negativeTransition);
                            }
                            else
                            {
                                updatedTransitions.Add((Transition)baseTransition.Clone());
                            }
                        }

                        updatedTransitions = updatedTransitions
                            .Except(updatedTransitionsBasis)
                            .ToList();
                    }
                }

                foreach (var updatedTransition in updatedTransitions)
                {
                    var tactic = sourceDpn.Context.MkTactic("ctx-solver-simplify");
                    var goal = sourceDpn.Context.MkGoal();
                    goal.Assert(updatedTransition.Guard.ActualConstraintExpression);

                    var result = tactic.Apply(goal);
                    var updatedConstraint = result.Subgoals[0].AsBoolExpr();
                    updatedTransition.Guard = new Guard(newDPN.Context, updatedTransition.Guard.BaseConstraintExpressions, updatedConstraint);
                    

                    foreach (var arc in transitionsPreset[sourceTransition])
                    {
                        refinedArcs.Add(new Arc(arc.place, updatedTransition, arc.weight));
                    }
                    foreach (var arc in transitionsPostset[sourceTransition])
                    {
                        refinedArcs.Add(new Arc(updatedTransition, arc.place, arc.weight));
                    }
                }

                refinedTransitions.AddRange(updatedTransitions);
            }

            newDPN.Transitions = refinedTransitions;
            newDPN.Arcs = refinedArcs;

            return newDPN;
        }

        public (DataPetriNet dpn, CoverabilityTree ct) TransformUsingCt(DataPetriNet sourceDpn, CoverabilityTree sourceCt = null)
        {
            DataPetriNet transformedDpn = sourceDpn;
            int sourceDpnTransitionCount;

            do
            {
                if (sourceCt == null)
                {
                    sourceCt = new CoverabilityTree(transformedDpn);
                    sourceCt.GenerateGraph();
                }

                if (sourceCt.LeafStates.Any(x=>x.StateType == CtStateType.StrictlyCovered))
                {
                    return (transformedDpn, sourceCt);
                }

                sourceDpnTransitionCount = transformedDpn.Transitions.Count;
                transformedDpn = PerformTransformationStep<CtState, CtTransition, CtArc, CtCycle>
                    (transformedDpn, cyclesFinder.GetCycles(sourceCt));
            } while (transformedDpn.Transitions.Count > sourceDpnTransitionCount);

            return (transformedDpn, sourceCt);
        }

        public (DataPetriNet dpn, ClassicalLabeledTransitionSystem lts) TransformUsingLts
            (DataPetriNet sourceDpn, ClassicalLabeledTransitionSystem sourceLts = null)
        {
            DataPetriNet transformedDpn = sourceDpn;
            int sourceDpnTransitionCount;

            do
            {
                if (sourceLts == null)
                {
                    sourceLts = new ClassicalLabeledTransitionSystem(transformedDpn);
                    sourceLts.GenerateGraph();
                }

                if (!sourceLts.IsFullGraph)
                {
                    return (transformedDpn, sourceLts);
                }

                sourceDpnTransitionCount = transformedDpn.Transitions.Count;
                transformedDpn = PerformTransformationStep<LtsState,LtsTransition,LtsArc,LtsCycle>
                    (transformedDpn, cyclesFinder.GetCycles(sourceLts));
            } while (transformedDpn.Transitions.Count > sourceDpnTransitionCount);

            return (transformedDpn, sourceLts);
        }



        private static void FillTransitionsArcs(DataPetriNet sourceDpn, Dictionary<Transition, List<(Place place, int weight)>> transitionsPreset, Dictionary<Transition, List<(Place place, int weight)>> transitionsPostset)
        {
            foreach (var transition in sourceDpn.Transitions)
            {
                transitionsPreset.Add(transition, new List<(Place place, int weight)>());
                transitionsPostset.Add(transition, new List<(Place place, int weight)>());
            }

            foreach (var arc in sourceDpn.Arcs)
            {
                if (arc.Type == ArcType.PlaceTransition)
                {
                    transitionsPreset[(Transition)arc.Destination].Add(((Place)arc.Source, arc.Weight));
                }
                else
                {
                    transitionsPostset[(Transition)arc.Source].Add(((Place)arc.Destination, arc.Weight));
                }
            }
        }
    }
}
