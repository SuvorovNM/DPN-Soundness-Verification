using DPN.Models.Abstractions;
using DPN.Models.DPNElements;

namespace DPN.SoundnessVerification.TransitionSystems
{
    public abstract class AbstractTransition : Node
    {
        public bool IsSilent { get; set; }
        public string NonRefinedTransitionId { get; set; }

        public AbstractTransition(Transition transition, bool isSilent = false)
        {
            IsSilent = isSilent;
            Label = transition.Label;
            Id = transition.Id;

            NonRefinedTransitionId = transition.IsSplit
                ? transition.BaseTransitionId
                : transition.Id;
        }
    }
}
