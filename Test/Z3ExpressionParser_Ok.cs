using Microsoft.Z3;

public class Z3ExpressionParser_Ok
{
    private readonly Context _ctx;
    private readonly Dictionary<string, ArithExpr> _variables;

    public Z3ExpressionParser_Ok(Context ctx)
    {
        _ctx = ctx;
        _variables = new Dictionary<string, ArithExpr>();
    }

    public BoolExpr Parse(string expression)
    {
        expression = expression.Replace(" ", "");
        int pos = 0;
        return ParseExpression(expression, ref pos);
    }

    private BoolExpr ParseExpression(string expr, ref int pos)
    {
        var expressions = new List<BoolExpr>();
        
        while (pos < expr.Length)
        {
            expressions.Add(ParseTerm(expr, ref pos));
            
            if (pos < expr.Length && expr[pos] == '|' && pos + 1 < expr.Length && expr[pos + 1] == '|')
            {
                pos += 2;
            }
            else
            {
                break;
            }
        }
        
        return expressions.Count == 1 ? expressions[0] : _ctx.MkOr(expressions.ToArray());
    }

    private BoolExpr ParseTerm(string expr, ref int pos)
    {
        var expressions = new List<BoolExpr>();
        
        while (pos < expr.Length)
        {
            expressions.Add(ParseComparison(expr, ref pos));
            
            if (pos < expr.Length && expr[pos] == '&' && pos + 1 < expr.Length && expr[pos + 1] == '&')
            {
                pos += 2;
            }
            else
            {
                break;
            }
        }
        
        return expressions.Count == 1 ? expressions[0] : _ctx.MkAnd(expressions.ToArray());
    }

    private BoolExpr ParseComparison(string expr, ref int pos)
    {
        SkipWhitespace(expr, ref pos);
        
        if (pos < expr.Length && expr[pos] == '(')
        {
            pos++; // Skip '('
            var result = ParseExpression(expr, ref pos);
            if (pos < expr.Length && expr[pos] == ')')
            {
                pos++; // Skip ')'
            }
            return result;
        }

        var left = ParseArithmetic(expr, ref pos);
        SkipWhitespace(expr, ref pos);

        // Parse comparison operator
        string op = ParseOperator(expr, ref pos);
        SkipWhitespace(expr, ref pos);

        var right = ParseArithmetic(expr, ref pos);
        SkipWhitespace(expr, ref pos);

        return op switch
        {
            ">" => _ctx.MkGt(left, right),
            "<" => _ctx.MkLt(left, right),
            ">=" => _ctx.MkGe(left, right),
            "<=" => _ctx.MkLe(left, right),
            "==" => _ctx.MkEq(left, right),
            "!=" => _ctx.MkNot(_ctx.MkEq(left, right)),
            _ => throw new ArgumentException($"Unknown operator: {op}")
        };
    }

    private ArithExpr ParseArithmetic(string expr, ref int pos)
    {
        SkipWhitespace(expr, ref pos);
        
        if (pos < expr.Length && expr[pos] == '(')
        {
            pos++; // Skip '('
            var result = ParseArithmetic(expr, ref pos);
            if (pos < expr.Length && expr[pos] == ')')
            {
                pos++; // Skip ')'
            }
            return result;
        }

        var left = ParseFactor(expr, ref pos);
        SkipWhitespace(expr, ref pos);

        while (pos < expr.Length && (expr[pos] == '+' || expr[pos] == '-' || expr[pos] == '*' || expr[pos] == '/'))
        {
            char opChar = expr[pos];
            pos++;
            SkipWhitespace(expr, ref pos);
            
            var right = ParseFactor(expr, ref pos);
            SkipWhitespace(expr, ref pos);

            left = opChar switch
            {
                '+' => _ctx.MkAdd(left, right),
                '-' => _ctx.MkSub(left, right),
                '*' => _ctx.MkMul(left, right),
                '/' => _ctx.MkDiv(left, right),
                _ => left
            };
        }

        return left;
    }

    private ArithExpr ParseFactor(string expr, ref int pos)
    {
        SkipWhitespace(expr, ref pos);
        
        if (pos < expr.Length && expr[pos] == '(')
        {
            pos++; // Skip '('
            var result = ParseArithmetic(expr, ref pos);
            if (pos < expr.Length && expr[pos] == ')')
            {
                pos++; // Skip ')'
            }
            return result;
        }

        // Parse variable or number
        int start = pos;
        while (pos < expr.Length && (char.IsLetterOrDigit(expr[pos]) || expr[pos] == '_'))
        {
            pos++;
        }

        if (pos == start)
        {
            throw new ArgumentException($"Expected variable or number at position {pos}");
        }

        string token = expr.Substring(start, pos - start);
        
        if (int.TryParse(token, out int value))
        {
            return _ctx.MkInt(value);
        }
        else
        {
            return GetOrCreateVariable(token);
        }
    }

    private string ParseOperator(string expr, ref int pos)
    {
        if (pos >= expr.Length) throw new ArgumentException("Unexpected end of expression");

        if (expr[pos] == '>' || expr[pos] == '<')
        {
            if (pos + 1 < expr.Length && expr[pos + 1] == '=')
            {
                string op = expr.Substring(pos, 2);
                pos += 2;
                return op;
            }
            else
            {
                string op = expr[pos].ToString();
                pos++;
                return op;
            }
        }
        else if (expr[pos] == '=')
        {
            if (pos + 1 < expr.Length && expr[pos + 1] == '=')
            {
                pos += 2;
                return "==";
            }
            else
            {
                throw new ArgumentException($"Unexpected character '=' at position {pos}");
            }
        }
        else if (expr[pos] == '!')
        {
            if (pos + 1 < expr.Length && expr[pos + 1] == '=')
            {
                pos += 2;
                return "!=";
            }
            else
            {
                throw new ArgumentException($"Unexpected character '!' at position {pos}");
            }
        }

        throw new ArgumentException($"Unknown operator at position {pos}");
    }

    private void SkipWhitespace(string expr, ref int pos)
    {
        // No-op since we remove all whitespace upfront
    }

    private ArithExpr GetOrCreateVariable(string name)
    {
        if (!_variables.ContainsKey(name))
        {
            _variables[name] = _ctx.MkIntConst(name);
        }
        return _variables[name];
    }

    public IReadOnlyDictionary<string, ArithExpr> Variables => _variables;
}