using DataPetriNetOnSmt.Enums;
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

        public ConstraintStateToVisualize(ConstraintState constraintState, ConstraintStateType stateType)
        {
            Id = constraintState.Id;

            ConstraintFormula = constraintState.Constraints.ToString();

            Tokens = constraintState.PlaceTokens.ToDictionary(x=>x.Key.Id, y=>y.Value);

            StateType = stateType;
        }

        public ConstraintStateToVisualize()
        {

        }
    }
}
