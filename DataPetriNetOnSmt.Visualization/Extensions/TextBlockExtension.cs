using DataPetriNetOnSmt.Enums;
using EnumsNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetVerificationDomain.GraphVisualized;

namespace DataPetriNetOnSmt.Visualization.Extensions
{
    public static class TextBlockExtension
    {
        public static void FormOutput(this TextBlock textBlock, GraphToVisualize graph)
        {
            ArgumentNullException.ThrowIfNull(graph);

            textBlock.FontSize = 14;
            textBlock.Inlines.Clear();

            switch (graph.SoundnessProperties?.SoundnessType)
            {
                case null or SoundnessType.None:
                    textBlock.Inlines.Add(FormGraphInfoLines(graph));
                    break;
                case SoundnessType.Classical or SoundnessType.Lazy:
                    textBlock.FormSoundnessVerificationLog(graph);
                    break;
            }
        }

        public static void FormSoundnessVerificationLog(this TextBlock textBlock, GraphToVisualize graph)
        {
            ArgumentNullException.ThrowIfNull(graph);

            textBlock.FontSize = 14;
            textBlock.Inlines.Clear();

            textBlock.Inlines.Add(new Bold(graph.SoundnessProperties!.Soundness
                ? new Run(FormSoundLine(graph.SoundnessProperties.SoundnessType)) { Foreground = Brushes.DarkGreen }
                : new Run(FormUnsoundLine(graph.SoundnessProperties.SoundnessType)) { Foreground = Brushes.DarkRed }));

            textBlock.Inlines.Add(new Bold(graph.SoundnessProperties.Boundedness
                ? new Run(FormBoundedLine())
                : new Run(FormUnboundedLine(isCoverability: false))));

            textBlock.Inlines.Add(FormGraphInfoLines(graph));

            if (graph.SoundnessProperties.Boundedness)
            {
                textBlock.Inlines.Add(FormStatesInfoLines(graph.States));
                textBlock.Inlines.Add(FormDeadTransitionsLine(graph.SoundnessProperties.DeadTransitions));
            }
        }

        private static string FormBoundedLine()
        {
            return "Process model is bounded. Full constraint graph is constructed.\n";
        }

        private static string FormUnboundedLine(bool isCoverability)
        {
            var fragmentConstructed =
                isCoverability ? string.Empty : " Only fragment of the constraint graph is constructed";
            return "Process model is unbounded." + fragmentConstructed + "\n";
        }

        private static string FormSoundLine(SoundnessType soundnessType)
        {
            return soundnessType switch
            {
                SoundnessType.Classical => "Classical Soundness is satisfied: \n\n",
                SoundnessType.Lazy => "Lazy Soundness is satisfied: \n\n",
                SoundnessType.None => string.Empty,
                _ => throw new ArgumentOutOfRangeException(nameof(soundnessType), soundnessType,
                    "Unknown soundness type.")
            };
        }

        private static string FormUnsoundLine(SoundnessType soundnessType)
        {
            return soundnessType switch
            {
                SoundnessType.Classical => "Classical Soundness is not satisfied: \n\n",
                SoundnessType.Lazy => "Lazy Soundness is not satisfied: \n\n",
                SoundnessType.None => string.Empty,
                _ => throw new ArgumentOutOfRangeException(nameof(soundnessType), soundnessType,
                    "Unknown soundness type.")
            };
        }

        private static string FormGraphInfoLines(GraphToVisualize graph)
        {
            return
                $"Constraint states: {graph.States.Count}. Constraint arcs: {graph.Arcs.Count}\n";
        }

        private static string FormStatesInfoLines(List<StateToVisualize> states)
        {
            var consideredStateTypes = Enum.GetValues<ConstraintStateType>()
                .Except(new[] { ConstraintStateType.Default, ConstraintStateType.StrictlyCovered })
                .ToArray();

            var stateTypes = consideredStateTypes.ToDictionary(stateType => stateType, _ => 0);

            foreach (var state in states)
            {
                foreach (var stateType in consideredStateTypes)
                {
                    if (state.StateType.HasFlag(stateType))
                    {
                        stateTypes[stateType]++;
                    }
                }
            }

            var stateInfoLines = string.Empty;
            foreach (var stateType in stateTypes)
            {
                var description = stateType.Key.AsString(EnumFormat.Description);

                stateInfoLines += $"{description}s: {stateType.Value}. ";
            }

            return stateInfoLines;
        }

        private static string FormDeadTransitionsLine(IList<string> deadTransitions)
        {
            var resultString = $"\nDead transitions count: {deadTransitions.Count}\n";
            return deadTransitions.Count > 0
                ? resultString + $"Dead transitions list: {string.Join(", ", deadTransitions)}"
                : resultString;
        }
    }
}