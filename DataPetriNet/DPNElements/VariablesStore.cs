using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet
{
    public class VariablesStore
    {
        private readonly Dictionary<string, long> integerVariablesDict;
        private readonly Dictionary<string, double> realVariablesDict;
        private readonly Dictionary<string, bool> booleanVariablesDict;
        private readonly Dictionary<string, string> stringVariablesDict;
        public VariablesStore()
        {
            integerVariablesDict = new Dictionary<string, long>();
            realVariablesDict = new Dictionary<string, double>();
            booleanVariablesDict = new Dictionary<string, bool>();
            stringVariablesDict = new Dictionary<string, string>();
        }

        public long ReadInteger(string name)
        {
            if (integerVariablesDict.TryGetValue(name, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException("No such integer variable with name = " + name);
        }

        public void WriteInteger(string name, long value)
        {
            integerVariablesDict[name] = value;
        }

        public double ReadReal(string name)
        {
            if (realVariablesDict.TryGetValue(name, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException("No such real variable with name = " + name);
        }

        public void WriteReal(string name, double value)
        {
            realVariablesDict[name] = value;
        }

        public bool ReadBool(string name)
        {
            if (booleanVariablesDict.TryGetValue(name, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException("No such boolean variable with name = " + name);
        }

        public void WriteBool(string name, bool value)
        {
            booleanVariablesDict[name] = value;
        }

        public string ReadString(string name)
        {
            if (stringVariablesDict.TryGetValue(name, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException("No such string variable with name = " + name);
        }

        public void WriteString(string name, string value)
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
