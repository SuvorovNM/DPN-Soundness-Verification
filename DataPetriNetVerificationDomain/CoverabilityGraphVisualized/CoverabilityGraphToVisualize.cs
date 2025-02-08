using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using DataPetriNetVerificationDomain.ConstraintGraphVisualized;

namespace DataPetriNetVerificationDomain.CoverabilityGraphVisualized
{
    public class CoverabilityGraphToVisualize
    {
        public List<CoverabilityStateToVisualize> CgStates { get; init; }
        public List<CoverabilityArcToVisualize> CgArcs { get; init; }
        public bool IsBounded { get; init; }
        public bool? IsSound { get; init; }

        public static CoverabilityGraphToVisualize FromCoverabilityGraph(CoverabilityGraph cg, SoundnessProperties? soundnessProperties = null)
        {
            var t = cg.ConstraintStates
                .Select(x => CoverabilityStateToVisualize.FromNode(x,
                    soundnessProperties.StateTypes.GetValueOrDefault(x, ConstraintStateType.Default)));
            
            return new CoverabilityGraphToVisualize
            {
                CgStates = cg.ConstraintStates
                    .Select(x => CoverabilityStateToVisualize.FromNode(x,
                        soundnessProperties.StateTypes.GetValueOrDefault(x, ConstraintStateType.Default)))

                            .ToList(),
                CgArcs = cg.ConstraintArcs
                            .Select(CoverabilityArcToVisualize.FromArc)
                            .ToList(),
                IsBounded = soundnessProperties?.Boundedness ??
                    cg.ConstraintStates.Any(s=>s.Marking.AsDictionary().Any(p=>p.Value == int.MaxValue)),
                IsSound = soundnessProperties?.Soundness
            };
        }
    }
}
