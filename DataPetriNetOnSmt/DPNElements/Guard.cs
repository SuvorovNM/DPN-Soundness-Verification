using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using Microsoft.Z3;
using System.Globalization;

namespace DataPetriNetOnSmt.DPNElements
{
    public class Guard : ICloneable
    {
        private readonly VariablesStore localVariables;
        private bool readNeedsToBeRecalculated = false;
        private Dictionary<string, DomainType> readVars = new Dictionary<string, DomainType>();
        public Context Context { get; set; }

        public bool IsSatisfied { get; private set; }
        public List<IConstraintExpression> BaseConstraintExpressions { get; init; }
        public BoolExpr ActualConstraintExpression { get; init; }

        public Dictionary<string, DomainType> WriteVars { get; init; }
        public Dictionary<string, DomainType> ReadVars 
        { 
            get 
            { 
                if (readNeedsToBeRecalculated)
                {
                    readVars = ActualConstraintExpression.GetTypedVarsDict(VariableType.Read);
                    readNeedsToBeRecalculated = false;
                }
                return readVars;
            }
            set
            {
                readVars = value;
            }
        }

        public Guard(Context ctx, List<IConstraintExpression>? baseConstraints = null)
        {
            if (baseConstraints == null)
            {
                BaseConstraintExpressions = new List<IConstraintExpression>();
                ActualConstraintExpression = ctx.MkTrue();
            }
            else
            {
                BaseConstraintExpressions = baseConstraints;
                ActualConstraintExpression = ctx.GetSmtExpression(baseConstraints);
                WriteVars = BaseConstraintExpressions.GetTypedVarsDict(VariableType.Written);
                ReadVars = BaseConstraintExpressions.GetTypedVarsDict(VariableType.Read);
            }

            localVariables = new VariablesStore();
            Context = ctx;            
        }

        public Guard(Context ctx, List<IConstraintExpression> baseConstraints, BoolExpr actualConstraintExpression)
        {
            BaseConstraintExpressions = baseConstraints;
            ActualConstraintExpression = actualConstraintExpression;
            localVariables = new VariablesStore();
            Context = ctx;

            WriteVars = BaseConstraintExpressions.GetTypedVarsDict(VariableType.Written);
            //ReadVars = ActualConstraintExpression.GetTypedVarsDict(VariableType.Read);
            readNeedsToBeRecalculated = true;
        }

        public bool Verify(VariablesStore globalVariables, Context ctx)
        {
            if (BaseConstraintExpressions.Count == 0)
            {
                IsSatisfied = true;
                return true;
            }

            // TODO: Probably, it is better to assign variables from the current guard, not all the vars
            var goal = ctx.MkGoal();

            AssignCurrentValues(globalVariables, ctx, goal);

            AssertGuardConstraints(ctx, goal);

            var s = ctx.MkSimpleSolver();
            foreach (var e in goal.Formulas)
                s.Assert(e);

            if (s.Check() == Status.SATISFIABLE)
            {
                AssignLocalVariables(s);
                IsSatisfied = true;
            }

            return IsSatisfied;
        }

        public static int GetDelimiter(List<IConstraintExpression> constraintStateDuringEvaluation)
        {
            // Find delimiter - OR expression
            var orExpressionIndex = constraintStateDuringEvaluation
                .GetRange(1, constraintStateDuringEvaluation.Count - 1) // TODO: Make search more effective
                .FindIndex(x => x.LogicalConnective == LogicalConnective.Or);

            // If OR exists, we only need expressions before first OR
            var delimiter = orExpressionIndex == -1
                ? constraintStateDuringEvaluation.Count
                : orExpressionIndex + 1;
            return delimiter;
        }

        private void AssignLocalVariables(Solver s)
        {
            var values = s.Model.Consts
                                .Where(x => x.Key.Name.ToString().EndsWith("_w"))
                                .ToDictionary(x => string.Concat(x.Key.Name.ToString().SkipLast(2)), y => y.Value);

            var writeVariables = BaseConstraintExpressions.GetExpressionsOfType(VariableType.Written)
                .Select(x => x.ConstraintVariable)
                .Distinct();

            var rnd = new Random();
            foreach (var writeVar in writeVariables)
            {
                var isFound = values.TryGetValue(writeVar.Name, out var value);
                switch (writeVar.Domain)
                {
                    case DomainType.Integer:
                        var intValue = isFound
                            ? ((IntNum)value).Int64
                            : rnd.NextInt64();
                        localVariables[DomainType.Integer].Write(writeVar.Name, new DefinableValue<long>(intValue));
                        break;

                    case DomainType.Real:
                        var realValue = isFound
                            ? ((RatNum)value).Double
                            : rnd.NextDouble() * rnd.NextInt64();
                        localVariables[DomainType.Real].Write(writeVar.Name, new DefinableValue<double>(realValue));
                        break;

                    case DomainType.Boolean:
                        var boolValue = isFound
                            ? ((BoolExpr)value).BoolValue == Z3_lbool.Z3_L_TRUE
                            : rnd.Next(0, 2) == 1;
                        localVariables[DomainType.Boolean].Write(writeVar.Name, new DefinableValue<bool>(boolValue));
                        break;

                    default:
                        throw new NotImplementedException("This type is not supported yet");
                }
            }
        }

        private void AssertGuardConstraints(Context ctx, Goal goal)
        {
            List<BoolExpr> disjuncts = new List<BoolExpr>();
            var currentIndex = 0;
            //disjuncts[currentIndex] = ConstraintExpressions[0].GetSmtExpression(ctx);
            foreach (var constraint in BaseConstraintExpressions)
            {
                if (constraint.LogicalConnective == LogicalConnective.And)
                {
                    disjuncts[currentIndex] = ctx.MkAnd(disjuncts[currentIndex], constraint.GetSmtExpression(ctx));
                }
                else
                {
                    disjuncts.Add(constraint.GetSmtExpression(ctx));
                    currentIndex++;
                }
            }

            var constraintExpression = disjuncts.Count > 1
                ? ctx.MkOr(disjuncts)
                : disjuncts[0];

            goal.Assert(constraintExpression);
        }

        private static void AssignCurrentValues(VariablesStore globalVariables, Context ctx, Goal goal)
        {
            foreach (var intVariableName in globalVariables[DomainType.Integer].GetKeys())
            {
                var intVariable = ctx.MkConst(ctx.MkSymbol(intVariableName + "_r"), ctx.MkIntSort());

                goal.Assert(ctx.MkEq(intVariable, ctx.MkInt((globalVariables[DomainType.Integer].Read(intVariableName) as DefinableValue<long>).Value)));
            }
            foreach (var realVariableName in globalVariables[DomainType.Real].GetKeys())
            {
                var realVariable = ctx.MkConst(ctx.MkSymbol(realVariableName + "_r"), ctx.MkRealSort());

                goal.Assert(ctx.MkEq(realVariable, ctx.MkReal((globalVariables[DomainType.Real].Read(realVariableName) as DefinableValue<double>).Value.ToString(CultureInfo.InvariantCulture))));
            }
            foreach (var boolVariableName in globalVariables[DomainType.Boolean].GetKeys())
            {
                var boolVariable = ctx.MkConst(ctx.MkSymbol(boolVariableName + "_r"), ctx.MkBoolSort());

                goal.Assert(ctx.MkEq(boolVariable, ctx.MkBool((globalVariables[DomainType.Boolean].Read(boolVariableName) as DefinableValue<bool>).Value)));
            }
        }

        public void UpdateGlobalVariables(VariablesStore globalVariables)
        {
            if (IsSatisfied)
            {
                var variablesToUpdate = BaseConstraintExpressions
                    .GetExpressionsOfType(VariableType.Written)
                    .Select(x => x.ConstraintVariable)
                    .Distinct();

                foreach (var variable in variablesToUpdate)
                {
                    globalVariables[variable.Domain].Write(variable.Name, localVariables[variable.Domain].Read(variable.Name));
                }

                ResetState();
            }
            else
            {
                throw new InvalidOperationException("The transition cannot fire - guard is not satisfied!");
            }
        }

        private void ResetState()
        {
            IsSatisfied = false;
            localVariables.Clear();
        }

        public object Clone()
        {
            var clonedGuard = new Guard(
                Context, 
                BaseConstraintExpressions.Select(x => x.Clone()).ToList(), 
                ActualConstraintExpression);
            return clonedGuard;
        }
    }
}
