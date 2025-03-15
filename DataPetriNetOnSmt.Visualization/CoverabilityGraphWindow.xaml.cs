using DataPetriNetOnSmt.Visualization.Extensions;
using System.Windows;
using System.Windows.Controls;
using DataPetriNetOnSmt.SoundnessVerification;
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
        public CoverabilityGraphWindow(GraphToVisualize coverabilityGraph)
        {
            var cgToGraphParser = new CoverabilityGraphToGraphParser();
            InitializeComponent();

            graphControl.Graph = cgToGraphParser.FormGraphBasedOnCg(coverabilityGraph);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            
            logControl.FormOutput(coverabilityGraph);
        }
    }
}
