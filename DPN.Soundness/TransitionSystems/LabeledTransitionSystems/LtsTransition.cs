using DPN.Models.DPNElements;
using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;

namespace DPN.Soundness.TransitionSystems.LabeledTransitionSystems
{
    public class LtsTransition : AbstractTransition
    {       
        public LtsTransition(Transition transition, bool isSilent = false) : base(transition, isSilent)
        {         
            
        }
    }
}
