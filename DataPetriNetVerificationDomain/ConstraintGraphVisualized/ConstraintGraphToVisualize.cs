using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataPetriNetVerificationDomain.GraphVisualized;

namespace DataPetriNetVerificationDomain.ConstraintGraphVisualized
{
    public class ConstraintGraphToVisualize
    {
        public List<StateToVisualize> ConstraintStates { get; init; }
        public List<ArcToVisualize> ConstraintArcs { get; init; }
        public SoundnessProperties SoundnessProperties { get; init; }


        public static ConstraintGraphToVisualize FromStateSpaceStructure
            (LabeledTransitionSystem lts, SoundnessProperties soundnessProperties)
        {
            return new ConstraintGraphToVisualize
            {
                ConstraintStates = lts.ConstraintStates
                .Select(x => StateToVisualize.FromNode(x,
                    soundnessProperties.StateTypes.GetValueOrDefault(x, ConstraintStateType.Default)))
                .ToList(),

                ConstraintArcs = lts.ConstraintArcs
                .Select(ArcToVisualize.FromArc)
                .ToList(),

                SoundnessProperties = soundnessProperties
            };
        }

        public ConstraintGraphToVisualize()
        {
            ConstraintStates = new List<StateToVisualize>();
            ConstraintArcs = new List<ArcToVisualize>();
        }
    }
}
