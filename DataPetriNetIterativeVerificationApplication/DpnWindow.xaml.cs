using DataPetriNetOnSmt;
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
    /// Логика взаимодействия для DpnWindow.xaml
    /// </summary>
    public partial class DpnWindow : Window
    {
        private readonly DPNToGraphParser dpnParser;
        public DpnWindow(DataPetriNet dpn)
        {
            InitializeComponent();
            dpnParser = new DPNToGraphParser();
            graphControl.Graph = dpnParser.FormGraphBasedOnDPN(dpn);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }
    }
}
