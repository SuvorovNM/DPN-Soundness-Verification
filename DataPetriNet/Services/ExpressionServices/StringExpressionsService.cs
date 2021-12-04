using DataPetriNet.Abstractions;
using DataPetriNet.DPNElements;
using DataPetriNet.DPNElements.Internals;
using DataPetriNet.Enums;
using DataPetriNet.Services.SourceServices;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataPetriNet.Services.ExpressionServices
{
    public class StringExpressionsService : IExpressionsService
    {
        private readonly Dictionary<string, List<ValueInterval<string>>> stringVariablesDict;

        public StringExpressionsService()
        {
            stringVariablesDict = new Dictionary<string, List<ValueInterval<string>>>();
        }

        public bool ExecuteExpression(ISourceService globalVariables, IConstraintExpression expression)
        {
            var stringExpression = expression as ConstraintExpression<string>;
            if (stringExpression.ConstraintVariable.VariableType == VariableType.Read)
            {
                return stringExpression.Evaluate(globalVariables.Read(stringExpression.ConstraintVariable.Name) as DefinableValue<string>);
            }
            else
            {
                if (!stringVariablesDict.ContainsKey(stringExpression.ConstraintVariable.Name))
                {
                    stringVariablesDict[stringExpression.ConstraintVariable.Name] = new List<ValueInterval<string>>();
                }

                stringVariablesDict[stringExpression.ConstraintVariable.Name].Add(stringExpression.GetValueInterval());
            }

            return true;
        }

        public bool SelectValue(string name, ISourceService values)
        {
            DefinableValue<string> selectedValue = default;

            var valueCanBeSelected = stringVariablesDict.ContainsKey(name) && TryInferValue(name, out selectedValue);
            if (valueCanBeSelected)
            {
                values.Write(name, selectedValue);
            }

            return valueCanBeSelected;
        }

        private bool TryInferValue(string name, out DefinableValue<string> value)
        {
            // Selected values by "=" sign
            var chosenEqualValues = stringVariablesDict[name]
                .Where(x => x.Start.HasValue)
                .Select(x => x.Start.Value)
                .Distinct()
                .ToList();

            if (chosenEqualValues.Count == 1)
            {
                // Values selected by "!=" sign must not intersect selected by "=" sign
                var noForbiddenEqualToChosenEqualValue = !stringVariablesDict[name]
                    .Where(x => x.ForbiddenValue.HasValue)
                    .Any(x => x.ForbiddenValue.Value == chosenEqualValues[0]);

                value = noForbiddenEqualToChosenEqualValue
                    ? chosenEqualValues[0]
                    : new DefinableValue<string>();

                return noForbiddenEqualToChosenEqualValue;
            }
            if (chosenEqualValues.Count == 0)
            {
                // Suppose that unforbidden string exists
                value = new DefinableValue<string>(GetNotForbiddenString(name));
                return true;
            }

            value = new DefinableValue<string>();
            return false;
        }

        private string GetNotForbiddenString(string name)
        {
            var forbiddenStrings = stringVariablesDict[name]
                .Where(x => x.ForbiddenValue.HasValue && x.ForbiddenValue.Value.IsDefined)
                .Select(x => x.ForbiddenValue.Value.Value);
            var selectedString = default(string);
            do
            {
                selectedString = Guid.NewGuid().ToString();
            } while (forbiddenStrings.Contains(selectedString));
            return selectedString;
        }

        public void Clear()
        {
            stringVariablesDict.Clear();
        }
    }
}
