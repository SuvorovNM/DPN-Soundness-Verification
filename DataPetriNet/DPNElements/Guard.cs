using DataPetriNet.Abstractions;
using DataPetriNet.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.DPNElements
{
    public class Guard
    {
        public List<IConstraintExpression> ConstraintExpressions { get; set; }

        public bool Verify(VariablesStore variables)
        {
            var constraintStateDuringEvaluation = new List<IConstraintExpression>(ConstraintExpressions);
            var expressionResult = true; // Check correctness of true assign

            do
            {
                // Block of ANDs which is currently evaluated
                List<IConstraintExpression> currentBlock;

                // Find delimiter - OR expression
                var orExpressionIndex = constraintStateDuringEvaluation
                    .FindIndex(x => x.LogicalConnective == LogicalConnective.Or);

                // If OR exists, we only need expressions before first OR
                if (orExpressionIndex >= 0)
                {
                    currentBlock = new List<IConstraintExpression>(constraintStateDuringEvaluation.GetRange(0, orExpressionIndex));
                    constraintStateDuringEvaluation.RemoveRange(0, orExpressionIndex);
                }
                // Otherwise, we need the whole expression
                else
                {
                    currentBlock = new List<IConstraintExpression>(constraintStateDuringEvaluation);
                    constraintStateDuringEvaluation.RemoveRange(0, constraintStateDuringEvaluation.Count);
                }

                var variablesSelector = new VariablesSelector();
                foreach(var expression in currentBlock)
                {
                    switch (expression.ConstraintVariable.Domain)
                    {
                        case DomainType.Boolean:
                            var booleanExpression = expression as ConstraintExpression<bool>;
                            if (booleanExpression.ConstraintVariable.VariableType == VariableType.Read)
                            {
                                expressionResult &= booleanExpression.Evaluate(variables.ReadBool(booleanExpression.ConstraintVariable.Name));
                            }
                            else
                            {
                                variablesSelector.AddValueIntervalToVariable(booleanExpression.ConstraintVariable.Name, booleanExpression.GetValueInterval());
                            }
                            break;
                    }
                }

                if (expressionResult)
                {
                    foreach(var variable in currentBlock
                        .Where(x=>x.ConstraintVariable.VariableType == VariableType.Written)
                        .Select(x => x.ConstraintVariable))
                    {
                        switch (variable.Domain)
                        {
                            case DomainType.Boolean:
                                expressionResult &= variablesSelector.TrySelectValue(variable.Name, out bool _);
                                break;
                        }
                        
                    }
                }

            } while (constraintStateDuringEvaluation.Count > 0 && !expressionResult);
        }

        
    }
}
