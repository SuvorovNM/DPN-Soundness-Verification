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
public class ClassicalSoundnessRepairer : ISoundnessRepairer
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
		ColoredCoverabilityGraph? coloredCoverabilityGraph = null;
		var transitionsUpdatedAtPreviousStep = new HashSet<string>();
		var transitionsToTrySimplify = new HashSet<string>();
		var totalModifiedTransitions = new HashSet<string>();
		var statesConstructed = 0;

		var transitionsDict = dpnToConsider.Transitions.ToDictionary(x => x.Id, y => y);
		ushort repairSteps = 0;


		/*coloredCoverabilityGraph = new ColoredCoverabilityGraph(dpnToConsider, withTau: false, tryReachAllOmegas: false);
		coloredCoverabilityGraph.GenerateGraph();
		statesConstructed += coloredCoverabilityGraph.ConstraintArcs.Count;
		(dpnToConsider, transitionsUpdatedAtPreviousStep) = MakeRepairStep(dpnToConsider, coloredCoverabilityGraph, transitionsDict);
		repairSteps++;
		totalModifiedTransitions.AddRange(transitionsUpdatedAtPreviousStep.Select(t => transitionsDict[t].BaseTransitionId));
		transitionsToTrySimplify = transitionsToTrySimplify.Union(transitionsUpdatedAtPreviousStep).ToHashSet();*/

		do
		{
			// TODO: перепроверить на 4 примере. Обратный мержинг дает уже не sound, что странно

			if (repairSteps >= 0)
			{
				var refinedDpn = coloredCoverabilityGraph == null
					? transformerToRefined.Transform(dpnToConsider, new Dictionary<string, string>()).RefinedDpn
					: transformerToRefined.Transform(dpnToConsider, coloredCoverabilityGraph).RefinedDpn;

				if (refinedDpn.Transitions.Count != dpnToConsider.Transitions.Count)
				{
					transitionsToTrySimplify = transitionsToTrySimplify
						.Intersect(refinedDpn.Transitions.Select(x => x.Id))
						.ToHashSet();
					transitionsUpdatedAtPreviousStep.Clear();

					dpnToConsider = refinedDpn;
					dpnToConsider.Transitions
						.ForEach(t => transitionsDict[t.Id] = t);
				}
			}


			coloredCoverabilityGraph = new ColoredCoverabilityGraph(dpnToConsider, withTau: true, tryReachAllOmegas: false);
			coloredCoverabilityGraph.GenerateGraph();
			statesConstructed += coloredCoverabilityGraph.ConstraintArcs.Count;

			var allNodesGreen = coloredCoverabilityGraph.StateColorDictionary.All(x => x.Value == CtStateColor.Green);
			var allNodesRed = coloredCoverabilityGraph.StateColorDictionary.All(x => x.Value == CtStateColor.Red);

			if (!allNodesGreen && !allNodesRed)
			{
				transitionsToTrySimplify = transitionsToTrySimplify.Union(transitionsUpdatedAtPreviousStep).ToHashSet();
				(dpnToConsider, transitionsUpdatedAtPreviousStep) = MakeRepairStep(dpnToConsider, coloredCoverabilityGraph, transitionsDict);
				totalModifiedTransitions.AddRange(transitionsUpdatedAtPreviousStep.Select(t => transitionsDict[t].BaseTransitionId));

				repairSteps++;
				transitionsToTrySimplify = transitionsToTrySimplify.Except(transitionsUpdatedAtPreviousStep).ToHashSet();

				// Rollback is actually performed with a delay of 1 step
				//TryRollbackTransitionGuards(dpnToConsider, coloredCoverabilityGraph, transitionsToTrySimplify, transitionsDict);
			}
			else
			{
				if (!allNodesRed)
				{
					//TryRollbackTransitionGuards(dpnToConsider, coloredCoverabilityGraph, transitionsToTrySimplify, transitionsDict);
				}
			}


			repairmentSuccessfullyFinished = allNodesGreen;
			repairmentFailed = allNodesRed;
		} while (!repairmentSuccessfullyFinished && !repairmentFailed);


		var refinementsCount = dpnToConsider.Transitions.Count - sourceDpn.Transitions.Count;
		if (repairmentSuccessfullyFinished)
		{
			RemoveDeadTransitions(dpnToConsider, coloredCoverabilityGraph!.ConstraintArcs.ToArray());

			RemoveIsolatedPlaces(dpnToConsider);

			if (mergeTransitionsBack)
				MergeTransitions(dpnToConsider, transitionsDict);
		}

		var resultDpn = repairmentSuccessfullyFinished
			? dpnToConsider
			: sourceDpn;

		var differentTransitions = dpnToConsider.Transitions
			.Where(t => t.Guard.ActualConstraintExpression.ToString() != sourceDpn.Transitions.First(st => st.BaseTransitionId == t.BaseTransitionId).Guard.ActualConstraintExpression.ToString())
			.Select(t => t.BaseTransitionId)
			.ToHashSet();
		var deletedTransitions = sourceDpn.Transitions.Select(t => t.Id).Except(dpnToConsider.Transitions.Select(t => t.Id));

		differentTransitions.AddRange(deletedTransitions);
		totalModifiedTransitions = totalModifiedTransitions.Except(differentTransitions).ToHashSet();

		return new RepairResult(
			resultDpn,
			repairmentSuccessfullyFinished,
			repairSteps,
			new RepairModifications(differentTransitions, totalModifiedTransitions),
			stopwatch.Elapsed,
			statesConstructed,
			refinementsCount);


		static void RemoveIsolatedPlaces(DataPetriNet sourceDpn)
		{
			sourceDpn.Places.RemoveAll(p =>
				!sourceDpn.Arcs.Select(a => a.Source.Id)
					.Union(sourceDpn.Arcs.Select(a => a.Destination.Id))
					.Contains(p.Id));
		}

		// One of the possible improvements is to build a tree of transition splits and merge changes taking into account the data from the tree, and not just the root
		static void MergeTransitions(DataPetriNet dpnToConsider, Dictionary<string, Transition> transitionsDict)
		{
			var baseTransitions = dpnToConsider.Transitions
				.GroupBy(x => x.BaseTransitionId)
				.Where(x => x.Any()); // && x.Key == "t1_1"

			var preset = new Dictionary<string, List<(Place place, int weight)>>();
			var postset = new Dictionary<string, List<(Place place, int weight)>>();

			TransformerToRefined.FillTransitionsArcs(dpnToConsider, preset, postset);

			foreach (var baseTransition in baseTransitions)
			{
				var resultantConstraint = (BoolExpr)dpnToConsider.Context.MkOr(baseTransition.Select(x => x.Guard.ActualConstraintExpression).ToArray());//.Simplify()
				//resultantConstraint = dpnToConsider.Context.AreEqual(resultantConstraint, transitionsDict[baseTransition.Key].Guard.ActualConstraintExpression) 
				//	? transitionsDict[baseTransition.Key].Guard.ActualConstraintExpression 
				//	: dpnToConsider.Context.SimplifyExpression(resultantConstraint);


				var transitionToInspect = baseTransition.First();
				var splitIndex = transitionToInspect.Label.IndexOfAny(['-', '+']);
				var label = transitionToInspect.Label[.. (splitIndex == -1 ? transitionToInspect.Label.Length : splitIndex)];
				var guard = Guard.MakeMerged(transitionToInspect.Guard, resultantConstraint, dpnToConsider.Variables);
				var transitionToAdd = new Transition(baseTransition.Key, guard, label: label);
				dpnToConsider.Transitions.RemoveAll(x => baseTransition.Contains(x));
				dpnToConsider.Transitions.Add(transitionToAdd);

				dpnToConsider.Arcs.RemoveAll(x => baseTransition.Contains(x.Source) || baseTransition.Contains(x.Destination));
				foreach (var inputArc in preset[transitionToInspect.Id])
				{
					dpnToConsider.Arcs.Add(new Arc(inputArc.place, transitionToAdd, inputArc.weight));
				}

				foreach (var ouputArc in postset[transitionToInspect.Id])
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

	private static void RemoveDeadTransitions<TState, TTransition>(DataPetriNet sourceDpn, AbstractArc<TState, TTransition>[] arcs)
		where TState : AbstractState
		where TTransition : AbstractTransition
	{
		var transitionsInCt = arcs
			.Where(x => !x.Transition.IsSilent)
			.Select(x => x.Transition.NonRefinedTransitionId) //.Id везде
			.ToHashSet();

		sourceDpn.Transitions.RemoveAll(x => !transitionsInCt.Contains(x.BaseTransitionId));
		sourceDpn.Arcs.RemoveAll(x => x.Source is Transition && !transitionsInCt.Contains(((Transition)(x.Source)).BaseTransitionId)
		                              || x.Destination is Transition && !transitionsInCt.Contains(((Transition)(x.Destination)).BaseTransitionId));
	}

	// Some transition restriction is redundant - we, thus, rollback what we can
	private static void TryRollbackTransitionGuards(DataPetriNet sourceDpn, ColoredCoverabilityGraph cg, HashSet<string> transitionsToTrySimplify, Dictionary<string, Transition> transitionsDict)
	{
		var expressionService = new ConstraintExpressionService(sourceDpn.Context);

		var arcsToConsider = cg.ConstraintArcs
			.Where(x => transitionsToTrySimplify.Contains(x.Transition.Id))
			.GroupBy(x => x.Transition.Id);

		var baseTauTransitionsGuards = new Dictionary<Transition, BoolExpr>();
		foreach (var transitionId in transitionsToTrySimplify)
		{
			var smtExpression = transitionsDict[transitionId].Guard.ConstraintExpressionBeforeUpdate; // TODO: это что-то странное
			var overwrittenVarNames = transitionsDict[transitionId].Guard.WriteVars;
			var readExpression = sourceDpn.Context.GetExistsExpression(smtExpression, overwrittenVarNames);

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

	private (DataPetriNet dpn, HashSet<string> updatedTransitions) MakeRepairStep(
		DataPetriNet sourceDpn,
		ColoredCoverabilityGraph cg,
		Dictionary<string, Transition> transitionsDict)
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

		var expressionsForTransitions = new Dictionary<string, List<BoolExpr>>();
		foreach (var transition in sourceDpn.Transitions)
		{
			expressionsForTransitions[transition.Id] = new List<BoolExpr>();
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
						var transitionToUpdate = transitionsDict[arcBetweenNodes.Id];
						var formulaToConjunct = sourceDpn.Context.MkNot(childNode.Constraints);

						var overwrittenVars = transitionToUpdate.Guard.WriteVars;

						foreach (var overwrittenVar in overwrittenVars)
						{
							var readVar = sourceDpn.Context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Read);
							var writeVar = sourceDpn.Context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Written);

							formulaToConjunct = (BoolExpr)formulaToConjunct.Substitute(readVar, writeVar);
						}

						expressionsForTransitions[transitionToUpdate.Id].Add(formulaToConjunct);
					}
				}
			}
		}

		var updatedTransitions = new HashSet<string>();
		foreach (var transition in sourceDpn.Transitions)
		{
			if (expressionsForTransitions[transition.Id].Count > 0)
			{
				expressionsForTransitions[transition.Id].Add(transition.Guard.ActualConstraintExpression);

				var newCondition = sourceDpn.Context.SimplifyExpression(sourceDpn.Context.MkAnd(expressionsForTransitions[transition.Id]));

				transition.Guard = Guard.MakeRepaired(transition.Guard, newCondition, sourceDpn.Variables);

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
		Dictionary<string, List<BoolExpr>> expressionsForTransitions,
		List<LtsArc> visitedArcs)
	{
		if (!parentsDict.TryGetValue(currentNode.Id, out var parents))
		{
			return;
		}
		
		foreach (var arc in parents.Except(visitedArcs))
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

				expressionsForTransitions[transitionsDict[arc.Transition.Id].Id].Add(formulaToConjunct);
			}
		}
	}
}