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

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    public class BoolExprImplicationService
    {
        private const long integerMax = long.MaxValue;
        private const long integerMin = long.MinValue;
        private const double realMax = 99999999999999;
        private const double realMin = -99999999999999;

        public BoolExpr GetImplicationOfGreaterExpression(
            IEnumerable<BoolExpr> concatenatedExpressionGroup,
            bool includeEquality,
            Expr varToOverwrite,
            Expr secondVar)
        {
            if (concatenatedExpressionGroup == null)
            {
                throw new ArgumentNullException(nameof(concatenatedExpressionGroup));
            }
            if (varToOverwrite == null)
            {
                throw new ArgumentNullException(nameof(varToOverwrite));
            }
            if (secondVar == null)
            {
                throw new ArgumentNullException(nameof(secondVar));
            }

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
                    return includeEquality
                        ? ContextProvider.Context.MkGe((ArithExpr)secondVar, (ArithExpr)minVarValue)
                        : ContextProvider.Context.MkGt((ArithExpr)secondVar, (ArithExpr)minVarValue);
                }

                // If no value restrictions return true
                return ContextProvider.Context.MkTrue();
            }

            // If expressionGroup is unsatisfiable, false is returned
            return ContextProvider.Context.MkFalse();
        }

        public BoolExpr GetImplicationOfLessExpression(
            IEnumerable<BoolExpr> concatenatedExpressionGroup,
            bool includeEquality,
            Expr varToOverwrite,
            Expr secondVar)
        {
            if (concatenatedExpressionGroup == null)
            {
                throw new ArgumentNullException(nameof(concatenatedExpressionGroup));
            }
            if (varToOverwrite == null)
            {
                throw new ArgumentNullException(nameof(varToOverwrite));
            }
            if (secondVar == null)
            {
                throw new ArgumentNullException(nameof(secondVar));
            }

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
                    return includeEquality
                        ? ContextProvider.Context.MkLe((ArithExpr)secondVar, (ArithExpr)maxVarValue)
                        : ContextProvider.Context.MkLt((ArithExpr)secondVar, (ArithExpr)maxVarValue);
                }
                // If no value restrictions return true
                return ContextProvider.Context.MkTrue();
            }

            // If expressionGroup is unsatisfiable, false is returned
            return ContextProvider.Context.MkFalse();
        }

        public BoolExpr? GetImplicationOfInequalityExpression(
            IEnumerable<BoolExpr> concatenatedExpressionGroup,
            Expr varToOverwrite,
            Expr secondVar)
        {
            if (concatenatedExpressionGroup == null)
            {
                throw new ArgumentNullException(nameof(concatenatedExpressionGroup));
            }
            if (varToOverwrite == null)
            {
                throw new ArgumentNullException(nameof(varToOverwrite));
            }
            if (secondVar == null)
            {
                throw new ArgumentNullException(nameof(secondVar));
            }

            var solver = ContextProvider.Context.MkSimpleSolver();
            solver.Assert(concatenatedExpressionGroup.ToArray());

            if (solver.Check() == Status.SATISFIABLE)
            {
                var firstValue = solver.Model.Consts
                    .FirstOrDefault(x => x.Key.Name.ToString() == varToOverwrite.ToString())
                    .Value;
                if (firstValue == null)
                {
                    return null;
                }

                solver = ContextProvider.Context.MkSimpleSolver();
                solver.Assert(concatenatedExpressionGroup.ToArray());

                var expressionToAdd = ContextProvider.Context.MkNot(ContextProvider.Context.MkEq(varToOverwrite, firstValue));
                solver.Assert(expressionToAdd);

                if (solver.Check() == Status.UNSATISFIABLE)
                {
                    return ContextProvider.Context.MkNot(ContextProvider.Context.MkEq(secondVar, firstValue));
                }
            }

            return null;
        }

        public BoolExpr GetImplicationOfEqualityExpression(
            Expr replacementVar,
            bool addNegation,
            BoolExpr? expressionToInspect,
            Expr? oldValue)
        {
            if (expressionToInspect == null)
            {
                throw new ArgumentNullException(nameof(expressionToInspect));
            }
            if (oldValue == null)
            {
                throw new ArgumentNullException(nameof(oldValue));
            }
            if (replacementVar == null)
            {
                throw new ArgumentNullException(nameof(replacementVar));
            }

            BoolExpr newExpression = null;
            if (expressionToInspect.IsEq)
            {
                newExpression = ContextProvider.Context.MkEq(replacementVar, oldValue);
            }
            if (expressionToInspect.IsGT)
            {
                newExpression = expressionToInspect.Args[0] == oldValue
                    ? ContextProvider.Context.MkGt((ArithExpr)oldValue, (ArithExpr)replacementVar)
                    : ContextProvider.Context.MkGt((ArithExpr)replacementVar, (ArithExpr)oldValue);
            }
            if (expressionToInspect.IsGE)
            {
                newExpression = expressionToInspect.Args[0] == oldValue
                   ? ContextProvider.Context.MkGe((ArithExpr)oldValue, (ArithExpr)replacementVar)
                   : ContextProvider.Context.MkGe((ArithExpr)replacementVar, (ArithExpr)oldValue);
            }
            if (expressionToInspect.IsLE)
            {
                newExpression = expressionToInspect.Args[0] == oldValue
                   ? ContextProvider.Context.MkLe((ArithExpr)oldValue, (ArithExpr)replacementVar)
                   : ContextProvider.Context.MkLe((ArithExpr)replacementVar, (ArithExpr)oldValue);
            }
            if (expressionToInspect.IsLT)
            {
                newExpression = expressionToInspect.Args[0] == oldValue
                    ? ContextProvider.Context.MkLt((ArithExpr)oldValue, (ArithExpr)replacementVar)
                    : ContextProvider.Context.MkLt((ArithExpr)replacementVar, (ArithExpr)oldValue);
            }

            if (addNegation)
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
            ArithExpr minimalPossibleValue = varToOverwrite.IsReal
                ? ContextProvider.Context.MkReal(realMin.ToString(CultureInfo.InvariantCulture))
                : ContextProvider.Context.MkInt(integerMin.ToString());

            ArithExpr maximalPossibleValue = varToOverwrite.IsReal
                ? ContextProvider.Context.MkReal(realMax.ToString(CultureInfo.InvariantCulture))
                : ContextProvider.Context.MkInt(integerMax.ToString());

            optimizer.Assert(ContextProvider.Context.MkGe((ArithExpr)varToOverwrite, minimalPossibleValue));
            optimizer.Assert(ContextProvider.Context.MkLe((ArithExpr)varToOverwrite, maximalPossibleValue));

            return optimizer;
        }
    }
}
