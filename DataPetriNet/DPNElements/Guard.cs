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
        private VariablesStore localVariables;
        public Guard()
        {
            ConstraintExpressions = new List<IConstraintExpression>();
            localVariables = new VariablesStore();
        }

        public bool Verify(VariablesStore globalVariables)
        {
            var constraintStateDuringEvaluation = new List<IConstraintExpression>(ConstraintExpressions);
            var expressionResult = true; // Check correctness of true assign

            do
            {
                localVariables = new VariablesStore();
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

                // Evaluate read expressions
                var variablesSelector = new VariablesSelector();
                foreach(var expression in currentBlock)
                {
                    switch (expression.ConstraintVariable.Domain)
                    {
                        case DomainType.Boolean:
                            var booleanExpression = expression as ConstraintExpression<bool>;
                            if (booleanExpression.ConstraintVariable.VariableType == VariableType.Read)
                            {
                                expressionResult &= booleanExpression.Evaluate(globalVariables.ReadBool(booleanExpression.ConstraintVariable.Name));
                            }
                            else
                            {
                                variablesSelector.AddValueIntervalToVariable(booleanExpression.ConstraintVariable.Name, booleanExpression.GetValueInterval());
                            }
                            break;
                        case DomainType.String:
                            var stringExpression = expression as ConstraintExpression<string>;
                            if (stringExpression.ConstraintVariable.VariableType == VariableType.Read)
                            {
                                expressionResult &= stringExpression.Evaluate(globalVariables.ReadString(stringExpression.ConstraintVariable.Name));
                            }
                            else
                            {
                                variablesSelector.AddValueIntervalToVariable(stringExpression.ConstraintVariable.Name, stringExpression.GetValueInterval());
                            }
                            break;
                        case DomainType.Integer:
                            var integerExpression = expression as ConstraintExpression<long>;
                            if (integerExpression.ConstraintVariable.VariableType == VariableType.Read)
                            {
                                expressionResult &= integerExpression.Evaluate(globalVariables.ReadInteger(integerExpression.ConstraintVariable.Name));
                            }
                            else
                            {
                                variablesSelector.AddValueIntervalToVariable(integerExpression.ConstraintVariable.Name, integerExpression.GetValueInterval());
                            }
                            break;
                        case DomainType.Real:
                            var realExpression = expression as ConstraintExpression<double>;
                            if (realExpression.ConstraintVariable.VariableType == VariableType.Read)
                            {
                                expressionResult &= realExpression.Evaluate(globalVariables.ReadReal(realExpression.ConstraintVariable.Name));
                            }
                            else
                            {
                                variablesSelector.AddValueIntervalToVariable(realExpression.ConstraintVariable.Name, realExpression.GetValueInterval());
                            }
                            break;
                    }
                }

                // Evaluate write expressions
                if (expressionResult)
                {
                    foreach(var variable in currentBlock
                        .Where(x=>x.ConstraintVariable.VariableType == VariableType.Written)
                        .Select(x => x.ConstraintVariable))
                    {
                        switch (variable.Domain)
                        {
                            case DomainType.Boolean:
                                expressionResult &= variablesSelector.TrySelectValue(variable.Name, out bool boolValue);
                                localVariables.WriteBool(variable.Name, boolValue);
                                break;
                            case DomainType.String:
                                expressionResult &= variablesSelector.TrySelectValue(variable.Name, out string stringValue);
                                localVariables.WriteString(variable.Name, stringValue);
                                break;
                            case DomainType.Integer:
                                expressionResult &= variablesSelector.TrySelectValue(variable.Name, out long integerValue);
                                localVariables.WriteInteger(variable.Name, integerValue);
                                break;
                            case DomainType.Real:
                                expressionResult &= variablesSelector.TrySelectValue(variable.Name, out double realValue);
                                localVariables.WriteReal(variable.Name, realValue);
                                break;
                        }
                        
                    }
                }

            } while (constraintStateDuringEvaluation.Count > 0 && !expressionResult);

            return expressionResult;
        }

        public void UpdateVariables(VariablesStore globalVariables)
        {
            var variablesToUpdate = ConstraintExpressions
                .Where(x => x.ConstraintVariable.VariableType == VariableType.Written)
                .Select(x => x.ConstraintVariable);

            foreach(var variable in variablesToUpdate)
            {
                switch (variable.Domain)
                {
                    case DomainType.Boolean:
                        globalVariables.WriteBool(variable.Name, localVariables.ReadBool(variable.Name));
                        break;
                    case DomainType.String:
                        globalVariables.WriteString(variable.Name, localVariables.ReadString(variable.Name));
                        break;
                    case DomainType.Integer:
                        globalVariables.WriteInteger(variable.Name, localVariables.ReadInteger(variable.Name));
                        break;
                    case DomainType.Real:
                        globalVariables.WriteReal(variable.Name, localVariables.ReadReal(variable.Name));
                        break;
                }
            }
        }
    }
}
