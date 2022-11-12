using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using Microsoft.VisualBasic;
using Microsoft.Z3;
using System.ComponentModel.DataAnnotations;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public class ConstraintGraph : LabeledTransitionSystem // TODO: insert Ids
    {
        public ConstraintGraph(DataPetriNet dataPetriNet, AbstractConstraintExpressionService abstractConstraintExpressionService)
        : base(dataPetriNet, abstractConstraintExpressionService)
        {

        }

        public ConstraintGraph()
        : base()
        {

        }

        public override void GenerateGraph(bool removeRedundantBlocks = false)
        {
            IsFullGraph = false;

            while (StatesToConsider.Count > 0)
            {
                var currentState = StatesToConsider.Pop();

                foreach (var transition in GetTransitionsWhichCanFire(currentState.PlaceTokens))
                {
                    var smtExpression = GetSmtExpression(transition.Guard.ConstraintExpressions);

                    var overwrittenVarNames = transition.Guard.ConstraintExpressions
                        .GetOverwrittenVarsDict();
                    var readExpression = GetReadExpression(smtExpression, overwrittenVarNames);

                    if (expressionService.CanBeSatisfied(expressionService.ConcatExpressions(currentState.Constraints, readExpression, overwrittenVarNames)))
                    {
                        var constraintsIfTransitionFires = expressionService
                            .ConcatExpressions(currentState.Constraints, transition.Guard.ConstraintExpressions, removeRedundantBlocks);

                        if (expressionService.CanBeSatisfied(constraintsIfTransitionFires))
                        {
                            var updatedMarking = transition.FireOnGivenMarking(currentState.PlaceTokens, DataPetriNet.Arcs);

                            if (IsMonotonicallyIncreasedWithUnchangedConstraints(updatedMarking, constraintsIfTransitionFires, currentState))
                            {
                                return; // The net is unbounded
                            }

                            AddNewState(currentState, new ConstraintTransition(transition), updatedMarking, constraintsIfTransitionFires);
                        }
                    }

                    var negatedGuardExpressions = Context.MkNot(readExpression);

                    if (!negatedGuardExpressions.IsTrue && !negatedGuardExpressions.IsFalse)
                    {
                        var constraintsIfSilentTransitionFires = expressionService
                            .ConcatExpressions(currentState.Constraints, negatedGuardExpressions, new Dictionary<string, DomainType>());

                        if (expressionService.CanBeSatisfied(constraintsIfSilentTransitionFires) &&
                            !expressionService.AreEqual(currentState.Constraints, constraintsIfSilentTransitionFires))
                        {
                            if (IsMonotonicallyIncreasedWithUnchangedConstraints(currentState.PlaceTokens, constraintsIfSilentTransitionFires, currentState))
                            {
                                return; // The net is unbound
                            }

                            AddNewState(currentState, new ConstraintTransition(transition, true), currentState.PlaceTokens, constraintsIfSilentTransitionFires);
                        }
                    }
                }
            }

            IsFullGraph = true;
        }
    }
}
