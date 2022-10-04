using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using Microsoft.Z3;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    public class ConstraintExpressionOperationServiceWithEqTacticConcat : AbstractConstraintExpressionService
    {
        public ConstraintExpressionOperationServiceWithEqTacticConcat(Context context) : base(context)
        {
        }

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

                var andExpression = Context.MkAnd(source, 
                    Context.MkAnd(currentTargetBlock.Select(x => x.GetSmtExpression(Context))));
                BoolExpr resultBlockExpression = andExpression;

                if (overwrittenVarNames.Count > 0)
                {
                    var variablesToOverwrite = new Expr[overwrittenVarNames.Count];
                    var currentArrayIndex = 0;
                    foreach (var keyValuePair in overwrittenVarNames)
                    {
                        variablesToOverwrite[currentArrayIndex++] = GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Read);
                    }

                    var existsExpression = Context.MkExists(variablesToOverwrite, andExpression);

                    Goal g = Context.MkGoal(true, true, false);
                    g.Assert((BoolExpr)existsExpression);
                    Tactic tac = Context.MkTactic("qe");
                    ApplyResult a = tac.Apply(g);
                    var expressionWithRemovedOverwrittenVars = a.Subgoals[0].AsBoolExpr();
            

                    foreach (var keyValuePair in overwrittenVarNames)
                    {
                        var sourceVar = GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Written);
                        var targetVar = GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Read);

                        expressionWithRemovedOverwrittenVars = (BoolExpr)expressionWithRemovedOverwrittenVars.Substitute(sourceVar, targetVar);
                    }
                    resultBlockExpression = expressionWithRemovedOverwrittenVars;
                }

                if (removeRedundantBlocks)
                {
                    if (resultBlockExpression.IsOr)
                    {
                        foreach (var block in resultBlockExpression.Args)
                        {
                            var solver = Context.MkSimpleSolver();
                            solver.Add((BoolExpr)block);
                            if (solver.Check() == Status.SATISFIABLE)
                            {
                                andBlockExpressions.Add((BoolExpr)block);
                            }
                        }
                    }
                    else
                    {
                        var solver = Context.MkSimpleSolver();
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
            } while (targetConstraintsDuringEvaluation.Count > 0);

            return andBlockExpressions.Count() == 1
                ? andBlockExpressions[0]
                : Context.MkOr(andBlockExpressions);
        }

        private Expr GenerateExpression(string variableName, DomainType domain, VariableType varType)
        {
            var nameSuffix = varType == VariableType.Written
                ? "_w"
                : "_r";

            return domain switch
            {
                DomainType.Integer => Context.MkIntConst(variableName + nameSuffix),
                DomainType.Real => Context.MkRealConst(variableName + nameSuffix),
                DomainType.Boolean => Context.MkBoolConst(variableName + nameSuffix),
                _ => throw new NotImplementedException("Domain type is not supported yet"),
            };
        }
    }
}
