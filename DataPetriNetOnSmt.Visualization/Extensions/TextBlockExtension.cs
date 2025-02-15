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
                case SoundnessType.Classical:
                    textBlock.FormSoundnessVerificationLog(graph);
                    break;
                case SoundnessType.RelaxedLazy:
                    textBlock.FormSoundnessVerificationLog(
                        graph,
                        detailedInfoAction:
                        () =>
                        {
                            textBlock.Inlines.Add(FormStatesInfoLines(graph.States));
                            textBlock.Inlines.Add(
                                FormUnfeasibleTransitionsLine(graph.SoundnessProperties.DeadTransitions));
                        });
                    break;
            }
        }

        public static void FormSoundnessVerificationLog(
            this TextBlock textBlock,
            GraphToVisualize graph,
            Action? detailedInfoAction = null)
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

            if (detailedInfoAction == null)
            {
                if (graph.SoundnessProperties.Boundedness)
                {
                    textBlock.Inlines.Add(FormStatesInfoLines(graph.States));
                    textBlock.Inlines.Add(
                        FormDeadTransitionsLine(graph.SoundnessProperties.DeadTransitions));
                }
            }
            else
            {
                detailedInfoAction.Invoke();
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
                SoundnessType.Classical => $"{nameof(SoundnessType.Classical)} Soundness is satisfied: \n\n",
                SoundnessType.RelaxedLazy => $"{nameof(SoundnessType.RelaxedLazy)} Soundness is satisfied: \n\n",
                SoundnessType.None => string.Empty,
                _ => throw new ArgumentOutOfRangeException(nameof(soundnessType), soundnessType,
                    "Unknown soundness type.")
            };
        }

        private static string FormUnsoundLine(SoundnessType soundnessType)
        {
            return soundnessType switch
            {
                SoundnessType.Classical => $"{nameof(SoundnessType.Classical)} Soundness is not satisfied: \n\n",
                SoundnessType.RelaxedLazy => $"{nameof(SoundnessType.RelaxedLazy)} Soundness is not satisfied: \n\n",
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
            return FormTransitionsLine(deadTransitions, "Dead");
        }

        private static string FormUnfeasibleTransitionsLine(IList<string> unfeasibleTransitions)
        {
            return FormTransitionsLine(unfeasibleTransitions, "Unfeasible");
        }

        private static string FormTransitionsLine(IList<string> deadTransitions, string transitionsTypeName)
        {
            var resultString = $"\n{transitionsTypeName} transitions count: {deadTransitions.Count}\n";
            return deadTransitions.Count > 0
                ? resultString + $"{transitionsTypeName} transitions list: {string.Join(", ", deadTransitions)}"
                : resultString;
        }
    }
}