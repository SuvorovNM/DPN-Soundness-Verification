using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using DataPetriNetVerificationDomain.ConstraintGraphVisualized;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetVerificationDomain.CoverabilityTreeVisualized
{
    public class CoverabilityStateToVisualize
    {
        public int Id { get; set; }
        public Dictionary<string, int> Tokens { get; set; }
        public string ConstraintFormula { get; set; }
        public CtStateColor StateColor { get; set; }

        public static CoverabilityStateToVisualize FromNode(CtState state)
        {
            return new CoverabilityStateToVisualize
            {
                Id = state.Id,
                ConstraintFormula = state.Constraints.ToString(),
                Tokens = state.Marking.AsDictionary(),
                StateColor = state.StateColor
            };
        }
    }
}
