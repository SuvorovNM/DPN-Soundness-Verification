using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using Microsoft.Z3;
using System.Diagnostics;

namespace DataPetriNetOnSmt.Abstractions
{
    public abstract class AbstractConstraintExpressionService
    {
        public List<IConstraintExpression> GetInvertedReadExpression(List<IConstraintExpression> sourceExpression)
        {
            if (sourceExpression is null)
            {
                throw new ArgumentNullException(nameof(sourceExpression));
            }
            if (sourceExpression.Count == 0)
            {
                return sourceExpression;
            }

            var blocks = new List<List<IConstraintExpression>>();
            var expressionDuringExecution = new List<IConstraintExpression>(sourceExpression);
            do
            {
                var expressionBlock = CutFirstExpressionBlock(expressionDuringExecution)
                    .GetExpressionsOfType(VariableType.Read)
                    .Select(x => x.GetInvertedExpression())
                    .ToList();

                if (expressionBlock.Count > 0)
                {
                    blocks.Add(expressionBlock);
                }
            } while (expressionDuringExecution.Count > 0);

            var allCombinations = GetAllPossibleCombos(blocks);
            return MakeSingleExpressionListFromMultipleLists(allCombinations);
        }

        private static List<IConstraintExpression> MakeSingleExpressionListFromMultipleLists(List<List<IConstraintExpression>> allCombinations)
        {
            var resultExpression = new List<IConstraintExpression>();

            foreach (var combination in allCombinations.Where(x => x.Count > 0))
            {
                var firstExpressionInBlock = combination[0].Clone();
                firstExpressionInBlock.LogicalConnective = LogicalConnective.Or;
                resultExpression.Add(firstExpressionInBlock);

                for (int i = 1; i < combination.Count; i++)
                {
                    var currentExpression = combination[i].Clone();
                    currentExpression.LogicalConnective = LogicalConnective.And;
                    resultExpression.Add(currentExpression);
                }
            }
            if (resultExpression.Count > 0)
            {
                resultExpression[0].LogicalConnective = LogicalConnective.Empty;
            }

            return resultExpression;
        }

        private static List<List<IConstraintExpression>> GetAllPossibleCombos(List<List<IConstraintExpression>> expressions)
        {
            IEnumerable<List<IConstraintExpression>> combos = new[] { new List<IConstraintExpression>() };

            foreach (var inner in expressions)
                combos = from c in combos
                         from i in inner
                         select c.Union(new List<IConstraintExpression> { i }).ToList();

            return combos.ToList();
        }

        public BoolExpr ShortenExpression(BoolExpr expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return (BoolExpr)expression.Simplify();
        }

        public bool CanBeSatisfied(BoolExpr expression)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            Solver s = ContextProvider.Context.MkSimpleSolver();
            s.Assert(expression);

            var result = s.Check() == Status.SATISFIABLE;
            stopwatch.Stop();

            return result;
        }

        public bool AreEqual(BoolExpr expressionSource, BoolExpr expressionTarget)
        {
            if (expressionSource is null)
            {
                throw new ArgumentNullException(nameof(expressionSource));
            }
            if (expressionTarget is null)
            {
                throw new ArgumentNullException(nameof(expressionTarget));
            }

            // 2 expressions are equal if [(not(x) and y) or (x and not(y))] is not satisfiable
            var exprWithSourceNegated = ContextProvider.Context.MkAnd(ContextProvider.Context.MkNot(expressionSource), expressionTarget);
            var exprWithTargetNegated = ContextProvider.Context.MkAnd(expressionSource, ContextProvider.Context.MkNot(expressionTarget));
            var expressionToCheck = ContextProvider.Context.MkOr(exprWithSourceNegated, exprWithTargetNegated);

            Solver s = ContextProvider.Context.MkSimpleSolver();
            s.Assert(expressionToCheck);

            var result = s.Check() == Status.UNSATISFIABLE;

            return result;
        }
        public abstract BoolExpr ConcatExpressions(BoolExpr source, List<IConstraintExpression> target, bool removeRedundantBlocks = false);


        protected static List<IConstraintExpression> CutFirstExpressionBlock(List<IConstraintExpression> sourceConstraintsDuringEvaluation)
        {
            if (sourceConstraintsDuringEvaluation.Count == 0)
            {
                return new List<IConstraintExpression>();
            }

            List<IConstraintExpression> currentBlock;
            var delimiter = Guard.GetDelimiter(sourceConstraintsDuringEvaluation);

            currentBlock = new List<IConstraintExpression>(sourceConstraintsDuringEvaluation.GetRange(0, delimiter));
            sourceConstraintsDuringEvaluation.RemoveRange(0, delimiter);
            return currentBlock;
        }

        protected static IEnumerable<BoolExpr[]> SplitSourceExpressionByOrDelimiter(BoolExpr source)
        {
            if (source.IsAnd)
            {
                var expressions = source.Args.Select(x => x as BoolExpr).ToArray();
                return new List<BoolExpr[]> { expressions };
            }

            if (source.IsOr)
            {
                var expressionList = new List<BoolExpr[]>();
                foreach (var expression in source.Args)
                {
                    expressionList.Add(expression.Args.Select(x => (BoolExpr)x).ToArray());
                }

                return expressionList;
            }

            return new List<BoolExpr[]>() { new BoolExpr[] { source } };
        }
    }
}
