using DataPetriNet.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.Services.ExpressionServices
{
    public class ExpressionsServiceStore
    {
        private readonly Dictionary<DomainType, IExpressionsService> expressionServices;

        public ExpressionsServiceStore()
        {
            expressionServices = new Dictionary<DomainType, IExpressionsService>
            {
                [DomainType.Boolean] = new BoolExpressionsService(),
                [DomainType.Integer] = new IntegerExpressionsService(),
                [DomainType.Real] = new RealExpressionsService(),
                [DomainType.String] = new StringExpressionsService()
            };
        }
        public IExpressionsService this[DomainType domain]
        {
            get
            {
                return expressionServices[domain];
            }
        }

        public void Clear()
        {
            foreach (var variableService in expressionServices.Values)
            {
                variableService.Clear();
            }
        }
    }
}
