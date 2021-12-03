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
    class RealExpressionsService : IExpressionsService
    {
        private readonly Dictionary<string, List<ValueInterval<double>>> realVariablesDict;
        private readonly Random randomGenerator;

        public RealExpressionsService()
        {
            realVariablesDict = new Dictionary<string, List<ValueInterval<double>>>();
            randomGenerator = new Random();
        }

        public bool ExecuteExpression(VariablesStore globalVariables, IConstraintExpression expression)
        {
            var realExpression = expression as ConstraintExpression<double>;
            if (realExpression.ConstraintVariable.VariableType == VariableType.Read)
            {
                return realExpression.Evaluate(globalVariables.ReadReal(realExpression.ConstraintVariable.Name));
            }
            else
            {
                if (!realVariablesDict.ContainsKey(realExpression.ConstraintVariable.Name))
                {
                    realVariablesDict[realExpression.ConstraintVariable.Name] = new List<ValueInterval<double>>();
                }

                realVariablesDict[realExpression.ConstraintVariable.Name].Add(realExpression.GetValueInterval());
            }

            return true;
        }

        public bool SelectValue(string name, VariablesStore values)
        {
            if (!realVariablesDict.ContainsKey(name))
            {
                values.WriteReal(name, new DefinableValue<double>());
                return true;
            }

            var inconsistentDefinition = realVariablesDict[name].Any(x => x.Start.HasValue && x.Start.Value.IsDefined) &&
                realVariablesDict[name].Any(x => x.Start.HasValue && !x.Start.Value.IsDefined);
            if (inconsistentDefinition)
            {
                return false;
            }
            if (realVariablesDict[name].All(x => x.Start.HasValue && !x.Start.Value.IsDefined))
            {
                values.WriteReal(name, new DefinableValue<double>());
                return true;
            }

            var minimalValue = double.MinValue;
            var maximalValue = double.MaxValue;

            var minValues = realVariablesDict[name].Where(x => x.Start.HasValue && x.Start.Value.IsDefined);
            if (minValues.Any())
            {
                minimalValue = minValues.Max(x => x.Start.Value.Value);
            }

            var maxValues = realVariablesDict[name].Where(x => x.End.HasValue && x.Start.Value.IsDefined);
            if (maxValues.Any())
            {
                maximalValue = maxValues.Min(x => x.End.Value.Value);
            }

            if (minimalValue > maximalValue)
            {
                return false;
            }
            if (!realVariablesDict[name].Any(x => x.ForbiddenValue.HasValue || x.ForbiddenValue.Value.IsDefined))
            {
                values.WriteReal(name, new DefinableValue<double> { Value = randomGenerator.NextDouble() * (maximalValue - minimalValue) + minimalValue });
                return true;
            }

            var forbiddenValues = GetForbiddenNumbers(realVariablesDict[name], minimalValue, maximalValue);
            var intervals = GenerateIntervals(minimalValue, maximalValue, forbiddenValues);

            if (intervals.Count == 0)
            {
                return false;
            }
            else
            {
                var intervalNumber = randomGenerator.Next(0, intervals.Count);
                values.WriteReal(name, new DefinableValue<double> { Value = DoubleRandom(intervals[intervalNumber].start, intervals[intervalNumber].end) });
                return true;
            }
        }

        public double DoubleRandom(double min, double max)
        {
            return randomGenerator.NextDouble() * (max - min) + min;
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

        private static List<(double start, double end)> GenerateIntervals(double minimalValue, double maximalValue, List<double> forbiddenValues)
        {
            var intervals = new List<(double start, double end)>(forbiddenValues.Count + 1);
            for (int i = 0; i < forbiddenValues.Count; i++)
            {
                if (i == 0)
                {
                    if (minimalValue != forbiddenValues[i])
                        intervals.Add((minimalValue, forbiddenValues[i] - double.Epsilon));
                }
                else if (i == forbiddenValues.Count)
                {
                    if (forbiddenValues[i] != maximalValue)
                        intervals.Add((forbiddenValues[i] + double.Epsilon, maximalValue));
                }
                else
                {
                    if (forbiddenValues[i - 1] + double.Epsilon != forbiddenValues[i])
                        intervals.Add((forbiddenValues[i - 1] + double.Epsilon, forbiddenValues[i] - double.Epsilon));
                }
            }

            return intervals;
        }


        public void Clear()
        {
            realVariablesDict.Clear();
        }
    }
}
