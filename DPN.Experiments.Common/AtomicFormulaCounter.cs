using Microsoft.Z3;

namespace DPN.Experiments.Common;

public static class AtomicFormulaCounter
{
    public static int CountAtomicFormulas(BoolExpr expr)
    {
        var visitedExpressions = new HashSet<Expr>();
        return CountAtomicFormulasRecursive(expr, visitedExpressions);
    }

    private static int CountAtomicFormulasRecursive(Expr expr, HashSet<Expr> visitedExpressions)
    {
	    // if (expr == null || visitedExpressions.Contains(expr))
        //    return 0;

        visitedExpressions.Add(expr);

        // Check if this is an atomic formula
        if (IsAtomicFormula(expr))
            return 1;

        // For boolean expressions, recursively check children
        if (expr is BoolExpr boolExpr)
        {
            int count = 0;
            foreach (var child in boolExpr.Args)
            {
                count += CountAtomicFormulasRecursive(child, visitedExpressions);
            }
            return count;
        }

        return 0;
    }

    private static bool IsAtomicFormula(Expr expr)
    {
        // Handle negations - check if the inner expression is atomic
        if (expr is BoolExpr boolExpr)
        {
            if (boolExpr.IsNot)
            {
                // For negation, check if the operand is atomic
                if (boolExpr.Args.Length == 1)
                {
                    return IsAtomicComparison(boolExpr.Args[0]);
                }
                return false;
            }

            // Check if it's a direct atomic comparison
            return IsAtomicComparison(expr);
        }

        return false;
    }

    private static bool IsAtomicComparison(Expr expr)
    {
        // Check if this is a relational operation (<, >, <=, >=, ==, !=)
        if (expr is BoolExpr boolExpr && boolExpr.IsApp)
        {
            var decl = boolExpr.FuncDecl;
            var declKind = decl.DeclKind;

            // Check if it's one of the relational operators
            if (declKind == Z3_decl_kind.Z3_OP_LT || declKind == Z3_decl_kind.Z3_OP_LE ||
                declKind == Z3_decl_kind.Z3_OP_GT || declKind == Z3_decl_kind.Z3_OP_GE ||
                declKind == Z3_decl_kind.Z3_OP_SLT || declKind == Z3_decl_kind.Z3_OP_SLEQ ||
                declKind == Z3_decl_kind.Z3_OP_SGT || declKind == Z3_decl_kind.Z3_OP_SGEQ ||
                declKind == Z3_decl_kind.Z3_OP_EQ || declKind == Z3_decl_kind.Z3_OP_DISTINCT)
            {
                // Verify it has exactly 2 arguments
                if (boolExpr.Args.Length == 2)
                {
                    // Check that both arguments are either variables or constants
                    return IsVariableOrConstant(boolExpr.Args[0]) && 
                           IsVariableOrConstant(boolExpr.Args[1]);
                }
            }
        }

        return false;
    }

    private static bool IsVariableOrConstant(Expr expr)
    {
        // Check if expression is a variable (uninterpreted constant)
        if (expr.IsApp && expr.FuncDecl.DeclKind == Z3_decl_kind.Z3_OP_UNINTERPRETED)
            return true;

        // Check if expression is a constant (numeral, etc.)
        if (expr.IsNumeral || expr.IsBool || expr.IsBVNumeral)
            return true;

        // Check for other constant types
        var declKind = expr.FuncDecl.DeclKind;
        return declKind == Z3_decl_kind.Z3_OP_TRUE ||
               declKind == Z3_decl_kind.Z3_OP_FALSE ||
               expr.IsConst;
    }
    
}