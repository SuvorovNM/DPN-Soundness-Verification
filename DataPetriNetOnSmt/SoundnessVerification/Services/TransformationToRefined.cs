﻿using DataPetriNetOnSmt.DPNElements;
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
    public class TransformationToRefined //: ITransformation
    {
        private CyclesFinder cyclesFinder;
        //public long MillisecondsForBoundednessCheck;
        //public long MillisecondsForTransformation;
        public TransformationToRefined()
        {
            cyclesFinder = new CyclesFinder();
        }

        public DataPetriNet Transform(DataPetriNet sourceDpn, ClassicalLabeledTransitionSystem lts)
        {
            var newDPN = (DataPetriNet)sourceDpn.Clone();
            var context = sourceDpn.Context;

            var transitionsPreset = new Dictionary<Transition, List<(Place place, int weight)>>();
            var transitionsPostset = new Dictionary<Transition, List<(Place place, int weight)>>();
            FillTransitionsArcs(newDPN, transitionsPreset, transitionsPostset);

            var cycles = cyclesFinder.GetCycles(lts);

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

                    // TODO: Не очень верно проверять на read "BaseConstraintExpressions"
                    var outputTransitions = cyclesWithTransition
                        .SelectMany(x => x.OutputArcs)
                        .Where(x => transitionsDict[x.Transition.Id].Guard.ReadVars//BaseConstraintExpressions
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

        public (DataPetriNet dpn, ClassicalLabeledTransitionSystem lts) Transform(DataPetriNet sourceDpn)
        {
            var lts = new ClassicalLabeledTransitionSystem(sourceDpn, new ConstraintExpressionOperationServiceWithEqTacticConcat(sourceDpn.Context));
            lts.GenerateGraph();

            if (!lts.IsFullGraph)
            {
                return (sourceDpn, lts);
            }

            var newDPN = Transform(sourceDpn, lts);

            return (newDPN, lts);
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