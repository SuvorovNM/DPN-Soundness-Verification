using System.Diagnostics;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using Microsoft.Z3;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;

public class CoverabilityGraph : LabeledTransitionSystem
{
    protected bool WithTauTransitions { get; init; }

    public CoverabilityGraph(DataPetriNet dataPetriNet, bool withTauTransitions = false) : base(dataPetriNet)
    {
        WithTauTransitions = withTauTransitions;
    }

    public override void GenerateGraph()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        var transitionGuards = new Dictionary<Transition, BoolExpr>();
        var tauTransitionsGuards = new Dictionary<Transition, BoolExpr>();
        foreach (var transition in DataPetriNet.Transitions)
        {
            var smtExpression = transition.Guard.ActualConstraintExpression;
            var overwrittenVarNames = transition.Guard.WriteVars;
            var readExpression = DataPetriNet.Context.GetReadExpression(smtExpression, overwrittenVarNames);
            transitionGuards.Add(transition, readExpression);

            var negatedGuardExpressions = DataPetriNet.Context.MkNot(readExpression);
            tauTransitionsGuards.Add(transition, negatedGuardExpressions);
        }

        var logged = false;

        while (StatesToConsider.Count > 0)
        {
            if (ConstraintStates.Count > 20000 && !logged)
            {
                foreach (var state in ConstraintStates)
                {
                    File.AppendAllText("C:\\workspace\\results.txt", $"[{state.Marking}]({state.Constraints})" + "\n");
                }

                logged = true;
            }

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

                        var coveredNode = FindParentNodeForWhichComparisonResultForCurrentNodeHolds
                            (stateToAddInfo, currentState, MarkingComparisonResult.GreaterThan);
                        if (coveredNode != null)
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
                    }
                }

                if (WithTauTransitions)
                {
                    var negatedGuardExpressions = tauTransitionsGuards[transition];

                    var constraintsIfSilentTransitionFires =
                        DataPetriNet.Context.MkAnd(currentState.Constraints, negatedGuardExpressions);

                    if (expressionService.CanBeSatisfied(constraintsIfSilentTransitionFires) &&
                        !expressionService.AreEqual(currentState.Constraints, constraintsIfSilentTransitionFires))
                    {
                        var stateToAddInfo = new BaseStateInfo(currentState.Marking,
                            (BoolExpr)constraintsIfSilentTransitionFires.Simplify());

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