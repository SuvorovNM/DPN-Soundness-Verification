using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;

namespace DataPetriNetVerificationDomain.CoverabilityGraphVisualized
{
    public class CoverabilityArcToVisualize
    {
        public string TransitionName { get; set; }
        public bool IsSilent { get; set; }
        public int SourceStateId { get; set; }
        public int TargetStateId { get; set; }

        public static CoverabilityArcToVisualize FromArc(LtsArc arc)
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
