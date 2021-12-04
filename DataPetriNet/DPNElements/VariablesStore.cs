using DataPetriNet.Enums;
using DataPetriNet.Services.SourceServices;
using System.Collections.Generic;

namespace DataPetriNet
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

        public void Clear()
        {
            foreach (var variableService in variableSources.Values)
            {
                variableService.Clear();
            }
        }
    }
}
