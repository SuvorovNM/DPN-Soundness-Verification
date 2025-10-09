using DataPetriNetIterativeVerificationApplication.Extensions;
using System.Windows;
using System.Windows.Controls;
using DPN.Parsers;
using DPN.Visualization.Converters;
using DPN.Visualization.Models;

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

            var constraintGraphToGraphParser = new LtsToGraphConverter();

            graphControl.Graph = constraintGraphToGraphParser.Convert(constraintGraph);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

            logControl.FormSoundnessVerificationLog(constraintGraph);
        }
    }
}
