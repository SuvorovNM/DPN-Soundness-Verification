using DataPetriNet.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.Services.SourceServices
{
    public class IntegerSourceService : ISourceService
    {
        private readonly Dictionary<string, DefinableValue<long>> integerVariablesDict;

        public IntegerSourceService()
        {
            integerVariablesDict = new Dictionary<string, DefinableValue<long>>();
        }

        public void Clear()
        {
            integerVariablesDict.Clear();
        }

        public IDefinableValue Read(string name)
        {
            if (integerVariablesDict.TryGetValue(name, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException("No such integer variable with name = " + name);
        }

        public void Write(string name, IDefinableValue value)
        {
            integerVariablesDict[name] = value as DefinableValue<long>;
        }

    }
}
