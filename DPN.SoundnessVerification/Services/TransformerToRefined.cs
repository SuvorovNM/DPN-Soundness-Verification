using DPN.Models;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using DPN.Models.Extensions;
using DPN.SoundnessVerification.TransitionSystems;
using Microsoft.Z3;

namespace DPN.SoundnessVerification.Services
{
    public class TransformerToRefined
    {
        private static HashSet<Transition> _outputTransitionsCheck= new HashSet<Transition>();
        private CyclesFinder cyclesFinder;
        public TransformerToRefined()
        {
            cyclesFinder = new CyclesFinder();
        }

        private DataPetriNet PerformTransformationStep<TAbsState, TAbsTransition, TAbsArc, TSelf>
            (DataPetriNet sourceDpn, List<TSelf> cycles)
            where TAbsArc : AbstractArc<TAbsState, TAbsTransition>
            where TAbsState : AbstractState
            where TAbsTransition : AbstractTransition
            where TSelf : Cycle<TAbsArc, TAbsState, TAbsTransition, TSelf>

        {

            var newDpn = (DataPetriNet)sourceDpn.Clone();
            var context = sourceDpn.Context;

            var transitionsPreset = new Dictionary<Transition, List<(Place place, int weight)>>();
            var transitionsPostset = new Dictionary<Transition, List<(Place place, int weight)>>();
            FillTransitionsArcs(newDpn, transitionsPreset, transitionsPostset);

            //var cycles = cyclesFinder.GetCycles(lts);

            var refinedTransitions = new List<Transition>();
            var refinedArcs = new List<Arc>();

            var transitionsDict = sourceDpn
                .Transitions
                .ToDictionary(x => x.Id);

            foreach (var sourceTransition in newDpn.Transitions)
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

                    _outputTransitionsCheck.AddRange(outputTransitions);

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
                            if (baseTransition.IsTau)
                            {
                                continue;
                            }
                            
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
                    var updatedConstraint = sourceDpn.Context.SimplifyExpression(updatedTransition.Guard.ActualConstraintExpression);
                    updatedTransition.Guard = Guard.MakeSimplified(updatedTransition.Guard, updatedConstraint);                                      

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

            newDpn.Transitions = refinedTransitions;
            newDpn.Arcs = refinedArcs;

            return newDpn;
        }

        public (DataPetriNet dpn, CoverabilityTree ct) TransformUsingCt(DataPetriNet sourceDpn, CoverabilityTree sourceCt = null)
        {
            DataPetriNet transformedDpn = sourceDpn;
            int sourceDpnTransitionCount;

            do
            {
                sourceCt = new CoverabilityTree(transformedDpn);
                sourceCt.GenerateGraph();

                if (sourceCt.LeafStates.Any(x => x.StateType == CtStateType.StrictlyCovered))
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
            _outputTransitionsCheck = new HashSet<Transition>();

            do
            {
                sourceLts = new ClassicalLabeledTransitionSystem(transformedDpn);
                sourceLts.GenerateGraph();

                if (!sourceLts.IsFullGraph)
                {
                    return (transformedDpn, sourceLts);
                }
                
                sourceDpnTransitionCount = transformedDpn.Transitions.Count;
                transformedDpn = PerformTransformationStep<LtsState, LtsTransition, LtsArc, LtsCycle>
                    (transformedDpn, cyclesFinder.GetCycles(sourceLts));
            } while (transformedDpn.Transitions.Count > sourceDpnTransitionCount);

            return (transformedDpn, sourceLts);
        }
        
        public (DataPetriNet dpn, CoverabilityGraph lts) TransformUsingCg
            (DataPetriNet sourceDpn, CoverabilityGraph sourceCg = null)
        {
            DataPetriNet transformedDpn = sourceDpn;
            int sourceDpnTransitionCount;
            _outputTransitionsCheck = new HashSet<Transition>();

            do
            {
                sourceCg = new CoverabilityGraph(transformedDpn);
                sourceCg.GenerateGraph();
                
                sourceDpnTransitionCount = transformedDpn.Transitions.Count;
                transformedDpn = PerformTransformationStep<LtsState, LtsTransition, LtsArc, LtsCycle>
                    (transformedDpn, cyclesFinder.GetCycles(sourceCg));
            } while (transformedDpn.Transitions.Count > sourceDpnTransitionCount);

            return (transformedDpn, sourceCg);
        }



        public static void FillTransitionsArcs(DataPetriNet sourceDpn, Dictionary<Transition, List<(Place place, int weight)>> transitionsPreset, Dictionary<Transition, List<(Place place, int weight)>> transitionsPostset)
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
