using DataPetriNetIterativeVerificationApplication.Extensions;
using DataPetriNetVerificationDomain.ConstraintGraphVisualized;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ToGraphParser;

namespace DataPetriNetIterativeVerificationApplication
{
    /// <summary>
    /// Логика взаимодействия для ConstraintGraphWindow.xaml
    /// </summary>
    public partial class LtsWindow : Window
    {
        public LtsWindow(ConstraintGraphToVisualize constraintGraph)
        {
            InitializeComponent();

            var constraintGraphToGraphParser = new LtsToGraphParser();

            graphControl.Graph = constraintGraphToGraphParser.FormGraphBasedOnCG(constraintGraph);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

            logControl.FormSoundnessVerificationLog(constraintGraph);
        }
    }
}
