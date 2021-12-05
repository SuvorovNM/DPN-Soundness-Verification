using DataPetriNet.Abstractions;
using DataPetriNet.Enums;
using DataPetriNet.Services.ExpressionServices;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataPetriNet.DPNElements
{
    public class Guard
    {
        private readonly VariablesStore localVariables;
        private readonly ExpressionsServiceStore expressionServices;

        public bool IsSatisfied { get; private set; }
        public List<IConstraintExpression> ConstraintExpressions { get; set; }
        public Guard()
        {
            ConstraintExpressions = new List<IConstraintExpression>();
            localVariables = new VariablesStore();
            expressionServices = new ExpressionsServiceStore();
        }

        public bool Verify(VariablesStore globalVariables)
        {
            var constraintStateDuringEvaluation = new List<IConstraintExpression>(ConstraintExpressions);
            if (constraintStateDuringEvaluation.Count == 0)
            {
                return true;
            }
            bool expressionResult;

            do
            {
                expressionResult = true;
                localVariables.Clear();

                // Block of ANDs which is currently evaluated
                List<IConstraintExpression> currentBlock;
                var delimiter = GetDelimiter(constraintStateDuringEvaluation);

                // TODO: If need randomness we can get first GetDelimiter randomly and the second one will be the nearest OR/EoL 

                currentBlock = new List<IConstraintExpression>(constraintStateDuringEvaluation.GetRange(0, delimiter));
                constraintStateDuringEvaluation.RemoveRange(0, delimiter);

                // Evaluate all expressions
                foreach (var expression in currentBlock.Where(x=>x.ConstraintVariable.VariableType == VariableType.Read))
                {
                    expressionResult &= expressionServices[expression.ConstraintVariable.Domain]
                                .EvaluateExpression(globalVariables[expression.ConstraintVariable.Domain], expression);
                }
                foreach (var expression in currentBlock.Where(x => x.ConstraintVariable.VariableType == VariableType.Written))
                {
                    expressionServices[expression.ConstraintVariable.Domain].AddValueInterval(expression);
                }

                // Select values for written variables
                if (expressionResult)
                {
                    foreach (var variable in currentBlock
                        .Where(x => x.ConstraintVariable.VariableType == VariableType.Written)
                        .Select(x => x.ConstraintVariable)
                        .Distinct())
                    {
                        expressionResult &= expressionServices[variable.Domain].TryInferValue(variable.Name, out var value);
                        if (expressionResult)
                        {
                            localVariables[variable.Domain].Write(variable.Name, value);
                        }
                    }
                }

            } while (constraintStateDuringEvaluation.Count > 0 && !expressionResult);

            IsSatisfied = expressionResult;
            return expressionResult;
        }

        public static int GetDelimiter(List<IConstraintExpression> constraintStateDuringEvaluation)
        {
            // Find delimiter - OR expression
            var orExpressionIndex = constraintStateDuringEvaluation
                .GetRange(1, constraintStateDuringEvaluation.Count - 1) // TODO: Make search more effective
                .FindIndex(x => x.LogicalConnective == LogicalConnective.Or);

            // If OR exists, we only need expressions before first OR
            var delimiter = orExpressionIndex == -1
                ? constraintStateDuringEvaluation.Count
                : orExpressionIndex + 1;
            return delimiter;
        }

        public void UpdateGlobalVariables(VariablesStore globalVariables)
        {
            if (IsSatisfied)
            {
                var variablesToUpdate = ConstraintExpressions
                    .Where(x => x.ConstraintVariable.VariableType == VariableType.Written)
                    .Select(x => x.ConstraintVariable)
                    .Distinct();

                foreach (var variable in variablesToUpdate)
                {
                    globalVariables[variable.Domain].Write(variable.Name, localVariables[variable.Domain].Read(variable.Name));
                }

                ResetState();
            }
            else
            {
                throw new InvalidOperationException("The transition cannot fire - guard is not satisfied!");
            }
        }

        private void ResetState()
        {
            IsSatisfied = false;
            localVariables.Clear();
            expressionServices.Clear();
        }
    }
}
