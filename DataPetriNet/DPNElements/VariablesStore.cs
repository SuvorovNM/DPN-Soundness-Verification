using DataPetriNet.Abstractions;
using DataPetriNet.DPNElements;
using DataPetriNet.Enums;
using DataPetriNet.Services.SourceServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet
{
    public class VariablesStore
    {
        private Dictionary<DomainType, ISourceService> variableSources;
        public VariablesStore()
        {
            variableSources = new Dictionary<DomainType, ISourceService>();
            variableSources[DomainType.Boolean] = new BoolSourceService();
            variableSources[DomainType.Integer] = new IntegerSourceService();
            variableSources[DomainType.Real] = new RealSourceService();
            variableSources[DomainType.String] = new StringSourceService();
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
            foreach(var variableService in variableSources.Values)
            {
                variableService.Clear();
            }
        }
    }
}
