using DataPetriNet.Abstractions;
using DataPetriNet.DPNElements.Internals;
using DataPetriNet.Enums;
using System;

namespace DataPetriNet.DPNElements
{
    public class ConstraintExpression<T> : IConstraintExpression
        where T : IEquatable<T>, IComparable<T>
    {
        public LogicalConnective LogicalConnective { get; set; }
        public ConstraintVariable ConstraintVariable { get; set; }
        public BinaryPredicate Predicate { get; set; }
        public DefinableValue<T> Constant { get; set; }

        public bool Equals(ConstraintExpression<T> other)
        {
            return ConstraintVariable == other.ConstraintVariable &&
                Predicate == other.Predicate &&
                Constant == other.Constant;
        }

        public bool Evaluate(DefinableValue<T> variableValue)
        {
            return Predicate switch
            {
                BinaryPredicate.Equal => variableValue.Equals(Constant),
                BinaryPredicate.Unequal => !variableValue.Equals(Constant),
                BinaryPredicate.GreaterThan => variableValue.Value.CompareTo(Constant.Value) > 0,
                BinaryPredicate.GreaterThanOrEqual => variableValue.Value.CompareTo(Constant.Value) >= 0,
                BinaryPredicate.LessThan => variableValue.Value.CompareTo(Constant.Value) < 0,
                BinaryPredicate.LessThanOrEqual => variableValue.Value.CompareTo(Constant.Value) <= 0,

                _ => true,
            };
        }

        public IConstraintExpression GetInvertedExpression()
        {
            var expression = new ConstraintExpression<T>();
            expression.Constant = Constant;
            expression.Predicate = (BinaryPredicate)(-(long)Predicate);
            expression.LogicalConnective = (LogicalConnective)(-(long)LogicalConnective);
            expression.ConstraintVariable = ConstraintVariable;

            return expression;
        }

        public ValueInterval<T> GetValueInterval()
        {
            return Predicate switch
            {
                BinaryPredicate.Equal => new ValueInterval<T> { Start = new IntervalPoint<T>(Constant), End = new IntervalPoint<T>(Constant) },
                BinaryPredicate.Unequal => new ValueInterval<T> { ForbiddenValue = new IntervalPoint<T>(Constant) },
                BinaryPredicate.GreaterThan => new ValueInterval<T> { Start = new IntervalPoint<T>(Constant), ForbiddenValue = new IntervalPoint<T>(Constant) },
                BinaryPredicate.GreaterThanOrEqual => new ValueInterval<T> { Start = new IntervalPoint<T>(Constant) },
                BinaryPredicate.LessThan => new ValueInterval<T> { End = new IntervalPoint<T>(Constant), ForbiddenValue = new IntervalPoint<T>(Constant) },
                BinaryPredicate.LessThanOrEqual => new ValueInterval<T> { End = new IntervalPoint<T>(Constant) },

                _ => throw new NotImplementedException("Operation is not supported")
            };
        }

        public static ConstraintExpression<T> GenerateUnequalExpression(string name, DomainType domain, DefinableValue<T> forbiddenValue)
        {
            return new ConstraintExpression<T>
            {
                Constant = forbiddenValue,
                LogicalConnective = LogicalConnective.And,
                Predicate = BinaryPredicate.Unequal,
                ConstraintVariable = new ConstraintVariable
                {
                    Domain = domain,
                    Name = name,
                    VariableType = VariableType.Read
                }
            };
        }

        public static ConstraintExpression<T> GenerateGreaterThanOrEqualExpression(string name, DomainType domain, T minimalValue)
        {
            return new ConstraintExpression<T>
            {
                Constant = new DefinableValue<T>(minimalValue),
                LogicalConnective = LogicalConnective.And,
                Predicate = BinaryPredicate.GreaterThanOrEqual,
                ConstraintVariable = new ConstraintVariable
                {
                    Domain = domain,
                    Name = name,
                    VariableType = VariableType.Read
                }
            };
        }

        public static ConstraintExpression<T> GenerateLessThanOrEqualExpression(string name, DomainType domain, T maximalValue)
        {
            return new ConstraintExpression<T>
            {
                Constant = new DefinableValue<T>(maximalValue),
                LogicalConnective = LogicalConnective.And,
                Predicate = BinaryPredicate.LessThanOrEqual,
                ConstraintVariable = new ConstraintVariable
                {
                    Domain = domain,
                    Name = name,
                    VariableType = VariableType.Read
                }
            };
        }

        public static ConstraintExpression<T> GenerateEqualExpression(string name, DomainType domain, DefinableValue<T> constantValue)
        {
            return new ConstraintExpression<T>
            {
                Constant = constantValue,
                LogicalConnective = LogicalConnective.And,
                Predicate = BinaryPredicate.Equal,
                ConstraintVariable = new ConstraintVariable
                {
                    Domain = domain,
                    Name = name,
                    VariableType = VariableType.Read
                }
            };
        }
    }
}
