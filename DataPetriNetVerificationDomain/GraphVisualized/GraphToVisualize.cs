using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;

namespace DataPetriNetVerificationDomain.GraphVisualized;

public class GraphToVisualize
{
    public List<StateToVisualize> States { get; init; }
    public List<ArcToVisualize> Arcs { get; init; }
    public SoundnessProperties? SoundnessProperties { get; init; }
    
    public static GraphToVisualize FromCoverabilityGraph(CoverabilityGraph cg, SoundnessProperties? soundnessProperties = null)
    {
        return new GraphToVisualize
        {
            States = cg.ConstraintStates
                .Select(x => StateToVisualize.FromNode(x,
                    soundnessProperties?.StateTypes.GetValueOrDefault(x, ConstraintStateType.Default) ?? ConstraintStateType.Default))
                .ToList(),
            Arcs = cg.ConstraintArcs
                .Select(ArcToVisualize.FromArc)
                .ToList(),
            SoundnessProperties = soundnessProperties
        };
    }
    
    public static GraphToVisualize FromLts
        (LabeledTransitionSystem lts, SoundnessProperties soundnessProperties)
    {
        return new GraphToVisualize
        {
            States = lts.ConstraintStates
                .Select(x => StateToVisualize.FromNode(x,
                    soundnessProperties.StateTypes.GetValueOrDefault(x, ConstraintStateType.Default)))
                .ToList(),

            Arcs = lts.ConstraintArcs
                .Select(ArcToVisualize.FromArc)
                .ToList(),

            SoundnessProperties = soundnessProperties
        };
    }
}