using DPN.Models.Enums;
using DPN.Models.SourceServices;

namespace DPN.Models.DPNElements
{
    public class VariablesStore
    {
        private readonly Dictionary<DomainType, ISourceService> variableSources = new()
        {
	        [DomainType.Boolean] = new BoolSourceService(),
	        [DomainType.Integer] = new IntegerSourceService(),
	        [DomainType.Real] = new RealSourceService()
        };

        public ISourceService this[DomainType domain]
        {
            get
            {
                return variableSources[domain];
            }
        }

        public (DomainType domain, string name)[] GetAllVariables()
        {
            var variables = new List<(DomainType, string)> ();
            foreach (var domainType in variableSources.Keys)
            {
                variables.AddRange(variableSources[domainType].GetKeys().Select(x => (domainType, x)));
            }

            return variables.ToArray();
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
