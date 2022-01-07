using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification
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
