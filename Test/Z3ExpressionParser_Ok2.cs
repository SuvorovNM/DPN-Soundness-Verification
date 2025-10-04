using Microsoft.Z3;

public class Z3ExpressionParser_Ok2
{
    private readonly Context _ctx;
    private readonly Dictionary<string, ArithExpr> _variables;

    public Z3ExpressionParser_Ok2(Context ctx)
    {
        _ctx = ctx;
        _variables = new Dictionary<string, ArithExpr>();
    }

    public BoolExpr Parse(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Expression cannot be empty");

        var tokens = Tokenize(expression);
        int index = 0;
        var result = ParseExpression(tokens, ref index);
        
        if (index != tokens.Count)
            throw new ArgumentException($"Unexpected token: {tokens[index]}");

        return result;
    }

    private List<string> Tokenize(string expression)
    {
        var tokens = new List<string>();
        int pos = 0;
        
        while (pos < expression.Length)
        {
            char c = expression[pos];
            
            if (char.IsWhiteSpace(c))
            {
                pos++;
                continue;
            }
            
            if (char.IsLetter(c) || c == '_')
            {
                int start = pos;
                while (pos < expression.Length && (char.IsLetterOrDigit(expression[pos]) || expression[pos] == '_'))
                    pos++;
                tokens.Add(expression.Substring(start, pos - start));
            }
            else if (char.IsDigit(c))
            {
                int start = pos;
                while (pos < expression.Length && char.IsDigit(expression[pos]))
                    pos++;
                tokens.Add(expression.Substring(start, pos - start));
            }
            else if (c == '(' || c == ')')
            {
                tokens.Add(c.ToString());
                pos++;
            }
            else if (c == '>' || c == '<' || c == '=' || c == '!')
            {
                if (pos + 1 < expression.Length && expression[pos + 1] == '=')
                {
                    tokens.Add(expression.Substring(pos, 2));
                    pos += 2;
                }
                else
                {
                    tokens.Add(c.ToString());
                    pos++;
                }
            }
            else if (c == '&' && pos + 1 < expression.Length && expression[pos + 1] == '&')
            {
                tokens.Add("&&");
                pos += 2;
            }
            else if (c == '|' && pos + 1 < expression.Length && expression[pos + 1] == '|')
            {
                tokens.Add("||");
                pos += 2;
            }
            else
            {
                throw new ArgumentException($"Unexpected character: {c}");
            }
        }
        
        return tokens;
    }

    private BoolExpr ParseExpression(List<string> tokens, ref int index)
    {
        var left = ParseAndExpression(tokens, ref index);
        
        while (index < tokens.Count && tokens[index] == "||")
        {
            index++;
            var right = ParseAndExpression(tokens, ref index);
            left = _ctx.MkOr(left, right);
        }
        
        return left;
    }

    private BoolExpr ParseAndExpression(List<string> tokens, ref int index)
    {
        var left = ParseComparison(tokens, ref index);
        
        while (index < tokens.Count && tokens[index] == "&&")
        {
            index++;
            var right = ParseComparison(tokens, ref index);
            left = _ctx.MkAnd(left, right);
        }
        
        return left;
    }

    private BoolExpr ParseComparison(List<string> tokens, ref int index)
    {
        if (index < tokens.Count && tokens[index] == "(")
        {
            index++;
            var result = ParseExpression(tokens, ref index);
            if (index >= tokens.Count || tokens[index] != ")")
                throw new ArgumentException("Unbalanced parentheses");
            index++;
            return result;
        }

        var left = ParseOperand(tokens, ref index);
        
        if (index >= tokens.Count || tokens[index] == ")" || tokens[index] == "&&" || tokens[index] == "||")
        {
            throw new ArgumentException($"Expected comparison operator after {left}");
        }

        string op = tokens[index];
        if (!IsComparisonOperator(op))
            throw new ArgumentException($"Expected comparison operator, got: {op}");
        
        index++;
        
        var right = ParseOperand(tokens, ref index);

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

    private ArithExpr ParseOperand(List<string> tokens, ref int index)
    {
        if (index >= tokens.Count)
            throw new ArgumentException("Unexpected end of expression");

        if (tokens[index] == "(")
        {
            // This shouldn't happen in comparisons without arithmetic, but handle it
            index++;
            var result = ParseOperand(tokens, ref index); // Simple recursion for nested parentheses
            if (index >= tokens.Count || tokens[index] != ")")
                throw new ArgumentException("Unbalanced parentheses");
            index++;
            return result;
        }

        string token = tokens[index];
        index++;

        if (int.TryParse(token, out int value))
        {
            return _ctx.MkInt(value);
        }
        else if (IsVariable(token))
        {
            return GetOrCreateVariable(token);
        }
        else
        {
            throw new ArgumentException($"Unexpected token: {token}");
        }
    }

    private bool IsComparisonOperator(string token)
    {
        return token == ">" || token == "<" || token == ">=" || token == "<=" || token == "==" || token == "!=";
    }

    private bool IsVariable(string token)
    {
        return !string.IsNullOrEmpty(token) && 
               char.IsLetter(token[0]) && 
               token.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private ArithExpr GetOrCreateVariable(string name)
    {
        if (!_variables.ContainsKey(name))
            _variables[name] = _ctx.MkIntConst(name);
        return _variables[name];
    }

    public IReadOnlyDictionary<string, ArithExpr> Variables => _variables;
}