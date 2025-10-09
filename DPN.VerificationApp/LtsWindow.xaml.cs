using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using DPN.Parsers;
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
        private readonly GraphToVisualize stateSpaceStructure;

        public LtsWindow(GraphToVisualize stateSpaceStructure, bool isOpenedFromFile)
        {
            InitializeComponent();

            IToGraphConverter constraintGraphToGraphParser = stateSpaceStructure.GraphType == GraphType.Lts
                ? new LtsToGraphConverter()
                : new CoverabilityGraphToGraphConverter();

            this.stateSpaceStructure = stateSpaceStructure;
            graphControl.Graph = constraintGraphToGraphParser.Convert(stateSpaceStructure);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            
            if (FindName("SaveMenu") is Menu menu && isOpenedFromFile)
                menu.Visibility = Visibility.Collapsed;

            logControl.FormOutput(stateSpaceStructure);
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