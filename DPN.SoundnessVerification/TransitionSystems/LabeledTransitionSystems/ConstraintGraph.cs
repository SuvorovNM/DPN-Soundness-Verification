using System.Diagnostics;
using DPN.Models;
using DPN.Models.Enums;
using DPN.Models.Extensions;

namespace DPN.SoundnessVerification.TransitionSystems
{
    public class ConstraintGraph : LabeledTransitionSystem // TODO: insert Ids
    {
        public ConstraintGraph(DataPetriNet dataPetriNet)
        : base(dataPetriNet)
        {

        }

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
                    var readExpression = DataPetriNet.Context.GetReadExpression(smtExpression, overwrittenVarNames);

                    if (ExpressionService.CanBeSatisfied(ExpressionService.ConcatExpressions(currentState.Constraints, readExpression, overwrittenVarNames)))
                    {
                        var constraintsIfTransitionFires = ExpressionService
                            .ConcatExpressions(currentState.Constraints, smtExpression, overwrittenVarNames);

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

                            AddNewState(currentState, new LtsTransition(transition), stateToAddInfo);
                        }
                    }

                    if (transition.IsTau)
                    {
                        continue;
                    }
                    
                    var negatedGuardExpressions = DataPetriNet.Context.MkNot(readExpression);

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
            stopwatch.Stop();
            Milliseconds = stopwatch.ElapsedMilliseconds;
            IsFullGraph = true;
        }
    }
}
