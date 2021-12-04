using DataPetriNet.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.Services.SourceServices
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
