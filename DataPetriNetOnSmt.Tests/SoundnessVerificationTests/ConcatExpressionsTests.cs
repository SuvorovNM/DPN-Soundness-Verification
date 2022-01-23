using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.Services;
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
        private readonly string e = "e";
        private readonly string f = "f";
        private readonly string g = "g";
        private readonly string h = "h";

        private ConstraintExpressionOperationServiceWithManualConcat constraintExpressionOperationService;

        [TestInitialize]
        public void Initialize()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            constraintExpressionOperationService = new ConstraintExpressionOperationServiceWithManualConcat();
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

        #region VOVExpressions

        [TestMethod]
        [DataRow(VariableType.Read)]
        [DataRow(VariableType.Written)]
        public void ConcatVOCExpressionWithVOVExpressionForUnusedVar(VariableType varType)
        {
            var sourceExpression = MakeVOCConstraintsExpression();
            var targetExpressionListWithSingleExpression = new List<IConstraintExpression>
            {
                MakeVOVConstraintForNonusedVar(varType)
            };

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
        [DataRow(VariableType.Read)]
        [DataRow(VariableType.Written)]
        public void ConcatCombinedExpressionWithVOVExpressionForUnusedVar(VariableType varType)
        {
            var sourceExpression = MakeCombinedConstraintsExpression();
            var targetExpressionListWithSingleExpression = new List<IConstraintExpression>
            {
                MakeVOVConstraintForNonusedVar(varType)
            };

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
        public void ConcatCombinedExpressionWithVOCExpressionForUsedVarWithNoPossibleImplications()
        {
            var sourceExpression = MakeCombinedConstraintsExpression();
            var targetExpressionListWithSingleExpression = new List<IConstraintExpression>
            {
                MakeVOCConstraintForUsedVar(VariableType.Written)
            };

            var expressionsToOverwriteInFirstBlock = new[] { sourceExpression.Args[0].Args[3] };
            var expectedFirstBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[0].Args
                .Except(expressionsToOverwriteInFirstBlock)
                .Select(x => x as BoolExpr)
                .Union(new[]
                {
                    MakeVOCConstraintForUsedVar(VariableType.Read).GetSmtExpression(ContextProvider.Context)
                }));

            var expressionsToOverwriteInSecondBlock = new[] { sourceExpression.Args[1].Args[0], sourceExpression.Args[1].Args[3] };
            var expectedSecondBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[1].Args
                .Except(expressionsToOverwriteInSecondBlock)
                .Select(x => x as BoolExpr)
                .Union(new[]
                {
                    MakeVOCConstraintForUsedVar(VariableType.Read).GetSmtExpression(ContextProvider.Context)
                }));

            var expectedExpression = ContextProvider.Context.MkOr(expectedFirstBlock, expectedSecondBlock);

            var resultExpression = constraintExpressionOperationService.ConcatExpressions(sourceExpression, targetExpressionListWithSingleExpression);

            Assert.AreEqual(expectedExpression.ToString(), resultExpression.ToString());
        }

        [TestMethod]
        public void ConcatCombinedExpressionWithVOCExpressionForUsedVarWithPossibleImplications()
        {
            var sourceExpression = MakeCombinedConstraintsExpression();
            var targetExpressionListWithSingleExpression = new List<IConstraintExpression>
            {
                MakeVOCConstraintForOverwrittenUsedVarWithExpectedImplications(VariableType.Written)
            };

            var expressionsToOverwriteInFirstBlock = new[]
            {
                sourceExpression.Args[0].Args[0],
                sourceExpression.Args[0].Args[1],
                sourceExpression.Args[0].Args[5],
            };
            var expectedFirstBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[0].Args
                .Except(expressionsToOverwriteInFirstBlock)
                .Select(x => x as BoolExpr)
                .Union(new[]
                {
                    new ConstraintVOCExpression<long>
                    {
                        ConstraintVariable = new ConstraintVariable
                        {
                            Domain = DomainType.Integer,
                            Name = e,
                            VariableType = VariableType.Read
                        },
                        LogicalConnective = LogicalConnective.Empty,
                        Predicate = BinaryPredicate.LessThan,
                        Constant = new DefinableValue<long>(10000) // To constant
                    }.GetSmtExpression(ContextProvider.Context),

                    MakeVOCConstraintForOverwrittenUsedVarWithExpectedImplications(VariableType.Read)
                        .GetSmtExpression(ContextProvider.Context)
                }));

            var expectedSecondBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[1].Args
                .Select(x => x as BoolExpr)
                .Union(new[]
                {
                    MakeVOCConstraintForOverwrittenUsedVarWithExpectedImplications(VariableType.Read)
                        .GetSmtExpression(ContextProvider.Context)
                }));

            var expectedExpression = ContextProvider.Context.MkOr(expectedFirstBlock, expectedSecondBlock);

            var resultExpression = constraintExpressionOperationService.ConcatExpressions(sourceExpression, targetExpressionListWithSingleExpression);

            Assert.AreEqual(expectedExpression.ToString(), resultExpression.ToString());
        }

        [TestMethod]
        public void ConcatCombinedExpressionWithVOVLessThanExpressionForUsedVarWithPossibleImplications()
        {
            var sourceExpression = MakeCombinedConstraintsExpression();
            var targetExpressionListWithSingleExpression = new List<IConstraintExpression>
            {
                MakeVOVLessThanConstraintForOverwrittenUsedVarWithExpectedImplications(VariableType.Written)
            };

            var expressionsToOverwriteInFirstBlock = new[]
            {
                sourceExpression.Args[0].Args[4],
                sourceExpression.Args[0].Args[5],
            };
            var expectedFirstBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[0].Args
                .Except(expressionsToOverwriteInFirstBlock)
                .Select(x => x as BoolExpr)
                .Union(new[]
                {
                    new ConstraintVOVExpression
                    {
                        ConstraintVariable = new ConstraintVariable
                        {
                            Domain = DomainType.Integer,
                            Name = f,
                            VariableType = VariableType.Read
                        },
                        LogicalConnective = LogicalConnective.Empty,
                        Predicate = BinaryPredicate.LessThan,
                        VariableToCompare = new ConstraintVariable
                        {
                            Domain = DomainType.Integer,
                            Name = a,
                            VariableType = VariableType.Read
                        }
                    }.GetSmtExpression(ContextProvider.Context),

                    MakeVOVLessThanConstraintForOverwrittenUsedVarWithExpectedImplications(VariableType.Read)
                        .GetSmtExpression(ContextProvider.Context)
                }));

            var expectedSecondBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[1].Args
                .Select(x => x as BoolExpr)
                .Union(new[]
                {
                    MakeVOVLessThanConstraintForOverwrittenUsedVarWithExpectedImplications(VariableType.Read)
                        .GetSmtExpression(ContextProvider.Context)
                }));

            var expectedExpression = ContextProvider.Context.MkOr(expectedFirstBlock, expectedSecondBlock);

            var resultExpression = constraintExpressionOperationService.ConcatExpressions(sourceExpression, targetExpressionListWithSingleExpression);

            Assert.AreEqual(expectedExpression.ToString(), resultExpression.ToString());
        }

        [TestMethod]
        [Description("f_w = a_r && a_w = e_r")]
        public void ConcatCombinedExpressionWithVOVEqualityExpressionsForUsedVarsWithPossibleImplications()
        {
            var sourceExpression = MakeCombinedConstraintsExpression();
            var targetExpressionListWithSingleExpression = new List<IConstraintExpression>
            {
                MakeVOVEqualityConstraintBetweenFVariableAndAVariable(VariableType.Written),
                MakeVOVEqualityConstraintBetweenAVariableAndEVariable(VariableType.Written)
            };

            var expressionsToOverwriteInFirstBlock = new[]
            {
                sourceExpression.Args[0].Args[0],
                sourceExpression.Args[0].Args[1],
                sourceExpression.Args[0].Args[4],
                sourceExpression.Args[0].Args[5],
            };
            var expectedFirstBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[0].Args
                .Except(expressionsToOverwriteInFirstBlock)
                .Select(x => x as BoolExpr)
                .Union(new[]
                {

                    new ConstraintVOCExpression<long>
                    {
                        ConstraintVariable = new ConstraintVariable
                        {
                            Domain = DomainType.Integer,
                            Name = e,
                            VariableType = VariableType.Read
                        },
                        LogicalConnective = LogicalConnective.Empty,
                        Predicate = BinaryPredicate.LessThan,
                        Constant = new DefinableValue<long>(10000)
                    }.GetSmtExpression(ContextProvider.Context),

                    new ConstraintVOCExpression<long>
                    {
                        ConstraintVariable = new ConstraintVariable
                        {
                            Domain = DomainType.Integer,
                            Name = f,
                            VariableType = VariableType.Read
                        },
                        LogicalConnective = LogicalConnective.Empty,
                        Predicate = BinaryPredicate.GreaterThan,
                        Constant = new DefinableValue<long>(-1)
                    }.GetSmtExpression(ContextProvider.Context),

                    new ConstraintVOCExpression<long>
                    {
                        ConstraintVariable = new ConstraintVariable
                        {
                            Domain = DomainType.Integer,
                            Name = f,
                            VariableType = VariableType.Read
                        },
                        LogicalConnective = LogicalConnective.Empty,
                        Predicate = BinaryPredicate.LessThanOrEqual,
                        Constant = new DefinableValue<long>(10000)
                    }.GetSmtExpression(ContextProvider.Context),

                    new ConstraintVOVExpression
                    {
                        ConstraintVariable = new ConstraintVariable
                        {
                            Domain = DomainType.Integer,
                            Name = e,
                            VariableType = VariableType.Read
                        },
                        LogicalConnective = LogicalConnective.Empty,
                        Predicate = BinaryPredicate.LessThan,
                        VariableToCompare = new ConstraintVariable
                        {
                            Domain = DomainType.Integer,
                            Name = f,
                            VariableType = VariableType.Read
                        }
                    }.GetSmtExpression(ContextProvider.Context),

                    MakeVOVEqualityConstraintBetweenAVariableAndEVariable(VariableType.Read)
                        .GetSmtExpression(ContextProvider.Context)
                }));

            var expectedSecondBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[1].Args
                .Select(x => x as BoolExpr)
                .Union(new[]
                {
                    MakeVOVEqualityConstraintBetweenAVariableAndEVariable(VariableType.Read)
                        .GetSmtExpression(ContextProvider.Context)
                }));

            var expectedExpression = ContextProvider.Context.MkOr(expectedFirstBlock, expectedSecondBlock);

            var resultExpression = constraintExpressionOperationService.ConcatExpressions(sourceExpression, targetExpressionListWithSingleExpression);

            Assert.AreEqual(expectedExpression.ToString(), resultExpression.ToString());
        }

        [TestMethod]
        [Description("f_w > a_r && a_w = e_r")]
        public void ConcatCombinedExpressionWithVOVExpressionsWhichOverwriteEachOtherVars()
        {
            var sourceExpression = MakeCombinedConstraintsExpression();
            var targetExpressionListWithSingleExpression = new List<IConstraintExpression>
            {
                MakeVOVGreaterThanConstraintBetweenFVariableAndAVariable(VariableType.Written),
                MakeVOVGreaterThanConstraintBetweenAVariableAndFVariable(VariableType.Written)
            };

            var expressionsToOverwriteInFirstBlock = new[]
            {
                sourceExpression.Args[0].Args[0],
                sourceExpression.Args[0].Args[1],
                sourceExpression.Args[0].Args[4],
                sourceExpression.Args[0].Args[5],
            };
            var expectedFirstBlock = ContextProvider.Context.MkAnd(sourceExpression.Args[0].Args
                .Except(expressionsToOverwriteInFirstBlock)
                .Select(x => x as BoolExpr)
                .Union(new[]
                {
                    new ConstraintVOCExpression<long>
                    {
                        ConstraintVariable = new ConstraintVariable
                        {
                            Domain = DomainType.Integer,
                            Name = e,
                            VariableType = VariableType.Read
                        },
                        LogicalConnective = LogicalConnective.Empty,
                        Predicate = BinaryPredicate.LessThan,
                        Constant = new DefinableValue<long>(10000)
                    }.GetSmtExpression(ContextProvider.Context),
                    new ConstraintVOCExpression<long>
                    {
                        ConstraintVariable = new ConstraintVariable
                        {
                            Domain = DomainType.Integer,
                            Name = f,
                            VariableType = VariableType.Read
                        },
                        LogicalConnective = LogicalConnective.Empty,
                        Predicate = BinaryPredicate.GreaterThan,
                        Constant = new DefinableValue<long>(0)
                    }.GetSmtExpression(ContextProvider.Context),
                }));

            var expectedSecondBlock = sourceExpression.Args[1];
            var expectedExpression = ContextProvider.Context.MkOr(expectedFirstBlock, (BoolExpr)expectedSecondBlock);

            var resultExpression = constraintExpressionOperationService.ConcatExpressions(sourceExpression, targetExpressionListWithSingleExpression);

            Assert.AreEqual(expectedExpression.ToString(), resultExpression.ToString());
        }

        [TestMethod]
        public void ConcatCombinedEqualityExpressionsWithVOVEqualityExpressions()
        {
            var sourceExpression = MakeCombinedConstraintsWithEqualitiesExpression();
            var targetExpressionListWithEqualityExpressions = MakeVOVEqualityExpressions(VariableType.Written);

            var expectedExpressionBlock = new List<BoolExpr>
            {
                new ConstraintVOCExpression<long>
                {
                    ConstraintVariable = new ConstraintVariable
                    {
                        Name = a,
                        Domain = DomainType.Integer,
                        VariableType = VariableType.Read
                    },
                    Constant = new DefinableValue<long>(1),
                    LogicalConnective = LogicalConnective.And,
                    Predicate = BinaryPredicate.Equal
                }.GetSmtExpression(ContextProvider.Context),

                new ConstraintVOCExpression<long>
                {
                    ConstraintVariable = new ConstraintVariable
                    {
                        Name = f,
                        Domain = DomainType.Integer,
                        VariableType = VariableType.Read
                    },
                    Constant = new DefinableValue<long>(1),
                    LogicalConnective = LogicalConnective.And,
                    Predicate = BinaryPredicate.Equal
                }.GetSmtExpression(ContextProvider.Context),

                new ConstraintVOCExpression<long>
                {
                    ConstraintVariable = new ConstraintVariable
                    {
                        Name = e,
                        Domain = DomainType.Integer,
                        VariableType = VariableType.Read
                    },
                    Constant = new DefinableValue<long>(1),
                    LogicalConnective = LogicalConnective.And,
                    Predicate = BinaryPredicate.Equal
                }.GetSmtExpression(ContextProvider.Context),

                MakeVOVEqualityExpressions(VariableType.Read)[^1].GetSmtExpression(ContextProvider.Context)
            };
            var expectedExpression = ContextProvider.Context.MkAnd(expectedExpressionBlock);

            var resultExpression = constraintExpressionOperationService.ConcatExpressions(sourceExpression, targetExpressionListWithEqualityExpressions);

            Assert.AreEqual(expectedExpression.ToString(), resultExpression.ToString());
        }

        [TestMethod]
        public void ConcatCombinedUnequalityExpressionsWithVOVUnequalityExpressions()
        {
            var sourceExpression = MakeCombinedConstraintsWithUnequalitiesExpression();
            var targetExpressionListWithUnequalityExpressions = MakeVOVUnequalityExpressions(VariableType.Written);

            var expectedExpressionBlock = new List<BoolExpr>
            {
                new ConstraintVOCExpression<long>
                {
                    ConstraintVariable = new ConstraintVariable
                    {
                        Name = a,
                        Domain = DomainType.Integer,
                        VariableType = VariableType.Read
                    },
                    Constant = new DefinableValue<long>(1),
                    LogicalConnective = LogicalConnective.And,
                    Predicate = BinaryPredicate.Unequal
                }.GetSmtExpression(ContextProvider.Context),

                new ConstraintVOCExpression<long>
                {
                    ConstraintVariable = new ConstraintVariable
                    {
                        Name = d,
                        Domain = DomainType.Integer,
                        VariableType = VariableType.Read
                    },
                    Constant = new DefinableValue<long>(1),
                    LogicalConnective = LogicalConnective.And,
                    Predicate = BinaryPredicate.Unequal
                }.GetSmtExpression(ContextProvider.Context),
            };
            var expectedExpression = ContextProvider.Context.MkAnd(expectedExpressionBlock);

            var resultExpression = constraintExpressionOperationService.ConcatExpressions(sourceExpression, targetExpressionListWithUnequalityExpressions);

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

        private BoolExpr MakeCombinedConstraintsExpression() // VOC + VOV
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

            var eVariable = ContextProvider.Context.MkConst(e + "_r", ContextProvider.Context.MkIntSort());
            var eVariableLessThanAVariable = ContextProvider.Context.MkLt((ArithExpr)eVariable, (ArithExpr)aVariable);

            var fVariable = ContextProvider.Context.MkConst(f + "_r", ContextProvider.Context.MkIntSort());
            var fVariableEqualsToEVariable = ContextProvider.Context.MkEq((ArithExpr)fVariable, (ArithExpr)eVariable);

            var firstBlockExpression = new BoolExpr[]
            {
                aVariableGreaterThanMinimum,
                aVariableLessThanOrEqualMaximum,
                bEqualTrue,
                cUnequalValue,
                fVariableEqualsToEVariable,
                eVariableLessThanAVariable
            };

            var cMaximum = ContextProvider.Context.MkReal(0);
            var cLessThanOrEqualMaximum = ContextProvider.Context.MkLe((ArithExpr)cVariable, cMaximum);

            var bUnequalTrue = ContextProvider.Context.MkNot(ContextProvider.Context.MkEq(bVariable, ContextProvider.Context.MkTrue()));

            var gVariable = ContextProvider.Context.MkConst(g + "_r", ContextProvider.Context.MkBoolSort());
            var gVariableUnequalBVariable = ContextProvider.Context.MkNot(ContextProvider.Context.MkEq(gVariable, bVariable));

            var hVariable = ContextProvider.Context.MkConst(h + "_r", ContextProvider.Context.MkRealSort());
            var hVariableGreaterThanOrEqualToVariableC = ContextProvider.Context.MkGe((ArithExpr)hVariable, (ArithExpr)cVariable);

            var secondBlockExpression = new BoolExpr[]
            {
                cLessThanOrEqualMaximum,
                bUnequalTrue,
                gVariableUnequalBVariable,
                hVariableGreaterThanOrEqualToVariableC
            };

            return ContextProvider.Context.MkOr(ContextProvider.Context.MkAnd(firstBlockExpression), ContextProvider.Context.MkAnd(secondBlockExpression));
        }

        private BoolExpr MakeCombinedConstraintsWithEqualitiesExpression()
        {
            var aVariable = ContextProvider.Context.MkConst(a + "_r", ContextProvider.Context.MkIntSort());
            var eVariable = ContextProvider.Context.MkConst(e + "_r", ContextProvider.Context.MkIntSort());
            var fVariable = ContextProvider.Context.MkConst(f + "_r", ContextProvider.Context.MkIntSort());
            var dVariable = ContextProvider.Context.MkConst(d + "_r", ContextProvider.Context.MkIntSort());

            var fEqualsA = ContextProvider.Context.MkEq(fVariable, aVariable);
            var aEqualsD = ContextProvider.Context.MkEq(aVariable, dVariable);
            var eEqualsF = ContextProvider.Context.MkEq(eVariable, fVariable);
            var dEquals1 = ContextProvider.Context.MkEq(dVariable, ContextProvider.Context.MkInt(1));

            var expression = new BoolExpr[]
            {
                fEqualsA,
                aEqualsD,
                eEqualsF,
                dEquals1
            };

            return ContextProvider.Context.MkAnd(expression);
        }

        private BoolExpr MakeCombinedConstraintsWithUnequalitiesExpression()
        {
            var aVariable = ContextProvider.Context.MkConst(a + "_r", ContextProvider.Context.MkIntSort());
            var eVariable = ContextProvider.Context.MkConst(e + "_r", ContextProvider.Context.MkIntSort());
            var fVariable = ContextProvider.Context.MkConst(f + "_r", ContextProvider.Context.MkIntSort());
            var dVariable = ContextProvider.Context.MkConst(d + "_r", ContextProvider.Context.MkIntSort());

            //var fUnequalsA = ContextProvider.Context.MkNot(ContextProvider.Context.MkEq(fVariable, aVariable));
            var fUnequalsE = ContextProvider.Context.MkNot(ContextProvider.Context.MkEq(fVariable, eVariable));
            var aUnequalsD = ContextProvider.Context.MkNot(ContextProvider.Context.MkEq(aVariable, dVariable));
            var dUnquals1 = ContextProvider.Context.MkEq(dVariable, ContextProvider.Context.MkInt(1));
            var eLess2 = ContextProvider.Context.MkLt((ArithExpr)eVariable, ContextProvider.Context.MkInt(2));
            var eGreater0 = ContextProvider.Context.MkGt((ArithExpr)eVariable, ContextProvider.Context.MkInt(0));

            var expression = new BoolExpr[]
            {
                //fUnequalsA,
                fUnequalsE,
                aUnequalsD,
                dUnquals1,
                eLess2,
                eGreater0
            };

            return ContextProvider.Context.MkAnd(expression);
        }

        private List<IConstraintExpression> MakeVOVUnequalityExpressions(VariableType varType)
        {
            return new List<IConstraintExpression>
            {
                new ConstraintVOVExpression
                {
                    ConstraintVariable = new ConstraintVariable
                    {
                        Name = f,
                        Domain = DomainType.Integer,
                        VariableType = varType
                    },
                    LogicalConnective = LogicalConnective.Empty,
                    Predicate = BinaryPredicate.Unequal,
                    VariableToCompare = new ConstraintVariable
                    {
                        Name = a,
                        Domain = DomainType.Integer,
                        VariableType = VariableType.Read
                    }
                },
                new ConstraintVOVExpression
                {
                    ConstraintVariable = new ConstraintVariable
                    {
                        Name = a,
                        Domain = DomainType.Integer,
                        VariableType = varType
                    },
                    LogicalConnective = LogicalConnective.And,
                    Predicate = BinaryPredicate.Unequal,
                    VariableToCompare = new ConstraintVariable
                    {
                        Name = d,
                        Domain = DomainType.Integer,
                        VariableType = VariableType.Read
                    }
                },
                new ConstraintVOVExpression
                {
                    ConstraintVariable = new ConstraintVariable
                    {
                        Name = d,
                        Domain = DomainType.Integer,
                        VariableType = varType
                    },
                    LogicalConnective = LogicalConnective.And,
                    Predicate = BinaryPredicate.Unequal,
                    VariableToCompare = new ConstraintVariable
                    {
                        Name = e,
                        Domain = DomainType.Integer,
                        VariableType = VariableType.Read
                    }
                },
                new ConstraintVOVExpression
                {
                    ConstraintVariable = new ConstraintVariable
                    {
                        Name = e,
                        Domain = DomainType.Integer,
                        VariableType = varType
                    },
                    LogicalConnective = LogicalConnective.And,
                    Predicate = BinaryPredicate.Unequal,
                    VariableToCompare = new ConstraintVariable
                    {
                        Name = f,
                        Domain = DomainType.Integer,
                        VariableType = VariableType.Read
                    }
                }
            };
        }

        private List<IConstraintExpression> MakeVOVEqualityExpressions(VariableType varType)
        {
            return new List<IConstraintExpression>
            {
                new ConstraintVOVExpression
                {
                    ConstraintVariable = new ConstraintVariable
                    {
                        Domain = DomainType.Integer,
                        Name = f,
                        VariableType = varType
                    },
                    LogicalConnective = LogicalConnective.Empty,
                    Predicate = BinaryPredicate.Equal,
                    VariableToCompare = new ConstraintVariable
                    {
                        Domain = DomainType.Integer,
                        Name = a,
                        VariableType = VariableType.Read
                    }
                },
                new ConstraintVOVExpression
                {
                    ConstraintVariable = new ConstraintVariable
                    {
                        Domain = DomainType.Integer,
                        Name = a,
                        VariableType = varType
                    },
                    LogicalConnective = LogicalConnective.And,
                    Predicate = BinaryPredicate.Equal,
                    VariableToCompare = new ConstraintVariable
                    {
                        Domain = DomainType.Integer,
                        Name = d,
                        VariableType = VariableType.Read
                    }
                },
                new ConstraintVOCExpression<long>
                {
                    ConstraintVariable = new ConstraintVariable
                    {
                        Domain = DomainType.Integer,
                        Name = d,
                        VariableType = varType
                    },
                    LogicalConnective = LogicalConnective.And,
                    Predicate = BinaryPredicate.Equal,
                    Constant = new DefinableValue<long>(5)
                }
            };
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
        private ConstraintVOCExpression<long> MakeVOCConstraintForOverwrittenUsedVarWithExpectedImplications(VariableType varType)
        {
            return new ConstraintVOCExpression<long>
            {
                ConstraintVariable = new ConstraintVariable
                {
                    Domain = DomainType.Integer,
                    Name = a,
                    VariableType = varType
                },
                LogicalConnective = LogicalConnective.Empty,
                Predicate = BinaryPredicate.Equal,
                Constant = new DefinableValue<long>(5555)
            };
        }

        private ConstraintVOVExpression MakeVOVConstraintForNonusedVar(VariableType varType)
        {
            return new ConstraintVOVExpression
            {
                ConstraintVariable = new ConstraintVariable
                {
                    Domain = DomainType.Integer,
                    Name = d,
                    VariableType = varType
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

        private ConstraintVOVExpression MakeVOVLessThanConstraintForOverwrittenUsedVarWithExpectedImplications(VariableType varType)
        {
            return new ConstraintVOVExpression
            {
                ConstraintVariable = new ConstraintVariable
                {
                    Domain = DomainType.Integer,
                    Name = e,
                    VariableType = varType
                },
                LogicalConnective = LogicalConnective.Empty,
                Predicate = BinaryPredicate.GreaterThanOrEqual,
                VariableToCompare = new ConstraintVariable
                {
                    Domain = DomainType.Integer,
                    Name = a,
                    VariableType = VariableType.Read
                }
            };
        }

        private ConstraintVOVExpression MakeVOVEqualityConstraintBetweenFVariableAndAVariable(VariableType varType)
        {
            return new ConstraintVOVExpression
            {
                ConstraintVariable = new ConstraintVariable
                {
                    Domain = DomainType.Integer,
                    Name = f,
                    VariableType = varType
                },
                LogicalConnective = LogicalConnective.Empty,
                Predicate = BinaryPredicate.Equal,
                VariableToCompare = new ConstraintVariable
                {
                    Domain = DomainType.Integer,
                    Name = a,
                    VariableType = VariableType.Read
                }
            };
        }

        private ConstraintVOVExpression MakeVOVGreaterThanConstraintBetweenFVariableAndAVariable(VariableType varType)
        {
            return new ConstraintVOVExpression
            {
                ConstraintVariable = new ConstraintVariable
                {
                    Domain = DomainType.Integer,
                    Name = f,
                    VariableType = varType
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

        private ConstraintVOVExpression MakeVOVGreaterThanConstraintBetweenAVariableAndFVariable(VariableType varType)
        {
            return new ConstraintVOVExpression
            {
                ConstraintVariable = new ConstraintVariable
                {
                    Domain = DomainType.Integer,
                    Name = a,
                    VariableType = varType
                },
                LogicalConnective = LogicalConnective.Empty,
                Predicate = BinaryPredicate.GreaterThan,
                VariableToCompare = new ConstraintVariable
                {
                    Domain = DomainType.Integer,
                    Name = f,
                    VariableType = VariableType.Read
                }
            };
        }

        private ConstraintVOVExpression MakeVOVEqualityConstraintBetweenAVariableAndEVariable(VariableType varType)
        {
            return new ConstraintVOVExpression
            {
                ConstraintVariable = new ConstraintVariable
                {
                    Domain = DomainType.Integer,
                    Name = a,
                    VariableType = varType
                },
                LogicalConnective = LogicalConnective.Empty,
                Predicate = BinaryPredicate.Equal,
                VariableToCompare = new ConstraintVariable
                {
                    Domain = DomainType.Integer,
                    Name = e,
                    VariableType = VariableType.Read
                }
            };
        }
    }
}
