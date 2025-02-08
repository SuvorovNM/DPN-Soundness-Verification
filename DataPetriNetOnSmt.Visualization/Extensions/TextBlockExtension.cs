using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using DataPetriNetVerificationDomain.ConstraintGraphVisualized;
using EnumsNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DataPetriNetVerificationDomain;
using DataPetriNetVerificationDomain.CoverabilityGraphVisualized;
using DataPetriNetVerificationDomain.GraphVisualized;

namespace DataPetriNetOnSmt.Visualization.Extensions
{
    public static class TextBlockExtension
    {
        public static void FormOutput(this TextBlock textBlock, CoverabilityGraphToVisualize graph, SoundnessType soundnessType)
        {
            ArgumentNullException.ThrowIfNull(graph);
            
            textBlock.FontSize = 14;
            textBlock.Inlines.Clear();
            
            switch (soundnessType)
            {
                case SoundnessType.None:
                    textBlock.Inlines.Add(FormGraphInfoLines(graph));
                    break;
                case SoundnessType.ClassicalSoundness or SoundnessType.LazySoundness:
                    textBlock.FormSoundnessVerificationLog(graph);
                    break;
            }
            
        }
        
        private static void FormSoundnessVerificationLog(this TextBlock textBlock, CoverabilityGraphToVisualize graph)
        {
            if (graph.IsSound != null)
            {
                textBlock.Inlines.Add(new Bold(graph.IsSound.Value
                    ? new Run(FormSoundLine()) { Foreground = Brushes.DarkGreen }
                    : new Run(FormUnsoundLine()) { Foreground = Brushes.DarkRed }));
            }

            textBlock.Inlines.Add(new Bold(graph.IsBounded
                ? new Run(FormBoundedLine())
                : new Run(FormUnboundedLine(isCoverability: true))));

            textBlock.Inlines.Add(FormGraphInfoLines(graph));

            if (graph.IsBounded)
            {
                textBlock.Inlines.Add(FormStatesInfoLines(graph.CgStates));
            }
        }

        public static void FormSoundnessVerificationLog(this TextBlock textBlock, ConstraintGraphToVisualize graph)
        {
            ArgumentNullException.ThrowIfNull(graph);

            textBlock.FontSize = 14;
            textBlock.Inlines.Clear();

            textBlock.Inlines.Add(new Bold(graph.IsSound
                ? new Run(FormSoundLine()) { Foreground = Brushes.DarkGreen }
                : new Run(FormUnsoundLine()) { Foreground = Brushes.DarkRed }));

            textBlock.Inlines.Add(new Bold(graph.IsBounded
                ? new Run(FormBoundedLine())
                : new Run(FormUnboundedLine(isCoverability: false))));

            textBlock.Inlines.Add(FormGraphInfoLines(graph));

            if (graph.IsBounded)
            {
                textBlock.Inlines.Add(FormStatesInfoLines(graph.ConstraintStates));
                textBlock.Inlines.Add(FormDeadTransitionsLine(graph.DeadTransitions));
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

        private static string FormSoundLine()
        {
            return "Process model is SOUND: \n\n";
        }

        private static string FormUnsoundLine()
        {
            return "Process model is UNSOUND: \n\n";
        }

        private static string FormGraphInfoLines(ConstraintGraphToVisualize graph)
        {
            return
                $"Constraint states: {graph.ConstraintStates.Count}. Constraint arcs: {graph.ConstraintArcs.Count}\n";
        }

        private static string FormGraphInfoLines(CoverabilityGraphToVisualize graph)
        {
            return $"Constraint states: {graph.CgStates.Count}. Constraint arcs: {graph.CgArcs.Count}\n";
        }

        private static string FormGraphInfoLines(ConstraintGraph graph)
        {
            return
                $"Constraint states: {graph.ConstraintStates.Count}. Constraint arcs: {graph.ConstraintArcs.Count}\n";
        }

        private static string FormStatesInfoLines(List<StateToVisualize> states)
        {
            var stateTypes = new Dictionary<ConstraintStateType, int>();
            var consideredStateTypes = Enum.GetValues<ConstraintStateType>()
                .Except(new[] { ConstraintStateType.Default, ConstraintStateType.StrictlyCovered });

            foreach (var stateType in consideredStateTypes)
            {
                stateTypes.Add(stateType, 0);
            }

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

        private static string FormStatesInfoLines(Dictionary<StateType, List<LtsState>> analysisResult)
        {
            var stateInfoLines = string.Empty;
            foreach (var stateType in analysisResult.Keys)
            {
                var description = stateType.AsString(EnumFormat.Description);

                stateInfoLines += $"{description}s: {analysisResult[stateType].Count}. ";
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