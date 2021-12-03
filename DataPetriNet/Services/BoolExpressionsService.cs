using DataPetriNet.Abstractions;
using DataPetriNet.DPNElements;
using DataPetriNet.DPNElements.Internals;
using DataPetriNet.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.Services
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

        public bool ExecuteExpression(VariablesStore globalVariables, IConstraintExpression expression)
        {
            var booleanExpression = expression as ConstraintExpression<bool>;
            if (booleanExpression.ConstraintVariable.VariableType == VariableType.Read)
            {
                return booleanExpression.Evaluate(globalVariables.ReadBool(booleanExpression.ConstraintVariable.Name));
            }
            else
            {
                if (!booleanVariablesDict.ContainsKey(booleanExpression.ConstraintVariable.Name))
                {
                    booleanVariablesDict[booleanExpression.ConstraintVariable.Name] = new List<ValueInterval<bool>>();
                }

                booleanVariablesDict[booleanExpression.ConstraintVariable.Name].Add(booleanExpression.GetValueInterval());
            }

            return true;
        }

        public bool SelectValue(string name, VariablesStore values)
        {
            DefinableValue<bool> selectedValue = default;

            var valueCanBeSelected = booleanVariablesDict.ContainsKey(name) && TryInferValue(name, out selectedValue);
            if (valueCanBeSelected)
            {
                values.WriteBool(name, selectedValue);
            }

            return valueCanBeSelected;
        }

        private bool TryInferValue(string name, out DefinableValue<bool> value)
        {
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
                        ? new DefinableValue<bool> { Value = !chosenUnequalValues.First(x => x.IsDefined).Value }
                        : new DefinableValue<bool> { Value = randomGenerator.Next(0, 2) == 0 };
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

        public void Clear()
        {
            booleanVariablesDict.Clear();
        }
    }
}
