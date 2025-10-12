using DPN.Models.DPNElements;
using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;

namespace DPN.Soundness.TransitionSystems.CoverabilityTree
{
    public class CtTransition : AbstractTransition
    {
        public CtTransition(Transition transition, bool isSilent = false) : base(transition, isSilent)
        {
        }
    }
}
