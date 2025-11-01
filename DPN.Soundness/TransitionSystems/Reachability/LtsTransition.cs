using DPN.Models.DPNElements;
using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;

namespace DPN.Soundness.TransitionSystems.Reachability
{
	internal class LtsTransition(Transition transition, bool isSilent = false) : AbstractTransition(transition, isSilent);
}
