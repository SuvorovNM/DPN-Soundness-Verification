using DPN.Models;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using DPN.Models.Extensions;
using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;
using Microsoft.Z3;

namespace DPN.Soundness.TransitionSystems.Reachability
{
	internal class ConstraintGraph(DataPetriNet dataPetriNet) : LabeledTransitionSystem(dataPetriNet)
	{
		public override void GenerateGraph()
		{
			IsFullGraph = false;

			var readConditions = DataPetriNet.Transitions
				.ToDictionary(t => t.Id, t => DataPetriNet.Context.GetExistsExpression(t.Guard.ActualConstraintExpression, t.Guard.WriteVars));

			while (StatesToConsider.Count > 0)
			{
				var currentState = StatesToConsider.Pop();

				foreach (var transition in currentState.Marking.GetEnabledTransitions(DataPetriNet))
				{
					TryAddStateResultedFromFiringNormalTransition(currentState, transition, out var isStrictlyCovering);

					if (isStrictlyCovering)
					{
						return;
					}


					if (transition.IsTau)
					{
						continue;
					}

					TryAddStateResultedFromFiringTauTransition(readConditions, transition, currentState, out var isTauStrictlyCovering);
					{
						if (isTauStrictlyCovering)
						{
							return;
						}
					}
				}
			}

			IsFullGraph = true;
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
			
			var existingStates = baseLts.ConstraintStates.Select(cs=>cs.Id).ToHashSet();

			StatesToConsider.Clear();
			baseLts.ConstraintStates.ForEach(s => StatesToConsider.Push(s));

			while (StatesToConsider.Count > 0)
			{
				var currentState = StatesToConsider.Pop();

				foreach (var transition in currentState.Marking.GetEnabledTransitions(DataPetriNet))
				{
					if (!existingStates.Contains(currentState.Id))
					{
						TryAddStateResultedFromFiringNormalTransition(currentState, transition, out var isStrictlyCovering);
						if (isStrictlyCovering)
						{
							IsFullGraph = false;
							return;
						}
					}

					if (transition.IsTau)
					{
						continue;
					}

					TryAddStateResultedFromFiringTauTransition(readConditions, transition, currentState, out var isTauStrictlyCovering);
					if (isTauStrictlyCovering)
					{
						IsFullGraph = false;
						return;
					}
				}
			}
			
			IsFullGraph = true;
		}

		private void TryAddStateResultedFromFiringTauTransition(Dictionary<string, BoolExpr> readConditions, Transition transition, LtsState currentState, out bool isStrictlyCovering)
		{
			var negatedGuardExpressions = DataPetriNet.Context.MkNot(readConditions[transition.Id]);

			if (!negatedGuardExpressions.IsTrue && !negatedGuardExpressions.IsFalse)
			{
				var constraintsIfSilentTransitionFires = ExpressionService
					.ConcatExpressions(currentState.Constraints, negatedGuardExpressions, new Dictionary<string, DomainType>());

				if (ExpressionService.CanBeSatisfied(constraintsIfSilentTransitionFires) &&
				    !ExpressionService.AreEqual(currentState.Constraints, constraintsIfSilentTransitionFires))
				{
					var stateToAddInfo = new BaseStateInfo(currentState.Marking, constraintsIfSilentTransitionFires);

					var coveredNode = FindParentNodeForWhichComparisonResultForCurrentNodeHolds
						(stateToAddInfo, currentState, MarkingComparisonResult.GreaterThan);
					if (coveredNode != null)
					{
						isStrictlyCovering = true;
					}

					AddNewState(currentState, new LtsTransition(transition, true), stateToAddInfo);
				}
			}

			isStrictlyCovering = false;
		}

		private void TryAddStateResultedFromFiringNormalTransition(LtsState currentState, Transition transition, out bool isStrictlyCovering)
		{
			var constraintsIfTransitionFires = ExpressionService
				.ConcatExpressions(currentState.Constraints, transition.Guard.ActualConstraintExpression, transition.Guard.WriteVars);

			if (ExpressionService.CanBeSatisfied(constraintsIfTransitionFires))
			{
				var updatedMarking = transition.FireOnGivenMarking(currentState.Marking, DataPetriNet.Arcs);
				var stateToAddInfo = new BaseStateInfo(updatedMarking, constraintsIfTransitionFires);

				var coveredNode = FindParentNodeForWhichComparisonResultForCurrentNodeHolds
					(stateToAddInfo, currentState, MarkingComparisonResult.GreaterThan);
				if (coveredNode != null)
				{
					isStrictlyCovering = true;
					return;
				}

				AddNewState(currentState, new LtsTransition(transition, transition.IsTau), stateToAddInfo);
			}

			isStrictlyCovering = false;
		}
	}
}