using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.Extensions
{
    public static class BoolExprExtensions
    {
        public static BoolExpr? GetExpressionWithoutNotClause(this BoolExpr sourceExpression)
        {
            if (sourceExpression == null)
            {
                throw new ArgumentNullException(nameof(sourceExpression));
            }

            return sourceExpression.IsNot
                    ? sourceExpression.Args[0] as BoolExpr
                    : sourceExpression;
        }
    }
}
