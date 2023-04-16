using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using System.Security.Cryptography.X509Certificates;

namespace DataPetriNetOnSmt.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<IConstraintExpression> GetExpressionsOfType(this IEnumerable<IConstraintExpression> expressions, VariableType varType)
        {
            return expressions.Where(x => x.ConstraintVariable.VariableType == varType);
        }
        
        public static Dictionary<string, DomainType> GetTypedVarsDict(this IEnumerable<IConstraintExpression> expressions, VariableType varType)
        {
            return expressions
                .Where(x => x.ConstraintVariable.VariableType == varType)
                .Select(x => x.ConstraintVariable)
                .Union(expressions
                    .Where(y=>y is ConstraintVOVExpression cexpr && cexpr.VariableToCompare.VariableType == varType)
                    .Select(y=>((ConstraintVOVExpression)y).VariableToCompare))
                .Distinct()
                .ToDictionary(x => x.Name, y => y.Domain);
        }
    }
}
