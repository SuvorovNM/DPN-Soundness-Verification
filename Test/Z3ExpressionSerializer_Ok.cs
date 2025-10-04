using Microsoft.Z3;

public class Z3ExpressionSerializer_Ok
{
    public string Serialize(BoolExpr expression)
    {
        if (expression == null)
            throw new ArgumentNullException(nameof(expression));

        return SerializeBoolExpr(expression);
    }

    private string SerializeBoolExpr(BoolExpr expr)
    {
        if (expr.IsAnd)
        {
            var andArgs = expr.Args.Cast<BoolExpr>();
            var parts = andArgs.Select(SerializeBoolExpr);
            return "(" + string.Join(" && ", parts) + ")";
        }
        else if (expr.IsOr)
        {
            var orArgs = expr.Args.Cast<BoolExpr>();
            var parts = orArgs.Select(SerializeBoolExpr);
            return "(" + string.Join(" || ", parts) + ")";
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
                return "!" + SerializeBoolExpr(notArg);
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
        var leftStr = SerializeArithExpr(left);
        var rightStr = SerializeArithExpr(right);
        return $"({leftStr} {op} {rightStr})";
    }

    private string SerializeArithExpr(Expr expr)
    {
        if (expr.IsAdd)
        {
            var addArgs = expr.Args;
            var parts = addArgs.Select(SerializeArithExpr);
            return "(" + string.Join(" + ", parts) + ")";
        }
        else if (expr.IsSub)
        {
            var subArgs = expr.Args;
            if (subArgs.Length == 2)
            {
                return $"({SerializeArithExpr(subArgs[0])} - {SerializeArithExpr(subArgs[1])})";
            }
            else
            {
                // Unary minus
                return $"-{SerializeArithExpr(subArgs[0])}";
            }
        }
        else if (expr.IsMul)
        {
            var mulArgs = expr.Args;
            var parts = mulArgs.Select(SerializeArithExpr);
            return "(" + string.Join(" * ", parts) + ")";
        }
        else if (expr.IsDiv)
        {
            var divArgs = expr.Args;
            if (divArgs.Length == 2)
            {
                return $"({SerializeArithExpr(divArgs[0])} / {SerializeArithExpr(divArgs[1])})";
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