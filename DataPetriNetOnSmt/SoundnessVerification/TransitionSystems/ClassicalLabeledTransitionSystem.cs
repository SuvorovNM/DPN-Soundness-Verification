﻿using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public class ClassicalLabeledTransitionSystem : LabeledTransitionSystem
    {
        public ClassicalLabeledTransitionSystem()
        : base()
        {

        }

        public ClassicalLabeledTransitionSystem(DataPetriNet dataPetriNet, AbstractConstraintExpressionService abstractConstraintExpressionService)
        : base(dataPetriNet, abstractConstraintExpressionService)
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
                    var smtExpression = transition.Guard.ActualConstraintExpression;
                    //Context.GetSmtExpression(transition.Guard.BaseConstraintExpressions);

                    var overwrittenVarNames = transition.Guard.BaseConstraintExpressions
                        .GetTypedVarsDict(VariableType.Written);
                    var readExpression = Context.GetReadExpression(smtExpression, overwrittenVarNames);

                    if (expressionService.CanBeSatisfied(expressionService.ConcatExpressions(currentState.Constraints, readExpression, overwrittenVarNames)))
                    {
                        var constraintsIfTransitionFires = expressionService
                            .ConcatExpressions(currentState.Constraints, transition.Guard.BaseConstraintExpressions, removeRedundantBlocks);

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
                }
            }

            IsFullGraph = true;
        }
    }
}
