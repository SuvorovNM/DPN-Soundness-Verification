using DataPetriNet.Abstractions;
using System.Collections.Generic;

namespace DataPetriNet.Services.SourceServices
{
    public class BoolSourceService : ISourceService
    {
        private readonly Dictionary<string, DefinableValue<bool>> booleanVariablesDict;
        public BoolSourceService()
        {
            booleanVariablesDict = new Dictionary<string, DefinableValue<bool>>();
        }

        public void Clear()
        {
            booleanVariablesDict.Clear();
        }

        public IDefinableValue Read(string name)
        {
            if (booleanVariablesDict.TryGetValue(name, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException("No such boolean variable with name = " + name);
        }

        public void Write(string name, IDefinableValue value)
        {
            booleanVariablesDict[name] = value as DefinableValue<bool>;
        }
    }
}
