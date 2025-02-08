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
using DataPetriNetVerificationDomain;
using DataPetriNetVerificationDomain.CoverabilityGraphVisualized;
using ToGraphParser;

namespace DataPetriNetOnSmt.Visualization
{
    /// <summary>
    /// Логика взаимодействия для ConstraintGraphWindow.xaml
    /// </summary>
    public partial class CoverabilityGraphWindow : Window
    {
        public CoverabilityGraphWindow(CoverabilityGraphToVisualize coverabilityGraph, SoundnessType soundnessType)
        {
            var cgToGraphParser = new CoverabilityGraphToGraphParser();
            InitializeComponent();

            graphControl.Graph = cgToGraphParser.FormGraphBasedOnCg(coverabilityGraph, soundnessType);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            
            logControl.FormOutput(coverabilityGraph, soundnessType);
        }
    }
}
