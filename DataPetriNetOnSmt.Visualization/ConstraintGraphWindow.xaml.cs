using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.Visualization.Extensions;
using DataPetriNetOnSmt.Visualization.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DataPetriNetOnSmt.Visualization
{
    /// <summary>
    /// Логика взаимодействия для ConstraintGraphWindow.xaml
    /// </summary>
    public partial class ConstraintGraphWindow : Window
    {
        private ConstraintGraphToGraphParser constraintGraphToGraphParser;
        public ConstraintGraphWindow(DataPetriNet dpn, ConstraintGraph constraintGraph)
        {
            InitializeComponent();

            constraintGraphToGraphParser = new ConstraintGraphToGraphParser();

            var typedStates = ConstraintGraphAnalyzer.GetStatesDividedByTypes(constraintGraph, dpn.Places.Where(x=>x.IsFinal).ToArray());
            graphControl.Graph = constraintGraphToGraphParser.FormGraphBasedOnCG(constraintGraph, typedStates);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

            logControl.FormSoundnessVerificationLog(constraintGraph, typedStates);            
        }
    }
}
