using DataPetriNet.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.ConstraintGraph
{
    public class ConstraintExpressionOperationService
    {
        public List<IConstraintExpression> InverseExpression(List<IConstraintExpression> expression)
        {
            throw new NotImplementedException();
        }

        public List<IConstraintExpression> ShortenExpression(List<IConstraintExpression> expression)
        {
            throw new NotImplementedException();
        }

        public bool CanBeSatisfied(List<IConstraintExpression> expression)
        {
            throw new NotImplementedException();
        }

        public List<IConstraintExpression> ConcatExpressions(List<IConstraintExpression> source, List<IConstraintExpression> target)
        {
            throw new NotImplementedException();
        }
    }
}
