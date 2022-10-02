using DataPetriNetGeneration;
using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.Visualization.Services;
using DataPetriNetTransformation;
using Microsoft.Win32;
using Microsoft.Z3;
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
        private readonly DPNToGraphParser dpnParser;
        private readonly PnmlParser pnmlParser;
        private readonly SampleDPNProvider dpnProvider;
        private readonly ITransformation dpnTransformation;
        private Context context;

        public MainWindow()
        {
            InitializeComponent();
            dpnParser = new DPNToGraphParser();
            dpnProvider = new SampleDPNProvider();
            pnmlParser = new PnmlParser();
            dpnTransformation = new TransformationToAtomicConstraints();
            context = new Context();

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

        private void GenerateModelItem_Click(object sender, RoutedEventArgs e)
        {
            ModelGenerationPropertiesWindow modelGenerationPropertiesWindow = new ModelGenerationPropertiesWindow();
            if (modelGenerationPropertiesWindow.ShowDialog() == true)
            {
                var dpnGenerator = new DPNGenerator(context);
                currentDisplayedNet = dpnGenerator.Generate(
                    modelGenerationPropertiesWindow.PlacesCount,
                    modelGenerationPropertiesWindow.TransitionCount,
                    modelGenerationPropertiesWindow.ExtraArcsCount,
                    modelGenerationPropertiesWindow.VarsCount,
                    modelGenerationPropertiesWindow.ConditionsCount);
                graphControl.Graph = dpnParser.FormGraphBasedOnDPN(currentDisplayedNet);
            }
        }

        private void DefaultVOCMenuItem_Click(object sender, RoutedEventArgs e)
        {
            currentDisplayedNet = dpnProvider.GetVOCDataPetriNet();
            graphControl.Graph = dpnParser.FormGraphBasedOnDPN(currentDisplayedNet);
        }
        private void DefaultVOVMenuItem_Click(object sender, RoutedEventArgs e)
        {
            currentDisplayedNet = dpnProvider.GetVOVDataPetriNet();
            graphControl.Graph = dpnParser.FormGraphBasedOnDPN(currentDisplayedNet);
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
                var constraintGraph = new ConstraintGraph(currentDisplayedNet, new ConstraintExpressionOperationServiceWithEqTacticConcat(currentDisplayedNet.Context));
                await Task.Run(() => constraintGraph.GenerateGraph(true));
                ConstraintGraphWindow constraintGraphWindow = new ConstraintGraphWindow(currentDisplayedNet, constraintGraph);
                constraintGraphWindow.Owner = this;
                constraintGraphWindow.Show();
            }
        }

        private async Task DisplayConstraintGraphManualConcat()
        {
            if (currentDisplayedNet != null)
            {
                var constraintGraph = new ConstraintGraph(currentDisplayedNet, new ConstraintExpressionOperationServiceWithManualConcat(currentDisplayedNet.Context));
                await Task.Run(() => constraintGraph.GenerateGraph(true));
                ConstraintGraphWindow constraintGraphWindow = new ConstraintGraphWindow(currentDisplayedNet, constraintGraph);
                constraintGraphWindow.Owner = this;
                constraintGraphWindow.Show();
            }
        }

        private void TransformModelItem_Click(object sender, RoutedEventArgs e)
        {
            //currentDisplayedNet
            currentDisplayedNet = dpnTransformation.Transform(currentDisplayedNet);
            graphControl.Graph = dpnParser.FormGraphBasedOnDPN(currentDisplayedNet);
        }
    }
}
