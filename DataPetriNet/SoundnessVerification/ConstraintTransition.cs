using DataPetriNet.Abstractions;
using DataPetriNet.DPNElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.SoundnessVerification
{
    public class ConstraintTransition : Node
    {
        public bool IsSilent { get; set; }

        public ConstraintTransition(Transition transition, bool isSilent = false)
        {
            IsSilent = isSilent;
            Label = transition.Label;
        }
    }
}
