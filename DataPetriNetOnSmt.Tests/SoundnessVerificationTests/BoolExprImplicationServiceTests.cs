using DataPetriNetOnSmt.SoundnessVerification;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.Tests.SoundnessVerificationTests
{
    [TestClass]
    public class BoolExprImplicationServiceTests
    {
        private BoolExprImplicationService exprImplicationService;
        private string varNameToOverwrite = "intVarToOverwrite";
        private long upperBound = 10000;
        private long lowerBound = -10000;
        private long valueInsideBounds = 1;

        [TestInitialize]
        public void Initialize()
        {
            exprImplicationService = new BoolExprImplicationService();
        }

        #region GetImplicationOfGreaterExpression
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetImplicationOfGreaterExpressionWithEmptyExpressionGroup(bool includeEquality)
        {
            var varToOverwrite = ContextProvider.Context.MkConst("intVarToOverwrite", ContextProvider.Context.MkIntSort());
            var secondVar = ContextProvider.Context.MkConst("secondIntVar", ContextProvider.Context.MkIntSort());
            var emptyExpressionGroup = new List<BoolExpr>();

            var resultExpression = exprImplicationService.GetImplicationOfGreaterExpression(
                emptyExpressionGroup, 
                includeEquality, 
                varToOverwrite, 
                secondVar);

            Assert.IsTrue(resultExpression.IsTrue);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetImplicationOfGreaterExpressionWithNoVarToOverwriteInExpressionGroup(bool includeEquality)
        {
            var varToOverwrite = ContextProvider.Context.MkConst("intVarToOverwrite", ContextProvider.Context.MkIntSort());
            var secondVar = ContextProvider.Context.MkConst("secondIntVar", ContextProvider.Context.MkIntSort());
            var expressionGroupWithNoOverwritten = new List<BoolExpr>() { GetExpressionWithNoVarOverwritten() };

            var resultExpression = exprImplicationService.GetImplicationOfGreaterExpression(
                expressionGroupWithNoOverwritten,
                includeEquality,
                varToOverwrite,
                secondVar);

            Assert.IsTrue(resultExpression.IsTrue);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetImplicationOfGreaterExpressionWithNoLowerBound(bool includeEquality)
        {
            var varToOverwrite = ContextProvider.Context.MkConst(varNameToOverwrite, ContextProvider.Context.MkIntSort());
            var secondVar = ContextProvider.Context.MkConst("secondIntVar", ContextProvider.Context.MkIntSort());

            var expressionGroupWithNoLowerBound = new List<BoolExpr>() { GetExpressionWithVarOverwrittenUpperBound() };

            var resultExpression = exprImplicationService.GetImplicationOfGreaterExpression(
                expressionGroupWithNoLowerBound,
                includeEquality,
                varToOverwrite,
                secondVar);

            Assert.IsTrue(resultExpression.IsTrue);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetImplicationOfGreaterThanExpressionWithLowerBound(bool includeEquality)
        {
            var varToOverwrite = ContextProvider.Context.MkConst(varNameToOverwrite, ContextProvider.Context.MkIntSort());
            var secondVar = ContextProvider.Context.MkConst("secondIntVar", ContextProvider.Context.MkIntSort());

            var expressionGroupWithLowerBound = new List<BoolExpr>() { GetExpressionWithVarOverwrittenLowerBound() };

            var resultExpression = exprImplicationService.GetImplicationOfGreaterExpression(
                expressionGroupWithLowerBound,
                includeEquality,
                varToOverwrite,
                secondVar);

            Assert.IsTrue(includeEquality ? resultExpression.IsGE : resultExpression.IsGT);
            Assert.AreEqual(secondVar.ToString(), resultExpression.Args[0].ToString());
            Assert.AreEqual(lowerBound.ToString(), resultExpression.Args[1].ToString());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetImplicationOfGreaterThanExpressionBasedOnMultipleConstraints(bool includeEquality)
        {
            var varToOverwrite = ContextProvider.Context.MkConst(varNameToOverwrite, ContextProvider.Context.MkIntSort());
            var secondVar = ContextProvider.Context.MkConst("secondIntVar", ContextProvider.Context.MkIntSort());

            var expressionGroupWithMultipleConstraints = new List<BoolExpr>() 
            { 
                GetExpressionWithVarOverwrittenLowerBound(),
                GetExpressionWithVarOverwrittenInsideBounds(),
                GetExpressionWithVarOverwrittenUpperBound()
            };

            var resultExpression = exprImplicationService.GetImplicationOfGreaterExpression(
                expressionGroupWithMultipleConstraints,
                includeEquality,
                varToOverwrite,
                secondVar);

            Assert.IsTrue(includeEquality ? resultExpression.IsGE : resultExpression.IsGT);
            Assert.AreEqual(secondVar.ToString(), resultExpression.Args[0].ToString());
            Assert.AreEqual(valueInsideBounds.ToString(), resultExpression.Args[1].ToString());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetImplicationOfGreaterThanExpressionBasedOnUnsatisfiableConstraints(bool includeEquality)
        {
            var varToOverwrite = ContextProvider.Context.MkConst(varNameToOverwrite, ContextProvider.Context.MkIntSort());
            var secondVar = ContextProvider.Context.MkConst("secondIntVar", ContextProvider.Context.MkIntSort());

            var expressionGroupWithUnsatisfiableConstraints = new List<BoolExpr>()
            {
                ContextProvider.Context.MkNot(GetExpressionWithVarOverwrittenLowerBound()),
                GetExpressionWithVarOverwrittenInsideBounds(),
                ContextProvider.Context.MkNot(GetExpressionWithVarOverwrittenUpperBound())
            };

            var resultExpression = exprImplicationService.GetImplicationOfGreaterExpression(
                expressionGroupWithUnsatisfiableConstraints,
                includeEquality,
                varToOverwrite,
                secondVar);

            Assert.IsTrue(resultExpression.IsFalse);
        }

        #endregion

        #region GetImplicationOfLessExpression
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetImplicationOfLessExpressionWithEmptyExpressionGroup(bool includeEquality)
        {
            var varToOverwrite = ContextProvider.Context.MkConst("intVarToOverwrite", ContextProvider.Context.MkIntSort());
            var secondVar = ContextProvider.Context.MkConst("secondIntVar", ContextProvider.Context.MkIntSort());
            var emptyExpressionGroup = new List<BoolExpr>();

            var resultExpression = exprImplicationService.GetImplicationOfGreaterExpression(
                emptyExpressionGroup,
                includeEquality,
                varToOverwrite,
                secondVar);

            Assert.IsTrue(resultExpression.IsTrue);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetImplicationOfLessExpressionWithNoVarToOverwriteInExpressionGroup(bool includeEquality)
        {
            var varToOverwrite = ContextProvider.Context.MkConst("intVarToOverwrite", ContextProvider.Context.MkIntSort());
            var secondVar = ContextProvider.Context.MkConst("secondIntVar", ContextProvider.Context.MkIntSort());
            var expressionGroupWithNoOverwritten = new List<BoolExpr>() { GetExpressionWithNoVarOverwritten() };

            var resultExpression = exprImplicationService.GetImplicationOfLessExpression(
                expressionGroupWithNoOverwritten,
                includeEquality,
                varToOverwrite,
                secondVar);

            Assert.IsTrue(resultExpression.IsTrue);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetImplicationOfLessExpressionWithNoUpperBound(bool includeEquality)
        {
            var varToOverwrite = ContextProvider.Context.MkConst(varNameToOverwrite, ContextProvider.Context.MkIntSort());
            var secondVar = ContextProvider.Context.MkConst("secondIntVar", ContextProvider.Context.MkIntSort());

            var expressionGroupWithNoLowerBound = new List<BoolExpr>() { GetExpressionWithVarOverwrittenLowerBound() };

            var resultExpression = exprImplicationService.GetImplicationOfLessExpression(
                expressionGroupWithNoLowerBound,
                includeEquality,
                varToOverwrite,
                secondVar);

            Assert.IsTrue(resultExpression.IsTrue);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetImplicationOfLessExpressionWithUpperBound(bool includeEquality)
        {
            var varToOverwrite = ContextProvider.Context.MkConst(varNameToOverwrite, ContextProvider.Context.MkIntSort());
            var secondVar = ContextProvider.Context.MkConst("secondIntVar", ContextProvider.Context.MkIntSort());

            var expressionGroupWithLowerBound = new List<BoolExpr>() { GetExpressionWithVarOverwrittenUpperBound() };

            var resultExpression = exprImplicationService.GetImplicationOfLessExpression(
                expressionGroupWithLowerBound,
                includeEquality,
                varToOverwrite,
                secondVar);

            Assert.IsTrue(includeEquality ? resultExpression.IsLE : resultExpression.IsLT);
            Assert.AreEqual(secondVar.ToString(), resultExpression.Args[0].ToString());
            Assert.AreEqual(upperBound.ToString(), resultExpression.Args[1].ToString());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetImplicationOfLessExpressionBasedOnMultipleConstraints(bool includeEquality)
        {
            var varToOverwrite = ContextProvider.Context.MkConst(varNameToOverwrite, ContextProvider.Context.MkIntSort());
            var secondVar = ContextProvider.Context.MkConst("secondIntVar", ContextProvider.Context.MkIntSort());

            var expressionGroupWithMultipleConstraints = new List<BoolExpr>()
            {
                GetExpressionWithVarOverwrittenLowerBound(),
                GetExpressionWithVarOverwrittenInsideBounds(),
                GetExpressionWithVarOverwrittenUpperBound()
            };

            var resultExpression = exprImplicationService.GetImplicationOfLessExpression(
                expressionGroupWithMultipleConstraints,
                includeEquality,
                varToOverwrite,
                secondVar);

            Assert.IsTrue(includeEquality ? resultExpression.IsLE : resultExpression.IsLT);
            Assert.AreEqual(secondVar.ToString(), resultExpression.Args[0].ToString());
            Assert.AreEqual(valueInsideBounds.ToString(), resultExpression.Args[1].ToString());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetImplicationOfLessExpressionBasedOnUnsatisfiableConstraints(bool includeEquality)
        {
            var varToOverwrite = ContextProvider.Context.MkConst(varNameToOverwrite, ContextProvider.Context.MkIntSort());
            var secondVar = ContextProvider.Context.MkConst("secondIntVar", ContextProvider.Context.MkIntSort());

            var expressionGroupWithUnsatisfiableConstraints = new List<BoolExpr>()
            {
                ContextProvider.Context.MkNot(GetExpressionWithVarOverwrittenLowerBound()),
                GetExpressionWithVarOverwrittenInsideBounds(),
                ContextProvider.Context.MkNot(GetExpressionWithVarOverwrittenUpperBound())
            };

            var resultExpression = exprImplicationService.GetImplicationOfLessExpression(
                expressionGroupWithUnsatisfiableConstraints,
                includeEquality,
                varToOverwrite,
                secondVar);

            Assert.IsTrue(resultExpression.IsFalse);
        }

        #endregion

        #region GetImplicationOfInequality
        [TestMethod]
        public void GetImplicationOfInequalityExpressionWithEmptyExpressionGroup()
        {
            var varToOverwrite = ContextProvider.Context.MkConst("intVarToOverwrite", ContextProvider.Context.MkIntSort());
            var secondVar = ContextProvider.Context.MkConst("secondIntVar", ContextProvider.Context.MkIntSort());
            var emptyExpressionGroup = new List<BoolExpr>();

            var resultExpression = exprImplicationService.GetImplicationOfInequalityExpression(
                emptyExpressionGroup,
                varToOverwrite,
                secondVar);

            Assert.IsNull(resultExpression);
        }

        [TestMethod]
        public void GetImplicationOfInequalityExpressionWithNoVarToOverwriteInExpressionGroup()
        {
            var varToOverwrite = ContextProvider.Context.MkConst("intVarToOverwrite", ContextProvider.Context.MkIntSort());
            var secondVar = ContextProvider.Context.MkConst("secondIntVar", ContextProvider.Context.MkIntSort());
            var expressionGroupWithNoOverwritten = new List<BoolExpr>() { GetExpressionWithNoVarOverwritten() };

            var resultExpression = exprImplicationService.GetImplicationOfInequalityExpression(
                expressionGroupWithNoOverwritten,
                varToOverwrite,
                secondVar);

            Assert.IsNull(resultExpression);
        }

        [TestMethod]
        [Description("Currently, if more than 1 value possible for variable overwritten, nothing can be obtained as a result of implication")]
        public void GetImplicationOfInequalityExpressionWithManyPossibleValuesForVarToOverwriteInExpressionGroup()
        {
            var varToOverwrite = ContextProvider.Context.MkConst("intVarToOverwrite", ContextProvider.Context.MkIntSort());
            var secondVar = ContextProvider.Context.MkConst("secondIntVar", ContextProvider.Context.MkIntSort());

            var expressionGroupWithMultipleConstraints = new List<BoolExpr>()
            {
                GetExpressionWithVarOverwrittenLowerBound(),
                GetExpressionWithVarOverwrittenUpperBound()
            };

            var resultExpression = exprImplicationService.GetImplicationOfInequalityExpression(
                expressionGroupWithMultipleConstraints,
                varToOverwrite,
                secondVar);

            Assert.IsNull(resultExpression);
        }

        [TestMethod]
        public void GetImplicationOfInequalityExpressionWithOnePossibleValueForVarToOverwriteAsEqualityInExpressionGroup()
        {
            var varToOverwrite = ContextProvider.Context.MkConst("intVarToOverwrite", ContextProvider.Context.MkIntSort());
            var secondVar = ContextProvider.Context.MkConst("secondIntVar", ContextProvider.Context.MkIntSort());
            var expressionGroupWithNoOverwritten = new List<BoolExpr>() { GetExpressionWithVarOverwrittenInsideBounds() };

            var resultExpression = exprImplicationService.GetImplicationOfInequalityExpression(
                expressionGroupWithNoOverwritten,
                varToOverwrite,
                secondVar);

            Assert.IsTrue(resultExpression.IsNot && resultExpression.Args[0].IsEq);
            Assert.AreEqual(secondVar.ToString(), resultExpression.Args[0].Args[0].ToString());
            Assert.AreEqual(valueInsideBounds.ToString(), resultExpression.Args[0].Args[1].ToString());
        }

        [TestMethod]
        public void GetImplicationOfInequalityExpressionWithOnePossibleValueForVarToOverwriteAsInequalitiesInExpressionGroup()
        {
            var varToOverwrite = ContextProvider.Context.MkConst("intVarToOverwrite", ContextProvider.Context.MkIntSort());
            var secondVar = ContextProvider.Context.MkConst("secondIntVar", ContextProvider.Context.MkIntSort());

            var eqExpression = GetExpressionWithVarOverwrittenInsideBounds();
            var geExpression = ContextProvider.Context.MkGe((ArithExpr)eqExpression.Args[0], (ArithExpr)eqExpression.Args[1]);
            var leExpression = ContextProvider.Context.MkLe((ArithExpr)eqExpression.Args[0], (ArithExpr)eqExpression.Args[1]);

            var expressionGroupWithNoOverwritten = new List<BoolExpr>() 
            {
                geExpression,
                leExpression
            };

            var resultExpression = exprImplicationService.GetImplicationOfInequalityExpression(
                expressionGroupWithNoOverwritten,
                varToOverwrite,
                secondVar);

            Assert.IsTrue(resultExpression.IsNot && resultExpression.Args[0].IsEq);
            Assert.AreEqual(secondVar.ToString(), resultExpression.Args[0].Args[0].ToString());
            Assert.AreEqual(valueInsideBounds.ToString(), resultExpression.Args[0].Args[1].ToString());
        }

        [TestMethod]
        public void GetImplicationOfInequalityExpressionWithUnsatisfiableConstraintsInExpressionGroup()
        {
            var varToOverwrite = ContextProvider.Context.MkConst("intVarToOverwrite", ContextProvider.Context.MkIntSort());
            var secondVar = ContextProvider.Context.MkConst("secondIntVar", ContextProvider.Context.MkIntSort());
            var expressionGroupWithNoOverwritten = new List<BoolExpr>() 
            {
                ContextProvider.Context.MkNot(GetExpressionWithVarOverwrittenLowerBound()),
                GetExpressionWithVarOverwrittenInsideBounds(),
                ContextProvider.Context.MkNot(GetExpressionWithVarOverwrittenUpperBound())
            };

            var resultExpression = exprImplicationService.GetImplicationOfInequalityExpression(
                expressionGroupWithNoOverwritten,
                varToOverwrite,
                secondVar);

            Assert.IsNull(resultExpression);
        }

        #endregion

        private BoolExpr GetExpressionWithNoVarOverwritten()
        {
            var intVariable = ContextProvider.Context.MkConst("varNotOverwritten", ContextProvider.Context.MkIntSort());
            var constant = ContextProvider.Context.MkInt(-1);

            return ContextProvider.Context.MkLe((ArithExpr)intVariable, constant);
        }

        private BoolExpr GetExpressionWithVarOverwrittenUpperBound()
        {
            var varToOverwrite = ContextProvider.Context.MkConst(varNameToOverwrite, ContextProvider.Context.MkIntSort());
            var upperBoundExpr = ContextProvider.Context.MkInt(upperBound);

            return ContextProvider.Context.MkLe((ArithExpr)varToOverwrite, upperBoundExpr);
        }

        private BoolExpr GetExpressionWithVarOverwrittenLowerBound()
        {
            var varToOverwrite = ContextProvider.Context.MkConst(varNameToOverwrite, ContextProvider.Context.MkIntSort());
            var lowerBoundExpr = ContextProvider.Context.MkInt(lowerBound);

            return ContextProvider.Context.MkGe((ArithExpr)varToOverwrite, lowerBoundExpr);
        }

        private BoolExpr GetExpressionWithVarOverwrittenInsideBounds()
        {
            var varToOverwrite = ContextProvider.Context.MkConst(varNameToOverwrite, ContextProvider.Context.MkIntSort());
            var insideValueExpr = ContextProvider.Context.MkInt(valueInsideBounds);

            return ContextProvider.Context.MkEq(varToOverwrite, insideValueExpr);
        }
    }
}
