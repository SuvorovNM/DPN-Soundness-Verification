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
    class RealExpressionsService : IExpressionsService
    {
        private readonly Dictionary<string, List<ValueInterval<double>>> realVariablesDict;
        private readonly Random randomGenerator;

        public RealExpressionsService()
        {
            realVariablesDict = new Dictionary<string, List<ValueInterval<double>>>();
            randomGenerator = new Random();
        }

        public bool EvaluateExpression(ISourceService globalVariables, IConstraintExpression expression)
        {
            var realExpression = expression as ConstraintExpression<double>;
            return realExpression.Evaluate(globalVariables.Read(realExpression.ConstraintVariable.Name) as DefinableValue<double>);
        }

        public void AddValueInterval(IConstraintExpression expression)
        {
            var realExpression = expression as ConstraintExpression<double>;
            if (!realVariablesDict.ContainsKey(realExpression.ConstraintVariable.Name))
            {
                realVariablesDict[realExpression.ConstraintVariable.Name] = new List<ValueInterval<double>>();
            }

            realVariablesDict[realExpression.ConstraintVariable.Name].Add(realExpression.GetValueInterval());
        }

        public bool TryInferValue(string name, out IDefinableValue value)
        {
            if (!realVariablesDict.ContainsKey(name))
            {
                value = default;
                return false;
            }

            var minimalValue = double.MinValue;
            var maximalValue = double.MaxValue;

            // all distinct values of parameter IsDefined:
            // if 2 simultaneously then no value can be selected
            // if 1 then borders should be updated if IsDefined = true, or null value can be assigned if it is not forbidden
            // if 0-1 then calculate intervals and choose randomly a value if any intervals exist
            var distinctIsDefinedValues = realVariablesDict[name]
                .Where(x => x.Start.HasValue)
                .Select(x => x.Start.Value.IsDefined)
                .Distinct()
                .ToList();

            if (distinctIsDefinedValues.Count == 1)
            {
                if (distinctIsDefinedValues[0] == false)
                {
                    var isNullAllowed = !realVariablesDict[name]
                        .Any(x => x.ForbiddenValue.HasValue && !x.ForbiddenValue.Value.IsDefined);

                    value = new DefinableValue<double>();
                    return isNullAllowed;
                }

                UpdateBorders(name, ref minimalValue, ref maximalValue);
            }
            if (distinctIsDefinedValues.Count <= 1)
            {
                var forbiddenValues = GetForbiddenNumbers(realVariablesDict[name], minimalValue, maximalValue);
                var intervals = GenerateIntervals(minimalValue, maximalValue, forbiddenValues);

                if (intervals.Count == 0)
                {
                    value = new DefinableValue<double>();
                }
                else
                {
                    var intervalNumber = randomGenerator.Next(0, intervals.Count);
                    value = new DefinableValue<double>(DoubleRandom(intervals[intervalNumber].start, intervals[intervalNumber].end));
                }

                return intervals.Count > 0;
            }

            value = new DefinableValue<double>();
            return false;
        }

        public bool GenerateExpressionsBasedOnIntervals(string name, out List<IConstraintExpression> constraintExpressions)
        {
            if (!realVariablesDict.ContainsKey(name))
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }

            constraintExpressions = new List<IConstraintExpression>();
            var minimalValue = double.MinValue;
            var maximalValue = double.MaxValue;

            var distinctIsDefinedValues = realVariablesDict[name]
                .Where(x => x.Start.HasValue)
                .Select(x => x.Start.Value.IsDefined)
                .Distinct()
                .ToList();

            if (distinctIsDefinedValues.Count == 1)
            {
                if (distinctIsDefinedValues[0] == false)
                {
                    var isNullAllowed = !realVariablesDict[name]
                        .Any(x => x.ForbiddenValue.HasValue && !x.ForbiddenValue.Value.IsDefined);

                    // Format a == b
                    if (isNullAllowed)
                        constraintExpressions.Add(ConstraintExpression<double>.GenerateEqualExpression(name, DomainType.Real, new DefinableValue<double>()));
                    return isNullAllowed;
                }

                UpdateBorders(name, ref minimalValue, ref maximalValue);
            }
            if (distinctIsDefinedValues.Count <= 1)
            {
                var forbiddenValues = GetForbiddenNumbers(realVariablesDict[name], minimalValue, maximalValue);
                var intervals = GenerateIntervals(minimalValue, maximalValue, forbiddenValues);
                if (intervals.Count == 0)
                {
                    return false;
                }

                minimalValue = intervals[0].start;
                maximalValue = intervals[^1].end;

                if (minimalValue == maximalValue)
                {
                    constraintExpressions.Add(ConstraintExpression<double>.GenerateEqualExpression(name, DomainType.Real, new DefinableValue<double>(minimalValue)));
                    return true;
                }

                // Format: a <= max && a >= min && a!=t1 && a!=t2...
                if (maximalValue != long.MaxValue)
                {
                    constraintExpressions.Add(ConstraintExpression<double>.GenerateLessThanOrEqualExpression(name, DomainType.Real, maximalValue));
                }
                if (minimalValue != long.MinValue)
                {
                    constraintExpressions.Add(ConstraintExpression<double>.GenerateGreaterThanOrEqualExpression(name, DomainType.Real, minimalValue));
                }

                foreach (var forbiddenValue in forbiddenValues)
                {
                    constraintExpressions.Add(ConstraintExpression<double>.GenerateUnequalExpression(name, DomainType.Real, new DefinableValue<double>(forbiddenValue)));
                }

                return true;
            }

            return false;
        }

        private void UpdateBorders(string name, ref double minimalValue, ref double maximalValue)
        {
            var minValues = realVariablesDict[name].Where(x => x.Start.HasValue && x.Start.Value.IsDefined);
            if (minValues.Any())
            {
                minimalValue = minValues.Max(x => x.Start.Value.Value);
            }

            var maxValues = realVariablesDict[name].Where(x => x.End.HasValue && x.End.Value.IsDefined);
            if (maxValues.Any())
            {
                maximalValue = maxValues.Min(x => x.End.Value.Value);
            }
        }

        private double DoubleRandom(double min, double max)
        {
            return randomGenerator.NextDouble() * (max - min) + min;
        }

        private List<double> GetForbiddenNumbers(List<ValueInterval<double>> valuesList, double minimalValue, double maximalValue)
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

        private static List<(double start, double end)> GenerateIntervals(double minimalValue, double maximalValue, List<double> forbiddenValues)
        {
            var intervals = new List<(double start, double end)>(forbiddenValues.Count + 1);
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
            }

            return intervals;
        }

        public void Clear()
        {
            realVariablesDict.Clear();
        }
    }
}
