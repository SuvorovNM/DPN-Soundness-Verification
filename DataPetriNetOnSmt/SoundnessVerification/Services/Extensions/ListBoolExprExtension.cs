using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.Services.Extensions
{
    public static class ListBoolExprExtension
    {
        public static void AddUniqueExpressions(this List<BoolExpr> source, IEnumerable<BoolExpr> expressionsToAdd)
        {
            var stringExpressions = source
                .Select(x => x.ToString())
                .ToArray();

            // Simplified version to avoid satisfiability checks
            foreach(var expr in expressionsToAdd)
            {
                if (!stringExpressions.Contains(expr.ToString()))
                {
                    source.Add(expr);
                }
            }
        }
    }
}
