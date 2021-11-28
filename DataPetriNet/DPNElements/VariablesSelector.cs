using DataPetriNet.DPNElements.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.DPNElements
{
    public class VariablesSelector
    {
        private readonly Dictionary<string, List<ValueInterval<long>>> integerVariablesDict;
        private readonly Dictionary<string, List<ValueInterval<double>>> realVariablesDict;
        private readonly Dictionary<string, List<ValueInterval<bool>>> booleanVariablesDict;
        private readonly Dictionary<string, List<ValueInterval<string>>> stringVariablesDict;
        private readonly Random randomGenerator;

        public VariablesSelector()
        {
            integerVariablesDict = new Dictionary<string, List<ValueInterval<long>>>();
            realVariablesDict = new Dictionary<string, List<ValueInterval<double>>>();
            booleanVariablesDict = new Dictionary<string, List<ValueInterval<bool>>>();
            stringVariablesDict = new Dictionary<string, List<ValueInterval<string>>>();
            randomGenerator = new Random();
        }

        public void AddValueIntervalToVariable(string name, ValueInterval<long> valueInterval)
        {
            if (!integerVariablesDict.ContainsKey(name))
            {
                integerVariablesDict[name] = new List<ValueInterval<long>>();
            }

            integerVariablesDict[name].Add(valueInterval);
        }

        public void AddValueIntervalToVariable(string name, ValueInterval<double> valueInterval)
        {
            if (!realVariablesDict.ContainsKey(name))
            {
                realVariablesDict[name] = new List<ValueInterval<double>>();
            }

            realVariablesDict[name].Add(valueInterval);
        }

        public void AddValueIntervalToVariable(string name, ValueInterval<bool> valueInterval)
        {
            if (!booleanVariablesDict.ContainsKey(name))
            {
                booleanVariablesDict[name] = new List<ValueInterval<bool>>();
            }

            booleanVariablesDict[name].Add(valueInterval);
        }

        public void AddValueIntervalToVariable(string name, ValueInterval<string> valueInterval)
        {
            if (!stringVariablesDict.ContainsKey(name))
            {
                stringVariablesDict[name] = new List<ValueInterval<string>>();
            }

            stringVariablesDict[name].Add(valueInterval);
        }

        public bool TrySelectValue(string name, out long value)
        {
            // If no value interval is assigned, return random value
            // TODO: Examine, if this case is possible
            if (!integerVariablesDict.ContainsKey(name))
            {
                value = (long)(randomGenerator.NextDouble() * long.MaxValue);
                return true;
            }

            // Consider equality (greater or equal) and multiple conditions like a >= 5 and a > 5
            var minimalValue = integerVariablesDict[name].Where(x => x.Start.HasValue).Max(x => x.Start.Value);
            var maximalValue = integerVariablesDict[name].Where(x => x.End.HasValue).Min(x => x.End.Value);

            if (minimalValue > maximalValue)
            {
                value = 0;
                return false;
            }
            if (!integerVariablesDict[name].Any(x => x.ForbiddenValue.HasValue))
            {
                value = LongRandom(minimalValue, maximalValue + 1);
                return true;
            }

            var forbiddenValues = integerVariablesDict[name]
                .Where(x => x.ForbiddenValue.HasValue
                && x.ForbiddenValue.Value >= minimalValue
                && x.ForbiddenValue.Value <= maximalValue)
                .Select(x => x.ForbiddenValue.Value)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

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
                    if (forbiddenValues[i]!= maximalValue)
                        intervals.Add((forbiddenValues[i] + 1, maximalValue));
                }
                else
                {
                    if (forbiddenValues[i - 1] + 1 != forbiddenValues[i])
                        intervals.Add((forbiddenValues[i - 1] + 1, forbiddenValues[i] - 1));
                }
            }
            if (intervals.Count == 0)
            {
                value = 0;
                return false;
            }
            else
            {
                var intervalNumber = randomGenerator.Next(0, intervals.Count);
                value = LongRandom(intervals[intervalNumber].start, intervals[intervalNumber].end);
                return true;
            }

            // Considering forbidden values
        }
        private long LongRandom(long min, long max)
        {
            byte[] buf = new byte[8];
            randomGenerator.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return Math.Abs(longRand % (max - min)) + min;
        }

        public bool TrySelectValue(string name, out bool value)
        {
            // If no value interval is assigned, return random value
            // TODO: Examine, if this case is possible
            if (!booleanVariablesDict.ContainsKey(name))
            {
                value = randomGenerator.Next(0,2) == 0;
                return true;
            }

            var boolValue = booleanVariablesDict[name].Where(x => x.Start.HasValue).Select(x => x.Start.Value).First();
            if (booleanVariablesDict[name].Any(x => x.Start.HasValue && x.Start.Value != boolValue)
                || booleanVariablesDict[name].Any(x => x.End.HasValue && x.End.Value != boolValue)
                || booleanVariablesDict[name].Any(x => x.ForbiddenValue.HasValue && x.ForbiddenValue.Value == boolValue))
            {
                value = false;
                return false;
            }

            value = boolValue;
            return true;
        }
    }
}
