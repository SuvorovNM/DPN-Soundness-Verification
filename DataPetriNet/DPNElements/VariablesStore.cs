using DataPetriNet.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet
{
    public class VariablesStore // TODO: Попробовать переопределить через dictionary как в Guard
    {
        private readonly Dictionary<string, DefinableValue<long>> integerVariablesDict;
        private readonly Dictionary<string, DefinableValue<double>> realVariablesDict;
        private readonly Dictionary<string, DefinableValue<bool>> booleanVariablesDict;
        private readonly Dictionary<string, DefinableValue<string>> stringVariablesDict;
        public VariablesStore()
        {
            integerVariablesDict = new Dictionary<string, DefinableValue<long>>();
            realVariablesDict = new Dictionary<string, DefinableValue<double>>();
            booleanVariablesDict = new Dictionary<string, DefinableValue<bool>>();
            stringVariablesDict = new Dictionary<string, DefinableValue<string>>();
        }

        public DefinableValue<long> ReadInteger(string name)
        {
            if (integerVariablesDict.TryGetValue(name, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException("No such integer variable with name = " + name);
        }

        public void WriteInteger(string name, DefinableValue<long> value)
        {
            integerVariablesDict[name] = value;
        }

        public DefinableValue<double> ReadReal(string name)
        {
            if (realVariablesDict.TryGetValue(name, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException("No such real variable with name = " + name);
        }

        public void WriteReal(string name, DefinableValue<double> value)
        {
            realVariablesDict[name] = value;
        }

        public DefinableValue<bool> ReadBool(string name)
        {
            if (booleanVariablesDict.TryGetValue(name, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException("No such boolean variable with name = " + name);
        }

        public void WriteBool(string name, DefinableValue<bool> value)
        {
            booleanVariablesDict[name] = value;
        }

        public DefinableValue<string> ReadString(string name)
        {
            if (stringVariablesDict.TryGetValue(name, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException("No such string variable with name = " + name);
        }

        public void WriteString(string name, DefinableValue<string> value)
        {
            stringVariablesDict[name] = value;
        }

        public void Clear()
        {
            integerVariablesDict.Clear();
            booleanVariablesDict.Clear();
            realVariablesDict.Clear();
            stringVariablesDict.Clear();
        }
    }
}
