using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using Microsoft.Z3;
using System.Globalization;

namespace DataPetriNetOnSmt.DPNElements
{
    public class Guard : ICloneable
    {
        private bool readNeedsToBeRecalculated = false;
        private Dictionary<string, DomainType> readVars = new Dictionary<string, DomainType>();
        public Context Context { get; set; }

        public List<IConstraintExpression> BaseConstraintExpressions { get; init; }
        public BoolExpr ActualConstraintExpression { get; init; }

        public Dictionary<string, DomainType> WriteVars { get; init; }
        public Dictionary<string, DomainType> ReadVars 
        { 
            get 
            { 
                if (readNeedsToBeRecalculated)
                {
                    readVars = ActualConstraintExpression.GetTypedVarsDict(VariableType.Read);
                    readNeedsToBeRecalculated = false;
                }
                return readVars;
            }
            set
            {
                readVars = value;
            }
        }

        public Guard(Context ctx, List<IConstraintExpression>? baseConstraints = null)
        {
            if (baseConstraints == null)
            {
                BaseConstraintExpressions = new List<IConstraintExpression>();
                ActualConstraintExpression = ctx.MkTrue();
                WriteVars = new Dictionary<string, DomainType>();
            }
            else
            {
                BaseConstraintExpressions = baseConstraints;
                ActualConstraintExpression = ctx.GetSmtExpression(baseConstraints);
                WriteVars = BaseConstraintExpressions.GetTypedVarsDict(VariableType.Written);
                ReadVars = BaseConstraintExpressions.GetTypedVarsDict(VariableType.Read);
            }

            Context = ctx;            
        }

        public Guard(Context ctx, List<IConstraintExpression> baseConstraints, BoolExpr actualConstraintExpression)
        {
            BaseConstraintExpressions = baseConstraints;
            ActualConstraintExpression = actualConstraintExpression;
            Context = ctx;

            WriteVars = BaseConstraintExpressions.GetTypedVarsDict(VariableType.Written);
            //ReadVars = ActualConstraintExpression.GetTypedVarsDict(VariableType.Read);

            readNeedsToBeRecalculated = true;
        }

        public object Clone()
        {
            var clonedGuard = new Guard(
                Context, 
                BaseConstraintExpressions.Select(x => x.Clone()).ToList(), 
                ActualConstraintExpression);
            return clonedGuard;
        }
    }
}
