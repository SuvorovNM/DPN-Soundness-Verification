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
        public List<IConstraintExpression> ConstraintExpressions { get; set; }
        public bool IsSatisfied { get; private set; }
        private readonly VariablesStore localVariables;
        private readonly Dictionary<DomainType, IExpressionsService> expressionServices;
        public Guard()
        {
            ConstraintExpressions = new List<IConstraintExpression>();
            localVariables = new VariablesStore();

            expressionServices = new Dictionary<DomainType, IExpressionsService>
            {
                [DomainType.Boolean] = new BoolExpressionsService(),
                [DomainType.Integer] = new IntegerExpressionsService(),
                [DomainType.Real] = new RealExpressionsService(),
                [DomainType.String] = new StringExpressionsService()
            };
        }

        public bool Verify(VariablesStore globalVariables)
        {
            var constraintStateDuringEvaluation = new List<IConstraintExpression>(ConstraintExpressions);
            var expressionResult = true; // Check correctness of true assign

            do
            {
                localVariables.Clear();

                // Block of ANDs which is currently evaluated
                List<IConstraintExpression> currentBlock;
                var delimiter = GetDelimiter(constraintStateDuringEvaluation);

                currentBlock = new List<IConstraintExpression>(constraintStateDuringEvaluation.GetRange(0, delimiter));
                constraintStateDuringEvaluation.RemoveRange(0, delimiter);

                // Evaluate all expressions
                foreach (var expression in currentBlock)
                {
                    expressionResult &= expressionServices[expression.ConstraintVariable.Domain]
                                .ExecuteExpression(globalVariables[expression.ConstraintVariable.Domain], expression);
                }

                // Select values for written variables
                if (expressionResult)
                {
                    foreach (var variable in currentBlock
                        .Where(x => x.ConstraintVariable.VariableType == VariableType.Written)
                        .Select(x => x.ConstraintVariable)
                        .Distinct())
                    {
                        expressionResult &= expressionServices[variable.Domain]
                                .SelectValue(variable.Name, localVariables[variable.Domain]);
                    }
                }

            } while (constraintStateDuringEvaluation.Count > 0 && !expressionResult);

            IsSatisfied = expressionResult;
            return expressionResult;
        }

        private static int GetDelimiter(List<IConstraintExpression> constraintStateDuringEvaluation)
        {
            // Find delimiter - OR expression
            var orExpressionIndex = constraintStateDuringEvaluation
                .FindIndex(x => x.LogicalConnective == LogicalConnective.Or);

            // If OR exists, we only need expressions before first OR
            var delimiter = orExpressionIndex == -1
                ? constraintStateDuringEvaluation.Count
                : orExpressionIndex;
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
        }
    }
}
