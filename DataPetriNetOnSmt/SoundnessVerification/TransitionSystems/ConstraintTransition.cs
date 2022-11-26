using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public class ConstraintTransition : Node
    {
        public bool IsSilent { get; set; }
        public string NonRefinedTransitionId { get; set; }

        public ConstraintTransition(Transition transition, bool isSilent = false)
        {
            IsSilent = isSilent;
            Label = transition.Label;
            Id = transition.Id;
            NonRefinedTransitionId = transition.IsSplitted 
                ? transition.BaseTransitionId
                : transition.Id;
        }
    }
}
