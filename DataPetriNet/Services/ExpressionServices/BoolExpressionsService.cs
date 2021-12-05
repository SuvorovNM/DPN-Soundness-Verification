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
    public class BoolExpressionsService : IExpressionsService
    {
        private readonly Dictionary<string, List<ValueInterval<bool>>> booleanVariablesDict;
        private readonly Random randomGenerator;
        private const int PossibleBoolValuesCount = 2; // True + False

        public BoolExpressionsService()
        {
            booleanVariablesDict = new Dictionary<string, List<ValueInterval<bool>>>();
            randomGenerator = new Random();
        }

        public bool EvaluateExpression(ISourceService globalVariables, IConstraintExpression expression)
        {
            var booleanExpression = expression as ConstraintExpression<bool>;

            return booleanExpression.Evaluate(globalVariables.Read(booleanExpression.ConstraintVariable.Name) as DefinableValue<bool>);
        }

        public void AddValueInterval(IConstraintExpression expression)
        {
            var booleanExpression = expression as ConstraintExpression<bool>;
            if (!booleanVariablesDict.ContainsKey(booleanExpression.ConstraintVariable.Name))
            {
                booleanVariablesDict[booleanExpression.ConstraintVariable.Name] = new List<ValueInterval<bool>>();
            }

            booleanVariablesDict[booleanExpression.ConstraintVariable.Name].Add(booleanExpression.GetValueInterval());
        }

        public bool TryInferValue(string name, out IDefinableValue value)
        {
            if (!booleanVariablesDict.ContainsKey(name))
            {
                value = default;
                return false;
            }

            // Selected values by "=" sign
            var chosenEqualValues = booleanVariablesDict[name]
                .Where(x => x.Start.HasValue)
                .Select(x => x.Start.Value)
                .Distinct()
                .ToList();

            if (chosenEqualValues.Count == 1)
            {
                // Values selected by "!=" sign must not intersect selected by "=" sign
                var noForbiddenEqualToChosenEqualValue = !booleanVariablesDict[name]
                    .Where(x => x.ForbiddenValue.HasValue)
                    .Any(x => x.ForbiddenValue.Value == chosenEqualValues[0]);

                value = noForbiddenEqualToChosenEqualValue
                    ? chosenEqualValues[0]
                    : new DefinableValue<bool>();

                return noForbiddenEqualToChosenEqualValue;
            }
            if (chosenEqualValues.Count == 0)
            {
                // Selected values by "!=" sign
                var chosenUnequalValues = booleanVariablesDict[name]
                    .Where(x => x.ForbiddenValue.HasValue)
                    .Select(x => x.ForbiddenValue.Value)
                    .Distinct();

                // TODO: clarify if undefined can be selected
                // Value can exist only if both true and false are not forbidden
                var allowedValuesExist = chosenUnequalValues.Count(x => x.IsDefined) < PossibleBoolValuesCount;

                if (allowedValuesExist)
                {
                    // Forbidden list can contain both null-values and normal values - only defined values needed for consideration
                    var valueExistInForbiddenList = chosenUnequalValues.Any(x => x.IsDefined);

                    // At current stage do not generate nulls
                    value = valueExistInForbiddenList
                        ? new DefinableValue<bool>(!chosenUnequalValues.First(x => x.IsDefined).Value)
                        : new DefinableValue<bool>(randomGenerator.Next(0, 2) == 0);
                }
                else
                {
                    value = new DefinableValue<bool>();
                }

                return allowedValuesExist;
            }

            value = new DefinableValue<bool>();
            return false;
        }

        public bool GenerateExpressionsBasedOnIntervals(string name, out List<IConstraintExpression> constraintExpressions)
        {
            if (!booleanVariablesDict.ContainsKey(name))
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }

            constraintExpressions = new List<IConstraintExpression>();

            var chosenEqualValues = booleanVariablesDict[name]
                .Where(x => x.Start.HasValue)
                .Select(x => x.Start.Value)
                .Distinct()
                .ToList();

            if (chosenEqualValues.Count == 1)
            {
                // Values selected by "!=" sign must not intersect selected by "=" sign
                var noForbiddenEqualToChosenEqualValue = !booleanVariablesDict[name]
                    .Where(x => x.ForbiddenValue.HasValue)
                    .Any(x => x.ForbiddenValue.Value == chosenEqualValues[0]);

                if (noForbiddenEqualToChosenEqualValue)
                {
                    constraintExpressions.Add(ConstraintExpression<bool>.GenerateEqualExpression(name, DomainType.Boolean, chosenEqualValues[0]));
                }

                return noForbiddenEqualToChosenEqualValue;
            }
            if (chosenEqualValues.Count == 0)
            {
                // Selected values by "!=" sign
                var chosenUnequalValues = booleanVariablesDict[name]
                    .Where(x => x.ForbiddenValue.HasValue)
                    .Select(x => x.ForbiddenValue.Value)
                    .Distinct();

                // TODO: clarify if undefined can be selected
                // Value can exist only if both true and false are not forbidden
                var allowedValuesExist = chosenUnequalValues.Count(x => x.IsDefined) < PossibleBoolValuesCount;

                if (allowedValuesExist)
                {
                    // Forbidden list can contain both null-values and normal values - only defined values needed for consideration
                    var isPossibleToInvert = chosenUnequalValues.Any(x => x.IsDefined) && chosenUnequalValues.Any(x=>!x.IsDefined);

                    if (isPossibleToInvert)
                    {
                        var invertedValue = new DefinableValue<bool>(!chosenUnequalValues.First(x => x.IsDefined).Value);
                        constraintExpressions.Add(ConstraintExpression<bool>.GenerateEqualExpression(name, DomainType.Boolean, invertedValue));
                    }
                    else
                    {
                        foreach(var forbiddenValue in chosenUnequalValues.OrderBy(x=>x.IsDefined))
                        {
                            constraintExpressions.Add(ConstraintExpression<bool>.GenerateUnequalExpression(name, DomainType.Boolean, forbiddenValue));
                        }
                    }
                }

                return allowedValuesExist;
            }

            return false;
        }

        public void Clear()
        {
            booleanVariablesDict.Clear();
        }
    }
}
