using DataPetriNet.Abstractions;
using DataPetriNet.DPNElements;
using DataPetriNet.DPNElements.Internals;

namespace DataPetriNet.Services
{
    public interface IExpressionsService
    {
        bool ExecuteExpression(VariablesStore globalVariables, IConstraintExpression expression);
        bool SelectValue(string name, VariablesStore values);
        void Clear();
        
    }
}