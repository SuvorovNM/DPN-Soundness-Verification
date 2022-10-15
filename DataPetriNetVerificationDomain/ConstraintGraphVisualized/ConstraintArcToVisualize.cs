using DataPetriNetOnSmt.SoundnessVerification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetVerificationDomain.ConstraintGraphVisualized
{
    public class ConstraintArcToVisualize
    {
        public string TransitionName { get; set; }
        public bool IsSilent { get; set; }
        public int SourceStateId { get; set; }
        public int TargetStateId { get; set; }

        public ConstraintArcToVisualize(ConstraintArc arc)
        {
            TransitionName = arc.Transition.Label;
            IsSilent = arc.Transition.IsSilent;
            SourceStateId = arc.SourceState.Id;
            TargetStateId = arc.TargetState.Id;
        }

        public ConstraintArcToVisualize()
        {

        }
    }
}
