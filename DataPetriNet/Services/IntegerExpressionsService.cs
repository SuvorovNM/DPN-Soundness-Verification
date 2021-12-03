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

        public void Clear()
        {
            integerVariablesDict.Clear();
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
            if (!integerVariablesDict.ContainsKey(name))
            {
                values.WriteInteger(name, new DefinableValue<long>());
                return true;
            }

            var inconsistentDefinition = integerVariablesDict[name].Any(x => x.Start.HasValue && x.Start.Value.IsDefined) &&
                integerVariablesDict[name].Any(x => x.Start.HasValue && !x.Start.Value.IsDefined);
            if (inconsistentDefinition)
            {
                return false;
            }
            if (integerVariablesDict[name].All(x => x.Start.HasValue && !x.Start.Value.IsDefined))
            {
                values.WriteInteger(name, new DefinableValue<long>());
                return true;
            }

            var minimalValue = long.MinValue;
            var maximalValue = long.MaxValue;

            // Consider equality (greater or equal) and multiple conditions like a >= 5 and a > 5
            var minValues = integerVariablesDict[name].Where(x => x.Start.HasValue && x.Start.Value.IsDefined);
            if (minValues.Any())
            {
                minimalValue = minValues.Max(x => x.Start.Value.Value);
            }

            var maxValues = integerVariablesDict[name].Where(x => x.End.HasValue && x.Start.Value.IsDefined);
            if (maxValues.Any())
            {
                maximalValue = maxValues.Min(x => x.End.Value.Value);
            }

            if (minimalValue > maximalValue)
            {
                return false;
            }
            if (!integerVariablesDict[name].Any(x => x.ForbiddenValue.HasValue || x.ForbiddenValue.Value.IsDefined))
            {
                values.WriteInteger(name, new DefinableValue<long> { Value = LongRandom(minimalValue, maximalValue + 1) });
                return true;
            }

            var forbiddenValues = GetForbiddenNumbers(integerVariablesDict[name], minimalValue, maximalValue);
            var intervals = GenerateIntevals(minimalValue, maximalValue, forbiddenValues);
            if (intervals.Count == 0)
            {
                return false;
            }
            else
            {
                var intervalNumber = randomGenerator.Next(0, intervals.Count);
                values.WriteInteger(name, new DefinableValue<long> { Value = LongRandom(intervals[intervalNumber].start, intervals[intervalNumber].end) });
                return true;
            }
        }

        private List<T> GetForbiddenNumbers<T>(List<ValueInterval<T>> valuesList, T minimalValue, T maximalValue)
            where T : IComparable<T>, IEquatable<T>
        {
            return valuesList
                .Where(x => x.ForbiddenValue.HasValue &&
                x.ForbiddenValue.Value.IsDefined &&
                x.ForbiddenValue.Value.Value.CompareTo(minimalValue) >= 0 &&
                x.ForbiddenValue.Value.Value.CompareTo(maximalValue) <= 0)
                .Select(x => x.ForbiddenValue.Value.Value)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        private static List<(long start, long end)> GenerateIntevals(long minimalValue, long maximalValue, List<long> forbiddenValues)
        {
            var intervals = new List<(long start, long end)>(forbiddenValues.Count + 1);
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

            return intervals;
        }

        private long LongRandom(long min, long max)
        {
            byte[] buf = new byte[8];
            randomGenerator.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return Math.Abs(longRand % (max - min)) + min;
        }
    }
}
