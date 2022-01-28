using DataPetriNetOnSmt.Abstractions;

namespace DataPetriNetOnSmt.SourceServices
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

        public IEnumerable<string> GetKeys()
        {
            return integerVariablesDict.Keys;
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
