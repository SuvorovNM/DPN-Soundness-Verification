using System;
using System.Collections.Generic;
using Microsoft.Z3;
using System.Linq;
using System.Text;
using System.Globalization;

public class Z3ExpressionSerializer
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
            var parts = andArgs.Select(arg => SerializeBoolExpr(arg, 1));
            var result = string.Join(" && ", parts);
            return parentPrecedence > 1 ? $"({result})" : result;
        }
        else if (expr.IsOr)
        {
            var orArgs = expr.Args.Cast<BoolExpr>();
            var parts = orArgs.Select(arg => SerializeBoolExpr(arg, 2));
            var result = string.Join(" || ", parts);
            return parentPrecedence > 2 ? $"({result})" : result;
        }
        else if (expr.IsNot)
        {
            var notArg = (BoolExpr)expr.Args[0];
            
            if (notArg.IsNot)
            {
                var innerArg = (BoolExpr)notArg.Args[0];
                return SerializeBoolExpr(innerArg, parentPrecedence);
            }
            else if (notArg.IsEq)
            {
                var left = notArg.Args[0];
                var right = notArg.Args[1];
                
                if (left.IsBool && right.IsBool)
                {
                    var leftStr = SerializeBoolOperand((BoolExpr)left);
                    var rightStr = SerializeBoolOperand((BoolExpr)right);
                    return $"({leftStr} != {rightStr})";
                }
                else
                {
                    var leftStr = SerializeRealOperand(left);
                    var rightStr = SerializeRealOperand(right);
                    return $"({leftStr} != {rightStr})";
                }
            }
            else if (notArg.IsTrue || notArg.IsFalse || notArg.IsConst)
            {
                return "!" + SerializeBoolOperand(notArg);
            }
            else
            {
                var inner = SerializeBoolExpr(notArg, 3);
                return $"!({inner})";
            }
        }
        else if (expr.IsEq)
        {
            var left = expr.Args[0];
            var right = expr.Args[1];
            
            if (left.IsBool && right.IsBool)
            {
                var leftStr = SerializeBoolOperand((BoolExpr)left);
                var rightStr = SerializeBoolOperand((BoolExpr)right);
                return $"({leftStr} == {rightStr})";
            }
            else
            {
                var leftStr = SerializeRealOperand(left);
                var rightStr = SerializeRealOperand(right);
                return $"({leftStr} == {rightStr})";
            }
        }
        else if (expr.IsGT)
        {
            var left = SerializeRealOperand(expr.Args[0]);
            var right = SerializeRealOperand(expr.Args[1]);
            return $"({left} > {right})";
        }
        else if (expr.IsLT)
        {
            var left = SerializeRealOperand(expr.Args[0]);
            var right = SerializeRealOperand(expr.Args[1]);
            return $"({left} < {right})";
        }
        else if (expr.IsGE)
        {
            var left = SerializeRealOperand(expr.Args[0]);
            var right = SerializeRealOperand(expr.Args[1]);
            return $"({left} >= {right})";
        }
        else if (expr.IsLE)
        {
            var left = SerializeRealOperand(expr.Args[0]);
            var right = SerializeRealOperand(expr.Args[1]);
            return $"({left} <= {right})";
        }
        else if (expr.IsTrue)
        {
            return "true";
        }
        else if (expr.IsFalse)
        {
            return "false";
        }
        else if (expr.IsConst && expr.IsBool)
        {
            return expr.ToString();
        }
        else
        {
            throw new ArgumentException($"Unsupported expression type: {expr}");
        }
    }

    private string SerializeBoolOperand(BoolExpr expr)
    {
        if (expr.IsTrue) return "true";
        if (expr.IsFalse) return "false";
        if (expr.IsConst) return expr.ToString();
        if (expr.IsNot)
        {
            var notArg = (BoolExpr)expr.Args[0];
            if (notArg.IsConst || notArg.IsTrue || notArg.IsFalse)
                return "!" + SerializeBoolOperand(notArg);
            else
                return $"!({SerializeBoolExpr(notArg, 0)})";
        }
        
        return $"({SerializeBoolExpr(expr, 0)})";
    }

    private string SerializeRealOperand(Expr expr)
    {
        if (expr is RatNum ratNum)
        {
            var value = ratNum.ToDecimalString(10);
            // Format integers without decimal point, reals with decimal point
            if (value.Contains("/") || value.Contains("."))
                return value;
            else
                return value + ".0";
        }
        else if (expr.IsConst)
        {
            return expr.ToString();
        }
        else if (expr.IsSub && expr.Args.Length == 2)
        {
            var left = expr.Args[0];
            var right = expr.Args[1];
            
            // Check if this is a unary minus (0 - value)
            if (left is RatNum leftRat && leftRat.ToDecimalString(10) == "0")
                return $"-{SerializeRealOperand(right)}";
        }
        
        return expr.ToString();
    }
}

/*public class Z3ExpressionSerializer
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
            var parts = andArgs.Select(arg => SerializeBoolExpr(arg, 1));
            var result = string.Join(" && ", parts);
            return parentPrecedence > 1 ? $"({result})" : result;
        }
        else if (expr.IsOr)
        {
            var orArgs = expr.Args.Cast<BoolExpr>();
            var parts = orArgs.Select(arg => SerializeBoolExpr(arg, 2));
            var result = string.Join(" || ", parts);
            return parentPrecedence > 2 ? $"({result})" : result;
        }
        else if (expr.IsNot)
        {
            var notArg = (BoolExpr)expr.Args[0];
            
            if (notArg.IsNot)
            {
                var innerArg = (BoolExpr)notArg.Args[0];
                return SerializeBoolExpr(innerArg, parentPrecedence);
            }
            else if (notArg.IsEq)
            {
                var left = notArg.Args[0];
                var right = notArg.Args[1];
                
                if (left.IsBool && right.IsBool)
                {
                    var leftStr = SerializeBoolOperand((BoolExpr)left);
                    var rightStr = SerializeBoolOperand((BoolExpr)right);
                    return $"({leftStr} != {rightStr})";
                }
                else
                {
                    var leftStr = SerializeNumericOperand(left);
                    var rightStr = SerializeNumericOperand(right);
                    return $"({leftStr} != {rightStr})";
                }
            }
            else if (notArg.IsTrue || notArg.IsFalse || notArg.IsConst)
            {
                return "!" + SerializeBoolOperand(notArg);
            }
            else
            {
                var inner = SerializeBoolExpr(notArg, 3);
                return $"!({inner})";
            }
        }
        else if (expr.IsEq)
        {
            var left = expr.Args[0];
            var right = expr.Args[1];
            
            if (left.IsBool && right.IsBool)
            {
                var leftStr = SerializeBoolOperand((BoolExpr)left);
                var rightStr = SerializeBoolOperand((BoolExpr)right);
                return $"({leftStr} == {rightStr})";
            }
            else
            {
                var leftStr = SerializeNumericOperand(left);
                var rightStr = SerializeNumericOperand(right);
                return $"({leftStr} == {rightStr})";
            }
        }
        else if (expr.IsGT)
        {
            var left = SerializeNumericOperand(expr.Args[0]);
            var right = SerializeNumericOperand(expr.Args[1]);
            return $"({left} > {right})";
        }
        else if (expr.IsLT)
        {
            var left = SerializeNumericOperand(expr.Args[0]);
            var right = SerializeNumericOperand(expr.Args[1]);
            return $"({left} < {right})";
        }
        else if (expr.IsGE)
        {
            var left = SerializeNumericOperand(expr.Args[0]);
            var right = SerializeNumericOperand(expr.Args[1]);
            return $"({left} >= {right})";
        }
        else if (expr.IsLE)
        {
            var left = SerializeNumericOperand(expr.Args[0]);
            var right = SerializeNumericOperand(expr.Args[1]);
            return $"({left} <= {right})";
        }
        else if (expr.IsTrue)
        {
            return "true";
        }
        else if (expr.IsFalse)
        {
            return "false";
        }
        else if (expr.IsConst && expr.IsBool)
        {
            return expr.ToString();
        }
        else
        {
            throw new ArgumentException($"Unsupported expression type: {expr}");
        }
    }

    private string SerializeBoolOperand(BoolExpr expr)
    {
        if (expr.IsTrue) return "true";
        if (expr.IsFalse) return "false";
        if (expr.IsConst) return expr.ToString();
        if (expr.IsNot)
        {
            var notArg = (BoolExpr)expr.Args[0];
            if (notArg.IsConst || notArg.IsTrue || notArg.IsFalse)
                return "!" + SerializeBoolOperand(notArg);
            else
                return $"!({SerializeBoolExpr(notArg, 0)})";
        }
        
        return $"({SerializeBoolExpr(expr, 0)})";
    }

    private string SerializeNumericOperand(Expr expr)
    {
        // Check if it's an integer constant
        if (expr is IntNum intNum)
        {
            return intNum.Int.ToString();
        }
        // Check if it's a real constant
        else if (expr is RatNum ratNum)
        {
            var value = ratNum.ToDecimalString(10);
            // Ensure real numbers have decimal point
            if (!value.Contains(".") && !value.Contains("/"))
                value += ".0";
            return value;
        }
        // Check if it's a variable
        else if (expr.IsConst)
        {
            return expr.ToString();
        }
        // Check if it's an integer-to-real conversion
        else if (expr.IsIntToReal)
        {
            var intExpr = expr.Args[0];
            return SerializeNumericOperand(intExpr);
        }
        // Check if it's a negative number
        else if (expr.IsSub && expr.Args.Length == 2)
        {
            var left = expr.Args[0];
            var right = expr.Args[1];
            
            // Check if this is a unary minus (0 - value)
            if (left is IntNum leftInt && leftInt.Int == 0)
                return $"-{SerializeNumericOperand(right)}";
            else if (left is RatNum leftRat && leftRat.ToDecimalString(10) == "0")
                return $"-{SerializeNumericOperand(right)}";
        }
        
        return expr.ToString();
    }
}*/