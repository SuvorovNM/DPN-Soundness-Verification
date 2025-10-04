using Microsoft.Z3;

namespace Test;

public class BooleanComparisonTests
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
    public void Parse_BooleanEqualityTrue()
    {
        var parser = new Z3ExpressionParser(_ctx);
        var result = parser.Parse("a == true");
        
        Assert.That(result.ToString(), Is.EqualTo("(= a true)"));
    }

    [Test]
    public void Parse_BooleanEqualityFalse()
    {
        var parser = new Z3ExpressionParser(_ctx);
        var result = parser.Parse("b == false");
        
        Assert.That(result.ToString(), Is.EqualTo("(= b false)"));
    }

    [Test]
    public void Parse_BooleanNotEqualTrue()
    {
        var parser = new Z3ExpressionParser(_ctx);
        var result = parser.Parse("a != true");
        
        Assert.That(result.ToString(), Is.EqualTo("(not (= a true))"));
    }

    [Test]
    public void Parse_BooleanNotEqualFalse()
    {
        var parser = new Z3ExpressionParser(_ctx);
        var result = parser.Parse("b != false");
        
        Assert.That(result.ToString(), Is.EqualTo("(not (= b false))"));
    }

    [Test]
    public void Parse_MixedBooleanAndArithmetic()
    {
        var parser = new Z3ExpressionParser(_ctx);
        var result = parser.Parse("a != true && !(c < 5.0)");

        var serializer = new Z3ExpressionSerializer();
        var r = serializer.Serialize(result);
        
        Assert.That(result.ToString(), Is.EqualTo("(and (not (= a true)) (not (< c 5.0)))"));
    }

    [Test]
    public void Parse_ComplexBooleanExpression()
    {
        var parser = new Z3ExpressionParser(_ctx);
        var result = parser.Parse("(a == true) || (b == false) && (c != true)");
        
        Assert.That(result.ToString(), Is.EqualTo("(or (= a true) (and (= b false) (not (= c true))))"));
    }

    [Test]
    public void Parse_BooleanLiteralsInParentheses()
    {
        var parser = new Z3ExpressionParser(_ctx);
        var result = parser.Parse("(true == a) && (false != b)");
        
        Assert.That(result.ToString(), Is.EqualTo("(and (= true a) (not (= false b)))"));
    }

    [Test]
    public void Serialize_BooleanEquality()
    {
        var serializer = new Z3ExpressionSerializer();
        
        var a = _ctx.MkBoolConst("a");
        var eqExpr = _ctx.MkEq(a, _ctx.MkTrue());
        
        var result = serializer.Serialize(eqExpr);
        
        Assert.That(result, Is.EqualTo("(a == true)"));
    }

    [Test]
    public void Serialize_BooleanNotEqual()
    {
        var serializer = new Z3ExpressionSerializer();
        
        var b = _ctx.MkBoolConst("b");
        var notEqExpr = _ctx.MkNot(_ctx.MkEq(b, _ctx.MkFalse()));
        
        var result = serializer.Serialize(notEqExpr);
        
        Assert.That(result, Is.EqualTo("(b != false)"));
    }

    [Test]
    public void Serialize_MixedBooleanExpression()
    {
        var serializer = new Z3ExpressionSerializer();
        
        var a = _ctx.MkBoolConst("a");
        var b = _ctx.MkBoolConst("b");
        var c = _ctx.MkIntConst("c");
        var five = _ctx.MkInt(5);
        
        var boolExpr = _ctx.MkNot(_ctx.MkEq(a, _ctx.MkTrue()));
        var arithExpr = _ctx.MkLt(c, five);
        var andExpr = _ctx.MkAnd(boolExpr, arithExpr);
        
        var result = serializer.Serialize(andExpr);
        
        Assert.That(result, Is.EqualTo("(a != true) && (c < 5)"));
    }

    [Test]
    public void RoundTrip_BooleanComparisons()
    {
        var testCases = new[]
        {
            "a == true",
            "b == false", 
            "c != true",
            "d != false",
            "a == true && b == false",
            "(x == true) || (y != false) && (z == true)",
            "flag == true && count > 5",
            "isValid != false || value < 10"
        };

        var parser = new Z3ExpressionParser(_ctx);
        var serializer = new Z3ExpressionSerializer();

        foreach (var testCase in testCases)
        {
            var parsed = parser.Parse(testCase);
            var serialized = serializer.Serialize(parsed);
            var reparsed = parser.Parse(serialized);
            
            using (var solver = _ctx.MkSolver())
            {
                solver.Add(_ctx.MkNot(_ctx.MkEq(parsed, reparsed)));
                var status = solver.Check();
                Assert.That(status, Is.EqualTo(Status.UNSATISFIABLE), 
                    $"Round-trip failed for: {testCase}");
            }
            
            TestContext.WriteLine($"Original:  {testCase}");
            TestContext.WriteLine($"Serialized: {serialized}");
            TestContext.WriteLine("");
        }
    }

    [Test]
    public void Parse_InvalidBooleanExpression_ThrowsException()
    {
        var parser = new Z3ExpressionParser(_ctx);
        
        // This should fail because you can't compare boolean with arithmetic
        Assert.That(() => parser.Parse("a == true > 5"), Throws.ArgumentException);
    }
}