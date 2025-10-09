using DPN.Models.DPNElements;

namespace DPN.SoundnessVerification.TransitionSystems
{
    public class CtTransition : AbstractTransition
    {
        public CtTransition(Transition transition, bool isSilent = false) : base(transition, isSilent)
        {
        }
    }
}
