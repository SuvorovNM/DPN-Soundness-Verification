using Microsoft.Z3;

public class Z3ExpressionSerializer_Ok2
{
    public string Serialize(BoolExpr expression)
    {
        if (expression == null)
            throw new ArgumentNullException(nameof(expression));

        return SerializeBoolExpr(expression, 0);
    }

    private string SerializeBoolExpr(BoolExpr expr, int parentPrecedence)
    {
        if (expr.IsAnd)
        {
            var andArgs = expr.Args.Cast<BoolExpr>();
            var parts = andArgs.Select(arg => SerializeBoolExpr(arg, 1)); // AND precedence = 1
            var result = string.Join(" && ", parts);
            return parentPrecedence > 1 ? $"({result})" : result;
        }
        else if (expr.IsOr)
        {
            var orArgs = expr.Args.Cast<BoolExpr>();
            var parts = orArgs.Select(arg => SerializeBoolExpr(arg, 2)); // OR precedence = 2
            var result = string.Join(" || ", parts);
            return parentPrecedence > 2 ? $"({result})" : result;
        }
        else if (expr.IsNot)
        {
            var notArg = (BoolExpr)expr.Args[0];
            if (notArg.IsEq)
            {
                // Handle != case
                var eqArgs = notArg.Args;
                return SerializeComparison(eqArgs[0], eqArgs[1], "!=");
            }
            else
            {
                return "!" + SerializeBoolExpr(notArg, 3); // NOT precedence = 3
            }
        }
        else if (expr.IsEq)
        {
            var eqArgs = expr.Args;
            return SerializeComparison(eqArgs[0], eqArgs[1], "==");
        }
        else if (expr.IsGT)
        {
            var gtArgs = expr.Args;
            return SerializeComparison(gtArgs[0], gtArgs[1], ">");
        }
        else if (expr.IsLT)
        {
            var ltArgs = expr.Args;
            return SerializeComparison(ltArgs[0], ltArgs[1], "<");
        }
        else if (expr.IsGE)
        {
            var geArgs = expr.Args;
            return SerializeComparison(geArgs[0], geArgs[1], ">=");
        }
        else if (expr.IsLE)
        {
            var leArgs = expr.Args;
            return SerializeComparison(leArgs[0], leArgs[1], "<=");
        }
        else
        {
            throw new ArgumentException($"Unsupported expression type: {expr}");
        }
    }

    private string SerializeComparison(Expr left, Expr right, string op)
    {
        var leftStr = SerializeArithExpr(left, 0);
        var rightStr = SerializeArithExpr(right, 0);
        return $"({leftStr} {op} {rightStr})";
    }

    private string SerializeArithExpr(Expr expr, int parentPrecedence)
    {
        if (expr.IsAdd)
        {
            var addArgs = expr.Args;
            var parts = addArgs.Select(arg => SerializeArithExpr(arg, 1)); // +- precedence = 1
            var result = string.Join(" + ", parts);
            return parentPrecedence > 1 ? $"({result})" : result;
        }
        else if (expr.IsSub)
        {
            var subArgs = expr.Args;
            if (subArgs.Length == 2)
            {
                var left = SerializeArithExpr(subArgs[0], 1);
                var right = SerializeArithExpr(subArgs[1], 2); // Right side of - needs higher precedence
                var result = $"{left} - {right}";
                return parentPrecedence > 1 ? $"({result})" : result;
            }
            else
            {
                // Unary minus
                return $"-{SerializeArithExpr(subArgs[0], 3)}"; // Unary - has highest precedence
            }
        }
        else if (expr.IsMul)
        {
            var mulArgs = expr.Args;
            var parts = mulArgs.Select(arg => SerializeArithExpr(arg, 2)); // */ precedence = 2
            var result = string.Join(" * ", parts);
            return parentPrecedence > 2 ? $"({result})" : result;
        }
        else if (expr.IsDiv)
        {
            var divArgs = expr.Args;
            if (divArgs.Length == 2)
            {
                var left = SerializeArithExpr(divArgs[0], 2);
                var right = SerializeArithExpr(divArgs[1], 3); // Right side of / needs higher precedence
                var result = $"{left} / {right}";
                return parentPrecedence > 2 ? $"({result})" : result;
            }
            else
            {
                throw new ArgumentException($"Unexpected division expression: {expr}");
            }
        }
        else if (expr.IsIntNum)
        {
            return expr.ToString();
        }
        else if (expr.IsConst)
        {
            return expr.ToString();
        }
        else
        {
            throw new ArgumentException($"Unsupported arithmetic expression: {expr}");
        }
    }
}