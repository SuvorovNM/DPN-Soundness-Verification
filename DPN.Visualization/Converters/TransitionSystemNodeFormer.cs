using DPN.Models.Enums;
using DPN.Soundness;
using DPN.Soundness.TransitionSystems;
using DPN.Visualization.Models;
using Microsoft.Msagl.Drawing;

namespace DPN.Visualization.Converters;

internal static class TransitionSystemNodeFormer
{
    internal static Node FormNode(StateToVisualize state, string tokens, string constraintFormula, SoundnessType soundnessType)
    {
        var nodeName = $"Id:{state.Id} [{tokens}] ({constraintFormula})";

        return soundnessType switch
        {
            SoundnessType.None => CreateNodeDespiteSoundness(state, nodeName),
            SoundnessType.Classical => CreateNodeForClassicalSoundness(state, nodeName),
            SoundnessType.RelaxedLazy => CreateNodeForLazySoundness(state, nodeName),
            _ => throw new ArgumentOutOfRangeException(nameof(soundnessType), soundnessType, "Unknown soundness type")
        };
    }

    private static Node CreateNodeDespiteSoundness(StateToVisualize state, string name)
    {
        var node = new Node(name)
        {
	        Attr =
	        {
		        Shape = Shape.Box
	        }
        };

        if (state.Tokens.Any(x => x.Value == int.MaxValue))
        {
            node.Attr.FillColor = Color.LightGray;
        }

        return node;
    }

    private static Node CreateNodeForClassicalSoundness(StateToVisualize state, string name)
    {
        var node = new Node(name)
        {
	        Attr =
	        {
		        Shape = Shape.Box
	        }
        };

        if (state.StateType.HasFlag(StateType.Initial))
        {
            node.Attr.LineWidth = 2;
        }

        if (state.StateType.HasFlag(StateType.Deadlock))
        {
            node.Attr.FillColor = Color.Pink;
        }

        if (state.StateType.HasFlag(StateType.Final))
        {
            node.Attr.FillColor = Color.LightGreen;
        }

        if (state.StateType.HasFlag(StateType.UncleanFinal))
        {
            node.Attr.FillColor = Color.LightBlue;
        }

        if (state.StateType.HasFlag(StateType.NoWayToFinalMarking))
        {
            node.Attr.Color = Color.Red;
        }

        if (state.StateType.HasFlag(StateType.StrictlyCovered))
        {
            node.Attr.FillColor = Color.Red;
        }

        return node;
    }

    private static Node CreateNodeForLazySoundness(StateToVisualize state, string name)
    {
        var node = new Node(name)
        {
	        Attr =
	        {
		        Shape = Shape.Box
	        }
        };

        if (state.StateType.HasFlag(StateType.Initial))
        {
            node.Attr.LineWidth = 2;
        }
        if (state.StateType.HasFlag(StateType.Deadlock))
        {
            node.Attr.FillColor = Color.Pink;
        }
        if (state.StateType.HasFlag(StateType.Final))
        {
            node.Attr.FillColor = Color.LightGreen;
        }
        if (state.StateType.HasFlag(StateType.NoWayToFinalMarking))
        {
            node.Attr.Color = Color.Red;
        }
        if (state.StateType.HasFlag(StateType.StrictlyCovered))
        {
            node.Attr.FillColor = Color.LightGray;
        }
        if (state.StateType.HasFlag(StateType.UncleanFinal))
        {
            node.Attr.FillColor = Color.LightBlue;
        }

        return node;
    }
}