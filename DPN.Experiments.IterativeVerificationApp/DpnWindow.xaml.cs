using System.Windows;
using System.Windows.Controls;
using DPN.Models;
using DPN.Visualization.Converters;

namespace DataPetriNetIterativeVerificationApplication
{
    /// <summary>
    /// Логика взаимодействия для DpnWindow.xaml
    /// </summary>
    public partial class DpnWindow : Window
    {
        public DpnWindow(DataPetriNet dpn)
        {
            InitializeComponent();
            var dpnConverter = new DpnToGraphConverter();
            graphControl.Graph = dpnConverter.ConvertToDpn(dpn);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }
    }
}
