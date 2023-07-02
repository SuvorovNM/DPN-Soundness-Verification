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
        private delegate BoolExpr ConnectExpressions(params BoolExpr[] expr);

        public Repairment()
        {
            transformerToRefined = new TransformerToRefined();
        }
        public (DataPetriNet dpn, bool result) RepairDpn(DataPetriNet sourceDpn, bool mergeTransitionsBack = false)
        {
            // Perform until stabilization
            // Termination - either if all are green or all are red

            var dpnToConsider = (DataPetriNet)sourceDpn.Clone();
            var repairmentSuccessfullyFinished = false;
            var repairmentFailed = false;
            var allGreenOnPreviousStep = true;
            CoverabilityTree currentCoverabilityTree;

            //(dpnToConsider, _) = transformerToRefined.TransformUsingCt(dpnToConsider);
            var transitionsDict = dpnToConsider.Transitions.ToDictionary(x => x.Id, y => y);

            do
            {
                // Perform only when output transition is modified
                // OR - refine only if all green
                if (allGreenOnPreviousStep)
                {
                    (var refinedDpn, _) = transformerToRefined.TransformUsingLts(dpnToConsider);
                    if (refinedDpn.Transitions.Count != dpnToConsider.Transitions.Count)
                    {
                        dpnToConsider = refinedDpn;
                        transitionsDict = dpnToConsider.Transitions.ToDictionary(x => x.Id, y => y);
                    }
                }

                currentCoverabilityTree = new CoverabilityTree(dpnToConsider, withTauTransitions: true);
                currentCoverabilityTree.GenerateGraph();

                var allNodesGreen = currentCoverabilityTree.ConstraintStates.All(x => x.StateColor == CtStateColor.Green);
                var allNodesRed = currentCoverabilityTree.ConstraintStates.All(x => x.StateColor == CtStateColor.Red);

                repairmentSuccessfullyFinished = allNodesGreen;
                repairmentFailed = allNodesRed;

                if (!repairmentSuccessfullyFinished && !repairmentFailed)
                {
                    dpnToConsider = MakeRepairStep(dpnToConsider, currentCoverabilityTree, transitionsDict);                    
                }

                repairmentSuccessfullyFinished &= allGreenOnPreviousStep;
                allGreenOnPreviousStep = allNodesGreen;

            } while (!repairmentSuccessfullyFinished && !repairmentFailed);


            if (repairmentSuccessfullyFinished)
            {
                RemoveDeadTransitions(dpnToConsider, currentCoverabilityTree);
                RemoveIsolatedPlaces(dpnToConsider);

                if (mergeTransitionsBack)
                    MergeTransitions(dpnToConsider);

                foreach (var transition in dpnToConsider.Transitions)
                {
                    var goal = dpnToConsider.Context.MkGoal();
                    var nnfTactic = dpnToConsider.Context.MkTactic("nnf");

                    goal.Assert(transition.Guard.ActualConstraintExpression);
                    var result = nnfTactic.Apply(goal).Subgoals[0].AsBoolExpr();
                    //var result = transition.Guard.ActualConstraintExpression;

                    var simplifyTactic = dpnToConsider.Context.MkTactic("ctx-simplify");
                    goal.Reset();
                    goal.Assert(result);
                    result = simplifyTactic.Apply(goal).Subgoals[0].Simplify().AsBoolExpr();

                    transition.Guard = new Guard(dpnToConsider.Context, transition.Guard.BaseConstraintExpressions, result);
                }
            }

            var resultDpn = repairmentSuccessfullyFinished
                ? dpnToConsider
                : sourceDpn;

            return (resultDpn, repairmentSuccessfullyFinished);

            static void RemoveDeadTransitions(DataPetriNet sourceDpn, CoverabilityTree currentCoverabilityTree)
            {
                var transitionsInCt = currentCoverabilityTree.ConstraintArcs
                                    .Where(x => !x.Transition.IsSilent)
                                    .Select(x => x.Transition.Id)
                                    .ToHashSet();

                sourceDpn.Transitions.RemoveAll(x => !transitionsInCt.Contains(x.Id));
                sourceDpn.Arcs.RemoveAll(x => x.Source is Transition && !transitionsInCt.Contains(x.Source.Id)
                    || x.Destination is Transition && !transitionsInCt.Contains(x.Destination.Id));
            }

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
                    var transitionToInspect = baseTransition.First();

                    var guard = new Guard(dpnToConsider.Context, transitionToInspect.Guard.BaseConstraintExpressions, resultantConstraint);
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
        public DataPetriNet MakeRepairStep(DataPetriNet sourceDpn, CoverabilityTree ct, Dictionary<string, Transition> transitionsDict)
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

            var simplifyTactic = sourceDpn.Context.MkTactic("ctx-simplify");
            var nnfTactic = sourceDpn.Context.MkTactic("nnf");
            foreach (var transition in sourceDpn.Transitions)
            {
                if (expressionsForTransitions[transition].Count > 0)
                {
                    expressionsForTransitions[transition].Add(transition.Guard.ActualConstraintExpression);
                    var goal = sourceDpn.Context.MkGoal();
                    goal.Assert(expressionsForTransitions[transition].ToArray());
                    var conditionToSet = simplifyTactic.Apply(goal).Subgoals[0].Simplify().AsBoolExpr();
                    goal.Reset();
                    goal.Assert(conditionToSet);
                    conditionToSet = nnfTactic.Apply(goal).Subgoals[0].AsBoolExpr();

                    var newCondition = SimplifyRecursive(sourceDpn.Context, conditionToSet);

                    transition.Guard = new Guard(sourceDpn.Context, transition.Guard.BaseConstraintExpressions,
                        newCondition);
                }
            }

            return sourceDpn;
        }

        private BoolExpr SimplifyRecursive(Context context, BoolExpr expr)
        {
            var simplifyTactic = context.MkTactic("ctx-solver-simplify");
            var currentFormula = expr;
            if (currentFormula.IsAnd || currentFormula.IsOr)
            {
                var simplifiedExpressions = new List<BoolExpr>(currentFormula.Args.Length);
                foreach (BoolExpr arg in currentFormula.Args)
                {
                    var simplifiedArgExpression = SimplifyRecursive(context, arg);
                    simplifiedExpressions.Add(simplifiedArgExpression);
                }

                BoolExpr simplifiedExpression;
                if (currentFormula.IsOr)
                {
                    simplifiedExpression = SimplifyDisjunction(context, simplifiedExpressions);
                }
                else
                {
                    simplifiedExpression = SimplifyConjunction(context, simplifiedExpressions);
                }

                simplifiedExpression = currentFormula.IsAnd
                    ? context.MkAnd(simplifiedExpressions)
                    : context.MkOr(simplifiedExpressions);

                var goal = context.MkGoal();
                goal.Assert(simplifiedExpression);
                return simplifyTactic.Apply(goal).Subgoals[0].Simplify().AsBoolExpr();
            }
            
            

            return expr;
        }

        private static BoolExpr Simplify(Context context, List<BoolExpr> simplifiedExpressions, ConnectExpressions connectAction)
        {
            var index = 0;

            while (index < simplifiedExpressions.Count)
            {
                var totalExpression = connectAction(simplifiedExpressions.ToArray());
                var cutExpression = connectAction(simplifiedExpressions.Except(new[] { simplifiedExpressions[index] }).ToArray());

                if (context.AreEqual(totalExpression, cutExpression))
                {
                    simplifiedExpressions.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            return connectAction(simplifiedExpressions.ToArray());
        }

        private static BoolExpr SimplifyDisjunction(Context context, List<BoolExpr> simplifiedExpressions)
        {
            return Simplify(context, simplifiedExpressions, context.MkOr);
        }

        private static BoolExpr SimplifyConjunction(Context context, List<BoolExpr> simplifiedExpressions)
        {
            return Simplify(context, simplifiedExpressions, context.MkAnd);
        }
    }
}
