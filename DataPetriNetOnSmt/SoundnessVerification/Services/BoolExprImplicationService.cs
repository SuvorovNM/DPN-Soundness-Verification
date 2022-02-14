using Microsoft.Z3;
using System.Globalization;

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

            if (varToOverwrite.IsReal || secondVar.IsReal)
            {
                var relaxedOptimizer = SetOptimizer(FormRelaxedExpressionList(concatenatedExpressionGroup), varToOverwrite);
                relaxedOptimizer.MkMinimize(varToOverwrite);

                var hardOptimizer = SetOptimizer(concatenatedExpressionGroup, varToOverwrite);
                hardOptimizer.MkMinimize(varToOverwrite);

                if (relaxedOptimizer.Check() == Status.SATISFIABLE && hardOptimizer.Check() == Status.SATISFIABLE)
                {
                    var relaxedMinVarValue = relaxedOptimizer.Model.Consts
                        .FirstOrDefault(x => x.Key.Name.ToString() == varToOverwrite.ToString())
                        .Value;
                    var hardMinVarValue = hardOptimizer.Model.Consts
                        .FirstOrDefault(x => x.Key.Name.ToString() == varToOverwrite.ToString())
                        .Value;

                    var canBeEquality = relaxedMinVarValue.ToString() == hardMinVarValue.ToString();

                    if (relaxedMinVarValue.IsRatNum && relaxedMinVarValue.ToString() != realMin.ToString()
                    || relaxedMinVarValue.IsIntNum && relaxedMinVarValue.ToString() != integerMin.ToString())
                    {
                        return includeEquality && canBeEquality
                            ? ContextProvider.Context.MkGe((ArithExpr)secondVar, (ArithExpr)relaxedMinVarValue)
                            : ContextProvider.Context.MkGt((ArithExpr)secondVar, (ArithExpr)relaxedMinVarValue);
                    }
                    return ContextProvider.Context.MkTrue();
                }
            }
            else
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
                        return includeEquality
                            ? ContextProvider.Context.MkGe((ArithExpr)secondVar, (ArithExpr)minVarValue)
                            : ContextProvider.Context.MkGt((ArithExpr)secondVar, (ArithExpr)minVarValue);
                    }

                    // If no value restrictions return true
                    return ContextProvider.Context.MkTrue();
                }
            }            

            // If expressionGroup is unsatisfiable, false is returned
            return ContextProvider.Context.MkFalse();
        }

        private static List<BoolExpr> FormRelaxedExpressionList(IEnumerable<BoolExpr> concatenatedExpressionGroup)
        {
            List<BoolExpr> expressionsToExamine = new List<BoolExpr>();
            foreach (var expression in concatenatedExpressionGroup)
            {
                if (expression.IsEq || expression.IsGE || expression.IsLE)
                {
                    expressionsToExamine.Add(expression);
                }
                if (expression.IsGT)
                {
                    var relaxedExpression = ContextProvider.Context.MkGe((ArithExpr)expression.Args[0], (ArithExpr)expression.Args[1]);
                    expressionsToExamine.Add(relaxedExpression);
                }
                if (expression.IsLT)
                {
                    var relaxedExpression = ContextProvider.Context.MkLe((ArithExpr)expression.Args[0], (ArithExpr)expression.Args[1]);
                    expressionsToExamine.Add(relaxedExpression);
                }
            }

            return expressionsToExamine;
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

            if (varToOverwrite.IsReal || secondVar.IsReal)
            {
                var relaxedOptimizer = SetOptimizer(FormRelaxedExpressionList(concatenatedExpressionGroup), varToOverwrite);
                relaxedOptimizer.MkMaximize(varToOverwrite);

                var hardOptimizer = SetOptimizer(concatenatedExpressionGroup, varToOverwrite);
                hardOptimizer.MkMaximize(varToOverwrite);

                if (relaxedOptimizer.Check() == Status.SATISFIABLE && hardOptimizer.Check() == Status.SATISFIABLE)
                {
                    var relaxedMaxVarValue = relaxedOptimizer.Model.Consts
                        .FirstOrDefault(x => x.Key.Name.ToString() == varToOverwrite.ToString())
                        .Value;
                    var hardMaxVarValue = hardOptimizer.Model.Consts
                        .FirstOrDefault(x => x.Key.Name.ToString() == varToOverwrite.ToString())
                        .Value;

                    var canBeEquality = relaxedMaxVarValue.ToString() == hardMaxVarValue.ToString();

                    if (relaxedMaxVarValue.IsRatNum && relaxedMaxVarValue.ToString() != realMax.ToString())
                    {
                        return includeEquality && canBeEquality
                            ? ContextProvider.Context.MkLe((ArithExpr)secondVar, (ArithExpr)relaxedMaxVarValue)
                            : ContextProvider.Context.MkLt((ArithExpr)secondVar, (ArithExpr)relaxedMaxVarValue);
                    }
                    return ContextProvider.Context.MkTrue();
                }
            }
            else
            {
                var optimizer = SetOptimizer(concatenatedExpressionGroup, varToOverwrite);
                optimizer.MkMaximize(varToOverwrite);

                if (optimizer.Check() == Status.SATISFIABLE)
                {
                    var maxVarValue = optimizer.Model.Consts
                        .FirstOrDefault(x => x.Key.Name.ToString() == varToOverwrite.ToString())
                        .Value;

                    if (maxVarValue.IsIntNum && maxVarValue.ToString() != integerMax.ToString())
                    {
                        return includeEquality
                            ? ContextProvider.Context.MkLe((ArithExpr)secondVar, (ArithExpr)maxVarValue)
                            : ContextProvider.Context.MkLt((ArithExpr)secondVar, (ArithExpr)maxVarValue);
                    }

                    // If no value restrictions return true
                    return ContextProvider.Context.MkTrue();
                }
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
