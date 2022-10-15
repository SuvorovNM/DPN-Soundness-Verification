using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetVerificationDomain.ConstraintGraphVisualized
{
    public class ConstraintGraphToVisualize
    {
        public List<ConstraintStateToVisualize> ConstraintStates { get; init; }
        public List<ConstraintArcToVisualize> ConstraintArcs { get; init; }
        public bool IsBounded { get; init; }
        public bool IsSound { get; init; }
        public List<string> DeadTransitions { get; init; }
        

        public ConstraintGraphToVisualize(ConstraintGraph constraintGraph, SoundnessProperties soundnessProperties)
        {
            IsBounded = soundnessProperties.Boundedness;
            IsSound = soundnessProperties.Soundness;

            ConstraintStates = constraintGraph.ConstraintStates
                .Select(x => new ConstraintStateToVisualize(x, soundnessProperties.StateTypes.GetValueOrDefault(x, ConstraintStateType.Default)))
                .ToList();

            ConstraintArcs = constraintGraph.ConstraintArcs
                .Select(x => new ConstraintArcToVisualize(x))
                .ToList();

            DeadTransitions = soundnessProperties.DeadTransitions;
        }

        public ConstraintGraphToVisualize()
        {
            ConstraintStates = new List<ConstraintStateToVisualize>();
            ConstraintArcs = new List<ConstraintArcToVisualize>();
        }
    }
}
