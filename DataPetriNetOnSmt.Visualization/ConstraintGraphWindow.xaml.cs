using DataPetriNetOnSmt.SoundnessVerification;
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
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            if (dpn.Places.All(x=>x.Tokens == 0)) // TODO: maybe add a relative amount of tokens based on arc weights?
            {
                dpn.Places[0].Tokens = 1;
            }

            var typedStates = ConstraintGraphAnalyzer.GetStatesDividedByTypes(constraintGraph, new[] { dpn.Places[^1] });
            var graphToVisualize = constraintGraphToGraphParser.FormGraphBasedOnCG(constraintGraph, typedStates);
            graphControl.Graph = graphToVisualize;
        }
    }
}
