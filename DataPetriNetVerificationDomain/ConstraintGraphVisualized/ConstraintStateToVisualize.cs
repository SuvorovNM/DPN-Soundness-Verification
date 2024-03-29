﻿using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetVerificationDomain.ConstraintGraphVisualized
{
    public class ConstraintStateToVisualize
    {
        public int Id { get; set; }
        public Dictionary<string, int> Tokens { get; set; }
        public string ConstraintFormula { get; set; }
        public ConstraintStateType StateType { get; set; }

        public static ConstraintStateToVisualize FromNode<AbsState>(AbsState state, ConstraintStateType stateType)
            where AbsState : AbstractState
        {
            return new ConstraintStateToVisualize
            {
                Id = state.Id,
                ConstraintFormula = state.Constraints.ToString(),
                Tokens = state.Marking.AsDictionary(),
                StateType = stateType
            };
        }

        public ConstraintStateToVisualize()
        {

        }
    }
}
