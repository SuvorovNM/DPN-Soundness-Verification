using DPN.Models.DPNElements;

namespace DPN.SoundnessVerification.TransitionSystems
{
    public class LtsTransition : AbstractTransition
    {       
        public LtsTransition(Transition transition, bool isSilent = false) : base(transition, isSilent)
        {         
            
        }
    }
}
