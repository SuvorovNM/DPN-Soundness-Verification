using DataPetriNetOnSmt.Abstractions;

namespace DataPetriNetOnSmt.SourceServices
{
    public interface ISourceService
    {
        IDefinableValue Read(string name);
        void Write(string name, IDefinableValue value);
        IEnumerable<string> GetKeys();
        void Clear();
    }
}
