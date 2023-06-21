using DataPetriNetOnSmt.DPNElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public class CtTransition : AbstractTransition
    {
        public CtTransition(Transition transition, bool isSilent = false) : base(transition, isSilent)
        {
        }
    }
}
