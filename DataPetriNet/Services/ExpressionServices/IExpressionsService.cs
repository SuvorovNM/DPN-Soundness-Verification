using DataPetriNet.Abstractions;
using DataPetriNet.DPNElements;
using DataPetriNet.DPNElements.Internals;
using DataPetriNet.Services.SourceServices;

namespace DataPetriNet.Services.ExpressionServices
{
    public interface IExpressionsService
    {
        bool ExecuteExpression(ISourceService globalVariables, IConstraintExpression expression);
        bool SelectValue(string name, ISourceService values);
        void Clear();

    }
}