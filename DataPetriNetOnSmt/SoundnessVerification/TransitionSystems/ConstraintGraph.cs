using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using Microsoft.VisualBasic;
using Microsoft.Z3;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
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

                foreach (var transition in GetEnabledTransitions(currentState.PlaceTokens))
                {
                    var smtExpression = transition.Guard.ActualConstraintExpression;
                        //Context.GetSmtExpression(transition.Guard.BaseConstraintExpressions);

                    var overwrittenVarNames = transition.Guard.WriteVars;
                    var readExpression = DataPetriNet.Context.GetReadExpression(smtExpression, overwrittenVarNames);

                    if (expressionService.CanBeSatisfied(expressionService.ConcatExpressions(currentState.Constraints, readExpression, overwrittenVarNames)))
                    {
                        var constraintsIfTransitionFires = expressionService
                            .ConcatExpressions(currentState.Constraints, smtExpression, overwrittenVarNames);

                        if (expressionService.CanBeSatisfied(constraintsIfTransitionFires))
                        {
                            var updatedMarking = transition.FireOnGivenMarking(currentState.PlaceTokens, DataPetriNet.Arcs);

                            if (IsMonotonicallyIncreasedWithSameConstraints(updatedMarking, constraintsIfTransitionFires, currentState))
                            {
                                return; // The net is unbounded
                            }

                            AddNewState(currentState, new ConstraintTransition(transition), updatedMarking, constraintsIfTransitionFires);
                        }
                    }

                    var negatedGuardExpressions = DataPetriNet.Context.MkNot(readExpression);

                    if (!negatedGuardExpressions.IsTrue && !negatedGuardExpressions.IsFalse)
                    {
                        var constraintsIfSilentTransitionFires = expressionService
                            .ConcatExpressions(currentState.Constraints, negatedGuardExpressions, new Dictionary<string, DomainType>());

                        if (expressionService.CanBeSatisfied(constraintsIfSilentTransitionFires) &&
                            !expressionService.AreEqual(currentState.Constraints, constraintsIfSilentTransitionFires))
                        {
                            if (IsMonotonicallyIncreasedWithSameConstraints(currentState.PlaceTokens, constraintsIfSilentTransitionFires, currentState))
                            {
                                return; // The net is unbound
                            }

                            AddNewState(currentState, new ConstraintTransition(transition, true), currentState.PlaceTokens, constraintsIfSilentTransitionFires);
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
