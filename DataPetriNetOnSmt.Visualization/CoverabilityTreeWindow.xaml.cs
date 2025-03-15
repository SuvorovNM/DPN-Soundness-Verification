using DataPetriNetParsers;
using DataPetriNetVerificationDomain.CoverabilityTreeVisualized;
using System.Windows;
using System.Windows.Controls;

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
