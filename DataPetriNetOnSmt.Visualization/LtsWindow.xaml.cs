using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataPetriNetOnSmt.Visualization.Extensions;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using DataPetriNetParsers;
using DataPetriNetVerificationDomain.GraphVisualized;
using Microsoft.Win32;

namespace DataPetriNetOnSmt.Visualization
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

            IToGraphParser constraintGraphToGraphParser = stateSpaceStructure.GraphType == GraphType.Lts
                ? new LtsToGraphParser()
                : new CoverabilityGraphToGraphParser();

            this.stateSpaceStructure = stateSpaceStructure;
            graphControl.Graph = constraintGraphToGraphParser.Parse(stateSpaceStructure);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            
            if (FindName("SaveMenu") is Menu menu && isOpenedFromFile)
                menu.Visibility = Visibility.Collapsed;

            logControl.FormSoundnessVerificationLog(stateSpaceStructure);
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