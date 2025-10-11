using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using DPN.Parsers;
using DPN.SoundnessVerification;
using DPN.SoundnessVerification.TransitionSystems;
using DPN.VerificationApp.Extensions;
using DPN.Visualization.Converters;
using DPN.Visualization.Models;
using Microsoft.Win32;

namespace DPN.VerificationApp
{
    /// <summary>
    /// Логика взаимодействия для ConstraintGraphWindow.xaml
    /// </summary>
    public partial class LtsWindow : Window
    {
        private readonly StateSpaceAbstraction stateSpaceStructure;

        //StateSpaceAbstraction stateSpaceAbstraction, SoundnessProperties soundnessProperties
        public LtsWindow(VerificationResult verificationResult, bool isOpenedFromFile)
        {
            InitializeComponent();

            var graphToVisualize = ToGraphToVisualizeConverter.Convert(verificationResult);
            IToGraphConverter constraintGraphToGraphParser = graphToVisualize.GraphType == GraphType.Lts
                ? new LtsToGraphConverter()
                : new CoverabilityGraphToGraphConverter();
            
            graphControl.Graph = constraintGraphToGraphParser.Convert(graphToVisualize);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            
            if (FindName("SaveMenu") is Menu menu && isOpenedFromFile)
                menu.Visibility = Visibility.Collapsed;

            logControl.FormOutput(graphToVisualize);
            
            stateSpaceStructure = verificationResult.StateSpaceAbstraction;
        }

        private void SaveCG_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new SaveFileDialog()
            {
                Filter = "CG files (*.cgml) | *.cgml"
            };
            if (ofd.ShowDialog() == true)
            {
                using (var fs = new FileStream(ofd.FileName, FileMode.OpenOrCreate))
                {
                    var cgmlParser = new CgmlParser();
                    var xdocument = cgmlParser.Serialize(stateSpaceStructure);

                    xdocument.Save(fs,SaveOptions.None);
                }
            }
        }
    }
}