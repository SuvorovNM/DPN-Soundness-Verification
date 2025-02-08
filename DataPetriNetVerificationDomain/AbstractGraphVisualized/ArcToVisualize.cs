using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using DataPetriNetVerificationDomain.CoverabilityGraphVisualized;

namespace DataPetriNetVerificationDomain.AbstractGraphVisualized;

public class ArcToVisualize
{
    public string TransitionName { get; set; }
    public bool IsSilent { get; set; }
    public int SourceStateId { get; set; }
    public int TargetStateId { get; set; }

    public static CoverabilityArcToVisualize FromArc<AbsArc,AbsState,AbsTransition>(AbsArc arc)
        where AbsArc : AbstractArc<AbsState,AbsTransition>
        where AbsState : AbstractState
        where AbsTransition : AbstractTransition
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