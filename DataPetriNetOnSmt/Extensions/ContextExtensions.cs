using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.Enums;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.Extensions
{
    public static class ContextExtensions
    {
        public static bool CanBeSatisfied(this Context context, BoolExpr expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            Solver s = context.MkSimpleSolver();
            s.Assert(expression);

            var result = s.Check() == Status.SATISFIABLE;

            return result;
        }

        public static BoolExpr GetReadExpression(this Context context, BoolExpr smtExpression, Dictionary<string, DomainType> overwrittenVarNames)
        {
            var variablesToOverwrite = new Expr[overwrittenVarNames.Count];
            var currentArrayIndex = 0;
            foreach (var keyValuePair in overwrittenVarNames)
            {
                variablesToOverwrite[currentArrayIndex++] = context.GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Written);
            }

            if (variablesToOverwrite.Length > 0)
            {
                var existsExpression = context.MkExists(variablesToOverwrite, smtExpression);

                Goal g = context.MkGoal(true, true, false);
                g.Assert((BoolExpr)existsExpression);
                Tactic tac = context.MkTactic("qe");
                ApplyResult a = tac.Apply(g);

                return a.Subgoals[0].AsBoolExpr();
            }
            else
            {
                return smtExpression;
            }
        }

        public static BoolExpr GetSmtExpression(this Context context, IList<IConstraintExpression> constraints)
        {
            List<BoolExpr> expressions = new List<BoolExpr>();

            var j = -1;

            for (int i = 0; i < constraints.Count; i++)
            {
                if (constraints[i].LogicalConnective == LogicalConnective.Or ||
                    constraints[i].LogicalConnective == LogicalConnective.Empty)
                {
                    j++;
                    var smtExpr = constraints[i].GetSmtExpression(context);
                    expressions.Add(smtExpr);
                }
                else
                {
                    expressions[j] = context.MkAnd(expressions[j], constraints[i].GetSmtExpression(context));
                }
            }

            var resultExpression = expressions.Count > 1
                ? context.MkOr(expressions)
                : expressions[0];

            return expressions.Count > 0
                ? resultExpression
                : context.MkTrue();
        }

        public static Expr GenerateExpression(this Context context, string variableName, DomainType domain, VariableType varType)
        {
            var nameSuffix = varType == VariableType.Written
                ? "_w"
                : "_r";

            return domain switch
            {
                DomainType.Integer => context.MkIntConst(variableName + nameSuffix),
                DomainType.Real => context.MkRealConst(variableName + nameSuffix),
                DomainType.Boolean => context.MkBoolConst(variableName + nameSuffix),
                _ => throw new NotImplementedException("Domain type is not supported yet"),
            };
        }

    }
}
