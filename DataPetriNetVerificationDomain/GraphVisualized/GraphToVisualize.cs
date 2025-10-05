using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;

namespace DataPetriNetVerificationDomain.GraphVisualized;

public class SoundnessPropertiesToVisualize
{
    public SoundnessPropertiesToVisualize(
        bool boundedness,
        string[] deadTransitions,
        bool? classicalSoundness,
        bool? relaxedLazySoundness)
    {
        Boundedness = boundedness;
        DeadTransitions = deadTransitions;
        ClassicalSoundness = classicalSoundness;
        RelaxedLazySoundness = relaxedLazySoundness;
    }

    public bool Boundedness { get; init; }
    public string[] DeadTransitions { get; init; }
    public bool? ClassicalSoundness { get; init; }
    public bool? RelaxedLazySoundness { get; init; }
    public bool Soundness => ClassicalSoundness.HasValue 
        ? ClassicalSoundness.Value 
        : RelaxedLazySoundness.HasValue
            ? RelaxedLazySoundness.Value
            : throw new ArgumentException("Cannot determine whether the model is sound");

    public static SoundnessPropertiesToVisualize FromSoundnessProperties(
        params SoundnessProperties[] soundnessProperties)
    {
        if (soundnessProperties.Length == 0)
        {
            throw new ArgumentException("Must have at least one soundness property", nameof(soundnessProperties));
        }

        if (soundnessProperties.Length == 1)
        {
            bool? classicalSoundness = soundnessProperties[0].SoundnessType == SoundnessType.Classical
                ? soundnessProperties[0].Soundness
                : null;
            bool? relaxedLazySoundness = soundnessProperties[0].SoundnessType == SoundnessType.RelaxedLazy
                ? soundnessProperties[0].Soundness
                : null;

            return new SoundnessPropertiesToVisualize(
                soundnessProperties[0].Boundedness,
                soundnessProperties[0].DeadTransitions,
                classicalSoundness,
                relaxedLazySoundness);
        }

        if (soundnessProperties.Length == 2)
        {
            var (classicalSoundnessProperties, relaxedLazySoundness) =
                soundnessProperties[0].SoundnessType == SoundnessType.Classical
                    ? (soundnessProperties[0], soundnessProperties[1])
                    : (soundnessProperties[1], soundnessProperties[0]);

            return new SoundnessPropertiesToVisualize(
                classicalSoundnessProperties.Boundedness,
                classicalSoundnessProperties.DeadTransitions,
                classicalSoundnessProperties.Soundness,
                relaxedLazySoundness.Soundness);
        }

        throw new ArgumentException("Too many soundness properties", nameof(soundnessProperties));
    }
}

public enum GraphType
{
    Lts,
    CoverabilityGraph,
    CoverabilityTree
}

public class GraphToVisualize
{
    public List<StateToVisualize> States { get; init; }
    public List<ArcToVisualize> Arcs { get; init; }
    public SoundnessPropertiesToVisualize? SoundnessProperties { get; init; }
    public bool IsFull { get; init; }
    public GraphType GraphType { get; init; }


    public static GraphToVisualize FromCoverabilityGraph(CoverabilityGraph cg,
        SoundnessProperties? soundnessProperties = null)
    {
        return new GraphToVisualize
        {
            States = cg.ConstraintStates
                .Select(x => StateToVisualize.FromNode(x,
                    soundnessProperties?.StateTypes.GetValueOrDefault(x, ConstraintStateType.Default) ??
                    ConstraintStateType.Default))
                .ToList(),

            Arcs = cg.ConstraintArcs
                .Select(ArcToVisualize.FromArc)
                .ToList(),

            SoundnessProperties = soundnessProperties != null
                ? SoundnessPropertiesToVisualize.FromSoundnessProperties(soundnessProperties)
                : null,

            IsFull = cg.IsFullGraph,

            GraphType = GraphType.CoverabilityGraph
        };
    }

    public static GraphToVisualize FromLts(LabeledTransitionSystem lts, SoundnessProperties? soundnessProperties)
    {
        return new GraphToVisualize
        {
            States = lts.ConstraintStates
                .Select(x => StateToVisualize.FromNode(x,
                    soundnessProperties?.StateTypes.GetValueOrDefault(x, ConstraintStateType.Default) ??
                    ConstraintStateType.Default))
                .ToList(),

            Arcs = lts.ConstraintArcs
                .Select(ArcToVisualize.FromArc)
                .ToList(),

            SoundnessProperties = soundnessProperties != null
                ? SoundnessPropertiesToVisualize.FromSoundnessProperties(soundnessProperties)
                : null,

            IsFull = lts.IsFullGraph,

            GraphType = GraphType.Lts
        };
    }

    public static GraphToVisualize FromCoverabilityTree(CoverabilityTree ct,
        SoundnessProperties? soundnessProperties = null)
    {
        var states = ct.ConstraintStates
            .Select(x => StateToVisualize.FromNode(x,
                soundnessProperties?.StateTypes.GetValueOrDefault(x, ConstraintStateType.Default) ??
                ConstraintStateType.Default))
            .ToList();

        var arcs = ct.ConstraintArcs
            .Select(ArcToVisualize.FromArc)
            .ToList();

        var soundnessProperties1 = soundnessProperties != null
            ? SoundnessPropertiesToVisualize.FromSoundnessProperties(soundnessProperties)
            : null;

        var isFull = true;

        var graphType = GraphType.CoverabilityTree;

        return new GraphToVisualize
        {
            States = states,
            Arcs = arcs,
            SoundnessProperties = soundnessProperties1,
            IsFull = isFull,
            GraphType = graphType
        };
    }
}