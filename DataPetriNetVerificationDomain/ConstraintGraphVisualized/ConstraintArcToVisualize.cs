﻿using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetVerificationDomain.ConstraintGraphVisualized
{
    public class ConstraintArcToVisualize
    {
        public string TransitionName { get; set; }
        public bool IsSilent { get; set; }
        public int SourceStateId { get; set; }
        public int TargetStateId { get; set; }

        public static ConstraintArcToVisualize FromArc<AbsState, AbsTransition>(AbstractArc<AbsState, AbsTransition> arc)
            where AbsState : AbstractState
            where AbsTransition : AbstractTransition
        {
            return new ConstraintArcToVisualize
            {
                TransitionName = arc.Transition.Label,
                IsSilent = arc.Transition.IsSilent,
                SourceStateId = arc.SourceState.Id,
                TargetStateId = arc.TargetState.Id
            };
        }

        public ConstraintArcToVisualize()
        {

        }
    }
}
