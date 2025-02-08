using DataPetriNetOnSmt.Visualization.Extensions;
using System.Windows;
using System.Windows.Controls;
using DataPetriNetParsers;
using DataPetriNetVerificationDomain;
using DataPetriNetVerificationDomain.GraphVisualized;

namespace DataPetriNetOnSmt.Visualization
{
    /// <summary>
    /// Логика взаимодействия для ConstraintGraphWindow.xaml
    /// </summary>
    public partial class CoverabilityGraphWindow : Window
    {
        public CoverabilityGraphWindow(GraphToVisualize coverabilityGraph, SoundnessType soundnessType)
        {
            var cgToGraphParser = new CoverabilityGraphToGraphParser();
            InitializeComponent();

            graphControl.Graph = cgToGraphParser.FormGraphBasedOnCg(coverabilityGraph, soundnessType);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            
            logControl.FormOutput(coverabilityGraph, soundnessType);
        }
    }
}
