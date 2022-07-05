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

        public Context Context { get; private set; }

        public BoolExprImplicationService(Context context)
        {
            Context = context;
        }

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
                            ? Context.MkGe((ArithExpr)secondVar, (ArithExpr)relaxedMinVarValue)
                            : Context.MkGt((ArithExpr)secondVar, (ArithExpr)relaxedMinVarValue);
                    }
                    return Context.MkTrue();
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
                            ? Context.MkGe((ArithExpr)secondVar, (ArithExpr)minVarValue)
                            : Context.MkGt((ArithExpr)secondVar, (ArithExpr)minVarValue);
                    }

                    // If no value restrictions return true
                    return Context.MkTrue();
                }
            }            

            // If expressionGroup is unsatisfiable, false is returned
            return Context.MkFalse();
        }

        private List<BoolExpr> FormRelaxedExpressionList(IEnumerable<BoolExpr> concatenatedExpressionGroup)
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
                    var relaxedExpression = Context.MkGe((ArithExpr)expression.Args[0], (ArithExpr)expression.Args[1]);
                    expressionsToExamine.Add(relaxedExpression);
                }
                if (expression.IsLT)
                {
                    var relaxedExpression = Context.MkLe((ArithExpr)expression.Args[0], (ArithExpr)expression.Args[1]);
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
                            ? Context.MkLe((ArithExpr)secondVar, (ArithExpr)relaxedMaxVarValue)
                            : Context.MkLt((ArithExpr)secondVar, (ArithExpr)relaxedMaxVarValue);
                    }
                    return Context.MkTrue();
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
                            ? Context.MkLe((ArithExpr)secondVar, (ArithExpr)maxVarValue)
                            : Context.MkLt((ArithExpr)secondVar, (ArithExpr)maxVarValue);
                    }

                    // If no value restrictions return true
                    return Context.MkTrue();
                }
            }

            // If expressionGroup is unsatisfiable, false is returned
            return Context.MkFalse();
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

            var solver = Context.MkSimpleSolver();
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

                solver = Context.MkSimpleSolver();
                solver.Assert(concatenatedExpressionGroup.ToArray());

                var expressionToAdd = Context.MkNot(Context.MkEq(varToOverwrite, firstValue));
                solver.Assert(expressionToAdd);

                if (solver.Check() == Status.UNSATISFIABLE)
                {
                    return Context.MkNot(Context.MkEq(secondVar, firstValue));
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
                newExpression = Context.MkEq(replacementVar, oldValue);
            }
            if (expressionToInspect.IsGT)
            {
                newExpression = expressionToInspect.Args[0] == oldValue
                    ? Context.MkGt((ArithExpr)oldValue, (ArithExpr)replacementVar)
                    : Context.MkGt((ArithExpr)replacementVar, (ArithExpr)oldValue);
            }
            if (expressionToInspect.IsGE)
            {
                newExpression = expressionToInspect.Args[0] == oldValue
                   ? Context.MkGe((ArithExpr)oldValue, (ArithExpr)replacementVar)
                   : Context.MkGe((ArithExpr)replacementVar, (ArithExpr)oldValue);
            }
            if (expressionToInspect.IsLE)
            {
                newExpression = expressionToInspect.Args[0] == oldValue
                   ? Context.MkLe((ArithExpr)oldValue, (ArithExpr)replacementVar)
                   : Context.MkLe((ArithExpr)replacementVar, (ArithExpr)oldValue);
            }
            if (expressionToInspect.IsLT)
            {
                newExpression = expressionToInspect.Args[0] == oldValue
                    ? Context.MkLt((ArithExpr)oldValue, (ArithExpr)replacementVar)
                    : Context.MkLt((ArithExpr)replacementVar, (ArithExpr)oldValue);
            }

            if (addNegation)
            {
                newExpression = Context.MkNot(newExpression);
            }

            return newExpression;
        }

        private Optimize SetOptimizer(IEnumerable<BoolExpr> concatenatedExpressionGroup, Expr? varToOverwrite)
        {
            var optimizer = Context.MkOptimize();
            foreach (var expression in concatenatedExpressionGroup)
            {
                optimizer.Assert(expression);
            }
            ArithExpr minimalPossibleValue = varToOverwrite.IsReal
                ? Context.MkReal(realMin.ToString(CultureInfo.InvariantCulture))
                : Context.MkInt(integerMin.ToString());

            ArithExpr maximalPossibleValue = varToOverwrite.IsReal
                ? Context.MkReal(realMax.ToString(CultureInfo.InvariantCulture))
                : Context.MkInt(integerMax.ToString());

            optimizer.Assert(Context.MkGe((ArithExpr)varToOverwrite, minimalPossibleValue));
            optimizer.Assert(Context.MkLe((ArithExpr)varToOverwrite, maximalPossibleValue));

            return optimizer;
        }
    }
}
