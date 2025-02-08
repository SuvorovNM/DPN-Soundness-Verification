using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;

namespace DataPetriNetVerificationDomain.GraphVisualized;

public class SoundnessPropertiesToVisualize
{
    public SoundnessPropertiesToVisualize(
        bool boundedness,
        string[] deadTransitions,
        bool soundness)
    {
        Boundedness = boundedness;
        DeadTransitions = deadTransitions;
        Soundness = soundness;
    }

    public SoundnessType SoundnessType { get; init; }
    public bool Boundedness { get; init; }
    public string[] DeadTransitions { get; init; }
    public bool Soundness { get; init; }

    public static SoundnessPropertiesToVisualize FromSoundnessProperties(SoundnessProperties soundnessProperties)
    {
        return new SoundnessPropertiesToVisualize(
            soundnessProperties.Boundedness,
            soundnessProperties.DeadTransitions,
            soundnessProperties.Soundness);
    }
}


public class GraphToVisualize
{
    public List<StateToVisualize> States { get; init; }
    public List<ArcToVisualize> Arcs { get; init; }
    public SoundnessPropertiesToVisualize? SoundnessProperties { get; init; }
    
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
            SoundnessProperties = soundnessProperties != null 
                ? SoundnessPropertiesToVisualize.FromSoundnessProperties(soundnessProperties) 
                : null
        };
    }
    
    public static GraphToVisualize FromLts
        (LabeledTransitionSystem lts, SoundnessProperties? soundnessProperties)
    {
        return new GraphToVisualize
        {
            States = lts.ConstraintStates
                .Select(x => StateToVisualize.FromNode(x,
                    soundnessProperties?.StateTypes.GetValueOrDefault(x, ConstraintStateType.Default) ?? ConstraintStateType.Default))
                .ToList(),

            Arcs = lts.ConstraintArcs
                .Select(ArcToVisualize.FromArc)
                .ToList(),

            SoundnessProperties = soundnessProperties != null 
                ? SoundnessPropertiesToVisualize.FromSoundnessProperties(soundnessProperties) 
                : null
        };
    }
}