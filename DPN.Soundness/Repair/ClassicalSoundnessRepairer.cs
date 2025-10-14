using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DPN.Models;
using DPN.Models.Abstractions;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using DPN.Models.Extensions;
using DPN.Soundness.Transformations;
using DPN.Soundness.TransitionSystems.Coverability;
using DPN.Soundness.TransitionSystems.Reachability;
using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;
using Microsoft.Z3;

namespace DPN.Soundness.Repair;

public static class ClassicalRepairSettingsConstants
{
	public const string MergeTransitionsBack = nameof(MergeTransitionsBack);
	public const string True = nameof(True);
	public const string False = nameof(False);
}

[SuppressMessage("ReSharper", "CoVariantArrayConversion")]
public class ClassicalSoundnessRepairer
{
	// Repair is done until stabilization.
	// Algorithm terminates either if no paths remain leading to failure points or if all paths start to leading to failure points
	public RepairResult Repair(DataPetriNet sourceDpn, Dictionary<string, string> repairProperties)
	{
		var mergeTransitionsBack = GetMergeTransitionsBackProperty(repairProperties);

		var transformerToRefined = new TransformerToRefined();
		var stopwatch = Stopwatch.StartNew();
		
		var dpnToConsider = (DataPetriNet)sourceDpn.Clone();
		bool repairmentSuccessfullyFinished;
		bool repairmentFailed;
		var firstIteration = true;
		var allGreenOnPreviousStep = false;
		ColoredConstraintGraph? coloredConstraintGraph = null;
		var transitionsUpdatedAtPreviousStep = new HashSet<string>();
		var transitionsToTrySimplify = new HashSet<string>();

		var transitionsDict = dpnToConsider.Transitions.ToDictionary(x => x.Id, y => y);
		uint repairSteps = 0;

		do
		{
			if (firstIteration || allGreenOnPreviousStep)
			{
				var refinedDpn = transformerToRefined
					.Transform(
						dpnToConsider, 
						new Dictionary<string, string>
						{
							{RefinementSettingsConstants.BaseStructure, RefinementSettingsConstants.FiniteReachabilityGraph}
						})
					.RefinedDpn;
				if (refinedDpn.Transitions.Count != dpnToConsider.Transitions.Count)
				{
					transitionsToTrySimplify = transitionsToTrySimplify
						.Intersect(refinedDpn.Transitions.Select(x => x.Id))
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
				// We can switch withTauTransitions to false if want only to make net bounded
				var coloredCoverabilityTree = new CoverabilityTree(dpnToConsider, stopOnCoveringFinalPosition: true, withTauTransitions: true);
				coloredCoverabilityTree.GenerateGraph();

				allNodesGreen = coloredCoverabilityTree.ConstraintStates.All(x => x.StateColor == CtStateColor.Green);
				allNodesRed = coloredCoverabilityTree.ConstraintStates.All(x => x.StateColor == CtStateColor.Red);

				if (!allNodesGreen && !allNodesRed)
				{
					(dpnToConsider, transitionsUpdatedAtPreviousStep) = MakeRepairStep(dpnToConsider, coloredCoverabilityTree, transitionsDict);
					repairSteps++;
					transitionsToTrySimplify = transitionsToTrySimplify.Union(transitionsUpdatedAtPreviousStep).ToHashSet();
				}
				else
				{
					RemoveDeadTransitions(dpnToConsider, coloredCoverabilityTree.ConstraintArcs.ToArray());

					return new RepairResult(dpnToConsider, allNodesGreen, 0, stopwatch.Elapsed);
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
					repairSteps++;
					transitionsToTrySimplify = transitionsToTrySimplify.Except(transitionsUpdatedAtPreviousStep).ToHashSet();

					TryRollbackTransitionGuards(dpnToConsider, coloredConstraintGraph, transitionsToTrySimplify, transitionsDict);
				}
			}

			repairmentSuccessfullyFinished = allNodesGreen && allGreenOnPreviousStep;
			repairmentFailed = allNodesRed;
			allGreenOnPreviousStep = allNodesGreen;

			firstIteration = false;
		} while (!repairmentSuccessfullyFinished && !repairmentFailed);


		if (repairmentSuccessfullyFinished)
		{
			if (coloredConstraintGraph != null)
				RemoveDeadTransitions(dpnToConsider, coloredConstraintGraph.ConstraintArcs.ToArray());

			RemoveIsolatedPlaces(dpnToConsider);

			if (mergeTransitionsBack)
				MergeTransitions(dpnToConsider);
			
			// TODO: add a step of rolling back transitions?
		}

		var resultDpn = repairmentSuccessfullyFinished
			? dpnToConsider
			: sourceDpn;

		return new RepairResult(resultDpn, repairmentSuccessfullyFinished, repairSteps, stopwatch.Elapsed);


		static void RemoveIsolatedPlaces(DataPetriNet sourceDpn)
		{
			sourceDpn.Places.RemoveAll(p => !sourceDpn.Arcs.Select(a => a.Source).Union(sourceDpn.Arcs.Select(a => a.Destination)).Contains(p));
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

	private static bool GetMergeTransitionsBackProperty(Dictionary<string, string> repairProperties)
	{
		var mergeTransitionsBack = true;
		if (repairProperties.TryGetValue(ClassicalRepairSettingsConstants.MergeTransitionsBack, out var mergeTransitionsBackString))
		{
			if (!bool.TryParse(mergeTransitionsBackString, out mergeTransitionsBack))
			{
				throw new ArgumentException($"Invalid value for parameter {nameof(ClassicalRepairSettingsConstants.MergeTransitionsBack)}");
			}
		}

		return mergeTransitionsBack;
	}

	private static void RemoveDeadTransitions<TState,TTransition>(DataPetriNet sourceDpn, AbstractArc<TState, TTransition>[] arcs) 
		where TState : AbstractState
		where TTransition : AbstractTransition
	{
		var transitionsInCt = arcs
			.Where(x => !x.Transition.IsSilent)
			.Select(x => x.Transition.Id)
			.ToHashSet();

		sourceDpn.Transitions.RemoveAll(x => !transitionsInCt.Contains(x.Id));
		sourceDpn.Arcs.RemoveAll(x => x.Source is Transition && !transitionsInCt.Contains(x.Source.Id)
		                              || x.Destination is Transition && !transitionsInCt.Contains(x.Destination.Id));
	}

	// Some transition restriction is redundant - we, thus, rollback what we can
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

	private (DataPetriNet dpn, HashSet<string> updatedTransitions) MakeRepairStep(DataPetriNet sourceDpn, ColoredConstraintGraph cg, Dictionary<string, Transition> transitionsDict)
	{
		var arcsDict = cg.ConstraintArcs
			.GroupBy(x => (x.SourceState.Id, x.TargetState))
			.ToDictionary(x => x.Key, y => y.Select(x => x.Transition).ToArray());
		var childrenDict = cg.ConstraintArcs
			.GroupBy(x => x.SourceState)
			.ToDictionary(x => x.Key.Id, y => y.Select(x => x.TargetState).ToArray());
		var parentsDict = cg.ConstraintArcs
			.GroupBy(x => x.TargetState)
			.ToDictionary(x => x.Key.Id, y => y.ToArray());

		// Find green nodes which contain red nodes
		var criticalNodes = cg.ConstraintStates
			.Where(x => cg.StateColorDictionary[x] == CtStateColor.Green &&
			            childrenDict.TryGetValue(x.Id, out var children) && children.Any(y => cg.StateColorDictionary[y] == CtStateColor.Red))
			.ToDictionary(x => x, y => childrenDict[y.Id].Where(x => cg.StateColorDictionary[x] == CtStateColor.Red).ToList());

		var expressionsForTransitions = new Dictionary<Transition, List<BoolExpr>>();
		foreach (var transition in sourceDpn.Transitions)
		{
			expressionsForTransitions[transition] = new List<BoolExpr>();
		}

		foreach (var nodeGroup in criticalNodes)
		{
			foreach (var childNode in nodeGroup.Value)
			{
				var arcsBetweenNodes = arcsDict[(nodeGroup.Key.Id, childNode)];

				foreach (var arcBetweenNodes in arcsBetweenNodes)
				{
					if (arcBetweenNodes.IsSilent)
					{
						// For each inverted (simple) path from parent, find ordinary t, add constraint to this t
						// Take nearest ordinary (non-silent) transition
						// The case when no such transition exists (tau from initial state) cannot exist

						//TODO: here error on dict (to repair dig transfer)
						UpdateUpperTransitionsRecursively(
							nodeGroup.Key,
							childNode.Constraints!,
							parentsDict,
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
		Dictionary<int, LtsArc[]> parentsDict,
		Dictionary<string, Transition> transitionsDict,
		Context context,
		Dictionary<Transition, List<BoolExpr>> expressionsForTransitions,
		List<LtsArc> visitedArcs)
	{
		foreach (var arc in parentsDict[currentNode.Id].Except(visitedArcs))
		{
			if (arc.Transition.IsSilent)
			{
				visitedArcs.Add(arc);
				UpdateUpperTransitionsRecursively(
					arc.SourceState,
					badNodeConstraint,
					parentsDict,
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

	private (DataPetriNet dpn, HashSet<string> updatedTransitions) MakeRepairStep(DataPetriNet sourceDpn, CoverabilityTree ct, Dictionary<string, Transition> transitionsDict)
	{
		var arcsDict = ct.ConstraintArcs
			.ToDictionary(x => (x.SourceState.Id, x.TargetState.Id), y => y.Transition);

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
				var arcBetweenNodes = arcsDict[(nodeGroup.Key!.Id, childNode.Id)];

				if (arcBetweenNodes.IsSilent)
				{
					// Take nearest ordinary (non-silent) transition
					// The case when no such transition exists (tau from initial state) cannot exist
					var parentNode = nodeGroup.Key;
					Transition? predecessorTransition = null;
					while (parentNode.ParentNode != null && predecessorTransition == null)
					{
						var arcToConsider = arcsDict[(parentNode.ParentNode.Id, parentNode.Id)];
						if (!arcToConsider.IsSilent)
						{
							predecessorTransition = transitionsDict[arcToConsider.Id];
						}

						parentNode = parentNode.ParentNode;
					}

					var overwrittenVars = predecessorTransition!.Guard.WriteVars;
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