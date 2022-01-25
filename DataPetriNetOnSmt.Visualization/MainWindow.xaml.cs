using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.Visualization.Services;
using Microsoft.Msagl.Drawing;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace DataPetriNetOnSmt.Visualization
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataPetriNet currentDisplayedNet;
        private DPNToGraphParser dpnParser;
        private PnmlParser pnmlParser;
        private SampleDPNProvider dpnProvider;

        public MainWindow()
        {
            InitializeComponent();
            dpnParser = new DPNToGraphParser();
            dpnProvider = new SampleDPNProvider();
            pnmlParser = new PnmlParser();

            currentDisplayedNet = dpnProvider.GetVOVDataPetriNet();
            graphControl.Graph = dpnParser.FormGraphBasedOnDPN(currentDisplayedNet);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Model files (*.pnmlx) | *.pnmlx";
            if (ofd.ShowDialog() == true)
            {
                XmlDocument xDoc = new XmlDocument();                
                xDoc.Load(ofd.FileName);

                currentDisplayedNet = pnmlParser.DeserializeDpn(xDoc);

                graphControl.Graph = dpnParser.FormGraphBasedOnDPN(currentDisplayedNet);
            }
        }

        private void DefaultVOCMenuItem_Click(object sender, RoutedEventArgs e)
        {
            graphControl.Graph = dpnParser.FormGraphBasedOnDPN(dpnProvider.GetVOCDataPetriNet());
        }
        private void DefaultVOVMenuItem_Click(object sender, RoutedEventArgs e)
        {
            graphControl.Graph = dpnParser.FormGraphBasedOnDPN(dpnProvider.GetVOVDataPetriNet());
        }
        private async void QeTacticSoundnessMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await DisplayConstraintGraphBasedOnQeConcat();
        }
        private async void ManualSoundnessMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await DisplayConstraintGraphManualConcat();
        }

        private async Task DisplayConstraintGraphBasedOnQeConcat()
        {
            if (currentDisplayedNet != null)
            {
                var constraintGraph = new ConstraintGraph(currentDisplayedNet, new ConstraintExpressionOperationServiceWithEqTacticConcat());
                await Task.Run(() => constraintGraph.GenerateGraph());
                ConstraintGraphWindow constraintGraphWindow = new ConstraintGraphWindow(currentDisplayedNet, constraintGraph);
                constraintGraphWindow.Owner = this;
                constraintGraphWindow.Show();
            }
        }

        private async Task DisplayConstraintGraphManualConcat()
        {
            if (currentDisplayedNet != null)
            {
                var constraintGraph = new ConstraintGraph(currentDisplayedNet, new ConstraintExpressionOperationServiceWithManualConcat());
                await Task.Run(() => constraintGraph.GenerateGraph());
                ConstraintGraphWindow constraintGraphWindow = new ConstraintGraphWindow(currentDisplayedNet, constraintGraph);
                constraintGraphWindow.Owner = this;
                constraintGraphWindow.Show();
            }
        }
    }
}
