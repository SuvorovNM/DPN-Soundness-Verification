using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public class LtsTransition : AbstractTransition
    {       
        public LtsTransition(Transition transition, bool isSilent = false) : base(transition, isSilent)
        {         
            
        }
    }
}
