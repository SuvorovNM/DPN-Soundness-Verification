using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using DataPetriNetOnSmt.SoundnessVerification.Services.Extensions;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    public class ConstraintExpressionServiceForRealsWithManualConcat : AbstractConstraintExpressionService
    {
        public ConstraintExpressionServiceForRealsWithManualConcat(Context context) : base(context)
        {
        }

        public override BoolExpr ConcatExpressions(BoolExpr source,
            List<IConstraintExpression> target,
            bool removeRedundantBlocks = false)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            if (target.Count == 0)
            {
                return source;
            }

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

                var variablesToOverwrite = new Expr[overwrittenVarNames.Count];
                var currentArrayIndex = 0;
                foreach (var keyValuePair in overwrittenVarNames)
                {
                    variablesToOverwrite[currentArrayIndex++] = GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Read);
                }

                // Presume that there is no OR-hierarcy: all ORs are only on the first level
                foreach (var sourceExpressionGroup in SplitSourceExpressionByOrDelimiter(source))
                {
                    // All source constraints + read constraints from target ones
                    var concatenatedExpressionGroup = sourceExpressionGroup
                        .Union(currentTargetBlock.Select(x => x.GetSmtExpression(Context)));

                    // Verify that current expression is satisfiable
                    var solver = Context.MkSimpleSolver();
                    solver.Add(concatenatedExpressionGroup);
                    if (solver.Check() == Status.SATISFIABLE)
                    {

                        var expressionGroupWithImplications = GenerateAllImplications(
                            concatenatedExpressionGroup,
                            variablesToOverwrite);

                        var expressionGroupWithoutOverwrittenVars = GetConstraintsWithoutOverwrittenVars(
                            expressionGroupWithImplications,
                            variablesToOverwrite);

                        var andBlockExpression = GetAndBlockExpression(expressionGroupWithoutOverwrittenVars);

                        andBlockExpression = SubstituteWriteVarsWithReadVars(andBlockExpression, overwrittenVarNames);
                        andBlockExpressions.Add(andBlockExpression);
                    }

                }
            } while (targetConstraintsDuringEvaluation.Count > 0);

            return andBlockExpressions.Count() == 1
                ? andBlockExpressions[0]
                : Context.MkOr(andBlockExpressions);

            throw new NotImplementedException();
        }

        // Do implications until the result does not stabilize
        private List<BoolExpr> GenerateAllImplications(IEnumerable<BoolExpr> constraints,
            Expr[] overwrittenVarNames)
        {
            var expressionsWithImplications = new List<BoolExpr>(constraints);
            if (overwrittenVarNames.Length == 0)
            {
                return expressionsWithImplications;
            }

            int previousExpressionCount;

            do
            {
                previousExpressionCount = expressionsWithImplications.Count;

                // We avoid expressions with both overwritten vars
                 var baseExpressionsForImplications = expressionsWithImplications
                    .Where(x => (x.IsNot && 
                        !x.Args[0].Args.Any(x=>x.IsNumeral) &&
                        x.Args[0].Args.Any(y => overwrittenVarNames.Contains(y)) &&
                        x.Args[0].Args.Any(y => !overwrittenVarNames.Contains(y))) ||
                        (!x.IsNot &&
                        !x.Args.Any(x => x.IsNumeral) && 
                        x.Args.Any(y => overwrittenVarNames.Contains(y)) &&
                        x.Args.Any(y => !overwrittenVarNames.Contains(y))))
                    .ToArray();

                var addedBaseExpressions = baseExpressionsForImplications
                    .Where(x => x.IsGE && baseExpressionsForImplications
                        .Contains(Context.MkLe((ArithExpr)x.Args[0], (ArithExpr)x.Args[1])) ||
                            x.IsGE && baseExpressionsForImplications
                        .Contains(Context.MkGe((ArithExpr)x.Args[1], (ArithExpr)x.Args[0])))
                    .Select(y => Context.MkEq((ArithExpr)y.Args[0], (ArithExpr)y.Args[1]));

                if (addedBaseExpressions.Count() > 0)
                {

                }

                baseExpressionsForImplications = baseExpressionsForImplications
                    .Union(addedBaseExpressions)
                    .ToArray();

                // Replace a>=b and a<=b with a = b

                foreach (var baseExpression in baseExpressionsForImplications)
                {
                    Expr varToStay;
                    Expr varToRemove;
                    if (baseExpression.IsNot)
                    {
                        varToStay = baseExpression.Args[0].Args
                            .First(x => !overwrittenVarNames.Contains(x));
                        varToRemove = baseExpression.Args[0].Args
                            .First(x => x != varToStay);
                    }
                    else
                    {
                        varToStay = baseExpression.Args
                            .First(x => !overwrittenVarNames.Contains(x));
                        varToRemove = baseExpression.Args
                            .First(x => x != varToStay);
                    }

                    // POSITION MATTERS
                    if (baseExpression.IsGT && baseExpression.Args[0] == varToStay ||
                        baseExpression.IsLT && baseExpression.Args[1] == varToStay)
                        expressionsWithImplications.AddUniqueExpressions(
                            GetGTImplications(varToStay, varToRemove, expressionsWithImplications));
                    if (baseExpression.IsGE && baseExpression.Args[0] == varToStay ||
                        baseExpression.IsLE && baseExpression.Args[1] == varToStay)
                        expressionsWithImplications.AddUniqueExpressions(
                            GetGEImplications(varToStay, varToRemove, expressionsWithImplications));
                    if (baseExpression.IsLT && baseExpression.Args[0] == varToStay ||
                        baseExpression.IsGT && baseExpression.Args[1] == varToStay)
                        expressionsWithImplications.AddUniqueExpressions(
                            GetLTImplications(varToStay, varToRemove, expressionsWithImplications));
                    if (baseExpression.IsLE && baseExpression.Args[0] == varToStay ||
                        baseExpression.IsGE && baseExpression.Args[1] == varToStay)
                        expressionsWithImplications.AddUniqueExpressions(
                            GetLEImplications(varToStay, varToRemove, expressionsWithImplications));
                    if (baseExpression.IsEq)
                        expressionsWithImplications.AddUniqueExpressions(
                            GetEqImplications(varToStay, varToRemove, expressionsWithImplications));
                    if (baseExpression.IsNot)
                        expressionsWithImplications.AddUniqueExpressions(
                            GetNotEqImplications(varToStay, varToRemove, expressionsWithImplications));
                }


            } while (expressionsWithImplications.Count != previousExpressionCount);

            return expressionsWithImplications;
        }

        #region Implications - maybe to different classes with the same interface?

        private List<BoolExpr> GetGTImplications(Expr varToStay, Expr varToRemove, IEnumerable<BoolExpr> existingExpressions)
        {
            var implications = new List<BoolExpr>();

            // Maybe ToString()?
            var expressionsToConsiderWithVarToRemoveInFirstPosition = existingExpressions
                .Where(x => x.IsGT || x.IsGE || x.IsEq)
                .Where(x => x.Args[0] == varToRemove);

            foreach (var expr in expressionsToConsiderWithVarToRemoveInFirstPosition)
            {
                // For integers, increment of Const by 1
                if (varToStay.IsInt && expr.Args[1].IsIntNum && expr.IsGT)
                {
                    var increasedConst = Context.MkInt(Int32.Parse(expr.Args[1].ToString()) + 1);
                    implications.Add(Context.MkGt((ArithExpr)varToStay, increasedConst));
                }
                else
                {
                    implications.Add(Context.MkGt((ArithExpr)varToStay, (ArithExpr)expr.Args[1]));
                }
            }

            var expressionsToConsiderWithVarToRemoveInSecondPosition = existingExpressions
                .Where(x => x.IsLT || x.IsLE || x.IsEq)
                .Where(x => x.Args[1] == varToRemove);

            foreach (var expr in expressionsToConsiderWithVarToRemoveInSecondPosition)
            {
                // For integers, increment of Const by 1
                if (varToStay.IsInt && expr.Args[0].IsIntNum && expr.IsLT)
                {
                    var increasedConst = Context.MkInt(Int32.Parse(expr.Args[0].ToString()) + 1);
                    implications.Add(Context.MkGt((ArithExpr)varToStay, increasedConst));
                }
                else
                {
                    implications.Add(Context.MkGt((ArithExpr)varToStay, (ArithExpr)expr.Args[0]));
                }
            }

            return implications;
        }

        private List<BoolExpr> GetGEImplications(Expr varToStay, Expr varToRemove, IEnumerable<BoolExpr> existingExpressions)
        {
            var implications = new List<BoolExpr>();

            // Maybe ToString()?
            var expressionsToConsiderWithVarToRemoveInFirstPosition = existingExpressions
                .Where(x => x.IsGT || x.IsGE || x.IsEq)
                .Where(x => x.Args[0] == varToRemove);

            foreach (var expr in expressionsToConsiderWithVarToRemoveInFirstPosition)
            {
                if (expr.IsGT)
                {
                    implications.Add(Context.MkGt((ArithExpr)varToStay, (ArithExpr)expr.Args[1]));
                }
                else
                {
                    implications.Add(Context.MkGe((ArithExpr)varToStay, (ArithExpr)expr.Args[1]));
                }
            }

            var expressionsToConsiderWithVarToRemoveInSecondPosition = existingExpressions
                .Where(x => x.IsLT || x.IsLE || x.IsEq)
                .Where(x => x.Args[1] == varToRemove);

            foreach (var expr in expressionsToConsiderWithVarToRemoveInSecondPosition)
            {
                if (expr.IsLT)
                {
                    implications.Add(Context.MkGt((ArithExpr)varToStay, (ArithExpr)expr.Args[0]));
                }
                else
                {
                    implications.Add(Context.MkGe((ArithExpr)varToStay, (ArithExpr)expr.Args[0]));
                }
            }

            return implications;
        }

        private List<BoolExpr> GetLTImplications(Expr varToStay, Expr varToRemove, IEnumerable<BoolExpr> existingExpressions)
        {
            var implications = new List<BoolExpr>();

            var expressionsToConsiderWithVarToRemoveInFirstPosition = existingExpressions
                .Where(x => x.IsLT || x.IsLE || x.IsEq)
                .Where(x => x.Args[0] == varToRemove);

            foreach (var expr in expressionsToConsiderWithVarToRemoveInFirstPosition)
            {
                if (varToStay.IsInt && expr.Args[1].IsIntNum && expr.IsLT)
                {
                    var increasedConst = Context.MkInt(Int32.Parse(expr.Args[1].ToString()) - 1);
                    implications.Add(Context.MkGt((ArithExpr)varToStay, increasedConst));
                }
                else
                {
                    implications.Add(Context.MkLt((ArithExpr)varToStay, (ArithExpr)expr.Args[1]));
                }
            }


            var expressionsToConsiderWithVarToRemoveInSecondPosition = existingExpressions
                .Where(x => x.IsGT || x.IsGE || x.IsEq)
                .Where(x => x.Args[1] == varToRemove);

            foreach (var expr in expressionsToConsiderWithVarToRemoveInSecondPosition)
            {
                if (varToStay.IsInt && expr.Args[0].IsIntNum && expr.IsGT)
                {
                    var increasedConst = Context.MkInt(Int32.Parse(expr.Args[0].ToString()) - 1);
                    implications.Add(Context.MkGt((ArithExpr)varToStay, increasedConst));
                }
                else
                {
                    implications.Add(Context.MkLt((ArithExpr)varToStay, (ArithExpr)expr.Args[0]));
                }
            }

            return implications;
        }

        private List<BoolExpr> GetLEImplications(Expr varToStay, Expr varToRemove, IEnumerable<BoolExpr> existingExpressions)
        {
            var implications = new List<BoolExpr>();

            var expressionsToConsiderWithVarToRemoveInFirstPosition = existingExpressions
                .Where(x => x.IsLT || x.IsLE || x.IsEq)
                .Where(x => x.Args[0] == varToRemove);

            foreach (var expr in expressionsToConsiderWithVarToRemoveInFirstPosition)
            {
                if (expr.IsLT)
                {
                    implications.Add(Context.MkLt((ArithExpr)varToStay, (ArithExpr)expr.Args[1]));
                }
                else
                {
                    implications.Add(Context.MkLe((ArithExpr)varToStay, (ArithExpr)expr.Args[1]));
                }
            }


            var expressionsToConsiderWithVarToRemoveInSecondPosition = existingExpressions
                .Where(x => x.IsGT || x.IsGE || x.IsEq)
                .Where(x => x.Args[1] == varToRemove);

            foreach (var expr in expressionsToConsiderWithVarToRemoveInSecondPosition)
            {
                if (expr.IsGT)
                {
                    implications.Add(Context.MkLt((ArithExpr)varToStay, (ArithExpr)expr.Args[0]));
                }
                else
                {
                    implications.Add(Context.MkLe((ArithExpr)varToStay, (ArithExpr)expr.Args[0]));
                }
            }

            return implications;
        }

        private List<BoolExpr> GetEqImplications(Expr varToStay, Expr varToRemove, IEnumerable<BoolExpr> existingExpressions)
        {
            var implications = new List<BoolExpr>();

            var expressionsToConsiderWithVarToRemoveInFirstPosition = existingExpressions
                .Where(x => !x.IsNot && x.Args[0] == varToRemove ||
                    x.IsNot && x.Args[0].Args[0] == varToRemove);

            foreach (var expr in expressionsToConsiderWithVarToRemoveInFirstPosition)
            {
                if (expr.IsGT)
                {
                    implications.Add(Context.MkGt((ArithExpr)varToStay, (ArithExpr)expr.Args[1]));
                }
                if (expr.IsGE)
                {
                    implications.Add(Context.MkGe((ArithExpr)varToStay, (ArithExpr)expr.Args[1]));
                }
                if (expr.IsLT)
                {
                    implications.Add(Context.MkLt((ArithExpr)varToStay, (ArithExpr)expr.Args[1]));
                }
                if (expr.IsLE)
                {
                    implications.Add(Context.MkLe((ArithExpr)varToStay, (ArithExpr)expr.Args[1]));
                }
                if (expr.IsEq)
                {
                    implications.Add(Context.MkEq((ArithExpr)varToStay, (ArithExpr)expr.Args[1]));
                }
                if (expr.IsNot)
                {
                    implications.Add(Context.MkNot(Context.MkEq((ArithExpr)varToStay, (ArithExpr)expr.Args[0].Args[1])));
                }
            }


            var expressionsToConsiderWithVarToRemoveInSecondPosition = existingExpressions
                .Where(x => !x.IsNot && x.Args[1] == varToRemove ||
                    x.IsNot && x.Args[0].Args[1] == varToRemove);

            foreach (var expr in expressionsToConsiderWithVarToRemoveInSecondPosition)
            {
                if (expr.IsGT)
                {
                    implications.Add(Context.MkLt((ArithExpr)varToStay, (ArithExpr)expr.Args[0]));
                }
                if (expr.IsGE)
                {
                    implications.Add(Context.MkLe((ArithExpr)varToStay, (ArithExpr)expr.Args[0]));
                }
                if (expr.IsLT)
                {
                    implications.Add(Context.MkGt((ArithExpr)varToStay, (ArithExpr)expr.Args[0]));
                }
                if (expr.IsLE)
                {
                    implications.Add(Context.MkGe((ArithExpr)varToStay, (ArithExpr)expr.Args[0]));
                }
                if (expr.IsEq)
                {
                    implications.Add(Context.MkEq((ArithExpr)varToStay, (ArithExpr)expr.Args[0]));
                }
                if (expr.IsNot)
                {
                    implications.Add(Context.MkNot(Context.MkEq((ArithExpr)varToStay, (ArithExpr)expr.Args[0].Args[0])));
                }
            }

            return implications;
        }

        // М.б. a>= c and a<=c || a == c. Проще просто на SAT проверить
        private List<BoolExpr> GetNotEqImplications(Expr varToStay, Expr varToRemove, IEnumerable<BoolExpr> existingExpressions)
        {
            var expressionsArray = existingExpressions.ToArray();

            var solver = Context.MkSimpleSolver();
            solver.Assert(expressionsArray);

            if (solver.Check() == Status.SATISFIABLE)
            {
                var firstValue = solver.Model.Consts
                    .FirstOrDefault(x => x.Key.Name.ToString() == varToRemove.ToString())
                    .Value;

                if (firstValue != null)
                {
                    solver = Context.MkSimpleSolver();
                    solver.Assert(expressionsArray);

                    var expressionToAdd = Context.MkNot(Context.MkEq(varToRemove, firstValue));
                    solver.Assert(expressionToAdd);

                    if (solver.Check() == Status.UNSATISFIABLE)
                    {
                        return new List<BoolExpr> { Context.MkNot(Context.MkEq(varToStay, firstValue)) };
                    }
                }
            }
            return new List<BoolExpr>();
        }

        #endregion

        private IEnumerable<BoolExpr> GetConstraintsWithoutOverwrittenVars(IEnumerable<BoolExpr> constraints,
            Expr[] overwrittenVarNames)
        {         
            return constraints
                .Where(x => !x.IsNot && x.Args.All(y => !overwrittenVarNames.Contains(y))
                || x.IsNot && x.Args[0].Args.All(y => !overwrittenVarNames.Contains(y)));
        }

        // TODO: To extension of BoolExpr
        private BoolExpr SubstituteWriteVarsWithReadVars(BoolExpr expression,
            IDictionary<string, DomainType> overwrittenVarNames)
        {
            foreach (var keyValuePair in overwrittenVarNames)
            {
                var sourceVar = GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Written);
                var targetVar = GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Read);

                expression = (BoolExpr)expression.Substitute(sourceVar, targetVar);
            }
            return expression;
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

        private BoolExpr GetAndBlockExpression(IEnumerable<BoolExpr> constraints)
        {
            return Context.MkAnd(constraints);
        }
    }
}
