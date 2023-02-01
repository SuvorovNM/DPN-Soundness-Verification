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

        public override BoolExpr ConcatExpressions(
            BoolExpr source, 
            BoolExpr target, 
            Dictionary<string, DomainType> overwrittenVars)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            

            var andExpression = Context.MkAnd(source, target);
            BoolExpr resultBlockExpression = andExpression;

            if (overwrittenVars.Count > 0)
            {
                var variablesToOverwrite = new Expr[overwrittenVars.Count];
                var currentArrayIndex = 0;
                foreach (var keyValuePair in overwrittenVars)
                {
                    variablesToOverwrite[currentArrayIndex++] = Context.GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Read);
                }

                var existsExpression = Context.MkExists(variablesToOverwrite, andExpression);

                Goal g = Context.MkGoal(true, true, false);
                g.Assert(existsExpression);
                Tactic tac = Context.MkTactic("qe");
                ApplyResult a = tac.Apply(g);
                var expressionWithRemovedOverwrittenVars = a.Subgoals[0].AsBoolExpr();


                foreach (var keyValuePair in overwrittenVars)
                {
                    var sourceVar = Context.GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Written);
                    var targetVar = Context.GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Read);

                    expressionWithRemovedOverwrittenVars = (BoolExpr)expressionWithRemovedOverwrittenVars.Substitute(sourceVar, targetVar);
                }
                resultBlockExpression = expressionWithRemovedOverwrittenVars;
            }


            return resultBlockExpression;
        }
    }
}
