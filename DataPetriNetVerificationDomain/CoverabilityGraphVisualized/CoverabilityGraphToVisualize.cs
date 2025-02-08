using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using DataPetriNetVerificationDomain.ConstraintGraphVisualized;
using DataPetriNetVerificationDomain.GraphVisualized;

namespace DataPetriNetVerificationDomain.CoverabilityGraphVisualized
{
    public class CoverabilityGraphToVisualize
    {
        public List<StateToVisualize> CgStates { get; init; }
        public List<ArcToVisualize> CgArcs { get; init; }
        public SoundnessProperties? SoundnessProperties { get; init; }

        public static CoverabilityGraphToVisualize FromCoverabilityGraph(CoverabilityGraph cg, SoundnessProperties? soundnessProperties = null)
        {
            return new CoverabilityGraphToVisualize
            {
                CgStates = cg.ConstraintStates
                    .Select(x => StateToVisualize.FromNode(x,
                        soundnessProperties?.StateTypes.GetValueOrDefault(x, ConstraintStateType.Default) ?? ConstraintStateType.Default))
                            .ToList(),
                CgArcs = cg.ConstraintArcs
                            .Select(ArcToVisualize.FromArc)
                            .ToList(),
                SoundnessProperties = soundnessProperties
            };
        }
    }
}
