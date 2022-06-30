using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using Microsoft.Z3;

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    public class ConstraintExpressionOperationServiceWithEqTacticConcat : AbstractConstraintExpressionService
    {
        public override BoolExpr ConcatExpressions(BoolExpr source, List<IConstraintExpression> target, bool removeRedundantBlocks = false)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            if (target.Count == 0)
            {
                return source;
            }

            // Presume that source does not have any 'not' expressions except for inequality
            var targetConstraintsDuringEvaluation = new List<IConstraintExpression>(target);
            var andBlockExpressions = new List<BoolExpr>();

            do
            {
                var currentTargetBlock = CutFirstExpressionBlock(targetConstraintsDuringEvaluation);

                var expressionsWithOverwrite = currentTargetBlock
                    .GetExpressionsOfType(VariableType.Written);
                var overwrittenVarNames = expressionsWithOverwrite
                    .Select(x => x.ConstraintVariable)
                    .Distinct()
                    .ToDictionary(x => x.Name, y => y.Domain);

                foreach (var sourceExpressionGroup in SplitSourceExpressionByOrDelimiter(source))
                {
                    // All source constraints + read constraints from target ones
                    var concatenatedExpressionGroup = sourceExpressionGroup
                        .Union(currentTargetBlock.Select(x => x.GetSmtExpression(ContextProvider.Context)));
                    var andExpression = ContextProvider.Context.MkAnd(concatenatedExpressionGroup);
                    BoolExpr resultBlockExpression = andExpression;

                    if (overwrittenVarNames.Count > 0)
                    {
                        var variablesToOverwrite = new Expr[overwrittenVarNames.Count];
                        var currentArrayIndex = 0;
                        foreach (var keyValuePair in overwrittenVarNames)
                        {
                            variablesToOverwrite[currentArrayIndex++] = GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Read);
                        }

                        var existsExpression = ContextProvider.Context.MkExists(variablesToOverwrite, andExpression);

                        Goal g = ContextProvider.Context.MkGoal(true, true, false);
                        g.Assert((BoolExpr)existsExpression);
                        Tactic tac = ContextProvider.Context.AndThen(ContextProvider.Context.MkTactic("qe"), ContextProvider.Context.MkTactic("simplify"));
                        ApplyResult a = tac.Apply(g);

                        var expressionWithRemovedOverwrittenVars = a.Subgoals[0].AsBoolExpr();
                        var tacticToCnf = ContextProvider.Context.MkTactic("tseitin-cnf");
                        var tacticToDnf = ContextProvider.Context.AndThen(
                            tacticToCnf, 
                            ContextProvider.Context.Repeat(ContextProvider.Context.OrElse(ContextProvider.Context.MkTactic("split-clause"), ContextProvider.Context.Skip())));


                        g = ContextProvider.Context.MkGoal(true, true, false);
                        g.Assert(expressionWithRemovedOverwrittenVars);
                        var result = tacticToDnf.Apply(g);

                        var dnfFormatExpressionArray = new BoolExpr[result.Subgoals.Length];
                        var i = 0;

                        foreach (var block in result.Subgoals)
                        {
                            dnfFormatExpressionArray[i++] = block.AsBoolExpr();
                        }

                        var dnfFormatExpression = ContextProvider.Context.MkOr(dnfFormatExpressionArray);

                        foreach (var keyValuePair in overwrittenVarNames)
                        {
                            var sourceVar = GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Written);
                            var targetVar = GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Read);

                            dnfFormatExpression = (BoolExpr)dnfFormatExpression.Substitute(sourceVar, targetVar);
                        }
                        resultBlockExpression = dnfFormatExpression;
                    }

                    if (removeRedundantBlocks)
                    {                        
                        if (resultBlockExpression.IsOr)
                        {                           
                            foreach (var block in resultBlockExpression.Args)
                            {
                                var solver = ContextProvider.Context.MkSimpleSolver();
                                solver.Add((BoolExpr)block);
                                if (solver.Check() == Status.SATISFIABLE)
                                {
                                    andBlockExpressions.Add((BoolExpr)block);
                                }
                            }
                        }
                        else
                        {
                            var solver = ContextProvider.Context.MkSimpleSolver();
                            solver.Add(resultBlockExpression);
                            if (solver.Check() == Status.SATISFIABLE)
                            {
                                andBlockExpressions.Add(resultBlockExpression);
                            }
                        }
                    }
                    else
                    {
                        if (resultBlockExpression.IsOr)
                        {
                            foreach (var block in resultBlockExpression.Args)
                            {
                                andBlockExpressions.Add((BoolExpr)block);
                            }
                        }
                        else
                        {
                            andBlockExpressions.Add(resultBlockExpression);
                        }

                    }
                }
            } while (targetConstraintsDuringEvaluation.Count > 0);

            return andBlockExpressions.Count() == 1
                ? andBlockExpressions[0]
                : ContextProvider.Context.MkOr(andBlockExpressions);
        }

        private Expr GenerateExpression(string variableName, DomainType domain, VariableType varType)
        {
            var nameSuffix = varType == VariableType.Written
                ? "_w"
                : "_r";

            return domain switch
            {
                DomainType.Integer => ContextProvider.Context.MkIntConst(variableName + nameSuffix),
                DomainType.Real => ContextProvider.Context.MkRealConst(variableName + nameSuffix),
                DomainType.Boolean => ContextProvider.Context.MkBoolConst(variableName + nameSuffix),
                _ => throw new NotImplementedException("Domain type is not supported yet"),
            };
        }
    }
}
