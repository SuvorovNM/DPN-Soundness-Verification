﻿using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using Microsoft.Z3;
using System.Globalization;

namespace DataPetriNetOnSmt.DPNElements
{
    public class Guard : ICloneable
    {
        private bool isRepaired = false;
        private bool readNeedsToBeRecalculated = false;
        private Dictionary<string, DomainType> readVars = new Dictionary<string, DomainType>();
        public Context Context { get; set; }

        public List<IConstraintExpression> BaseConstraintExpressions { get; init; }
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
                BaseConstraintExpressions = new List<IConstraintExpression>();
                ActualConstraintExpression = ctx.MkTrue();
                ConstraintExpressionBeforeUpdate = ctx.MkTrue();
                WriteVars = new Dictionary<string, DomainType>();
            }
            else
            {
                BaseConstraintExpressions = baseConstraints;
                var smtExpression = ctx.GetSmtExpression(baseConstraints);
                ActualConstraintExpression = smtExpression;
                ConstraintExpressionBeforeUpdate = smtExpression;
                WriteVars = BaseConstraintExpressions.GetTypedVarsDict(VariableType.Written);
                ReadVars = BaseConstraintExpressions.GetTypedVarsDict(VariableType.Read);
            }

            Context = ctx;            
        }

        public static Guard MakeRefined(Guard baseGuard, BoolExpr updatedConstraintExpression)
        {
            return new Guard
            {
                Context = baseGuard.Context,
                BaseConstraintExpressions = baseGuard.BaseConstraintExpressions,
                ActualConstraintExpression = updatedConstraintExpression,
                ConstraintExpressionBeforeUpdate = baseGuard.ActualConstraintExpression,

                WriteVars = baseGuard.BaseConstraintExpressions.GetTypedVarsDict(VariableType.Written),
                readNeedsToBeRecalculated = true,
                isRepaired = false
            };
        }
        public static Guard MakeRepaired(Guard baseGuard, BoolExpr updatedConstraintExpression)
        {
            return new Guard
            {
                Context = baseGuard.Context,
                BaseConstraintExpressions = baseGuard.BaseConstraintExpressions,
                ActualConstraintExpression = updatedConstraintExpression,
                ConstraintExpressionBeforeUpdate = baseGuard.isRepaired 
                    ? baseGuard.ConstraintExpressionBeforeUpdate
                    : baseGuard.ActualConstraintExpression,

                WriteVars = baseGuard.BaseConstraintExpressions.GetTypedVarsDict(VariableType.Written),
                readNeedsToBeRecalculated = true,
                isRepaired = true
            };
        }
        public static Guard MakeSimplified(Guard baseGuard, BoolExpr updatedConstraintExpression)
        {
            return new Guard
            {
                Context = baseGuard.Context,
                BaseConstraintExpressions = baseGuard.BaseConstraintExpressions,
                ActualConstraintExpression = updatedConstraintExpression,
                ConstraintExpressionBeforeUpdate = baseGuard.isRepaired
                    ? baseGuard.ConstraintExpressionBeforeUpdate
                    : updatedConstraintExpression,

                WriteVars = baseGuard.BaseConstraintExpressions.GetTypedVarsDict(VariableType.Written),
                readNeedsToBeRecalculated = true,
                isRepaired = baseGuard.isRepaired
            };
        }

        public static Guard MakeMerged(Guard baseGuard, BoolExpr mergedConstraintExpression)
        {
            return new Guard
            {
                Context = baseGuard.Context,
                BaseConstraintExpressions = baseGuard.BaseConstraintExpressions,
                ActualConstraintExpression = mergedConstraintExpression,
                ConstraintExpressionBeforeUpdate = baseGuard.isRepaired
                    ? baseGuard.ConstraintExpressionBeforeUpdate
                    : baseGuard.ActualConstraintExpression,

                WriteVars = baseGuard.BaseConstraintExpressions.GetTypedVarsDict(VariableType.Written),
                readNeedsToBeRecalculated = true,
                isRepaired = baseGuard.isRepaired
            };
        }

       private Guard(Guard baseGuard, BoolExpr updatedConstraintExpression)
        {
            Context = baseGuard.Context;
            BaseConstraintExpressions = baseGuard.BaseConstraintExpressions;
            ActualConstraintExpression = updatedConstraintExpression;
            ConstraintExpressionBeforeUpdate = baseGuard.ActualConstraintExpression;

            WriteVars = BaseConstraintExpressions.GetTypedVarsDict(VariableType.Written);
            readNeedsToBeRecalculated = true;
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
            var clonedGuard = new Guard(
                this,
                ActualConstraintExpression);
            return clonedGuard;
        }
    }
}
