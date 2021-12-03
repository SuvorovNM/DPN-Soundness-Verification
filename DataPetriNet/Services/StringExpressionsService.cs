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
    public class StringExpressionsService : IExpressionsService
    {
        private readonly Dictionary<string, List<ValueInterval<string>>> stringVariablesDict;
        private readonly Random randomGenerator;

        public StringExpressionsService()
        {
            stringVariablesDict = new Dictionary<string, List<ValueInterval<string>>>();
            randomGenerator = new Random();
        }

        public bool ExecuteExpression(VariablesStore globalVariables, IConstraintExpression expression)
        {
            var stringExpression = expression as ConstraintExpression<string>;
            if (stringExpression.ConstraintVariable.VariableType == VariableType.Read)
            {
                return stringExpression.Evaluate(globalVariables.ReadString(stringExpression.ConstraintVariable.Name));
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

        public bool SelectValue(string name, VariablesStore values)
        {
            if (!stringVariablesDict.ContainsKey(name))
            {
                values.WriteString(name, new DefinableValue<string>());
                return true;
            }

            var inconsistentDefinition = stringVariablesDict[name].Any(x => x.Start.HasValue && x.Start.Value.IsDefined) &&
                stringVariablesDict[name].Any(x => x.Start.HasValue && !x.Start.Value.IsDefined);
            if (inconsistentDefinition)
            {
                return false;
            }
            if (stringVariablesDict[name].All(x => x.Start.HasValue && !x.Start.Value.IsDefined))
            {
                values.WriteString(name, new DefinableValue<string>());
                return true;
            }

            if (stringVariablesDict[name].All(x => x.ForbiddenValue.HasValue && x.ForbiddenValue.Value.IsDefined))
            {
                values.WriteString(name, new DefinableValue<string> { Value = GetNotForbiddenString(name) });
                return true;
            }
            if (stringVariablesDict[name].All(x => x.ForbiddenValue.HasValue && !x.ForbiddenValue.Value.IsDefined))
            {
                values.WriteString(name, new DefinableValue<string> { Value = StringRandom() });
                return true;
            }

            var firstSelectedStringValue = stringVariablesDict[name]
                .Where(x => x.Start.HasValue && x.Start.Value.IsDefined)
                .Select(x => x.Start.Value.Value)
                .FirstOrDefault();

            if (IsIncorrect(name, firstSelectedStringValue))
            {
                return false;
            }

            values.WriteString(name, new DefinableValue<string> { Value = firstSelectedStringValue });
            return true;
        }

        private string StringRandom()
        {
            return Guid.NewGuid().ToString();
        }

        private string GetNotForbiddenString(string name)
        {
            var forbiddenStrings = stringVariablesDict[name]
                .Where(x => x.ForbiddenValue.HasValue && x.ForbiddenValue.Value.IsDefined)
                .Select(x => x.ForbiddenValue.Value.Value);
            var selectedString = default(string);
            do
            {
                selectedString = StringRandom();
            } while (forbiddenStrings.Contains(selectedString));
            return selectedString;
        }

        private bool IsIncorrect(string name, string stringValue)
        {
            return stringVariablesDict[name].Any(x => x.Start.HasValue && x.Start.Value.IsDefined && x.Start.Value.Value != stringValue)
                            || stringVariablesDict[name].Any(x => x.End.HasValue && x.End.Value.IsDefined && x.End.Value.Value != stringValue)
                            || stringVariablesDict[name].Any(x => x.ForbiddenValue.HasValue && x.ForbiddenValue.Value.IsDefined && x.ForbiddenValue.Value.Value == stringValue);
        }

        public void Clear()
        {
            stringVariablesDict.Clear();
        }
    }
}
