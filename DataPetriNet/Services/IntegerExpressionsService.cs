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
    class IntegerExpressionsService : IExpressionsService
    {
        private readonly Dictionary<string, List<ValueInterval<long>>> integerVariablesDict;
        private readonly Random randomGenerator;

        public IntegerExpressionsService()
        {
            integerVariablesDict = new Dictionary<string, List<ValueInterval<long>>>();
            randomGenerator = new Random();
        }

        public bool ExecuteExpression(VariablesStore globalVariables, IConstraintExpression expression)
        {
            var integerExpression = expression as ConstraintExpression<long>;
            if (integerExpression.ConstraintVariable.VariableType == VariableType.Read)
            {
                return integerExpression.Evaluate(globalVariables.ReadInteger(integerExpression.ConstraintVariable.Name));
            }
            else
            {
                if (!integerVariablesDict.ContainsKey(integerExpression.ConstraintVariable.Name))
                {
                    integerVariablesDict[integerExpression.ConstraintVariable.Name] = new List<ValueInterval<long>>();
                }

                integerVariablesDict[integerExpression.ConstraintVariable.Name].Add(integerExpression.GetValueInterval());
            }

            return true;
        }

        public bool SelectValue(string name, VariablesStore values)
        {
            DefinableValue<long> selectedValue = default;

            var valueCanBeSelected = integerVariablesDict.ContainsKey(name) && TryInferValue(name, out selectedValue);
            if (valueCanBeSelected)
            {
                values.WriteInteger(name, selectedValue);
            }

            return valueCanBeSelected;
        }

        private bool TryInferValue(string name, out DefinableValue<long> value)
        {
            var minimalValue = long.MinValue;
            var maximalValue = long.MaxValue;

            // all distinct values of parameter IsDefined:
            // if 2 simultaneously then no value can be selected
            // if 1 then borders should be updated if IsDefined = true, or null value can be assigned if it is not forbidden
            // if 0-1 then calculate intervals and choose randomly a value if any intervals exist
            var distinctIsDefinedValues = integerVariablesDict[name]
                .Where(x => x.Start.HasValue)
                .Select(x => x.Start.Value.IsDefined)
                .Distinct()
                .ToList();

            if (distinctIsDefinedValues.Count == 1)
            {
                if (distinctIsDefinedValues[0] == false)
                {
                    var isNullAllowed = !integerVariablesDict[name]
                        .Any(x => x.ForbiddenValue.HasValue && !x.ForbiddenValue.Value.IsDefined);

                    value = new DefinableValue<long>();
                    return isNullAllowed;
                }

                UpdateBorders(name, ref minimalValue, ref maximalValue);
            }
            if (distinctIsDefinedValues.Count <= 1)
            {
                var forbiddenValues = GetForbiddenNumbers(integerVariablesDict[name], minimalValue, maximalValue);
                var intervals = GenerateIntervals(minimalValue, maximalValue, forbiddenValues);

                if (intervals.Count == 0)
                {
                    value = new DefinableValue<long>();
                }
                else
                {
                    var intervalNumber = randomGenerator.Next(0, intervals.Count);
                    value = new DefinableValue<long> { Value = LongRandom(intervals[intervalNumber].start, intervals[intervalNumber].end) };
                }

                return intervals.Count > 0;
            }

            value = new DefinableValue<long>();
            return false;
        }

        private void UpdateBorders(string name, ref long minimalValue, ref long maximalValue)
        {
            var minValues = integerVariablesDict[name].Where(x => x.Start.HasValue && x.Start.Value.IsDefined);
            if (minValues.Any())
            {
                minimalValue = minValues.Max(x => x.Start.Value.Value);
            }

            var maxValues = integerVariablesDict[name].Where(x => x.End.HasValue && x.End.Value.IsDefined);
            if (maxValues.Any())
            {
                maximalValue = maxValues.Min(x => x.End.Value.Value);
            }
        }

        private static List<long> GetForbiddenNumbers(List<ValueInterval<long>> valuesList, long minimalValue, long maximalValue)
        {
            return valuesList
                .Where(x => x.ForbiddenValue.HasValue &&
                            x.ForbiddenValue.Value.IsDefined &&
                            x.ForbiddenValue.Value.Value >= minimalValue &&
                            x.ForbiddenValue.Value.Value <= maximalValue)
                .Select(x => x.ForbiddenValue.Value.Value)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        private static List<(long start, long end)> GenerateIntervals(long minimalValue, long maximalValue, List<long> forbiddenValues)
        {
            var intervals = new List<(long start, long end)>(forbiddenValues.Count + 1);
            if (maximalValue < minimalValue)
            {
                return intervals;
            }

            if (!forbiddenValues.Any())
            {
                intervals.Add((minimalValue, maximalValue));
            }
            else
            {
                for (int i = 0; i < forbiddenValues.Count; i++)
                {
                    if (i == 0)
                    {
                        if (minimalValue != forbiddenValues[i])
                            intervals.Add((minimalValue, forbiddenValues[i] - 1));
                    }
                    else if (i == forbiddenValues.Count)
                    {
                        if (forbiddenValues[i] != maximalValue)
                            intervals.Add((forbiddenValues[i] + 1, maximalValue));
                    }
                    else
                    {
                        if (forbiddenValues[i - 1] + 1 != forbiddenValues[i])
                            intervals.Add((forbiddenValues[i - 1] + 1, forbiddenValues[i] - 1));
                    }
                }
            }

            return intervals;
        }

        private long LongRandom(long min, long max)
        {
            byte[] buf = new byte[8];
            randomGenerator.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return Math.Abs(longRand % (max - min)) + min;
        }

        public void Clear()
        {
            integerVariablesDict.Clear();
        }
    }
}
