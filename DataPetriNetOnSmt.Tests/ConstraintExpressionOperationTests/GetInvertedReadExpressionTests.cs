using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.Tests.ConstraintExpressionOperationTests
{
    [TestClass]
    public class GetInvertedReadExpressionTests
    {
        private static int variableIndex = 0;
        private ConstraintExpressionOperationService constraintExpressionOperationService;
        [TestInitialize]
        public void Initialize()
        {
            constraintExpressionOperationService = new ConstraintExpressionOperationService();
        }


        [TestMethod]
        public void InvertNullExpression()
        {
            Assert.ThrowsException<ArgumentNullException>(() => constraintExpressionOperationService.GetInvertedReadExpression(null));
        }

        [TestMethod]
        public void InvertEmptyExpression()
        {
            var emptyExpressionList = new List<IConstraintExpression>();

            var invertedList = constraintExpressionOperationService.GetInvertedReadExpression(emptyExpressionList);

            Assert.IsTrue(invertedList.Count == 0);
        }

        [TestMethod]
        public void InvertSingleReadVOCExpression()
        {
            ConstraintVOCExpression<long> readExpression = GenerateReadVOCExpression();
            var epxressionListWithSingleReadExpression = new List<IConstraintExpression> { readExpression };

            var invertedList = constraintExpressionOperationService.GetInvertedReadExpression(epxressionListWithSingleReadExpression);

            Assert.IsTrue(invertedList.Count == 1);
            Assert.AreEqual(LogicalConnective.Empty, invertedList[0].LogicalConnective);
            Assert.AreEqual((int)readExpression.Predicate, -(int)invertedList[0].Predicate);
        }

        [TestMethod]
        public void InvertSingleReadVOVExpression()
        {
            ConstraintVOVExpression readExpression = GenerateReadVOVExpression();
            var expressionListWithSingleReadExpression = new List<IConstraintExpression>() { readExpression };

            var invertedList = constraintExpressionOperationService.GetInvertedReadExpression(expressionListWithSingleReadExpression);

            Assert.IsTrue(invertedList.Count == 1);
            Assert.AreEqual(LogicalConnective.Empty, invertedList[0].LogicalConnective);
            Assert.AreEqual((int)readExpression.Predicate, -(int)invertedList[0].Predicate);
        }

        [TestMethod]
        public void InvertSingleWriteExpression()
        {
            IConstraintExpression expression = GenerateWriteExpression();
            var expressionListWithSingleWriteExpression = new List<IConstraintExpression>() { expression };

            var invertedList = constraintExpressionOperationService.GetInvertedReadExpression(expressionListWithSingleWriteExpression);

            Assert.IsTrue(invertedList.Count == 0);
        }

        [TestMethod]
        public void InvertConjunctionOfReadAndWriteExpressions()
        {
            IConstraintExpression writeExpression = GenerateWriteExpression();
            IConstraintExpression readExpression = GenerateReadVOCExpression();
            readExpression.LogicalConnective = LogicalConnective.And;

            var expressionListWithConjunctionOfReadAndWriteExpressions = new List<IConstraintExpression>() { writeExpression, readExpression };

            var invertedList = constraintExpressionOperationService.GetInvertedReadExpression(expressionListWithConjunctionOfReadAndWriteExpressions);

            Assert.IsTrue(invertedList.Count == 1);
            Assert.AreEqual(readExpression.ConstraintVariable, invertedList[0].ConstraintVariable);
            Assert.AreEqual(LogicalConnective.Empty, invertedList[0].LogicalConnective);
            Assert.AreEqual((int)readExpression.Predicate, -(int)invertedList[0].Predicate);
        }

        [TestMethod]
        [DataRow(LogicalConnective.And)]
        [DataRow(LogicalConnective.Or)]
        public void InvertCombinationOfWriteExpressions(LogicalConnective connective)
        {
            IConstraintExpression writeExpression1 = GenerateWriteExpression();
            IConstraintExpression writeExpression2 = GenerateWriteExpression();
            writeExpression2.LogicalConnective = connective;

            var expressionListWithCombinationOfWriteExpressions = new List<IConstraintExpression>() { writeExpression1, writeExpression2 };

            var invertedList = constraintExpressionOperationService.GetInvertedReadExpression(expressionListWithCombinationOfWriteExpressions);

            Assert.IsTrue(invertedList.Count == 0);
        }

        [TestMethod]
        [DataRow(LogicalConnective.And)]
        [DataRow(LogicalConnective.Or)]
        public void InvertCombinationOfReadExpressions(LogicalConnective connective)
        {
            IConstraintExpression readExpression1 = GenerateReadVOVExpression();
            IConstraintExpression readExpression2 = GenerateReadVOCExpression();
            readExpression2.LogicalConnective = connective;

            var expressionListWithCombinationOfReadExpressions = new List<IConstraintExpression>() { readExpression1, readExpression2 };

            var invertedList = constraintExpressionOperationService.GetInvertedReadExpression(expressionListWithCombinationOfReadExpressions);

            Assert.IsTrue(invertedList.Count == 2);
            Assert.AreEqual(readExpression1.ConstraintVariable, invertedList[0].ConstraintVariable);
            Assert.AreEqual(readExpression2.ConstraintVariable, invertedList[1].ConstraintVariable);
            Assert.AreEqual(LogicalConnective.Empty, invertedList[0].LogicalConnective);
            Assert.AreEqual((int)connective, -(int)invertedList[1].LogicalConnective);
            Assert.AreEqual((int)readExpression1.Predicate, -(int)invertedList[0].Predicate);
            Assert.AreEqual((int)readExpression2.Predicate, -(int)invertedList[1].Predicate);
        }

        [TestMethod]
        [Description("Used for cases such as a_r OR b_w AND c_r - logical connective should be correctly reassigned")]
        public void InvertCombinationOfReadOrWriteAndReadExpression()
        {
            IConstraintExpression readExpression1 = GenerateReadVOVExpression();
            IConstraintExpression writeExpression = GenerateWriteExpression();
            writeExpression.LogicalConnective = LogicalConnective.Or;
            IConstraintExpression readExpression2 = GenerateReadVOCExpression();
            readExpression2.LogicalConnective = LogicalConnective.And;

            var expressionListWithCombinationOfReadAndWriteExpressions = new List<IConstraintExpression>() 
            { 
                readExpression1, 
                writeExpression, 
                readExpression2 
            };

            var invertedList = constraintExpressionOperationService.GetInvertedReadExpression(expressionListWithCombinationOfReadAndWriteExpressions);

            Assert.IsTrue(invertedList.Count == 2);
            Assert.AreEqual(readExpression1.ConstraintVariable, invertedList[0].ConstraintVariable);
            Assert.AreEqual(readExpression2.ConstraintVariable, invertedList[1].ConstraintVariable);
            Assert.AreEqual(LogicalConnective.Empty, invertedList[0].LogicalConnective);
            Assert.AreEqual((int)writeExpression.LogicalConnective, -(int)invertedList[1].LogicalConnective);
            Assert.AreEqual((int)readExpression1.Predicate, -(int)invertedList[0].Predicate);
            Assert.AreEqual((int)readExpression2.Predicate, -(int)invertedList[1].Predicate);
        }

        [TestMethod]
        [Description("Used for cases such as a_r AND b_r OR c_r AND d_r - result must be a cartesian product to overcome hierarchy")]
        public void InvertDisjunctionOfConjunctionsOfReadExpressions()
        {
            IConstraintExpression readExpression1 = GenerateReadVOVExpression();
            IConstraintExpression readExpression2 = GenerateReadVOCExpression();
            readExpression2.LogicalConnective = LogicalConnective.And;
            IConstraintExpression readExpression3 = GenerateReadVOCExpression();
            readExpression3.LogicalConnective = LogicalConnective.Or;
            IConstraintExpression readExpression4 = GenerateReadVOCExpression();
            readExpression4.LogicalConnective = LogicalConnective.And;

            var expressionListWithCombinationOfReadExpressions = new List<IConstraintExpression>() 
            { 
                readExpression1, 
                readExpression2,
                readExpression3,
                readExpression4
            };

            var invertedList = constraintExpressionOperationService.GetInvertedReadExpression(expressionListWithCombinationOfReadExpressions);

            Assert.IsTrue(invertedList.Count == 2 * 2 * 2);
            Assert.IsTrue(invertedList.Count(x => x.ConstraintVariable == readExpression1.ConstraintVariable) == 2);
            Assert.IsTrue(invertedList.Count(x => x.ConstraintVariable == readExpression2.ConstraintVariable) == 2);
            Assert.IsTrue(invertedList.Count(x => x.ConstraintVariable == readExpression3.ConstraintVariable) == 2);
            Assert.IsTrue(invertedList.Count(x => x.ConstraintVariable == readExpression4.ConstraintVariable) == 2);

            Assert.IsTrue(invertedList[0].LogicalConnective == LogicalConnective.Empty);
            for (int i = 1; i < invertedList.Count; i += 2)
                Assert.IsTrue(invertedList[i].LogicalConnective == LogicalConnective.And);
            for (int i = 2; i < invertedList.Count; i += 2)
                Assert.IsTrue(invertedList[i].LogicalConnective == LogicalConnective.Or);
        }

        private static IConstraintExpression GenerateWriteExpression()
        {
            return new ConstraintVOCExpression<double>
            {
                Constant = new DefinableValue<double>(1111.44),
                LogicalConnective = LogicalConnective.Empty,
                ConstraintVariable = new ConstraintVariable
                {
                    VariableType = VariableType.Written,
                    Domain = DomainType.Integer,
                    Name = "TestRealVar" + variableIndex++
                },
                Predicate = BinaryPredicate.LessThan
            };
        }

        private static ConstraintVOCExpression<long> GenerateReadVOCExpression()
        {
            return new ConstraintVOCExpression<long>
            {
                Constant = new DefinableValue<long>(5000),
                LogicalConnective = LogicalConnective.Empty,
                ConstraintVariable = new ConstraintVariable
                {
                    VariableType = VariableType.Read,
                    Domain = DomainType.Integer,
                    Name = "TestIntegerVar" + variableIndex++
                },
                Predicate = BinaryPredicate.GreaterThan
            };
        }

        private static ConstraintVOVExpression GenerateReadVOVExpression()
        {
            return new ConstraintVOVExpression
            {
                LogicalConnective = LogicalConnective.Empty,
                Predicate = BinaryPredicate.Equal,
                ConstraintVariable = new ConstraintVariable
                {
                    VariableType = VariableType.Read,
                    Domain = DomainType.Boolean,
                    Name = "TestBoolVar" + variableIndex++
                },
                VariableToCompare = new ConstraintVariable
                {
                    VariableType = VariableType.Read,
                    Domain = DomainType.Boolean,
                    Name = "SecondTestBoolVar" + variableIndex++
                }
            };
        }
    }
}
