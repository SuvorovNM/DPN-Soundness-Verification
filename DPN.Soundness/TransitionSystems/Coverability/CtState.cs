using DPN.Models.Enums;
using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;
using Microsoft.Z3;

namespace DPN.Soundness.TransitionSystems.Coverability
{
	internal class CtState : AbstractState
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
