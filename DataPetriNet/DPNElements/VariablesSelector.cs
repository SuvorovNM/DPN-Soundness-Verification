using DataPetriNet.Abstractions;
using DataPetriNet.DPNElements.Internals;
using System;
using System.Collections.Generic;
using System.IO;
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

        public bool TrySelectValue(string name, out DefinableValue<long> value)
        {
            // If no value interval is assigned, return random value
            // TODO: Examine, if this case is possible
            if (!integerVariablesDict.ContainsKey(name))
            {
                value = new DefinableValue<long>();
                return true;
            }

            var inconsistentDefinition = integerVariablesDict[name].Any(x => x.Start.HasValue && x.Start.Value.IsDefined) &&
                integerVariablesDict[name].Any(x => x.Start.HasValue && !x.Start.Value.IsDefined);
            if (inconsistentDefinition)
            {
                value = new DefinableValue<long>();
                return false;
            }
            if (integerVariablesDict[name].All(x => x.Start.HasValue && !x.Start.Value.IsDefined))
            {
                value = new DefinableValue<long>();
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
                value = new DefinableValue<long>();
                return false;
            }
            if (!integerVariablesDict[name].Any(x => x.ForbiddenValue.HasValue || x.ForbiddenValue.Value.IsDefined))
            {
                value = new DefinableValue<long> { Value = LongRandom(minimalValue, maximalValue + 1) };
                return true;
            }

            var forbiddenValues = GetForbiddenNumbers(integerVariablesDict[name], minimalValue, maximalValue);
            var intervals = GenerateIntevals(minimalValue, maximalValue, forbiddenValues);
            if (intervals.Count == 0)
            {
                value = new DefinableValue<long>();
                return false;
            }
            else
            {
                var intervalNumber = randomGenerator.Next(0, intervals.Count);
                value = new DefinableValue<long> { Value = LongRandom(intervals[intervalNumber].start, intervals[intervalNumber].end) };
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

        public bool TrySelectValue(string name, out DefinableValue<double> value)
        {
            // If no value interval is assigned, return random value
            // TODO: Examine, if this case is possible
            if (!realVariablesDict.ContainsKey(name))
            {
                value = new DefinableValue<double>();
                return true;
            }

            var inconsistentDefinition = realVariablesDict[name].Any(x => x.Start.HasValue && x.Start.Value.IsDefined) &&
                realVariablesDict[name].Any(x => x.Start.HasValue && !x.Start.Value.IsDefined);
            if (inconsistentDefinition)
            {
                value = new DefinableValue<double>();
                return false;
            }
            if (realVariablesDict[name].All(x => x.Start.HasValue && !x.Start.Value.IsDefined))
            {
                value = new DefinableValue<double>();
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
                value = new DefinableValue<double>();
                return false;
            }
            if (!realVariablesDict[name].Any(x => x.ForbiddenValue.HasValue || x.ForbiddenValue.Value.IsDefined))
            {
                value = new DefinableValue<double> { Value = randomGenerator.NextDouble() * (maximalValue - minimalValue) + minimalValue };
                return true;
            }

            var forbiddenValues = GetForbiddenNumbers(realVariablesDict[name], minimalValue, maximalValue);
            var intervals = GenerateIntervals(minimalValue, maximalValue, forbiddenValues);

            if (intervals.Count == 0)
            {
                value = new DefinableValue<double>();
                return false;
            }
            else
            {
                var intervalNumber = randomGenerator.Next(0, intervals.Count);
                value = new DefinableValue<double> { Value = DoubleRandom(intervals[intervalNumber].start, intervals[intervalNumber].end) };
                return true;
            }
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

        public double DoubleRandom(double min, double max)
        {
            return randomGenerator.NextDouble() * (max - min) + min;
        }

        public bool TrySelectValue(string name, out DefinableValue<bool> value)
        {
            // If no value interval is assigned, return random value
            // TODO: Examine, if this case is possible
            if (!booleanVariablesDict.ContainsKey(name))
            {
                value = new DefinableValue<bool>();
                return true;
            }

            var inconsistentDefinition = booleanVariablesDict[name].Any(x => x.Start.HasValue && x.Start.Value.IsDefined) &&
                booleanVariablesDict[name].Any(x => x.Start.HasValue && !x.Start.Value.IsDefined);
            if (inconsistentDefinition)
            {
                value = new DefinableValue<bool>();
                return false;
            }
            if (booleanVariablesDict[name].All(x => x.Start.HasValue && !x.Start.Value.IsDefined))
            {
                value = new DefinableValue<bool>();
                return true;
            }

            // Bool value can only be equal or unequal - checking that only 1 value has been chosen 
            var boolValue = default(bool);
            if (booleanVariablesDict[name]
                .Any(x => x.Start.HasValue && x.Start.Value.IsDefined))
            {
                boolValue = booleanVariablesDict[name]
                .Where(x => x.Start.HasValue && x.Start.Value.IsDefined)
                .Select(x => x.Start.Value.Value)
                .FirstOrDefault();
            }
            else if (booleanVariablesDict[name]
                .Any(x => x.ForbiddenValue.HasValue && x.Start.Value.IsDefined))
            {
                boolValue = !booleanVariablesDict[name]
                .Where(x => x.ForbiddenValue.HasValue && x.Start.Value.IsDefined)
                .Select(x => x.ForbiddenValue.Value.Value)
                .FirstOrDefault();
            }
            else
            {
                value = new DefinableValue<bool>();
                return true;
            }
            if (IsIncorrect(name, boolValue))
            {
                value = new DefinableValue<bool>();
                return false;
            }

            value = new DefinableValue<bool> { Value = boolValue };
            return true;
        }

        public bool TrySelectValue(string name, out DefinableValue<string> value)
        {
            // If no value interval is assigned, return random value
            // TODO: Examine, if this case is possible
            if (!stringVariablesDict.ContainsKey(name))
            {
                value = new DefinableValue<string>();
                return true;
            }

            var inconsistentDefinition = stringVariablesDict[name].Any(x => x.Start.HasValue && x.Start.Value.IsDefined) &&
                stringVariablesDict[name].Any(x => x.Start.HasValue && !x.Start.Value.IsDefined);
            if (inconsistentDefinition)
            {
                value = new DefinableValue<string>();
                return false;
            }
            if (stringVariablesDict[name].All(x => x.Start.HasValue && !x.Start.Value.IsDefined))
            {
                value = new DefinableValue<string>();
                return true;
            }

            if (stringVariablesDict[name].All(x => x.ForbiddenValue.HasValue))
            {
                value = new DefinableValue<string> { Value = GetNotForbiddenString(name) };
                return true;
            }

            var firstSelectedStringValue = stringVariablesDict[name]
                .Where(x => x.Start.HasValue && x.Start.Value.IsDefined)
                .Select(x => x.Start.Value.Value)
                .FirstOrDefault();

            if (IsIncorrect(name, firstSelectedStringValue))
            {
                value = new DefinableValue<string>();
                return false;
            }

            value = new DefinableValue<string> { Value = firstSelectedStringValue };
            return true;
        }

        private string StringRandom()
        {
            return Guid.NewGuid().ToString();
        }

        private string GetNotForbiddenString(string name)
        {
            var forbiddenStrings = stringVariablesDict[name]
                .Where(x=>x.ForbiddenValue.HasValue && x.ForbiddenValue.Value.IsDefined)
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


        private bool IsIncorrect(string name, bool boolValue)
        {
            return booleanVariablesDict[name].Any(x => x.Start.HasValue && x.Start.Value.IsDefined && x.Start.Value.Value != boolValue)
                            || booleanVariablesDict[name].Any(x => x.End.HasValue && x.End.Value.IsDefined && x.End.Value.Value != boolValue)
                            || booleanVariablesDict[name].Any(x => x.ForbiddenValue.HasValue && x.ForbiddenValue.Value.IsDefined && x.ForbiddenValue.Value.Value == boolValue);
        }
    }
}
