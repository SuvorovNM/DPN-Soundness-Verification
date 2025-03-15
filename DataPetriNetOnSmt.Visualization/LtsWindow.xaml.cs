using DataPetriNetOnSmt.Visualization.Extensions;
using System.Windows;
using System.Windows.Controls;
using DataPetriNetParsers;
using DataPetriNetVerificationDomain.GraphVisualized;

namespace DataPetriNetOnSmt.Visualization
{
    /// <summary>
    /// Логика взаимодействия для ConstraintGraphWindow.xaml
    /// </summary>
    public partial class LtsWindow : Window
    {
        public LtsWindow(GraphToVisualize constraintGraph)
        {
            InitializeComponent();

            var constraintGraphToGraphParser = new LtsToGraphParser();

            graphControl.Graph = constraintGraphToGraphParser.FormGraphBasedOnCG(constraintGraph);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

            logControl.FormSoundnessVerificationLog(constraintGraph);
        }
    }
}
