using DataPetriNet.Abstractions;
using DataPetriNet.DPNElements.Internals;
using DataPetriNet.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.DPNElements
{
    public class ConstraintExpression<T> : IConstraintExpression
        // TODO: consider write-read vars + AND/OR
        where T : IEquatable<T>, IComparable<T>
    {
        public LogicalConnective LogicalConnective { get; set; }
        public ConstraintVariable ConstraintVariable { get; set; }
        public BinaryPredicate Predicate { get; set; }
        public T Constant { get; set; }

        public bool Evaluate(T variableValue)
        {
            return Predicate switch
            {
                BinaryPredicate.Equal => variableValue.Equals(Constant),
                BinaryPredicate.Unequal => !variableValue.Equals(Constant),
                BinaryPredicate.GreaterThan => variableValue.CompareTo(Constant) > 0,
                BinaryPredicate.GreaterThenOrEqual => variableValue.CompareTo(Constant) >= 0,
                BinaryPredicate.LessThan => variableValue.CompareTo(Constant) < 0,
                BinaryPredicate.LessThanOrEqual => variableValue.CompareTo(Constant) <= 0,

                _ => true,
            };
        }

        public ValueInterval<T> GetValueInterval()
        {
            return Predicate switch
            {
                BinaryPredicate.Equal => new ValueInterval<T> { Start = new IntervalPoint<T>(Constant), End = new IntervalPoint<T>(Constant) },
                BinaryPredicate.Unequal => new ValueInterval<T> { ForbiddenValue = new IntervalPoint<T>(Constant) },
                BinaryPredicate.GreaterThan => new ValueInterval<T> { Start = new IntervalPoint<T>(Constant), ForbiddenValue = new IntervalPoint<T>(Constant) },
                BinaryPredicate.GreaterThenOrEqual => new ValueInterval<T> { Start = new IntervalPoint<T>(Constant) },
                BinaryPredicate.LessThan => new ValueInterval<T> { End = new IntervalPoint<T>(Constant), ForbiddenValue = new IntervalPoint<T>(Constant) },
                BinaryPredicate.LessThanOrEqual => new ValueInterval<T> { End = new IntervalPoint<T>(Constant) },

                _ => throw new NotImplementedException("Operation is not supported")
            };
        }
    }
}
