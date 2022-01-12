using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.Tests.SoundnessVerificationTests
{
    [TestClass]
    public class ConcatExpressionsTests
    {
        private readonly string a = "a";
        private readonly string b = "b";
        private readonly string c = "c";
        private readonly string d = "d";

        private ConstraintExpressionOperationService constraintExpressionOperationService;

        [TestInitialize]
        public void Initialize()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            constraintExpressionOperationService = new ConstraintExpressionOperationService();
        }

        [TestMethod]
        public void ConcatSourceExpressionWithEmptyList()
        {
            var sourceExpression = MakeVOCConstraintsExpression();
            var emptyTargetExpressionList = new List<IConstraintExpression>();

            var resultExpression = constraintExpressionOperationService.ConcatExpressions(sourceExpression, emptyTargetExpressionList);

            Assert.AreEqual(sourceExpression, resultExpression);
        }

        #region VOCExpressions

        [TestMethod]
        [DataRow(VariableType.Read)]
        [DataRow(VariableType.Written)]
        public void ConcatVOCExpressionWithSingleVOCExpressionForNonusedVar(VariableType varType)
        {
            var sourceExpression = MakeVOCConstraintsExpression();
            var targetExpressionListWithSingleExpression = new List<IConstraintExpression> { MakeVOCConstraintForNonusedVar(varType) };

            var resultExpression = constraintExpressionOperationService.ConcatExpressions(sourceExpression, targetExpressionListWithSingleExpression);

            Assert.AreEqual(sourceExpression.Args.Length, resultExpression.Args.Length);
            for (int i = 0; i < sourceExpression.Args.Length; i++)
            {
                for (int j = 0; j < sourceExpression.Args[i].Args.Length; j++)
                {
                    Assert.AreEqual(sourceExpression.Args[i].Args[j], resultExpression.Args[i].Args[j]);
                }
                Assert.AreEqual(
                    targetExpressionListWithSingleExpression[0].GetSmtExpression(ContextProvider.Context).ToString().Replace("_w", "_r"),
                    resultExpression.Args[i].Args[^1].ToString());
            }
        }

        [TestMethod]
        public void ConcatVOCExpressionWithConjunctionOfReadVOCExpressions()
        {
            var sourceExpression = MakeVOCConstraintsExpression();
            var targetFirstConjunctionOperand = MakeVOCConstraintForNonusedVar(VariableType.Read);
            var targetSecondConjunctionOperand = MakeVOCConstraintForUsedVar(VariableType.Read);
            targetSecondConjunctionOperand.LogicalConnective = LogicalConnective.And;

            var targetExpressionListConjunction = new List<IConstraintExpression>
            {
                targetFirstConjunctionOperand,
                targetSecondConjunctionOperand
            };

            var resultExpression = constraintExpressionOperationService.ConcatExpressions(sourceExpression, targetExpressionListConjunction);
            Assert.AreEqual(sourceExpression.Args.Length, resultExpression.Args.Length);
            for (int i = 0; i < sourceExpression.Args.Length; i++)
            {
                for (int j = 0; j < sourceExpression.Args[i].Args.Length; j++)
                {
                    Assert.AreEqual(sourceExpression.Args[i].Args[j], resultExpression.Args[i].Args[j]);
                }
                Assert.AreEqual(
                    targetExpressionListConjunction[0].GetSmtExpression(ContextProvider.Context).ToString().Replace("_w", "_r"),
                    resultExpression.Args[i].Args[^2].ToString());
                Assert.AreEqual(
                    targetExpressionListConjunction[1].GetSmtExpression(ContextProvider.Context).ToString().Replace("_w", "_r"),
                    resultExpression.Args[i].Args[^1].ToString());
            }
        }

        [TestMethod]
        public void ConcatVOCExpressionWithConjunctionOfWriteVOCExpressions()
        {
            var sourceExpression = MakeVOCConstraintsExpression();
            var targetFirstConjunctionOperand = MakeVOCConstraintForNonusedVar(VariableType.Written);
            var targetSecondConjunctionOperand = MakeVOCConstraintForUsedVar(VariableType.Written);
            targetSecondConjunctionOperand.LogicalConnective = LogicalConnective.And;

            var targetExpressionListConjunction = new List<IConstraintExpression>
            {
                targetFirstConjunctionOperand,
                targetSecondConjunctionOperand
            };

            var expectedFirstBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[0].Args
                .SkipLast(1)
                .Select(x => x as BoolExpr)
                .Union(new[]
                {
                    MakeVOCConstraintForNonusedVar(VariableType.Read).GetSmtExpression(ContextProvider.Context),
                    MakeVOCConstraintForUsedVar(VariableType.Read).GetSmtExpression(ContextProvider.Context)
                }));
            var expectedSecondBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[1].Args
                .Select(x => x as BoolExpr)
                .Skip(1)
                .Union(new[]
                {
                    MakeVOCConstraintForNonusedVar(VariableType.Read).GetSmtExpression(ContextProvider.Context),
                    MakeVOCConstraintForUsedVar(VariableType.Read).GetSmtExpression(ContextProvider.Context)
                }));

            var resultExpression = constraintExpressionOperationService.ConcatExpressions(sourceExpression, targetExpressionListConjunction);

            Assert.AreEqual(sourceExpression.Args.Length, resultExpression.Args.Length);
            Assert.AreEqual(expectedFirstBlock.ToString(), resultExpression.Args[0].ToString());
            Assert.AreEqual(expectedSecondBlock.ToString(), resultExpression.Args[1].ToString());
        }

        [TestMethod]
        public void ConcatVOCExpressionWithDisjunctionOfReadVOCExpressions()
        {
            var sourceExpression = MakeVOCConstraintsExpression();
            var targetFirstDisjunctionOperand = MakeVOCConstraintForNonusedVar(VariableType.Read);
            var targetSecondDisjunctionOperand = MakeVOCConstraintForUsedVar(VariableType.Read);
            targetSecondDisjunctionOperand.LogicalConnective = LogicalConnective.Or;

            var targetExpressionListDisjunction = new List<IConstraintExpression>
            {
                targetFirstDisjunctionOperand,
                targetSecondDisjunctionOperand
            };

            var resultExpression = constraintExpressionOperationService.ConcatExpressions(sourceExpression, targetExpressionListDisjunction);
            Assert.AreEqual(sourceExpression.Args.Length * targetExpressionListDisjunction.Count, resultExpression.Args.Length);
            for (int i = 0; i < sourceExpression.Args.Length; i++)
            {
                Assert.AreEqual(sourceExpression.Args[i].Args.Length + 1, resultExpression.Args[i].Args.Length);
                Assert.AreEqual(sourceExpression.Args[i].Args.Length + 1, resultExpression.Args[i + 2].Args.Length);

                for (int j = 0; j < sourceExpression.Args[i].Args.Length; j++)
                {
                    Assert.AreEqual(sourceExpression.Args[i].Args[j], resultExpression.Args[i].Args[j]);
                    Assert.AreEqual(sourceExpression.Args[i].Args[j], resultExpression.Args[i + 2].Args[j]);
                }
            }

            Assert.IsTrue(resultExpression.Args.Count(x => x.Args[^1].ToString() == targetExpressionListDisjunction[0].GetSmtExpression(ContextProvider.Context).ToString().Replace("_w", "_r")) == 2);
            Assert.IsTrue(resultExpression.Args.Count(x => x.Args[^1].ToString() == targetExpressionListDisjunction[1].GetSmtExpression(ContextProvider.Context).ToString().Replace("_w", "_r")) == 2);
        }

        [TestMethod]
        public void ConcatVOCExpressionWithDisjunctionOfWriteVOCExpressions()
        {
            var sourceExpression = MakeVOCConstraintsExpression();
            var targetFirstDisjunctionOperand = MakeVOCConstraintForNonusedVar(VariableType.Written);
            var targetSecondDisjunctionOperand = MakeVOCConstraintForUsedVar(VariableType.Written);
            targetSecondDisjunctionOperand.LogicalConnective = LogicalConnective.Or;

            var targetExpressionListDisjunction = new List<IConstraintExpression>
            {
                targetFirstDisjunctionOperand,
                targetSecondDisjunctionOperand
            };

            var expectedFirstBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[0].Args
                .Select(x => x as BoolExpr)
                .Union(new[]
                {
                    MakeVOCConstraintForNonusedVar(VariableType.Read).GetSmtExpression(ContextProvider.Context),
                }));
            var expectedSecondBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[1].Args
                .Select(x => x as BoolExpr)
                .Union(new[]
                {
                    MakeVOCConstraintForNonusedVar(VariableType.Read).GetSmtExpression(ContextProvider.Context),
                }));
            var expectedThirdBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[0].Args
                .SkipLast(1)
                .Select(x => x as BoolExpr)
                .Union(new[]
                {
                    MakeVOCConstraintForUsedVar(VariableType.Read).GetSmtExpression(ContextProvider.Context)
                }));
            var expectedFourthBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[1].Args
                .Select(x => x as BoolExpr)
                .Skip(1)
                .Union(new[]
                {
                    MakeVOCConstraintForUsedVar(VariableType.Read).GetSmtExpression(ContextProvider.Context)
                }));
            var expectedExpression = ContextProvider.Context.MkOr(new[]
            {
                expectedFirstBlock,
                expectedSecondBlock,
                expectedThirdBlock,
                expectedFourthBlock
            });

            var resultExpression = constraintExpressionOperationService.ConcatExpressions(sourceExpression, targetExpressionListDisjunction);

            Assert.AreEqual(expectedExpression.ToString(), resultExpression.ToString());
        }

        [TestMethod]
        [Description("Variable in target list has to be overwritten")]
        [DataRow(true)]
        [DataRow(false)]
        public void ConcatVOCExpressionWithConjunctionOfReadAndWriteVOCExpressionsWithSameVariable(bool isInvertedOrder)
        {
            var sourceExpression = MakeVOCConstraintsExpression();
            var targetFirstConjunctionOperand = MakeVOCConstraintForUsedVar(VariableType.Read);
            targetFirstConjunctionOperand.Constant = new DefinableValue<double>(900000);
            var targetSecondConjunctionOperand = MakeVOCConstraintForUsedVar(VariableType.Written);
            targetSecondConjunctionOperand.LogicalConnective = LogicalConnective.And;

            var targetExpressionListConjunction = new List<IConstraintExpression>
            {
                targetFirstConjunctionOperand,
                targetSecondConjunctionOperand
            };
            if (isInvertedOrder)
            {
                targetExpressionListConjunction.Reverse();
            }

            var expectedFirstBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[0].Args
                .SkipLast(1)
                .Select(x => x as BoolExpr)
                .Union(new[]
                {
                    MakeVOCConstraintForUsedVar(VariableType.Read).GetSmtExpression(ContextProvider.Context)
                }));
            var expectedSecondBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[1].Args
                .Select(x => x as BoolExpr)
                .Skip(1)
                .Union(new[]
                {
                    MakeVOCConstraintForUsedVar(VariableType.Read).GetSmtExpression(ContextProvider.Context)
                }));

            var resultExpression = constraintExpressionOperationService.ConcatExpressions(sourceExpression, targetExpressionListConjunction);

            Assert.AreEqual(sourceExpression.Args.Length, resultExpression.Args.Length);
            Assert.AreEqual(expectedFirstBlock.ToString(), resultExpression.Args[0].ToString());
            Assert.AreEqual(expectedSecondBlock.ToString(), resultExpression.Args[1].ToString());
        }

        [TestMethod]
        [Description("Variable constraints must differ in blocks")]
        public void ConcatVOCExpressionWithDisjunctionOfReadAndWriteVOCExpressionsWithSameVariable()
        {
            var sourceExpression = MakeVOCConstraintsExpression();
            var targetFirstConjunctionOperand = MakeVOCConstraintForUsedVar(VariableType.Read);
            targetFirstConjunctionOperand.Constant = new DefinableValue<double>(900000);
            var targetSecondConjunctionOperand = MakeVOCConstraintForUsedVar(VariableType.Written);
            targetSecondConjunctionOperand.LogicalConnective = LogicalConnective.Or;

            var targetExpressionListConjunction = new List<IConstraintExpression>
            {
                targetFirstConjunctionOperand,
                targetSecondConjunctionOperand
            };

            var expectedFirstBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[0].Args
                .Select(x => x as BoolExpr)
                .Union(new[]
                {
                    targetFirstConjunctionOperand.GetSmtExpression(ContextProvider.Context)
                }));
            var expectedSecondBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[1].Args
                .Select(x => x as BoolExpr)
                .Union(new[]
                {
                    targetFirstConjunctionOperand.GetSmtExpression(ContextProvider.Context)
                }));
            var expectedThirdBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[0].Args
                .SkipLast(1)
                .Select(x => x as BoolExpr)
                .Union(new[]
                {
                    MakeVOCConstraintForUsedVar(VariableType.Read).GetSmtExpression(ContextProvider.Context)
                }));
            var expectedFourthBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[1].Args
                .Select(x => x as BoolExpr)
                .Skip(1)
                .Union(new[]
                {
                    MakeVOCConstraintForUsedVar(VariableType.Read).GetSmtExpression(ContextProvider.Context)
                }));
            var expectedExpression = ContextProvider.Context.MkOr(new[]
            {
                expectedFirstBlock,
                expectedSecondBlock,
                expectedThirdBlock,
                expectedFourthBlock
            });

            var resultExpression = constraintExpressionOperationService.ConcatExpressions(sourceExpression, targetExpressionListConjunction);

            Assert.AreEqual(expectedExpression.ToString(), resultExpression.ToString());
        }

        #endregion

        private BoolExpr MakeVOCConstraintsExpression()
        {
            var aVariable = ContextProvider.Context.MkConst(a + "_r", ContextProvider.Context.MkIntSort());
            var aMinimum = ContextProvider.Context.MkInt(-1);
            var aMaximum = ContextProvider.Context.MkInt(10000);
            var aVariableGreaterThanMinimum = ContextProvider.Context.MkGt((ArithExpr)aVariable, aMinimum);
            var aVariableLessThanOrEqualMaximum = ContextProvider.Context.MkLe((ArithExpr)aVariable, aMaximum);

            var bVariable = ContextProvider.Context.MkConst(b + "_r", ContextProvider.Context.MkBoolSort());
            var bEqualTrue = ContextProvider.Context.MkEq(bVariable, ContextProvider.Context.MkTrue());

            var cVariable = ContextProvider.Context.MkConst(c + "_r", ContextProvider.Context.MkRealSort());
            var cUnequalValue = ContextProvider.Context.MkNot(ContextProvider.Context.MkEq(cVariable, ContextProvider.Context.MkReal(10.444.ToString())));

            var firstBlockExpression = new BoolExpr[]
            {
                aVariableGreaterThanMinimum,
                aVariableLessThanOrEqualMaximum,
                bEqualTrue,
                cUnequalValue
            };

            var cMaximum = ContextProvider.Context.MkReal(0);
            var cLessThanOrEqualMaximum = ContextProvider.Context.MkLe((ArithExpr)cVariable, cMaximum);

            var bUnequalTrue = ContextProvider.Context.MkNot(ContextProvider.Context.MkEq(bVariable, ContextProvider.Context.MkTrue()));

            var secondBlockExpression = new BoolExpr[]
            {
                cLessThanOrEqualMaximum,
                bUnequalTrue,
            };

            return ContextProvider.Context.MkOr(ContextProvider.Context.MkAnd(firstBlockExpression), ContextProvider.Context.MkAnd(secondBlockExpression));
        }

        private ConstraintVOCExpression<double> MakeVOCConstraintForUsedVar(VariableType varType)
        {
            return new ConstraintVOCExpression<double>
            {
                ConstraintVariable = new ConstraintVariable
                {
                    Domain = DomainType.Real,
                    Name = c,
                    VariableType = varType
                },
                LogicalConnective = LogicalConnective.Empty,
                Predicate = BinaryPredicate.GreaterThan,
                Constant = new DefinableValue<double>(10.1991)
            };
        }
        private ConstraintVOCExpression<long> MakeVOCConstraintForNonusedVar(VariableType varType)
        {
            return new ConstraintVOCExpression<long>
            {
                ConstraintVariable = new ConstraintVariable
                {
                    Domain = DomainType.Integer,
                    Name = d,
                    VariableType = varType
                },
                LogicalConnective = LogicalConnective.Empty,
                Predicate = BinaryPredicate.GreaterThan,
                Constant = new DefinableValue<long>(10000)
            };
        }

        private ConstraintVOVExpression MakeReadVOVConstraintForNonusedVar()
        {
            return new ConstraintVOVExpression
            {
                ConstraintVariable = new ConstraintVariable
                {
                    Domain = DomainType.Integer,
                    Name = d,
                    VariableType = VariableType.Read
                },
                LogicalConnective = LogicalConnective.Empty,
                Predicate = BinaryPredicate.GreaterThan,
                VariableToCompare = new ConstraintVariable
                {
                    Domain = DomainType.Integer,
                    Name = a,
                    VariableType = VariableType.Read
                }
            };
        }
    }
}
