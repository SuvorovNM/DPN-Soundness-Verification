using System;
using Microsoft.Z3;
using NUnit.Framework;

[TestFixture]
public class BidirectionalZ3ExpressionTests
{
    private Context _ctx;

    [SetUp]
    public void SetUp()
    {
        _ctx = new Context();
    }

    [TearDown]
    public void TearDown()
    {
        _ctx?.Dispose();
    }

    [Test]
    public void RoundTrip_SimpleComparison()
    {
        TestRoundTrip("a > b");
    }

    [Test]
    public void RoundTrip_ComparisonWithParentheses()
    {
        TestRoundTrip("(a > b)");
    }

    [Test]
    public void RoundTrip_LogicalAnd()
    {
        TestRoundTrip("(a > b) && (b < 10)");
    }

    [Test]
    public void RoundTrip_LogicalOr()
    {
        TestRoundTrip("(a > b) || (b < 10)");
    }

    [Test]
    public void RoundTrip_ComplexNestedExpression()
    {
        TestRoundTrip("(((a > b) && (c == true)) || (b == 15.6) || (a < 10))");
    }

    [Test]
    public void RoundTrip_MultipleAndWithOr()
    {
        TestRoundTrip("((a > b) && (b < 10) && (a > 5) || (a < 3))");
    }

    [Test]
    public void RoundTrip_NotEqualOperator()
    {
        TestRoundTrip("a != b");
    }

    [Test]
    public void RoundTrip_EqualOperator()
    {
        TestRoundTrip("a == b");
    }

    [Test]
    public void RoundTrip_GreaterThanOrEqual()
    {
        TestRoundTrip("!(a >= b)");
    }

    [Test]
    public void RoundTrip_LessThanOrEqual()
    {
        TestRoundTrip("a <= b");
    }

    [Test]
    public void RoundTrip_ComplexMixedExpression()
    {
        TestRoundTrip("(x > 5) && (y < 10) || (z == 15) && (w != 20)");
    }

    [Test]
    public void RoundTrip_ExpressionWithArithmetic()
    {
        TestRoundTrip("(a + b) > (c * d)");
    }

    [Test]
    public void RoundTrip_NegativeNumbers()
    {
        TestRoundTrip("a > -5");
    }


    [Test]
    public void Serialize_SimpleAndExpression()
    {
        var serializer = new Z3ExpressionSerializer();
        
        // Create a simple AND expression manually
        var a = _ctx.MkIntConst("a");
        var b = _ctx.MkIntConst("b");
        var five = _ctx.MkInt(5);
        var ten = _ctx.MkInt(10);
        
        var expr1 = _ctx.MkGt(a, five);
        var expr2 = _ctx.MkLt(b, ten);
        var andExpr = _ctx.MkAnd(expr1, expr2);
        
        var result = serializer.Serialize(andExpr);
        
        Assert.That(result, Is.EqualTo("(a > 5) && (b < 10)"));
    }

    [Test]
    public void Serialize_SimpleOrExpression()
    {
        var serializer = new Z3ExpressionSerializer();
        
        var a = _ctx.MkIntConst("a");
        var b = _ctx.MkIntConst("b");
        var five = _ctx.MkInt(5);
        var ten = _ctx.MkInt(10);
        
        var expr1 = _ctx.MkGt(a, five);
        var expr2 = _ctx.MkLt(b, ten);
        var orExpr = _ctx.MkOr(expr1, expr2);
        
        var result = serializer.Serialize(orExpr);
        
        Assert.That(result, Is.EqualTo("(a > 5) || (b < 10)"));
    }

    [Test]
    public void Serialize_NotEqualExpression()
    {
        var serializer = new Z3ExpressionSerializer();
        
        var a = _ctx.MkIntConst("a");
        var b = _ctx.MkIntConst("b");
        var eqExpr = _ctx.MkEq(a, b);
        var notEqualExpr = _ctx.MkNot(eqExpr);
        
        var result = serializer.Serialize(notEqualExpr);
        
        Assert.That(result, Is.EqualTo("(a != b)"));
    }

    [Test]
    public void ParseThenSerialize_PreservesMeaning()
    {
        var parser = new Z3ExpressionParser(_ctx);
        var serializer = new Z3ExpressionSerializer();
        
        string original = "(a > 5) && (b < 10) || (c == 15)";
        var parsed = parser.Parse(original);
        var serialized = serializer.Serialize(parsed);
        
        // Parse the serialized version back
        var reparsed = parser.Parse(serialized);
        
        // Both expressions should be logically equivalent
        using (var solver = _ctx.MkSolver())
        {
            // Test that original → parsed and serialized → reparsed are equivalent
            solver.Add(_ctx.MkNot(_ctx.MkEq(parsed, reparsed)));
            var status = solver.Check();
            Assert.That(status, Is.EqualTo(Status.UNSATISFIABLE), 
                "Original and round-tripped expressions should be equivalent");
        }
    }

    private void TestRoundTrip(string expression)
    {
        var parser = new Z3ExpressionParser(_ctx);
        var serializer = new Z3ExpressionSerializer();
        
        try
        {
            var parsed = parser.Parse(expression);
            var serialized = serializer.Serialize(parsed);
            
            // The serialized version should be parseable
            var reparsed = parser.Parse(serialized);
            
            // Test logical equivalence
            using (var solver = _ctx.MkSolver())
            {
                solver.Add(_ctx.MkNot(_ctx.MkEq(parsed, reparsed)));
                var status = solver.Check();
                Assert.That(status, Is.EqualTo(Status.UNSATISFIABLE),
                    $"Round-trip failed for: {expression}");
            }
            
            TestContext.WriteLine($"Original: {expression}");
            TestContext.WriteLine($"Serialized: {serialized}");
            TestContext.WriteLine($"Round-trip successful");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Round-trip failed for '{expression}': {ex.Message}");
        }
    }
}