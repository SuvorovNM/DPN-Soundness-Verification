using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public class LtsTransition : AbstractTransition
    {
        public string NonRefinedTransitionId { get; set; }

        public LtsTransition(Transition transition, bool isSilent = false) : base(transition, isSilent)
        {         
            NonRefinedTransitionId = transition.IsSplitted 
                ? transition.BaseTransitionId
                : transition.Id;
        }
    }
}
