using DPN.Models.Extensions;
using Microsoft.Z3;
using DPN.Models.Abstractions;
using DPN.Models.Enums;

namespace DPN.Models.DPNElements
{
    public class Guard : ICloneable
    {
        private bool isRepaired = false;
        private bool readNeedsToBeRecalculated = false;
        private Dictionary<string, DomainType> readVars = new Dictionary<string, DomainType>();
        public Context Context { get; set; }

        public BoolExpr BaseConstraintExpressions { get; init; }
        public BoolExpr ConstraintExpressionBeforeUpdate { get; init; }
        public BoolExpr ActualConstraintExpression { get; private set; }

        public Dictionary<string, DomainType> WriteVars { get; init; }
        public Dictionary<string, DomainType> ReadVars 
        { 
            get 
            { 
                if (readNeedsToBeRecalculated)
                {
                    readVars = ActualConstraintExpression.GetTypedVarsDict(VariableType.Read);
                    readNeedsToBeRecalculated = false;
                }
                return readVars;
            }
            set
            {
                readVars = value;
            }
        }

        private Guard()
        {

        }

        public Guard(Context ctx, List<IConstraintExpression>? baseConstraints = null)
        {
            if (baseConstraints == null)
            {
                var trueExpression = ctx.MkTrue();
                BaseConstraintExpressions = trueExpression;
                ActualConstraintExpression = trueExpression;
                ConstraintExpressionBeforeUpdate = trueExpression;
                WriteVars = new Dictionary<string, DomainType>();
            }
            else
            {
                var smtExpression = ctx.GetSmtExpression(baseConstraints);
                BaseConstraintExpressions = smtExpression;
                ActualConstraintExpression = smtExpression;
                ConstraintExpressionBeforeUpdate = smtExpression;
                WriteVars = BaseConstraintExpressions.GetTypedVarsDict(VariableType.Written);
                ReadVars = BaseConstraintExpressions.GetTypedVarsDict(VariableType.Read);
            }

            Context = ctx;            
        }
        
        public Guard(Context ctx, BoolExpr? smtExpression)
        {
            if (smtExpression == null)
            {
                var trueExpression = ctx.MkTrue();
                
                BaseConstraintExpressions = trueExpression;
                ActualConstraintExpression = trueExpression;
                ConstraintExpressionBeforeUpdate = trueExpression;
                WriteVars = new Dictionary<string, DomainType>();
            }
            else
            {
                BaseConstraintExpressions = smtExpression;
                ActualConstraintExpression = smtExpression;
                ConstraintExpressionBeforeUpdate = smtExpression;
                WriteVars = smtExpression.GetTypedVarsDict(VariableType.Written);
                ReadVars = smtExpression.GetTypedVarsDict(VariableType.Read);
            }

            Context = ctx;            
        }

        public static Guard MakeRefined(Guard baseGuard, BoolExpr updatedConstraintExpression, VariablesStore variables)
        {
            return new Guard
            {
                Context = baseGuard.Context,
                BaseConstraintExpressions = baseGuard.BaseConstraintExpressions,
                ActualConstraintExpression = updatedConstraintExpression,
                ConstraintExpressionBeforeUpdate = baseGuard.ActualConstraintExpression,

                WriteVars = updatedConstraintExpression.GetTypedVarsDict(VariableType.Written, variables),
                ReadVars = updatedConstraintExpression.GetTypedVarsDict(VariableType.Read, variables),
                //readNeedsToBeRecalculated = true,
                isRepaired = false
            };
        }
        public static Guard MakeRepaired(Guard baseGuard, BoolExpr updatedConstraintExpression, VariablesStore variables)
        {
            return new Guard
            {
                Context = baseGuard.Context,
                BaseConstraintExpressions = baseGuard.BaseConstraintExpressions,
                ActualConstraintExpression = updatedConstraintExpression,
                ConstraintExpressionBeforeUpdate = baseGuard.isRepaired 
                    ? baseGuard.ConstraintExpressionBeforeUpdate
                    : baseGuard.ActualConstraintExpression,

                WriteVars = updatedConstraintExpression.GetTypedVarsDict(VariableType.Written, variables),
                ReadVars = updatedConstraintExpression.GetTypedVarsDict(VariableType.Read, variables),
                readNeedsToBeRecalculated = false,
                isRepaired = true
            };
        }

        public static Guard MakeMerged(Guard baseGuard, BoolExpr mergedConstraintExpression, VariablesStore variables)
        {
            return new Guard
            {
                Context = baseGuard.Context,
                BaseConstraintExpressions = baseGuard.BaseConstraintExpressions,
                ActualConstraintExpression = mergedConstraintExpression,
                ConstraintExpressionBeforeUpdate = baseGuard.isRepaired
                    ? baseGuard.ConstraintExpressionBeforeUpdate
                    : baseGuard.ActualConstraintExpression,

                WriteVars = mergedConstraintExpression.GetTypedVarsDict(VariableType.Written, variables),//baseGuard.ActualConstraintExpression
                ReadVars = mergedConstraintExpression.GetTypedVarsDict(VariableType.Read, variables),
                readNeedsToBeRecalculated = false,
                isRepaired = baseGuard.isRepaired
            };
        }

       private Guard(Guard baseGuard)
        {
            Context = baseGuard.Context;
            BaseConstraintExpressions = baseGuard.BaseConstraintExpressions;
            ActualConstraintExpression = baseGuard.ActualConstraintExpression;
            ConstraintExpressionBeforeUpdate = baseGuard.ActualConstraintExpression;

            WriteVars = baseGuard.WriteVars;
            ReadVars = baseGuard.ReadVars;
            readNeedsToBeRecalculated = false;
            isRepaired = baseGuard.isRepaired;
        }

        public void UndoRepairment()
        {
            if (isRepaired)
            {
                ActualConstraintExpression = ConstraintExpressionBeforeUpdate;
            }
            else
            {
                throw new InvalidOperationException("The transition is not repaired!");
            }
        }

        /*public Guard(Context ctx, List<IConstraintExpression> baseConstraints, BoolExpr actualConstraintExpression)
        {
            BaseConstraintExpressions = baseConstraints;
            ActualConstraintExpression = actualConstraintExpression;
            Context = ctx;

            WriteVars = BaseConstraintExpressions.GetTypedVarsDict(VariableType.Written);
            //ReadVars = ActualConstraintExpression.GetTypedVarsDict(VariableType.Read);

            readNeedsToBeRecalculated = true;
        }*/

        public object Clone()
        {
            var clonedGuard = new Guard(this);
            return clonedGuard;
        }
    }
}
