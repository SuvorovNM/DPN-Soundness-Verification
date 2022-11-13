using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    public class TransformationToRefined : ITransformation
    {
        private CyclesFinder cyclesFinder;
        public TransformationToRefined()
        {
            cyclesFinder = new CyclesFinder();
        }

        public DataPetriNet Transform(DataPetriNet sourceDpn)
        {
            var newDPN = (DataPetriNet)sourceDpn.Clone();
            var context = newDPN.Context;

            var transitionsPreset = new Dictionary<Transition, List<(Place place, int weight)>>();
            var transitionsPostset = new Dictionary<Transition, List<(Place place, int weight)>>();
            FillTransitionsArcs(newDPN, transitionsPreset, transitionsPostset);

            var lts = new ClassicalLabeledTransitionSystem(sourceDpn, new ConstraintExpressionOperationServiceWithEqTacticConcat(sourceDpn.Context));
            lts.GenerateGraph();
            var cycles = cyclesFinder.GetCycles(lts);

            var refinedTransitions = new List<Transition>();
            var refinedArcs = new List<Arc>();

            var transitionsDict = sourceDpn
                .Transitions
                .ToDictionary(x => x.Id);

            foreach (var sourceTransition in newDPN.Transitions)
            {
                var cyclesWithTransition = cycles
                    .Where(x => x.CycleArcs.Any(y => y.Transition.Id == sourceTransition.Id));

                var outputTransitions = cyclesWithTransition
                    .SelectMany(x => x.OutputArcs)
                    .Where(x => transitionsDict[x.Transition.Id].Guard.BaseConstraintExpressions.GetTypedVarsDict(VariableType.Read)
                        .Intersect(sourceTransition.Guard.BaseConstraintExpressions.GetTypedVarsDict(VariableType.Written)).Any())
                    .Select(x => transitionsDict[x.Transition.Id]);

                var updatedTransitions = new List<Transition> { sourceTransition };
                var overwrittenVarsInSourceTransition = sourceTransition.Guard.BaseConstraintExpressions.GetTypedVarsDict(VariableType.Written);

                foreach (var outputTransition in outputTransitions)
                {
                    var updatedTransitionsBasis = new List<Transition>(updatedTransitions);

                    var smtFormula = context.GetSmtExpression(outputTransition.Guard.BaseConstraintExpressions);
                    var overwrittenVarsInOutTransition = outputTransition.Guard.BaseConstraintExpressions.GetTypedVarsDict(VariableType.Written);
                    var readFormula = context.GetReadExpression(smtFormula, overwrittenVarsInOutTransition);


                    var formulaToConjunct = readFormula;
                    foreach (var overwrittenVar in overwrittenVarsInSourceTransition)
                    {
                        var readVar = context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Read);
                        var writeVar = context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Written);

                        formulaToConjunct = (BoolExpr)formulaToConjunct.Substitute(readVar, writeVar);
                    }

                    foreach(var baseTransition in updatedTransitionsBasis)
                    {
                        var isPositiveSatisfiable = context.CanBeSatisfied
                            (context.MkAnd(
                            baseTransition.Guard.ActualConstraintExpression,
                            formulaToConjunct));
                        var isNegativeSatisfiable = context.CanBeSatisfied
                            (context.MkAnd(
                            baseTransition.Guard.ActualConstraintExpression,
                            context.MkNot(formulaToConjunct)));

                        if (!isPositiveSatisfiable || !isNegativeSatisfiable)
                        {
                            updatedTransitions.Add((Transition)baseTransition.Clone());
                        }
                        else
                        {
                            var transitionPositive = (Transition)baseTransition.Clone();
                            transitionPositive.Id = transitionPositive.Id + "+" + outputTransition.Id;
                            transitionPositive.Label = transitionPositive.Id;
                            transitionPositive.Guard.ActualConstraintExpression = context.MkAnd(
                                transitionPositive.Guard.ActualConstraintExpression,
                                formulaToConjunct);
                            updatedTransitions.Add(transitionPositive);

                            var transitionNegative = (Transition)baseTransition.Clone();
                            transitionNegative.Id = transitionNegative.Id + "-" + outputTransition.Id;
                            transitionNegative.Label = transitionNegative.Id;
                            transitionNegative.Guard.ActualConstraintExpression = context.MkAnd(
                                transitionNegative.Guard.ActualConstraintExpression,
                                context.MkNot(formulaToConjunct));
                            updatedTransitions.Add(transitionNegative);
                        }
                    }

                    updatedTransitions = updatedTransitions
                        .Except(updatedTransitionsBasis)
                        .ToList();
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
