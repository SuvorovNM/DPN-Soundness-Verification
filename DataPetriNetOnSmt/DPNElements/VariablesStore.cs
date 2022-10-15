using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SourceServices;

namespace DataPetriNetOnSmt.DPNElements
{
    public class VariablesStore
    {
        private readonly Dictionary<DomainType, ISourceService> variableSources;
        public VariablesStore()
        {
            variableSources = new Dictionary<DomainType, ISourceService>
            {
                [DomainType.Boolean] = new BoolSourceService(),
                [DomainType.Integer] = new IntegerSourceService(),
                [DomainType.Real] = new RealSourceService(),
                [DomainType.String] = new StringSourceService()
            };
        }

        public ISourceService this[DomainType domain]
        {
            get
            {
                return variableSources[domain];
            }
        }

        public List<string> GetAllVariables()
        {
            return variableSources[DomainType.Boolean].GetKeys()
                .Union(variableSources[DomainType.Integer].GetKeys())
                .Union(variableSources[DomainType.Real].GetKeys())
                .Union(variableSources[DomainType.String].GetKeys())
                .ToList();
        }

        public void Clear()
        {
            foreach (var variableService in variableSources.Values)
            {
                variableService.Clear();
            }
        }
    }
}
