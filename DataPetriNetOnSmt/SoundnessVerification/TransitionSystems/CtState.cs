using DataPetriNetOnSmt.Enums;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public class CtState : AbstractState//<CtState>
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
