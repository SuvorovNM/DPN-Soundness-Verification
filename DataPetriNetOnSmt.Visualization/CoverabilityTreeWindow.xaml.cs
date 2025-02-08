using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using DataPetriNetParsers;
using DataPetriNetVerificationDomain.CoverabilityTreeVisualized;
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
using DataPetriNetVerificationDomain.CoverabilityGraphVisualized;

namespace DataPetriNetOnSmt.Visualization
{
    /// <summary>
    /// Логика взаимодействия для CoverabilityGraphWindow.xaml
    /// </summary>
    public partial class CoverabilityTreeWindow : Window
    {
        public CoverabilityTreeWindow(CoverabilityTreeToVisualize coverabilityTree)
        {
            var ctParser = new CoverabilityTreeToGraphParser();
            InitializeComponent();

            graphControl.Graph = ctParser.FormGraphBasedOnCt(coverabilityTree);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

            logControl.Text = $"Nodes: {coverabilityTree.CtStates.Count}. Arcs: {coverabilityTree.CtArcs.Count}.";
        }
    }
}
