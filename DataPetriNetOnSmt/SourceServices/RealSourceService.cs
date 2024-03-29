﻿using DataPetriNetOnSmt.Abstractions;

namespace DataPetriNetOnSmt.SourceServices
{
    public class RealSourceService : ISourceService
    {
        private readonly Dictionary<string, DefinableValue<double>> realVariablesDict;

        public RealSourceService()
        {
            realVariablesDict = new Dictionary<string, DefinableValue<double>>();
        }

        public void Clear()
        {
            realVariablesDict.Clear();
        }

        public IEnumerable<string> GetKeys()
        {
            return realVariablesDict.Keys;
        }

        public IDefinableValue Read(string name)
        {
            if (realVariablesDict.TryGetValue(name, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException("No such real variable with name = " + name);
        }


        public void Write(string name, IDefinableValue value)
        {
            realVariablesDict[name] = value as DefinableValue<double>;
        }
    }
}
