using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;

namespace DataPetriNetVerificationDomain.GraphVisualized;

public class ArcToVisualize
{
    public string TransitionName { get; set; }
    public bool IsSilent { get; set; }
    public int SourceStateId { get; set; }
    public int TargetStateId { get; set; }

    public static ArcToVisualize FromArc(LtsArc arc)
    {
        return new ArcToVisualize
        {
            TransitionName = arc.Transition.Label,
            IsSilent = arc.Transition.IsSilent,
            SourceStateId = arc.SourceState.Id,
            TargetStateId = arc.TargetState.Id
        };
    }
}