using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.Visualization.Extensions;
using DataPetriNetOnSmt.Visualization.Services;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DataPetriNetOnSmt.Visualization
{
    /// <summary>
    /// Логика взаимодействия для ConstraintGraphWindow.xaml
    /// </summary>
    public partial class ConstraintGraphWindow : Window
    {
        private readonly ConstraintGraphToGraphParser constraintGraphToGraphParser;
        public ConstraintGraphWindow(DataPetriNet dpn, ConstraintGraph constraintGraph)
        {
            InitializeComponent();

            constraintGraphToGraphParser = new ConstraintGraphToGraphParser();

            var typedStates = ConstraintGraphAnalyzer.GetStatesDividedByTypes(constraintGraph, dpn.Places.Where(x => x.IsFinal).ToArray());
            graphControl.Graph = constraintGraphToGraphParser.FormGraphBasedOnCG(constraintGraph, typedStates);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

            logControl.FormSoundnessVerificationLog(dpn, constraintGraph, typedStates);
        }
    }
}
