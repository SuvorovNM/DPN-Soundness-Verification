using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification
{
    public class BoolExprImplicationService
    {
        private const long integerMax = long.MaxValue;
        private const long integerMin = long.MinValue;
        private const double realMax = 99999999999999;
        private const double realMin = -99999999999999;

        public BoolExpr GetImplicationOfGreaterExpression(IEnumerable<BoolExpr> concatenatedExpressionGroup, BinaryPredicate predicate, Expr varToOverwrite, Expr secondVar)
        {
            var optimizer = SetOptimizer(concatenatedExpressionGroup, varToOverwrite);

            optimizer.MkMinimize(varToOverwrite);
            if (optimizer.Check() == Status.SATISFIABLE)
            {
                var minVarValue = optimizer.Model.Consts
                    .FirstOrDefault(x => x.Key.Name.ToString() == varToOverwrite.ToString())
                    .Value;

                if (minVarValue.IsRatNum && minVarValue.ToString() != realMin.ToString()
                    || minVarValue.IsIntNum && minVarValue.ToString() != integerMin.ToString())
                {
                    return predicate == BinaryPredicate.GreaterThan
                        ? ContextProvider.Context.MkGt((ArithExpr)secondVar, (ArithExpr)minVarValue)
                        : ContextProvider.Context.MkGe((ArithExpr)secondVar, (ArithExpr)minVarValue);
                }
            }

            return ContextProvider.Context.MkFalse();
        }

        public BoolExpr GetImplicationOfLessExpression(IEnumerable<BoolExpr> concatenatedExpressionGroup, BinaryPredicate predicate, Expr varToOverwrite, Expr secondVar)
        {
            var optimizer = SetOptimizer(concatenatedExpressionGroup, varToOverwrite);

            optimizer.MkMaximize(varToOverwrite);
            if (optimizer.Check() == Status.SATISFIABLE)
            {
                var maxVarValue = optimizer.Model.Consts
                    .FirstOrDefault(x => x.Key.Name.ToString() == varToOverwrite.ToString())
                    .Value;

                if (maxVarValue.IsRatNum && maxVarValue.ToString() != realMax.ToString()
                    || maxVarValue.IsIntNum && maxVarValue.ToString() != integerMax.ToString())
                {
                    return predicate == BinaryPredicate.LessThan
                        ? ContextProvider.Context.MkLt((ArithExpr)secondVar, (ArithExpr)maxVarValue)
                        : ContextProvider.Context.MkLe((ArithExpr)secondVar, (ArithExpr)maxVarValue);
                }
            }

            return ContextProvider.Context.MkFalse();
        }

        public BoolExpr GetImplicationOfEqualityExpression(
            Expr replacementVar,
            BoolExpr readExpression,
            BoolExpr? expressionToInspect,
            Expr? oldValue,
            int operandToSave)
        {
            BoolExpr newExpression = null;
            if (expressionToInspect.IsEq)
            {
                newExpression = ContextProvider.Context.MkEq(replacementVar, oldValue);
            }
            if (expressionToInspect.IsGT)
            {
                newExpression = operandToSave == 0
                    ? ContextProvider.Context.MkGt((ArithExpr)oldValue, (ArithExpr)replacementVar)
                    : ContextProvider.Context.MkGt((ArithExpr)replacementVar, (ArithExpr)oldValue);
            }
            if (expressionToInspect.IsGE)
            {
                newExpression = operandToSave == 0
                   ? ContextProvider.Context.MkGe((ArithExpr)oldValue, (ArithExpr)replacementVar)
                   : ContextProvider.Context.MkGe((ArithExpr)replacementVar, (ArithExpr)oldValue);
            }
            if (expressionToInspect.IsLE)
            {
                newExpression = operandToSave == 0
                   ? ContextProvider.Context.MkLe((ArithExpr)oldValue, (ArithExpr)replacementVar)
                   : ContextProvider.Context.MkLe((ArithExpr)replacementVar, (ArithExpr)oldValue);
            }
            if (expressionToInspect.IsLT)
            {
                newExpression = operandToSave == 0
                    ? ContextProvider.Context.MkLt((ArithExpr)oldValue, (ArithExpr)replacementVar)
                    : ContextProvider.Context.MkLt((ArithExpr)replacementVar, (ArithExpr)oldValue);
            }

            if (readExpression.IsNot)
            {
                newExpression = ContextProvider.Context.MkNot(newExpression);
            }

            return newExpression;
        }

        private Optimize SetOptimizer(IEnumerable<BoolExpr> concatenatedExpressionGroup, Expr? varToOverwrite)
        {
            var optimizer = ContextProvider.Context.MkOptimize();
            foreach (var expression in concatenatedExpressionGroup)
            {
                optimizer.Assert(expression);
            }
            ArithExpr minimalPossibleValue = varToOverwrite.IsRatNum
                ? ContextProvider.Context.MkReal(realMin.ToString(CultureInfo.InvariantCulture))
                : ContextProvider.Context.MkInt(integerMin.ToString());

            ArithExpr maximalPossibleValue = varToOverwrite.IsRatNum
                ? ContextProvider.Context.MkReal(realMax.ToString(CultureInfo.InvariantCulture))
                : ContextProvider.Context.MkInt(integerMax.ToString());

            optimizer.Assert(ContextProvider.Context.MkGe((ArithExpr)varToOverwrite, minimalPossibleValue));
            optimizer.Assert(ContextProvider.Context.MkLe((ArithExpr)varToOverwrite, maximalPossibleValue));

            return optimizer;
        }
    }
}
