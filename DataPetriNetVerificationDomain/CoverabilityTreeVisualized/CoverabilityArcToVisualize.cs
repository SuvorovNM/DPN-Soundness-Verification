using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using DataPetriNetVerificationDomain.ConstraintGraphVisualized;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetVerificationDomain.CoverabilityTreeVisualized
{
    public class CoverabilityArcToVisualize
    {
        public string TransitionName { get; set; }
        public bool IsSilent { get; set; }
        public int SourceStateId { get; set; }
        public int TargetStateId { get; set; }

        public static CoverabilityArcToVisualize FromArc(CtArc arc)
        {
            return new CoverabilityArcToVisualize
            {
                TransitionName = arc.Transition.Label,
                IsSilent = arc.Transition.IsSilent,
                SourceStateId = arc.SourceState.Id,
                TargetStateId = arc.TargetState.Id
            };
        }
    }
}
