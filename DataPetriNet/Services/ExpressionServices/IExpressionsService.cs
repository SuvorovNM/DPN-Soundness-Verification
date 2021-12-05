using DataPetriNet.Abstractions;
using DataPetriNet.Services.SourceServices;
using System.Collections.Generic;

namespace DataPetriNet.Services.ExpressionServices
{
    public interface IExpressionsService
    {
        bool EvaluateExpression(ISourceService globalVariables, IConstraintExpression expression);
        void AddValueInterval(IConstraintExpression expression);
        bool TryInferValue(string name, out IDefinableValue value);
        bool GenerateExpressionsBasedOnIntervals(string name, out List<IConstraintExpression> constraintExpressions);
        void Clear();

    }
}