using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;

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
