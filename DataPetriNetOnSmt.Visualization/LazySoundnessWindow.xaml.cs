using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.Visualization.Extensions;
using DataPetriNetVerificationDomain.ConstraintGraphVisualized;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DataPetriNetParsers;
using DataPetriNetVerificationDomain.CoverabilityGraphVisualized;
using ToGraphParser;

namespace DataPetriNetOnSmt.Visualization
{
    /// <summary>
    /// Логика взаимодействия для ConstraintGraphWindow.xaml
    /// </summary>
    public partial class LazySoundnessWindow : Window
    {
        public LazySoundnessWindow(CoverabilityGraphToVisualize coverabilityGraph)
        {
            var cgToGraphParser = new CoverabilityGraphToGraphParser();
            InitializeComponent();

            graphControl.Graph = cgToGraphParser.FormGraphBasedOnCg(coverabilityGraph);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

            logControl.Text = $"Nodes: {coverabilityGraph.CgStates.Count}. Arcs: {coverabilityGraph.CgArcs.Count}.";
        }
    }
}
