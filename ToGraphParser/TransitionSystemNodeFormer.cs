using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetVerificationDomain;
using DataPetriNetVerificationDomain.GraphVisualized;
using Microsoft.Msagl.Drawing;

namespace DataPetriNetParsers;

public static class TransitionSystemNodeFormer
{
    public static Node FormNode(StateToVisualize state, string tokens, string constraintFormula, SoundnessType soundnessType)
    {
        var nodeName = $"Id:{state.Id} [{tokens}] ({constraintFormula})";

        return soundnessType switch
        {
            SoundnessType.None => CreateNodeDespiteSoundness(state, nodeName),
            SoundnessType.ClassicalSoundness => CreateNodeForClassicalSoundness(state, nodeName),
            SoundnessType.LazySoundness => CreateNodeForLazySoundness(state, nodeName),
            _ => throw new ArgumentOutOfRangeException(nameof(soundnessType), soundnessType, "Unknown soundness type")
        };
    }

    private static Node CreateNodeDespiteSoundness(StateToVisualize state, string name)
    {
        var node = new Node(name);
        node.Attr.Shape = Shape.Box;

        if (state.Tokens.Any(x => x.Value == int.MaxValue))
        {
            node.Attr.FillColor = Color.LightGray;
        }

        return node;
    }

    private static Node CreateNodeForClassicalSoundness(StateToVisualize state, string name)
    {
        var node = new Node(name);
        node.Attr.Shape = Shape.Box;
        
        if (state.StateType.HasFlag(ConstraintStateType.Initial))
        {
            node.Attr.LineWidth = 2;
        }

        if (state.StateType.HasFlag(ConstraintStateType.Deadlock))
        {
            node.Attr.FillColor = Color.Pink;
        }

        if (state.StateType.HasFlag(ConstraintStateType.Final))
        {
            node.Attr.FillColor = Color.LightGreen;
        }

        if (state.StateType.HasFlag(ConstraintStateType.UncleanFinal))
        {
            node.Attr.FillColor = Color.LightBlue;
        }

        if (state.StateType.HasFlag(ConstraintStateType.NoWayToFinalMarking))
        {
            node.Attr.Color = Color.Red;
        }

        if (state.StateType.HasFlag(ConstraintStateType.StrictlyCovered))
        {
            node.Attr.FillColor = Color.Red;
        }

        return node;
    }

    private static Node CreateNodeForLazySoundness(StateToVisualize state, string name)
    {
        var node = new Node(name);
        node.Attr.Shape = Shape.Box;

        if (state.StateType.HasFlag(ConstraintStateType.Initial))
        {
            node.Attr.LineWidth = 2;
        }
        if (state.StateType.HasFlag(ConstraintStateType.Deadlock))
        {
            node.Attr.FillColor = Color.Pink;
        }
        if (state.StateType.HasFlag(ConstraintStateType.Final))
        {
            node.Attr.FillColor = Color.LightGreen;
        }
        if (state.StateType.HasFlag(ConstraintStateType.NoWayToFinalMarking))
        {
            node.Attr.Color = Color.Red;
        }
        if (state.StateType.HasFlag(ConstraintStateType.StrictlyCovered))
        {
            node.Attr.FillColor = Color.LightGray;
        }
        if (state.StateType.HasFlag(ConstraintStateType.UncleanFinal))
        {
            node.Attr.FillColor = Color.LightBlue;
        }

        return node;
    }
}