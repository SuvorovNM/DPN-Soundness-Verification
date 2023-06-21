using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public abstract class AbstractTransition : Node
    {
        public bool IsSilent { get; set; }
        public string NonRefinedTransitionId { get; set; }

        public AbstractTransition(Transition transition, bool isSilent = false)
        {
            IsSilent = isSilent;
            Label = transition.Label;
            Id = transition.Id;

            NonRefinedTransitionId = transition.IsSplitted
                ? transition.BaseTransitionId
                : transition.Id;
        }
    }
}
