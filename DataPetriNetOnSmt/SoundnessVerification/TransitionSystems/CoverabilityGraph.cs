using System.Diagnostics;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using Microsoft.Z3;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;

public class CoverabilityGraph : LabeledTransitionSystem
{
    private bool StopOnCoveringFinalPosition { get; init; }
    private Place FinalPosition { get; init; }

    public CoverabilityGraph(DataPetriNet dataPetriNet, bool stopOnCoveringFinalPosition = false)
        : base(dataPetriNet)
    {
        StopOnCoveringFinalPosition = stopOnCoveringFinalPosition;
        FinalPosition = dataPetriNet.Places.Single(p => p.IsFinal);
    }

    public override void GenerateGraph()
    {
        var stopwatch = Stopwatch.StartNew();
        var transitionGuards = new Dictionary<Transition, BoolExpr>();
        foreach (var transition in DataPetriNet.Transitions)
        {
            var smtExpression = transition.Guard.ActualConstraintExpression;
            var overwrittenVarNames = transition.Guard.WriteVars;
            var readExpression = DataPetriNet.Context.GetReadExpression(smtExpression, overwrittenVarNames);
            transitionGuards.Add(transition, readExpression);
        }
        
        while (StatesToConsider.Count > 0)
        {
            var currentState = StatesToConsider.Pop();

            foreach (var transition in currentState.Marking.GetEnabledTransitions(DataPetriNet))
            {
                var smtExpression = transition.Guard.ActualConstraintExpression;

                var overwrittenVarNames = transition.Guard.WriteVars;
                var readExpression = transitionGuards[transition];

                if (expressionService.CanBeSatisfied(expressionService.ConcatExpressions(currentState.Constraints,
                        readExpression, overwrittenVarNames)))
                {
                    var constraintsIfTransitionFires = expressionService
                        .ConcatExpressions(currentState.Constraints, smtExpression, overwrittenVarNames);

                    if (expressionService.CanBeSatisfied(constraintsIfTransitionFires))
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
                            stopwatch.Stop();
                            Milliseconds = stopwatch.ElapsedMilliseconds;
                            IsFullGraph = false;
                            return;
                        }
                    }
                }
            }
        }

        stopwatch.Stop();
        Milliseconds = stopwatch.ElapsedMilliseconds;
        IsFullGraph = true;
    }

    private IEnumerable<LtsState> FindAllParentNodesForWhichComparisonResultForCurrentNodeHolds
        (BaseStateInfo stateInfo, LtsState parentNode, MarkingComparisonResult comparisonResult)
    {
        return from stateInGraph in parentNode.ParentStates.Union(new[] { parentNode })
            let isConditionHoldsForTokens = stateInfo.Marking.CompareTo(stateInGraph.Marking) == comparisonResult
            where isConditionHoldsForTokens &&
                  expressionService.AreEqual(stateInGraph.Constraints, stateInfo.Constraints)
            select stateInGraph;//DoesTargetCoverSource
    }
}