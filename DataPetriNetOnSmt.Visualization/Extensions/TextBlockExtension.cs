using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification;
using EnumsNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DataPetriNetOnSmt.Visualization.Extensions
{
    public static class TextBlockExtension
    {
        public static void FormSoundnessVerificationLog(this TextBlock textBlock, DataPetriNet dpn, ConstraintGraph graph, Dictionary<StateType, List<ConstraintState>> analysisResult)
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

            textBlock.FontSize = 14;

            var deadTransitions = dpn.Transitions
                    .Select(x => x.Id)
                    .Except(graph.ConstraintArcs.Where(x => !x.Transition.IsSilent).Select(x => x.Transition.Id))
                    .ToList();

            var isSound = !analysisResult[StateType.NoWayToFinalMarking].Any()
                && !analysisResult[StateType.UncleanFinal].Any()
                && !analysisResult[StateType.Deadlock].Any()
                && deadTransitions.Count == 0;

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

        private static string FormGraphInfoLines(ConstraintGraph graph)
        {
            return $"Constraint states: {graph.ConstraintStates.Count}. Constraint arcs: {graph.ConstraintArcs.Count}\n";
        }

        private static string FormStatesInfoLines(Dictionary<StateType, List<ConstraintState>> analysisResult)
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
