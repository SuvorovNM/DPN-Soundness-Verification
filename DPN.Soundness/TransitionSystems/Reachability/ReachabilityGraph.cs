using System.Diagnostics;
using DPN.Models;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;

namespace DPN.Soundness.TransitionSystems.Reachability
{
	internal class ReachabilityGraph(DataPetriNet dataPetriNet) : LabeledTransitionSystem(dataPetriNet)
	{
		public override void GenerateGraph()
		{
			IsFullGraph = false;
			Stopwatch stopwatch = Stopwatch.StartNew();

			while (StatesToConsider.Count > 0)
			{
				var currentState = StatesToConsider.Pop();

				foreach (var transition in currentState.Marking.GetEnabledTransitions(DataPetriNet))
				{
					var smtExpression = transition.Guard.ActualConstraintExpression;

					var overwrittenVarNames = transition.Guard.WriteVars;

					var constraintsIfTransitionFires = ExpressionService
						.ConcatExpressions(currentState.Constraints, smtExpression, overwrittenVarNames);

					

					if (ExpressionService.CanBeSatisfied(constraintsIfTransitionFires))
					{
						var updatedMarking = (Marking)transition.FireOnGivenMarking(currentState.Marking, DataPetriNet.Arcs);
						var stateToAddInfo = new BaseStateInfo(updatedMarking, constraintsIfTransitionFires);
						
						if (transition.IsTau && updatedMarking.CompareTo(currentState.Marking) != MarkingComparisonResult.Equal)
						{
						
						}

						var coveredNode = FindParentNodeForWhichComparisonResultForCurrentNodeHolds
							(stateToAddInfo, currentState, MarkingComparisonResult.GreaterThan);
						if (coveredNode != null)
						{
							return; // The net is unbounded
						}

						AddNewState(currentState, new LtsTransition(transition), stateToAddInfo);
					}
				}
			}

			stopwatch.Stop();
			Milliseconds = stopwatch.ElapsedMilliseconds;
			IsFullGraph = true;
		}
	}
}