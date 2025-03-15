using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;

namespace DataPetriNetVerificationDomain.CoverabilityTreeVisualized
{
    public class CoverabilityTreeToVisualize
    {
        public List<CoverabilityStateToVisualize> CtStates { get; init; }
        public List<CoverabilityArcToVisualize> CtArcs { get; init; }

        public static CoverabilityTreeToVisualize FromCoverabilityTree(CoverabilityTree ct)
        {
            return new CoverabilityTreeToVisualize
            {
                CtStates = ct.ConstraintStates
                            .Select(x => CoverabilityStateToVisualize.FromNode(x))
                            .ToList(),
                CtArcs = ct.ConstraintArcs
                            .Select(x => CoverabilityArcToVisualize.FromArc(x))
                            .ToList()
            };
        }
    }
}
