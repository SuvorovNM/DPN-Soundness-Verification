using DPN.Models.Extensions;
using Microsoft.Z3;
using DPN.Models.Enums;

namespace DPN.Models.DPNElements
{
    public class Guard : ICloneable
    {
        public Context Context { get; }
        public BoolExpr ActualConstraintExpression { get; }

        public Dictionary<string, DomainType> WriteVars { get; }
        public Dictionary<string, DomainType> ReadVars { get; }
        
        
        public Guard(Context ctx, BoolExpr? smtExpression, VariablesStore? variables = null)
        {
            if (smtExpression == null)
            {
                var trueExpression = ctx.MkTrue();
                
                ActualConstraintExpression = trueExpression;
                WriteVars = new Dictionary<string, DomainType>();
                ReadVars = new  Dictionary<string, DomainType>();
            }
            else
            {
                ActualConstraintExpression = smtExpression;
                WriteVars = smtExpression.GetTypedVarsDict(VariableType.Written, variables);
                ReadVars = smtExpression.GetTypedVarsDict(VariableType.Read, variables);
            }

            Context = ctx;            
        }

       private Guard(Guard baseGuard)
        {
            Context = baseGuard.Context;
            ActualConstraintExpression = baseGuard.ActualConstraintExpression;

            WriteVars = baseGuard.WriteVars;
            ReadVars = baseGuard.ReadVars;
        }

        public object Clone()
        {
            var clonedGuard = new Guard(this);
            return clonedGuard;
        }
    }
}
