using DPN.Models.Enums;
using DPN.SoundnessVerification;
using DPN.SoundnessVerification.TransitionSystems;

namespace DPN.Visualization.Models;

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