using DataPetriNetOnSmt.Enums;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.Extensions
{
    public static class ContextExtensions
    {
        public static Expr GenerateExpression(this Context context, string variableName, DomainType domain, VariableType varType)
        {
            var nameSuffix = varType == VariableType.Written
                ? "_w"
                : "_r";

            return domain switch
            {
                DomainType.Integer => context.MkIntConst(variableName + nameSuffix),
                DomainType.Real => context.MkRealConst(variableName + nameSuffix),
                DomainType.Boolean => context.MkBoolConst(variableName + nameSuffix),
                _ => throw new NotImplementedException("Domain type is not supported yet"),
            };
        }

    }
}
