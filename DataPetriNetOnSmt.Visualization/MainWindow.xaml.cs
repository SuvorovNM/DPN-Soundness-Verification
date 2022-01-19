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

namespace DataPetriNetOnSmt.Visualization
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataPetriNet currentDisplayedNet;
        private DPNToGraphParser dpnParser;
        private SampleDPNProvider dpnProvider;

        public MainWindow()
        {
            InitializeComponent();
            dpnParser = new DPNToGraphParser();
            dpnProvider = new SampleDPNProvider();
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //var ofd = new OpenFileDialog();
            //ofd.Filter = "Model files (*.pnmlx) | *.pnmlx";
            //if (ofd.ShowDialog() == true)
            {
                //Stream stream = ofd.OpenFile();
                currentDisplayedNet = dpnProvider.GetVOVDataPetriNet();// TODO: add serialization
                //stream.Close();

                graphControl.Graph = dpnParser.FormGraphBasedOnDPN(currentDisplayedNet);
                // Add graph to control
            }
        }

        private void CheckSoundnessMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open new window with CG
        }
    }
}
