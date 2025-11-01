using System.Globalization;
using DPN.Models.Enums;
using Microsoft.Z3;

namespace DPN.Parsers;

internal class Z3ExpressionParser(Context ctx, Dictionary<string, DomainType> variablesToTypes)
{
	private readonly Dictionary<string, BoolExpr> _boolVariables = new();
    private readonly Dictionary<string, RealExpr> _realVariables = new();
    private readonly Dictionary<string, IntExpr> _intVariables = new();

    public BoolExpr Parse(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression) || expression.Equals("true", StringComparison.OrdinalIgnoreCase))
            return ctx.MkTrue();

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
                string token = expression.Substring(start, pos - start);
                
                if (token == "true" || token == "false")
                {
                    tokens.Add(token);
                }
                else
                {
                    tokens.Add(token);
                }
            }
            else if (char.IsDigit(c) || c == '.' || (c == '-' && (pos == 0 || IsOperatorOrParen(tokens.Last()))))
            {
                int start = pos;
                if (c == '-') pos++;
                
                bool hasDecimal = false;
                while (pos < expression.Length && (char.IsDigit(expression[pos]) || expression[pos] == '.'))
                {
                    if (expression[pos] == '.')
                    {
                        if (hasDecimal)
                            throw new ArgumentException("Invalid number: multiple decimal points");
                        hasDecimal = true;
                    }
                    pos++;
                }
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
                else if (c == '!')
                {
                    tokens.Add("!");
                    pos++;
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

    private bool IsOperatorOrParen(string token)
    {
        return token == "(" || token == "&&" || token == "||" || 
               token == ">" || token == "<" || token == ">=" || token == "<=" || token == "==" || token == "!=" ||
               token == "!" || token == "true" || token == "false";
    }

    private BoolExpr ParseExpression(List<string> tokens, ref int index)
    {
        var left = ParseAndExpression(tokens, ref index);
        
        while (index < tokens.Count && tokens[index] == "||")
        {
            index++;
            var right = ParseAndExpression(tokens, ref index);
            left = ctx.MkOr(left, right);
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
            left = ctx.MkAnd(left, right);
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

        if (index < tokens.Count && tokens[index] == "!")
        {
            index++;
            var operand = ParseComparison(tokens, ref index);
            return ctx.MkNot(operand);
        }

        var boolComparison = TryParseBooleanComparison(tokens, ref index);
        if (boolComparison != null)
        {
            return boolComparison;
        }

        var left = ParseNumericOperand(tokens, ref index);
        
        if (index >= tokens.Count || tokens[index] == ")" || tokens[index] == "&&" || tokens[index] == "||")
        {
            throw new ArgumentException("Expected comparison operator after numeric operand");
        }

        string op = tokens[index];
        if (!IsComparisonOperator(op))
            throw new ArgumentException($"Expected comparison operator, got: {op}");
        
        index++;
        
        var right = ParseNumericOperand(tokens, ref index);

        // All numerics are real, no type promotion needed
        return op switch
        {
            ">" => ctx.MkGt(left, right),
            "<" => ctx.MkLt(left, right),
            ">=" => ctx.MkGe(left, right),
            "<=" => ctx.MkLe(left, right),
            "==" => ctx.MkEq(left, right),
            "!=" => ctx.MkNot(ctx.MkEq(left, right)),
            _ => throw new ArgumentException($"Unknown operator: {op}")
        };
    }

    private BoolExpr TryParseBooleanComparison(List<string> tokens, ref int index)
    {
        int savedIndex = index;
        
        try
        {
            BoolExpr left = ParseBooleanOperand(tokens, ref index);
            
            if (index >= tokens.Count || !IsComparisonOperator(tokens[index]))
                return null;

            string op = tokens[index];
            index++;
            
            BoolExpr right = ParseBooleanOperand(tokens, ref index);
            
            return op switch
            {
                "==" => ctx.MkEq(left, right),
                "!=" => ctx.MkNot(ctx.MkEq(left, right)),
                _ => throw new ArgumentException($"Invalid boolean operator: {op}")
            };
        }
        catch
        {
            index = savedIndex;
            return null;
        }
    }

    private BoolExpr ParseBooleanOperand(List<string> tokens, ref int index)
    {
        if (index >= tokens.Count)
            throw new ArgumentException("Unexpected end of expression");

        if (tokens[index] == "(")
        {
            index++;
            var result = ParseExpression(tokens, ref index);
            if (index >= tokens.Count || tokens[index] != ")")
                throw new ArgumentException("Unbalanced parentheses");
            index++;
            return result;
        }

        if (tokens[index] == "!")
        {
            index++;
            var operand = ParseBooleanOperand(tokens, ref index);
            return ctx.MkNot(operand);
        }

        string token = tokens[index];
        index++;

        if (token.Equals("true",  StringComparison.InvariantCultureIgnoreCase))
        {
            return ctx.MkBool(true);
        }
        else if (token.Equals("false",  StringComparison.InvariantCultureIgnoreCase))
        {
            return ctx.MkBool(false);
        }
        else if (IsVariable(token))
        {
            return GetOrCreateBoolVariable(token);
        }
        else
        {
            throw new ArgumentException($"Expected boolean operand, got: {token}");
        }
    }

    private ArithExpr ParseNumericOperand(List<string> tokens, ref int index)
    {
        if (index >= tokens.Count)
            throw new ArgumentException("Unexpected end of expression");

        if (tokens[index] == "(")
        {
            index++;
            var result = ParseNumericOperand(tokens, ref index);
            if (index >= tokens.Count || tokens[index] != ")")
                throw new ArgumentException("Unbalanced parentheses");
            index++;
            return result;
        }

        if (tokens[index] == "-")
        {
            index++;
            var operand = ParseNumericOperand(tokens, ref index);
            if (operand.IsInt || operand.IsIntNum)
            {
                return (IntExpr)ctx.MkSub(ctx.MkInt(0), operand);
            }

            if (operand.IsReal || operand.IsRatNum)
            {
                return (RealExpr)ctx.MkSub(ctx.MkReal(0), operand);
            }
            
            throw new ArgumentException($"Invalid numeric operand: {tokens[index]}");
        }
        
        var secondOperand = index >= 1 && IsComparisonOperator(tokens[index - 1])
            ? tokens[index - 2]
            : tokens[index + 2];
        var domainType = _intVariables.ContainsKey(secondOperand)
            ? DomainType.Integer
            : DomainType.Real;

        string token = tokens[index];
        index++;

        if (int.TryParse(token, out int intValue))
        {
            // Convert integer literals to real
            return domainType == DomainType.Integer ? ctx.MkInt(intValue) :  ctx.MkReal(intValue);
        }
        else if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double realValue))
        {
            return domainType == DomainType.Integer ? 
                ctx.MkInt((int)realValue) : 
                ctx.MkReal(realValue.ToString(CultureInfo.InvariantCulture));
        }
        else if (IsVariable(token))
        {
            return GetOrCreateNumericVariable(token);
        }
        else
        {
            throw new ArgumentException($"Expected numeric operand, got: {token}");
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
               token.All(c => char.IsLetterOrDigit(c) || c == '_') &&
               token != "true" && token != "false";
    }

    private BoolExpr GetOrCreateBoolVariable(string name)
    {
        if (!_boolVariables.ContainsKey(name))
            _boolVariables[name] = ctx.MkBoolConst(name);
        return _boolVariables[name];
    }

    private ArithExpr GetOrCreateNumericVariable(string name)
    {
        if (!variablesToTypes.TryGetValue(name, out var type))
            throw new ArgumentException($"Undefined variable: {name}");
        
        switch (type)
        {
            case DomainType.Integer:
                if (!_intVariables.ContainsKey(name))
                    _intVariables[name] = ctx.MkIntConst(name);
                return _intVariables[name];
            case DomainType.Real:
                if (!_realVariables.ContainsKey(name))
                    _realVariables[name] = ctx.MkRealConst(name);
                return _realVariables[name];
            case DomainType.Boolean:
            default:
                throw new ArgumentOutOfRangeException("Unsupported variable type: " + type);
        }
    }
}