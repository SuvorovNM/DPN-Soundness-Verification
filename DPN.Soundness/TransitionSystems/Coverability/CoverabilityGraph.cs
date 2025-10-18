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
    private bool StopOnCoveringFinalPosition { get; }
    private bool TryReachAllOmegas { get; }
    private bool WithTauTransitions { get; }
    private Place FinalPosition { get; }

    public CoverabilityGraph(
	    DataPetriNet dataPetriNet, 
	    bool stopOnCoveringFinalPosition = false, 
	    bool tryReachAllOmegas = true,
	    bool withTauTransitions = false)
        : base(dataPetriNet)
    {
        StopOnCoveringFinalPosition = stopOnCoveringFinalPosition;
        TryReachAllOmegas = tryReachAllOmegas;
        WithTauTransitions = withTauTransitions;
        FinalPosition = dataPetriNet.Places.Single(p => p.IsFinal);
    }

    public override void GenerateGraph()
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
	    
        while (StatesToConsider.Count > 0)
        {
            var currentState = StatesToConsider.Pop();
            if (currentState.Marking.AsDictionary().Any(placeTokens => placeTokens.Value == int.MaxValue))
            {
	            continue;
            }

            foreach (var transition in currentState.Marking.GetEnabledTransitions(DataPetriNet))
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


	                AddNewState(currentState, new LtsTransition(transition), stateToAddInfo);


	                if (StopOnCoveringFinalPosition && stateToAddInfo.Marking[FinalPosition] > 1)
	                {
		                IsFullGraph = false;
		                return;
	                }
                }
                
                if (WithTauTransitions)
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
            select stateInGraph;//DoesTargetCoverSource
    }
}