using DataPetriNetIterativeVerificationApplication.Extensions;
using System.Windows;
using System.Windows.Controls;
using DataPetriNetParsers;
using DataPetriNetVerificationDomain.GraphVisualized;

namespace DataPetriNetIterativeVerificationApplication
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

            graphControl.Graph = constraintGraphToGraphParser.Parse(constraintGraph);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

            logControl.FormSoundnessVerificationLog(constraintGraph);
        }
    }
}
