using DPN.Models.Abstractions;
using DPN.Models.DPNElements;

namespace DPN.Soundness.TransitionSystems.StateSpaceAbstraction
{
	internal class AbstractTransition : Node
    {
        public bool IsSilent { get; set; }
        public string NonRefinedTransitionId { get; set; }

        protected AbstractTransition(Transition transition, bool isSilent = false)
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
