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
    public class ConstraintGraphToVisualize
    {
        public List<ConstraintStateToVisualize> ConstraintStates { get; init; }
        public List<ConstraintArcToVisualize> ConstraintArcs { get; init; }
        public bool IsBounded { get; init; }
        public bool IsSound { get; init; }
        public List<string> DeadTransitions { get; init; }


        public static ConstraintGraphToVisualize FromStateSpaceStructure<AbsState, AbsTransition, AbsArc>
            (AbstractStateSpaceStructure<AbsState, AbsTransition, AbsArc> lts, SoundnessProperties soundnessProperties)
            where AbsState : AbstractState, new()
            where AbsTransition : AbstractTransition
            where AbsArc : AbstractArc<AbsState, AbsTransition>
        {
            return new ConstraintGraphToVisualize
            {
                IsBounded = soundnessProperties.Boundedness,
                IsSound = soundnessProperties.Soundness,

                ConstraintStates = lts.ConstraintStates
                .Select(x => ConstraintStateToVisualize.FromNode(x,
                    soundnessProperties.StateTypes.GetValueOrDefault(x, ConstraintStateType.Default)))
                .ToList(),

                ConstraintArcs = lts.ConstraintArcs
                .Select(x => ConstraintArcToVisualize.FromArc(x))
                .ToList(),

                DeadTransitions = soundnessProperties.DeadTransitions
            };
        }

        public ConstraintGraphToVisualize()
        {
            ConstraintStates = new List<ConstraintStateToVisualize>();
            ConstraintArcs = new List<ConstraintArcToVisualize>();
        }
    }
}
