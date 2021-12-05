using DataPetriNet.Abstractions;
using System.Collections.Generic;

namespace DataPetriNet.Services.SourceServices
{
    class StringSourceService : ISourceService
    {
        private readonly Dictionary<string, DefinableValue<string>> stringVariablesDict;

        public StringSourceService()
        {
            stringVariablesDict = new Dictionary<string, DefinableValue<string>>();
        }

        public void Clear()
        {
            stringVariablesDict.Clear();
        }

        public IEnumerable<string> GetKeys()
        {
            return stringVariablesDict.Keys;
        }

        public IDefinableValue Read(string name)
        {
            if (stringVariablesDict.TryGetValue(name, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException("No such string variable with name = " + name);
        }

        public void Write(string name, IDefinableValue value)
        {
            stringVariablesDict[name] = value as DefinableValue<string>;
        }
    }
}
