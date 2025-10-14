using DPN.Models.DPNElements;
using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;

namespace DPN.Soundness.TransitionSystems.Coverability
{
    public class CtTransition(Transition transition, bool isSilent = false) : AbstractTransition(transition, isSilent);
}
