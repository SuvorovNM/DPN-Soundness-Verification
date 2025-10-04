using System;
using System.Collections.Generic;
using Microsoft.Z3;
using System.Linq;
using System.Globalization;

public class Z3ExpressionParser
{
    private readonly Context _ctx;
    private readonly Dictionary<string, BoolExpr> _boolVariables;
    private readonly Dictionary<string, RealExpr> _realVariables;

    public Z3ExpressionParser(Context ctx)
    {
        _ctx = ctx;
        _boolVariables = new Dictionary<string, BoolExpr>();
        _realVariables = new Dictionary<string, RealExpr>();
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

        if (index < tokens.Count && tokens[index] == "!")
        {
            index++;
            var operand = ParseComparison(tokens, ref index);
            return _ctx.MkNot(operand);
        }

        var boolComparison = TryParseBooleanComparison(tokens, ref index);
        if (boolComparison != null)
        {
            return boolComparison;
        }

        var left = ParseRealOperand(tokens, ref index);
        
        if (index >= tokens.Count || tokens[index] == ")" || tokens[index] == "&&" || tokens[index] == "||")
        {
            throw new ArgumentException("Expected comparison operator after numeric operand");
        }

        string op = tokens[index];
        if (!IsComparisonOperator(op))
            throw new ArgumentException($"Expected comparison operator, got: {op}");
        
        index++;
        
        var right = ParseRealOperand(tokens, ref index);

        // All numerics are real, no type promotion needed
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
                "==" => _ctx.MkEq(left, right),
                "!=" => _ctx.MkNot(_ctx.MkEq(left, right)),
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
            return _ctx.MkNot(operand);
        }

        string token = tokens[index];
        index++;

        if (token == "true")
        {
            return _ctx.MkTrue();
        }
        else if (token == "false")
        {
            return _ctx.MkFalse();
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

    private RealExpr ParseRealOperand(List<string> tokens, ref int index)
    {
        if (index >= tokens.Count)
            throw new ArgumentException("Unexpected end of expression");

        if (tokens[index] == "(")
        {
            index++;
            var result = ParseRealOperand(tokens, ref index);
            if (index >= tokens.Count || tokens[index] != ")")
                throw new ArgumentException("Unbalanced parentheses");
            index++;
            return result;
        }

        if (tokens[index] == "-")
        {
            index++;
            var operand = ParseRealOperand(tokens, ref index);
            return (RealExpr)_ctx.MkSub(_ctx.MkReal(0), operand);
        }

        string token = tokens[index];
        index++;

        if (int.TryParse(token, out int intValue))
        {
            // Convert integer literals to real
            return _ctx.MkReal(intValue);
        }
        else if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double realValue))
        {
            return _ctx.MkReal(realValue.ToString(CultureInfo.InvariantCulture));
        }
        else if (IsVariable(token))
        {
            return GetOrCreateRealVariable(token);
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
            _boolVariables[name] = _ctx.MkBoolConst(name);
        return _boolVariables[name];
    }

    private RealExpr GetOrCreateRealVariable(string name)
    {
        if (!_realVariables.ContainsKey(name))
            _realVariables[name] = _ctx.MkRealConst(name);
        return _realVariables[name];
    }

    public IReadOnlyDictionary<string, BoolExpr> BoolVariables => _boolVariables;
    public IReadOnlyDictionary<string, RealExpr> RealVariables => _realVariables;
}









/*public class Z3ExpressionParser
{
    private readonly Context _ctx;
    private readonly Dictionary<string, BoolExpr> _boolVariables;
    private readonly Dictionary<string, ArithExpr> _intVariables;
    private readonly Dictionary<string, ArithExpr> _realVariables;

    public Z3ExpressionParser(Context ctx)
    {
        _ctx = ctx;
        _boolVariables = new Dictionary<string, BoolExpr>();
        _intVariables = new Dictionary<string, ArithExpr>();
        _realVariables = new Dictionary<string, ArithExpr>();
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

        if (index < tokens.Count && tokens[index] == "!")
        {
            index++;
            var operand = ParseComparison(tokens, ref index);
            return _ctx.MkNot(operand);
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

        // Handle type compatibility
        if (left is IntExpr leftInt && right is RealExpr rightReal)
        {
            left = _ctx.MkInt2Real(leftInt);
        }
        else if (left is RealExpr leftReal && right is IntExpr rightInt)
        {
            right = _ctx.MkInt2Real(rightInt);
        }

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
                "==" => _ctx.MkEq(left, right),
                "!=" => _ctx.MkNot(_ctx.MkEq(left, right)),
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
            return _ctx.MkNot(operand);
        }

        string token = tokens[index];
        index++;

        if (token == "true")
        {
            return _ctx.MkTrue();
        }
        else if (token == "false")
        {
            return _ctx.MkFalse();
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
            if (operand is IntExpr intExpr)
                return _ctx.MkSub(_ctx.MkInt(0), intExpr);
            else
                return _ctx.MkSub(_ctx.MkReal(0), (RealExpr)operand);
        }

        string token = tokens[index];
        index++;

        if (int.TryParse(token, out int intValue))
        {
            return _ctx.MkInt(intValue);
        }
        else if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double realValue))
        {
            return _ctx.MkReal(realValue.ToString(CultureInfo.InvariantCulture));
        }
        else if (IsVariable(token))
        {
            // Check if variable already exists with a specific type
            if (_intVariables.ContainsKey(token))
                return _intVariables[token];
            else if (_realVariables.ContainsKey(token))
                return _realVariables[token];
            else
            {
                // New variable - look ahead to infer type from comparison
                return InferAndCreateVariableType(tokens, index, token);
            }
        }
        else
        {
            throw new ArgumentException($"Expected numeric operand, got: {token}");
        }
    }

    private ArithExpr InferAndCreateVariableType(List<string> tokens, int currentIndex, string variableName)
    {
        // Look ahead to see what we're comparing with
        for (int i = currentIndex; i < tokens.Count; i++)
        {
            if (tokens[i] == ")" || tokens[i] == "&&" || tokens[i] == "||")
                break;

            if (IsComparisonOperator(tokens[i]))
            {
                // Found a comparison operator, check the next token
                if (i + 1 < tokens.Count)
                {
                    string nextToken = tokens[i + 1];
                    if (int.TryParse(nextToken, out _))
                    {
                        // Comparing with integer - create integer variable
                        return GetOrCreateIntVariable(variableName);
                    }
                    else if (double.TryParse(nextToken, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    {
                        // Comparing with real - create real variable
                        return GetOrCreateRealVariable(variableName);
                    }
                    else if (IsVariable(nextToken))
                    {
                        // Comparing with another variable - check if that variable has a type
                        if (_intVariables.ContainsKey(nextToken))
                            return GetOrCreateIntVariable(variableName);
                        else if (_realVariables.ContainsKey(nextToken))
                            return GetOrCreateRealVariable(variableName);
                    }
                }
                break;
            }
        }

        // Default to integer if we can't infer from context
        return GetOrCreateIntVariable(variableName);
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
            _boolVariables[name] = _ctx.MkBoolConst(name);
        return _boolVariables[name];
    }

    private ArithExpr GetOrCreateIntVariable(string name)
    {
        if (!_intVariables.ContainsKey(name))
            _intVariables[name] = _ctx.MkIntConst(name);
        return _intVariables[name];
    }

    private ArithExpr GetOrCreateRealVariable(string name)
    {
        if (!_realVariables.ContainsKey(name))
            _realVariables[name] = _ctx.MkRealConst(name);
        return _realVariables[name];
    }

    public IReadOnlyDictionary<string, BoolExpr> BoolVariables => _boolVariables;
    public IReadOnlyDictionary<string, ArithExpr> IntVariables => _intVariables;
    public IReadOnlyDictionary<string, ArithExpr> RealVariables => _realVariables;
}*/