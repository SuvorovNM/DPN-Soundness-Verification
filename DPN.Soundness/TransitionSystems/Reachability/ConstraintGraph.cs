using DPN.Models;
using DPN.Models.Enums;
using DPN.Models.Extensions;
using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;

namespace DPN.Soundness.TransitionSystems.Reachability
{
    internal class ConstraintGraph(DataPetriNet dataPetriNet) : LabeledTransitionSystem(dataPetriNet)
    {
	    public override void GenerateGraph()
        {
            IsFullGraph = false;
            
            var readConditions = dataPetriNet.Transitions
	            .ToDictionary(t=>t.Id, t=>DataPetriNet.Context.GetExistsExpression(t.Guard.ActualConstraintExpression, t.Guard.WriteVars));

            while (StatesToConsider.Count > 0)
            {
                var currentState = StatesToConsider.Pop();

                foreach (var transition in currentState.Marking.GetEnabledTransitions(DataPetriNet))
                {
	                if (transition.Id == "t1_2")
	                {
		                
	                }

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
		                    return; // The net is unbounded
	                    }

	                    AddNewState(currentState, new LtsTransition(transition, transition.IsTau), stateToAddInfo);
                    }

                    if (transition.IsTau)
                    {
                        continue;
                    }
                    
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
                                return; // The net is unbounded
                            }

                            AddNewState(currentState, new LtsTransition(transition, true), stateToAddInfo);                           
                        }
                    }
                }
            }
            IsFullGraph = true;
        }
    }
}
