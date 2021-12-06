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

        public bool EvaluateExpression(ISourceService globalVariables, IConstraintExpression expression)
        {
            var stringExpression = expression as ConstraintExpression<string>;
            return stringExpression.Evaluate(globalVariables.Read(stringExpression.ConstraintVariable.Name) as DefinableValue<string>);
        }

        public void AddValueInterval(IConstraintExpression expression)
        {
            var stringExpression = expression as ConstraintExpression<string>;
            if (!stringVariablesDict.ContainsKey(stringExpression.ConstraintVariable.Name))
            {
                stringVariablesDict[stringExpression.ConstraintVariable.Name] = new List<ValueInterval<string>>();
            }

            stringVariablesDict[stringExpression.ConstraintVariable.Name].Add(stringExpression.GetValueInterval());
        }

        public bool TryInferValue(string name, out IDefinableValue value)
        {
            if (!stringVariablesDict.ContainsKey(name))
            {
                value = default;
                return false;
            }

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
                    .Any(x => x.ForbiddenValue.Value.Equals(chosenEqualValues[0]));

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

        public bool GenerateExpressionsBasedOnIntervals(string name, out List<IConstraintExpression> constraintExpressions)
        {
            if (!stringVariablesDict.ContainsKey(name))
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }

            constraintExpressions = new List<IConstraintExpression>();

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
                    .Any(x => x.ForbiddenValue.Value.Equals(chosenEqualValues[0]));

                if (noForbiddenEqualToChosenEqualValue)
                {
                    constraintExpressions.Add(ConstraintExpression<string>.GenerateEqualExpression(name, DomainType.String, chosenEqualValues[0]));
                }

                return noForbiddenEqualToChosenEqualValue;
            }
            if (chosenEqualValues.Count == 0)
            {
                var forbiddenStrings = stringVariablesDict[name]
                    .Where(x => x.ForbiddenValue.HasValue)
                    .Select(x => x.ForbiddenValue.Value);

                foreach (var forbiddenString in forbiddenStrings.OrderBy(x=>x.IsDefined).ThenBy(x=>x.Value))
                {
                    constraintExpressions.Add(ConstraintExpression<string>.GenerateUnequalExpression(name, DomainType.String, forbiddenString));
                }

                return true;
            }

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
