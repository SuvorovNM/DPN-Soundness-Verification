using DataPetriNet.Abstractions;

namespace DataPetriNet.Services.SourceServices
{
    public interface ISourceService
    {
        IDefinableValue Read(string name);
        void Write(string name, IDefinableValue value);
        void Clear();
    }
}
