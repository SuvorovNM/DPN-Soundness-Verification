using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetVerificationDomain.ConstraintGraphVisualized
{
    public class LtsToVisualize
    {
        public List<ConstraintStateToVisualize> ConstraintStates { get; init; }
        public List<ConstraintArcToVisualize> ConstraintArcs { get; init; }
        public bool IsBounded { get; init; }
        public bool IsSound { get; init; }
        public List<string> DeadTransitions { get; init; }
        

        public LtsToVisualize(LabeledTransitionSystem lts, SoundnessProperties soundnessProperties)
        {
            IsBounded = soundnessProperties.Boundedness;
            IsSound = soundnessProperties.Soundness;

            ConstraintStates = lts.ConstraintStates
                .Select(x => new ConstraintStateToVisualize(x, soundnessProperties.StateTypes.GetValueOrDefault(x, ConstraintStateType.Default)))
                .ToList();

            ConstraintArcs = lts.ConstraintArcs
                .Select(x => new ConstraintArcToVisualize(x))
                .ToList();

            DeadTransitions = soundnessProperties.DeadTransitions;
        }

        public LtsToVisualize()
        {
            ConstraintStates = new List<ConstraintStateToVisualize>();
            ConstraintArcs = new List<ConstraintArcToVisualize>();
        }
    }
}
