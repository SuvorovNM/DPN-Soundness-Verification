using DataPetriNetOnSmt;
using DataPetriNetOnSmt.Enums;
using EnumsNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DPN.SoundnessVerification.TransitionSystems;
using DPN.Visualization.Models;

namespace DataPetriNetIterativeVerificationApplication.Extensions
{
    public static class TextBlockExtension
    {
        public static void FormSoundnessVerificationLog(this TextBlock textBlock, GraphToVisualize graph)
        {
            ArgumentNullException.ThrowIfNull(graph);

            textBlock.FontSize = 14;
            textBlock.Inlines.Clear();

            textBlock.Inlines.Add(new Bold(graph.SoundnessProperties!.Soundness
                ? new Run(FormSoundLine()) { Foreground = Brushes.DarkGreen }
                : new Run(FormUnsoundLine()) { Foreground = Brushes.DarkRed }));

            textBlock.Inlines.Add(new Bold(graph.SoundnessProperties.Boundedness
                ? new Run(FormBoundedLine())
                : new Run(FormUnboundedLine())));

            textBlock.Inlines.Add(FormGraphInfoLines(graph));

            if (graph.SoundnessProperties.Boundedness)
            {
                textBlock.Inlines.Add(FormStatesInfoLines(graph.States));
                textBlock.Inlines.Add(FormDeadTransitionsLine(graph.SoundnessProperties.DeadTransitions));
            }
        }
        public static void FormSoundnessVerificationLog(this TextBlock textBlock, DataPetriNet dpn, ConstraintGraph graph, Dictionary<StateType, List<LtsState>> analysisResult)
        {
            if (textBlock == null)
            {
                throw new ArgumentNullException(nameof(textBlock));
            }
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }
            if (analysisResult == null)
            {
                throw new ArgumentNullException(nameof(analysisResult));
            }

            var deadTransitions = dpn.Transitions
                    .Select(x => x.Id)
                    .Except(graph.ConstraintArcs.Where(x => !x.Transition.IsSilent).Select(x => x.Transition.Id))
                    .ToList();

            var isSound = graph.IsFullGraph
                && !analysisResult[StateType.NoWayToFinalMarking].Any()
                && !analysisResult[StateType.UncleanFinal].Any()
                && !analysisResult[StateType.Deadlock].Any()
                && deadTransitions.Count == 0;

            textBlock.FontSize = 14;
            textBlock.Inlines.Clear();

            textBlock.Inlines.Add(new Bold(isSound
                ? new Run(FormSoundLine()) { Foreground = Brushes.DarkGreen }
                : new Run(FormUnsoundLine()) { Foreground = Brushes.DarkRed }));

            textBlock.Inlines.Add(new Bold(graph.IsFullGraph
                ? new Run(FormBoundedLine())
                : new Run(FormUnboundedLine())));

            textBlock.Inlines.Add(FormGraphInfoLines(graph));

            if (graph.IsFullGraph)
            {
                textBlock.Inlines.Add(FormStatesInfoLines(analysisResult));
                textBlock.Inlines.Add(FormDeadTransitionsLine(deadTransitions));
            }
        }

        private static string FormBoundedLine()
        {
            return "Process model is bounded. Full constraint graph is constructed.\n";
        }

        private static string FormUnboundedLine()
        {
            return "Process model is unbounded. Only fragment of the constraint graph is constructed.\n";
        }

        private static string FormSoundLine()
        {
            return "Process model is SOUND: \n\n";
        }

        private static string FormUnsoundLine()
        {
            return "Process model is UNSOUND: \n\n";
        }

        private static string FormGraphInfoLines(GraphToVisualize graph)
        {
            return $"Constraint states: {graph.States.Count}. Constraint arcs: {graph.Arcs.Count}\n";
        }

        private static string FormGraphInfoLines(ConstraintGraph graph)
        {
            return $"Constraint states: {graph.ConstraintStates.Count}. Constraint arcs: {graph.ConstraintArcs.Count}\n";
        }

        private static string FormStatesInfoLines(List<StateToVisualize> states)
        {
            var stateTypes = new Dictionary<ConstraintStateType, int>();
            foreach (var stateType in Enum.GetValues<ConstraintStateType>())
            {
                stateTypes.Add(stateType, 0);
            }

            foreach (var state in states)
            {
                foreach (var stateType in Enum.GetValues<ConstraintStateType>())
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
