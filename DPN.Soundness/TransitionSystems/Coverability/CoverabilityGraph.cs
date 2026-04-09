using DPN.Models;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using DPN.Models.Extensions;
using DPN.Soundness.TransitionSystems.Reachability;
using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;
using Microsoft.Z3;

namespace DPN.Soundness.TransitionSystems.Coverability;

internal class CoverabilityGraph : LabeledTransitionSystem
{
	private bool ContinueBranchIfUnboundedPlaceFound { get; }
	private bool StopOnCoveringFinalPosition { get; }
	private bool TryReachAllOmegas { get; }
	private bool WithTauTransitions { get; }
	private Place FinalPosition { get; }

	public CoverabilityGraph(
		DataPetriNet dataPetriNet,
		bool continueBranchIfUnboundedPlaceFound,
		bool stopOnCoveringFinalPosition = false,
		bool tryReachAllOmegas = true,
		bool withTauTransitions = false)
		: base(dataPetriNet)
	{
		ContinueBranchIfUnboundedPlaceFound = continueBranchIfUnboundedPlaceFound;
		StopOnCoveringFinalPosition = stopOnCoveringFinalPosition;
		TryReachAllOmegas = tryReachAllOmegas;
		WithTauTransitions = withTauTransitions;
		FinalPosition = dataPetriNet.Places.Single(p => p.IsFinal);
	}

	public override void GenerateGraph()
	{
		var tauTransitionsGuards = GetTauTransitionsGuards();

		while (StatesToConsider.Count > 0)
		{
			var currentState = StatesToConsider.Pop();
			if (!ContinueBranchIfUnboundedPlaceFound && currentState.Marking.AsDictionary().Any(placeTokens => placeTokens.Value == int.MaxValue))
			{
				continue;
			}

			foreach (var transition in currentState.Marking.GetEnabledTransitions(DataPetriNet))
			{
				if (!TryAddStateResultedFromFiringNormalTransition(transition, currentState))
				{
					return;
				}

				if (WithTauTransitions)
				{
					AddStateResultedFromFiringTauTransitionIfPossible(tauTransitionsGuards, transition, currentState);
				}
			}
		}

		IsFullGraph = true;
	}

	private Dictionary<Transition, BoolExpr> GetTauTransitionsGuards()
	{
		var tauTransitionsGuards = new Dictionary<Transition, BoolExpr>();
		foreach (var transition in DataPetriNet.Transitions)
		{
			var smtExpression = transition.Guard.ActualConstraintExpression;
			var overwrittenVarNames = transition.Guard.WriteVars;
			var readExpression = DataPetriNet.Context.GetExistsExpression(smtExpression, overwrittenVarNames);

			var negatedGuardExpressions = DataPetriNet.Context.MkNot(readExpression);
			tauTransitionsGuards.Add(transition, negatedGuardExpressions);
		}

		return tauTransitionsGuards;
	}

	private void AddStateResultedFromFiringTauTransitionIfPossible(Dictionary<Transition, BoolExpr> tauTransitionsGuards, Transition transition, LtsState currentState)
	{
		var negatedGuardExpressions = tauTransitionsGuards[transition];

		var constraintsIfSilentTransitionFires = DataPetriNet.Context.MkAnd(currentState.Constraints, negatedGuardExpressions);

		if (ExpressionService.CanBeSatisfied(constraintsIfSilentTransitionFires) &&
		    !ExpressionService.AreEqual(currentState.Constraints, constraintsIfSilentTransitionFires))
		{
			var stateToAddInfo = new BaseStateInfo(currentState.Marking, (BoolExpr)constraintsIfSilentTransitionFires.Simplify());

			AddNewState(currentState, new LtsTransition(transition, true), stateToAddInfo);
		}
	}

	private bool TryAddStateResultedFromFiringNormalTransition(Transition transition, LtsState currentState)
	{
		var smtExpression = transition.Guard.ActualConstraintExpression;

		var overwrittenVarNames = transition.Guard.WriteVars;

		var constraintsIfTransitionFires = ExpressionService
			.ConcatExpressions(currentState.Constraints, smtExpression, overwrittenVarNames);

		if (ExpressionService.CanBeSatisfied(constraintsIfTransitionFires))
		{
			var updatedMarking = transition.FireOnGivenMarking(currentState.Marking, DataPetriNet.Arcs);
			var stateToAddInfo = new BaseStateInfo(updatedMarking, constraintsIfTransitionFires);

			var coveredNodes = FindAllParentNodesForWhichComparisonResultForCurrentNodeHolds
				(stateToAddInfo, currentState, MarkingComparisonResult.GreaterThan);
			foreach (var coveredNode in coveredNodes)
			{
				foreach (var place in DataPetriNet.Places)
				{
					if (coveredNode.Marking[place] < updatedMarking[place])
					{
						updatedMarking[place] = int.MaxValue;
					}
				}
			}


			AddNewState(currentState, new LtsTransition(transition, transition.IsTau), stateToAddInfo);


			if (StopOnCoveringFinalPosition && stateToAddInfo.Marking[FinalPosition] > 1)
			{
				IsFullGraph = false;
				return false;
			}
		}

		return true;
	}

	public override void GenerateGraph(LabeledTransitionSystem baseLts)
	{
		if (!baseLts.IsFullGraph)
		{
			throw new ArgumentException("Constraint graph construction based on an LTS can only be done if the LTS is fully constructed");
		}

		InitialState = baseLts.InitialState;
		ConstraintStates = baseLts.ConstraintStates;
		ConstraintArcs = baseLts.ConstraintArcs;
		IsFullGraph = baseLts.IsFullGraph;

		var readConditions = DataPetriNet.Transitions
			.ToDictionary(t => t.Id, t => DataPetriNet.Context.GetExistsExpression(t.Guard.ActualConstraintExpression, t.Guard.WriteVars));
		var existingStates = baseLts.ConstraintStates.Select(cs => cs.Id).ToHashSet();
		var tauTransitionsGuards = GetTauTransitionsGuards();

		StatesToConsider.Clear();
		baseLts.ConstraintStates.ForEach(s => StatesToConsider.Push(s));

		while (StatesToConsider.Count > 0)
		{
			var currentState = StatesToConsider.Pop();

			foreach (var transition in currentState.Marking.GetEnabledTransitions(DataPetriNet))
			{
				if (!existingStates.Contains(currentState.Id))
				{
					if (!TryAddStateResultedFromFiringNormalTransition(transition, currentState))
					{
						return;
					}
				}

				if (transition.IsTau)
				{
					continue;
				}

				if (WithTauTransitions)
				{
					AddStateResultedFromFiringTauTransitionIfPossible(tauTransitionsGuards, transition, currentState);
				}
			}
		}

		IsFullGraph = true;
	}

	private IEnumerable<LtsState> FindAllParentNodesForWhichComparisonResultForCurrentNodeHolds
		(BaseStateInfo stateInfo, LtsState parentNode, MarkingComparisonResult comparisonResult)
	{
		return from stateInGraph in parentNode.ParentStates.Union(new[] { parentNode })
			let isConditionHoldsForTokens = stateInfo.Marking.CompareTo(stateInGraph.Marking) == comparisonResult
			where isConditionHoldsForTokens &&
			      ExpressionService.AreEqual(stateInGraph.Constraints, stateInfo.Constraints)
			select stateInGraph; //DoesTargetCoverSource
	}
}