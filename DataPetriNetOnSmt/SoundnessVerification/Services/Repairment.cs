using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    public class Repairment
    {
        private TransformerToRefined transformerToRefined;

        public Repairment()
        {
            transformerToRefined = new TransformerToRefined();
            
        }
        public (DataPetriNet dpn, bool result) RepairDpn(DataPetriNet sourceDpn, bool mergeTransitionsBack = true)
        {
            // Perform until stabilization
            // Termination - either if all are green or all are red

            var dpnToConsider = (DataPetriNet)sourceDpn.Clone();
            var repairmentSuccessfullyFinished = false;
            var repairmentFailed = false;
            var firstIteration = true;
            var allGreenOnPreviousStep = false;
            //CoverabilityTree currentCoverabilityTree = null;
            ColoredConstraintGraph coloredConstraintGraph = null;
            HashSet<string> transitionsUpdatedAtPreviousStep= new HashSet<string>();
            HashSet<string> transitionsToTrySimplify= new HashSet<string>();

            var transitionsDict = dpnToConsider.Transitions.ToDictionary(x => x.Id, y => y);
            //var dpnBeforeLastRefinement = dpnToConsider;

            do
            {
                if (firstIteration || allGreenOnPreviousStep) 
                {
                    /*if (allGreenOnPreviousStep)
                    {
                        MergeTransitions(dpnToConsider);
                    }*/
                    // Maybe merge using a tree?

                    //dpnBeforeLastRefinement = dpnToConsider;

                    (var refinedDpn, _) = transformerToRefined.TransformUsingLts(dpnToConsider);
                    if (refinedDpn.Transitions.Count != dpnToConsider.Transitions.Count)
                    {
                        transitionsToTrySimplify = transitionsToTrySimplify
                            .Intersect(refinedDpn.Transitions.Select(x=>x.Id))
                            .ToHashSet();
                        transitionsUpdatedAtPreviousStep.Clear();

                        dpnToConsider = refinedDpn;
                        transitionsDict = dpnToConsider.Transitions.ToDictionary(x => x.Id, y => y);
                    }
                }

                bool allNodesGreen;
                bool allNodesRed;

                if (firstIteration)
                {
                    // We can switch to false if want only to make net bounded
                    var coloredCoverabilityTree = new CoverabilityTree(dpnToConsider, withTauTransitions: true);
                    coloredCoverabilityTree.GenerateGraph();

                    allNodesGreen = coloredCoverabilityTree.ConstraintStates.All(x => x.StateColor == CtStateColor.Green);
                    allNodesRed = coloredCoverabilityTree.ConstraintStates.All(x => x.StateColor == CtStateColor.Red);

                    if (!allNodesGreen && !allNodesRed)
                    {
                        (dpnToConsider, transitionsUpdatedAtPreviousStep) = MakeRepairStep(dpnToConsider, coloredCoverabilityTree, transitionsDict);
                        transitionsToTrySimplify = transitionsToTrySimplify.Union(transitionsUpdatedAtPreviousStep).ToHashSet();
                    }
                    else
                    {
                        RemoveDeadTransitions(dpnToConsider, coloredCoverabilityTree);
                    }
                }
                else
                {
                    coloredConstraintGraph = new ColoredConstraintGraph(dpnToConsider);
                    coloredConstraintGraph.GenerateGraph();

                    allNodesGreen = coloredConstraintGraph.StateColorDictionary.All(x => x.Value == CtStateColor.Green);
                    allNodesRed = coloredConstraintGraph.StateColorDictionary.All(x => x.Value == CtStateColor.Red);

                    if (!allNodesGreen && !allNodesRed)
                    {
                        transitionsToTrySimplify = transitionsToTrySimplify.Union(transitionsUpdatedAtPreviousStep).ToHashSet();
                        (dpnToConsider, transitionsUpdatedAtPreviousStep) = MakeRepairStep(dpnToConsider, coloredConstraintGraph, transitionsDict);
                        transitionsToTrySimplify = transitionsToTrySimplify.Except(transitionsUpdatedAtPreviousStep).ToHashSet();
                        
                        TryRollbackTransitionGuards(dpnToConsider, coloredConstraintGraph, transitionsToTrySimplify, transitionsDict);
                    }
                }

                repairmentSuccessfullyFinished = allNodesGreen && allGreenOnPreviousStep;
                repairmentFailed = allNodesRed;
                allGreenOnPreviousStep = allNodesGreen;

                firstIteration = false;
            } while (!repairmentSuccessfullyFinished && !repairmentFailed);

            transitionsToTrySimplify = transitionsToTrySimplify.Union(transitionsUpdatedAtPreviousStep).ToHashSet();
            TryRollbackTransitionGuards(dpnToConsider, coloredConstraintGraph, transitionsToTrySimplify, transitionsDict);


            if (repairmentSuccessfullyFinished)
            {
                if (coloredConstraintGraph != null)
                    RemoveDeadTransitions(dpnToConsider, coloredConstraintGraph);
                
                RemoveIsolatedPlaces(dpnToConsider);

                if (mergeTransitionsBack)
                    MergeTransitions(dpnToConsider);
            }

            var resultDpn = repairmentSuccessfullyFinished
                ? dpnToConsider
                : sourceDpn;

            return (resultDpn, repairmentSuccessfullyFinished);
            

            static void RemoveIsolatedPlaces(DataPetriNet sourceDpn)
            {
                sourceDpn.Places.RemoveAll(x => !sourceDpn.Arcs.Select(x => x.Source).Union(sourceDpn.Arcs.Select(x => x.Destination)).Contains(x));
            }

            static void MergeTransitions(DataPetriNet dpnToConsider)
            {
                var baseTransitions = dpnToConsider.Transitions
                                    .GroupBy(x => x.BaseTransitionId)
                                    .Where(x => x.Count() > 1);

                var preset = new Dictionary<Transition, List<(Place place, int weight)>>();
                var postset = new Dictionary<Transition, List<(Place place, int weight)>>();

                TransformerToRefined.FillTransitionsArcs(dpnToConsider, preset, postset);

                foreach (var baseTransition in baseTransitions)
                {
                    var resultantConstraint = (BoolExpr)dpnToConsider.Context.MkOr(baseTransition.Select(x => x.Guard.ActualConstraintExpression)).Simplify();
                    resultantConstraint = dpnToConsider.Context.SimplifyExpression(resultantConstraint);

                    var transitionToInspect = baseTransition.First();

                    var guard = Guard.MakeMerged(transitionToInspect.Guard, resultantConstraint);
                    var transitionToAdd = new Transition(baseTransition.Key, guard);
                    dpnToConsider.Transitions.RemoveAll(x => baseTransition.Contains(x));
                    dpnToConsider.Transitions.Add(transitionToAdd);

                    dpnToConsider.Arcs.RemoveAll(x => baseTransition.Contains(x.Source) || baseTransition.Contains(x.Destination));
                    foreach (var inputArc in preset[transitionToInspect])
                    {
                        dpnToConsider.Arcs.Add(new Arc(inputArc.place, transitionToAdd, inputArc.weight));
                    }
                    foreach (var ouputArc in postset[transitionToInspect])
                    {
                        dpnToConsider.Arcs.Add(new Arc(transitionToAdd, ouputArc.place, ouputArc.weight));
                    }
                }
            }
        }

        private static void RemoveDeadTransitions(DataPetriNet sourceDpn, ColoredConstraintGraph cg)
        {
            var transitionsInCt = cg.ConstraintArcs
                                .Where(x => !x.Transition.IsSilent)
                                .Select(x => x.Transition.Id)
                                .ToHashSet();

            sourceDpn.Transitions.RemoveAll(x => !transitionsInCt.Contains(x.Id));
            sourceDpn.Arcs.RemoveAll(x => x.Source is Transition && !transitionsInCt.Contains(x.Source.Id)
                || x.Destination is Transition && !transitionsInCt.Contains(x.Destination.Id));
        }

        private static void RemoveDeadTransitions(DataPetriNet sourceDpn, CoverabilityTree cg)
        {
            var transitionsInCt = cg.ConstraintArcs
                                .Where(x => !x.Transition.IsSilent)
                                .Select(x => x.Transition.Id)
                                .ToHashSet();

            sourceDpn.Transitions.RemoveAll(x => !transitionsInCt.Contains(x.Id));
            sourceDpn.Arcs.RemoveAll(x => x.Source is Transition && !transitionsInCt.Contains(x.Source.Id)
                || x.Destination is Transition && !transitionsInCt.Contains(x.Destination.Id));
        }

        private static void TryRollbackTransitionGuards(DataPetriNet sourceDpn, ConstraintGraph constraintGraph, HashSet<string> transitionsToTrySimplify, Dictionary<string, Transition> transitionsDict)
        {
            var expressionService = new ConstraintExpressionService(sourceDpn.Context);

            var arcsToConsider = constraintGraph.ConstraintArcs
                .Where(x => transitionsToTrySimplify.Contains(x.Transition.Id))
                .GroupBy(x => x.Transition.Id);

            var baseTauTransitionsGuards = new Dictionary<Transition, BoolExpr>();
            foreach (var transitionId in transitionsToTrySimplify)
            {
                var smtExpression = transitionsDict[transitionId].Guard.ConstraintExpressionBeforeUpdate;
                var overwrittenVarNames = transitionsDict[transitionId].Guard.WriteVars;
                var readExpression = sourceDpn.Context.GetReadExpression(smtExpression, overwrittenVarNames);

                var negatedGuardExpressions = sourceDpn.Context.MkNot(readExpression);
                baseTauTransitionsGuards.Add(transitionsDict[transitionId], negatedGuardExpressions);
            }

            foreach (var arcGroup in arcsToConsider)
            {
                var transition = transitionsDict[arcGroup.Key];
                var canBeReplacedWithSourceConstraint = true;

                var baseTransitionConstraint = transition.Guard.ConstraintExpressionBeforeUpdate;
                var baseTauTransitionGuard = baseTauTransitionsGuards[transition];
                var overwrittenVarNames = transition.Guard.WriteVars;



                foreach (var arc in arcGroup)
                {
                    var resultantConstraintExpression = arc.Transition.IsSilent
                        ? expressionService
                        .ConcatExpressions(arc.SourceState.Constraints, baseTauTransitionGuard, new Dictionary<string, DomainType>())
                        : expressionService
                        .ConcatExpressions(arc.SourceState.Constraints, baseTransitionConstraint, overwrittenVarNames);

                    canBeReplacedWithSourceConstraint &= expressionService.AreEqual(resultantConstraintExpression, arc.TargetState.Constraints);
                    if (!canBeReplacedWithSourceConstraint)
                    {
                        break;
                    }
                }

                if (canBeReplacedWithSourceConstraint)
                {
                    transition.Guard.UndoRepairment();
                }
            }
        }

        public (DataPetriNet dpn, HashSet<string> updatedTransitions) MakeRepairStep(DataPetriNet sourceDpn, ColoredConstraintGraph cg, Dictionary<string, Transition> transitionsDict)
        {
            var arcsDict = cg.ConstraintArcs
                .GroupBy(x=> (x.SourceState, x.TargetState))
                .ToDictionary(x => x.Key, y => y.Select(x=>x.Transition).ToArray());
            var childrenDict = cg.ConstraintArcs
                .GroupBy(x => x.SourceState)
                .ToDictionary(x=>x.Key, y=>y.Select(x=>x.TargetState).ToArray());
            var parentsDict = cg.ConstraintArcs
                .GroupBy(x => x.TargetState)
                .ToDictionary(x => x.Key, y => y.ToArray());

            // Find green nodes which contain red nodes
            var criticalNodes = cg.ConstraintStates
                .Where(x => cg.StateColorDictionary[x] == CtStateColor.Green &&
                    childrenDict.TryGetValue(x, out var children) && children.Any(y => cg.StateColorDictionary[y] == CtStateColor.Red))
                .ToDictionary(x => x, y => childrenDict[y].Where(x => cg.StateColorDictionary[x] == CtStateColor.Red).ToList());

            var expressionsForTransitions = new Dictionary<Transition, List<BoolExpr>>();
            foreach (var transition in sourceDpn.Transitions)
            {
                expressionsForTransitions[transition] = new List<BoolExpr>();
            }

            foreach (var nodeGroup in criticalNodes)
            {
                foreach (var childNode in nodeGroup.Value)
                {
                    var arcsBetweenNodes = arcsDict[(nodeGroup.Key, childNode)];

                    foreach (var arcBetweenNodes in arcsBetweenNodes)
                    {
                        if (arcBetweenNodes.IsSilent)
                        {
                            // For each inverted (simple) path from parent, find ordinary t, add constraint to this t


                            // Take nearest ordinary (non-silent) transition
                            // The case when no such transition exists (tau from initial state) cannot exist

                            UpdateUpperTransitionsRecursively(
                                nodeGroup.Key,
                                childNode.Constraints,
                                parentsDict,
                                childrenDict,
                                transitionsDict,
                                sourceDpn.Context,
                                expressionsForTransitions,
                                new List<LtsArc>());


                        }
                        else
                        {
                            // It is ok

                            var transitionToUpdate = transitionsDict[arcBetweenNodes.Id];
                            var formulaToConjunct = sourceDpn.Context.MkNot(childNode.Constraints);

                            var overwrittenVars = transitionToUpdate.Guard.WriteVars;

                            foreach (var overwrittenVar in overwrittenVars)
                            {
                                var readVar = sourceDpn.Context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Read);
                                var writeVar = sourceDpn.Context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Written);

                                formulaToConjunct = (BoolExpr)formulaToConjunct.Substitute(readVar, writeVar);
                            }

                            expressionsForTransitions[transitionToUpdate].Add(formulaToConjunct);
                        }
                    }
                }
            }

            var updatedTransitions = new HashSet<string>();
            foreach (var transition in sourceDpn.Transitions)
            {
                if (expressionsForTransitions[transition].Count > 0)
                {
                    expressionsForTransitions[transition].Add(transition.Guard.ActualConstraintExpression);

                    var newCondition = sourceDpn.Context.SimplifyExpression(sourceDpn.Context.MkAnd(expressionsForTransitions[transition]));

                    transition.Guard = Guard.MakeRepaired(transition.Guard, newCondition);

                    updatedTransitions.Add(transition.Id);
                }
            }

            return (sourceDpn, updatedTransitions);
        }

        // Maybe move dictionaries to static
        private void UpdateUpperTransitionsRecursively(
            LtsState currentNode, 
            BoolExpr badNodeConstraint,
            Dictionary<LtsState, LtsArc[]> parentsDict, 
            Dictionary<LtsState, LtsState[]> childrenDict,
            Dictionary<string, Transition> transitionsDict,
            Context context,
            Dictionary<Transition, List<BoolExpr>> expressionsForTransitions,
            List<LtsArc> visitedArcs)
        {
            foreach (var arc in parentsDict[currentNode].Except(visitedArcs))
            {
                if (arc.Transition.IsSilent)
                {
                    visitedArcs.Add(arc);
                    // Cycles! Consider only simple paths!!!
                    UpdateUpperTransitionsRecursively(
                        arc.SourceState, 
                        badNodeConstraint, 
                        parentsDict, 
                        childrenDict, 
                        transitionsDict, 
                        context,
                        expressionsForTransitions,
                        visitedArcs);
                    visitedArcs.Remove(arc);
                }
                else
                {
                    var overwrittenVars = transitionsDict[arc.Transition.Id].Guard.WriteVars;
                    var formulaToConjunct = context.MkNot(badNodeConstraint);

                    foreach (var overwrittenVar in overwrittenVars)
                    {
                        var readVar = context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Read);
                        var writeVar = context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Written);

                        formulaToConjunct = (BoolExpr)formulaToConjunct.Substitute(readVar, writeVar);
                    }

                    expressionsForTransitions[transitionsDict[arc.Transition.Id]].Add(formulaToConjunct);
                }
            }
        }

        public (DataPetriNet dpn, HashSet<string> updatedTransitions) MakeRepairStep(DataPetriNet sourceDpn, CoverabilityTree ct, Dictionary<string, Transition> transitionsDict)
        {
            var arcsDict = ct.ConstraintArcs.ToDictionary(x => (x.SourceState, x.TargetState), y => y.Transition);

            // Find green nodes which contain red nodes
            var criticalNodes = ct.ConstraintStates
                .Where(x => x.StateColor == CtStateColor.Red && x.ParentNode != null && x.ParentNode.StateColor == CtStateColor.Green)
                .GroupBy(x => x.ParentNode)
                .ToList();

            var expressionsForTransitions = new Dictionary<Transition, List<BoolExpr>>();
            foreach (var transition in sourceDpn.Transitions)
            {
                expressionsForTransitions[transition] = new List<BoolExpr>();
            }

            foreach (var nodeGroup in criticalNodes)
            {
                foreach (var childNode in nodeGroup)
                {
                    var arcBetweenNodes = arcsDict[(nodeGroup.Key, childNode)];

                    if (arcBetweenNodes.IsSilent)
                    {
                        // Take nearest ordinary (non-silent) transition
                        // The case when no such transition exists (tau from initial state) cannot exist
                        var parentNode = nodeGroup.Key;
                        Transition predecessorTransition = null;
                        while (parentNode.ParentNode != null && predecessorTransition == null)
                        {
                            var arcToConsider = arcsDict[(parentNode.ParentNode, parentNode)];
                            if (!arcToConsider.IsSilent)
                            {
                                predecessorTransition = transitionsDict[arcToConsider.Id];
                            }
                            parentNode = parentNode.ParentNode;
                        }

                        var overwrittenVars = predecessorTransition.Guard.WriteVars;
                        var formulaToConjunct = sourceDpn.Context.MkNot(childNode.Constraints);

                        foreach (var overwrittenVar in overwrittenVars)
                        {
                            var readVar = sourceDpn.Context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Read);
                            var writeVar = sourceDpn.Context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Written);

                            formulaToConjunct = (BoolExpr)formulaToConjunct.Substitute(readVar, writeVar);
                        }

                        expressionsForTransitions[predecessorTransition].Add(formulaToConjunct);
                    }
                    else
                    {
                        var transitionToUpdate = transitionsDict[arcBetweenNodes.Id];
                        var formulaToConjunct = sourceDpn.Context.MkNot(childNode.Constraints);

                        var overwrittenVars = transitionToUpdate.Guard.WriteVars;

                        foreach (var overwrittenVar in overwrittenVars)
                        {
                            var readVar = sourceDpn.Context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Read);
                            var writeVar = sourceDpn.Context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Written);

                            formulaToConjunct = (BoolExpr)formulaToConjunct.Substitute(readVar, writeVar);
                        }

                        expressionsForTransitions[transitionToUpdate].Add(formulaToConjunct);
                    }
                }
            }

            var updatedTransitions = new HashSet<string>();
            foreach (var transition in sourceDpn.Transitions)
            {
                if (expressionsForTransitions[transition].Count > 0)
                {
                    expressionsForTransitions[transition].Add(transition.Guard.ActualConstraintExpression);

                    var newCondition = sourceDpn.Context.SimplifyExpression(sourceDpn.Context.MkAnd(expressionsForTransitions[transition]));

                    transition.Guard = Guard.MakeRepaired(transition.Guard, newCondition);

                    updatedTransitions.Add(transition.Id);
                }
            }

            return (sourceDpn, updatedTransitions);
        }

        

        
    }
}
