using DPN.Models.Enums;
using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;
using Microsoft.Z3;

namespace DPN.Soundness.TransitionSystems.Coverability
{
    public class CtStateEqualityComparer : IEqualityComparer<CtState?>
    {
        public bool Equals(CtState? x, CtState? y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            if (y == null || x == null)
                return false;

            if (x.Constraints != null && y.Constraints != null)
            {
                return x.Constraints.Equals(y.Constraints) &&
                    x.StateType == y.StateType &&
                    x.StateColor == y.StateColor &&
                    x.Marking.CompareTo(y.Marking) == MarkingComparisonResult.Equal;
            }
            else if (x.Constraints == null && y.Constraints == null)
            {
                return x.StateType == y.StateType &&
                    x.StateColor == y.StateColor &&
                    x.Marking.CompareTo(y.Marking) == MarkingComparisonResult.Equal;
            }

            return false;
        }

        public int GetHashCode(CtState obj)
        {
            if (obj == null)
                return 0;
            return 7 * obj.Marking.AsDictionary().Sum(x => x.Key.GetHashCode() * x.Value) + 6803 * obj.Constraints?.GetHashCode() ?? 0;

        }
    }
    public class CtState : AbstractState
    {
        public CtStateType StateType { get; set; } = CtStateType.NonCovered;
        public CtStateColor StateColor { get; set; } = CtStateColor.Undefined;
        public CtState? CoveredNode { get; set; }
        public CtState? ParentNode { get; set; }

        public CtState() { }
        public CtState(Context context) : base(context)
        {

        }
        public CtState(BaseStateInfo stateInfo, CtState parent, CtStateType stateType = CtStateType.NonCovered, CtState? coveredNode = null) : base(stateInfo)
        {
            ParentNode = parent;
            StateType = stateType;
            CoveredNode = coveredNode;
            // Currently, we postpone definition of the color
        }
       
    }
}
