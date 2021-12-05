using DataPetriNet.Abstractions;
using System.Collections.Generic;

namespace DataPetriNet.Services.SourceServices
{
    public interface ISourceService
    {
        IDefinableValue Read(string name);
        void Write(string name, IDefinableValue value);
        IEnumerable<string> GetKeys();
        void Clear();
    }
}
