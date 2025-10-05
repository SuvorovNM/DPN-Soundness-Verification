using DataPetriNetGeneration;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using DataPetriNetOnSmt.Visualization.Services;
using DataPetriNetParsers;
using DataPetriNetVerificationDomain.CoverabilityTreeVisualized;
using Microsoft.Win32;
using Microsoft.Z3;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using DataPetriNetVerificationDomain.GraphVisualized;
using ToGraphParser;

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
        private Context context;

        public MainWindow()
        {
            InitializeComponent();
            dpnParser = new DPNToGraphParser();
            dpnProvider = new SampleDPNProvider();
            pnmlParser = new PnmlParser();
            //dpnTransformation = new TransformationToAtomicConstraints();
            context = new Context();
            Microsoft.Z3.Global.SetParameter("parallel.enable", "true");
            Microsoft.Z3.Global.SetParameter("threads", "4");
            Microsoft.Z3.Global.SetParameter("arith.propagation_mode", "2");

            currentDisplayedNet = dpnProvider.GetVOVDataPetriNet();
            graphControl.Graph = dpnParser.FormGraphBasedOnDPN(currentDisplayedNet);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }



        private void OpenDpn_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Model files (*.pnmlx) | *.pnmlx"
            };
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

        private async void ConstructCoverabilityTree_Click(object sender, RoutedEventArgs e)
        {
            var coverabilityTree = new CoverabilityTree(currentDisplayedNet, withTauTransitions: true);
            var ctToVisualize = await ConstructAndAnalyzeCoverabilityTree(coverabilityTree);
            VisualizeCoverabilityTree(ctToVisualize);
        }

        private async void ConstructCoverabilityGraph_Click(object sender, RoutedEventArgs e)
        {
            var coverabilityGraph = new CoverabilityGraph(currentDisplayedNet);
            var cgToVisualize = await ConstructAndAnalyzeCoverabilityGraph(coverabilityGraph);
            VisualizeCoverabilityGraph(cgToVisualize);
        }
        
        private async void CheckLazySoundnessDirectItem_Click(object sender, RoutedEventArgs e)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var cgToVisualize = await CheckLazySoundness(
                currentDisplayedNet, 
                new CoverabilityGraph(currentDisplayedNet, stopOnCoveringFinalPosition: true));
            
            stopwatch.Stop();
            MessageBox.Show($"Time spent: {stopwatch.ElapsedMilliseconds}ms");

            VisualizeCoverabilityGraph(cgToVisualize);
        }

        private async void CheckSoundnessDirectItem_Click(object sender, RoutedEventArgs e)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var dpnTransformation = new TransformerToRefined();
            (var dpn, var lts) = dpnTransformation.TransformUsingLts(currentDisplayedNet);
            if (lts.IsFullGraph)
            {
                var constraintGraph = new ConstraintGraph(dpn);
                var constraintGraphToVisualize = await CheckSoundness(dpn, constraintGraph);

                stopwatch.Stop();
                MessageBox.Show($"Time spent: {stopwatch.ElapsedMilliseconds}ms");

                VisualizeConstraintGraph(constraintGraphToVisualize);
            }
            else
            {
                var soundnessProperties = SoundnessAnalyzer.CheckSoundness(dpn, lts);

                stopwatch.Stop();
                MessageBox.Show($"Time spent: {stopwatch.ElapsedMilliseconds}ms");

                VisualizeConstraintGraph(GraphToVisualize.FromLts(lts, soundnessProperties));
            }
        }

        private async void CheckSoundnessImprovedItem_Click(object sender, RoutedEventArgs e)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var lts = new ClassicalLabeledTransitionSystem(currentDisplayedNet);

            var ltsToVisualize = await CheckSoundness(currentDisplayedNet, lts);

            if (ltsToVisualize.SoundnessProperties!.Soundness)
            {
                var cg = new ConstraintGraph(currentDisplayedNet);
                ltsToVisualize = await CheckSoundness(currentDisplayedNet, cg);

                if (ltsToVisualize.SoundnessProperties!.Soundness)
                {
                    var dpnTransformation = new TransformerToRefined();
                    (var dpn, _) = dpnTransformation.TransformUsingLts(currentDisplayedNet);

                    var constraintGraph = new ConstraintGraph(dpn);
                    ltsToVisualize = await CheckSoundness(dpn, constraintGraph);
                }
            }

            stopwatch.Stop();

            MessageBox.Show($"Time spent: {stopwatch.ElapsedMilliseconds}ms");

            VisualizeConstraintGraph(ltsToVisualize);
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
            var constraintGraph = new ConstraintGraph(currentDisplayedNet);
            var constraintGraphToVisualize = await CheckSoundness(currentDisplayedNet, constraintGraph);
            VisualizeConstraintGraph(constraintGraphToVisualize);
        }

        private void VisualizeCoverabilityTree(CoverabilityTreeToVisualize coverabilityTreeToVisualize)
        {
            var coverabilityTreeWindow = new CoverabilityTreeWindow(coverabilityTreeToVisualize)
            {
                Owner = this
            };
            coverabilityTreeWindow.Show();
        }
        
        private void VisualizeCoverabilityGraph(GraphToVisualize coverabilityGraphToVisualize)
        {
            var coverabilityTreeWindow = new LtsWindow(coverabilityGraphToVisualize, isOpenedFromFile: false)
            {
                Owner = this
            };
            coverabilityTreeWindow.Show();
        }

        private void VisualizeConstraintGraph(GraphToVisualize constraintGraphToVisualize)
        {
            var constraintGraphWindow = new LtsWindow(constraintGraphToVisualize, isOpenedFromFile: false)
            {
                Owner = this
            };
            constraintGraphWindow.Show();
        }

        private async Task<GraphToVisualize> CheckSoundness(DataPetriNet dpn, LabeledTransitionSystem lts)
        {
            await Task.Run(lts.GenerateGraph);
            var soundnessProperties = SoundnessAnalyzer.CheckSoundness(dpn, lts);

            return GraphToVisualize.FromLts(lts, soundnessProperties);
        }

        private async Task<GraphToVisualize> CheckLazySoundness(DataPetriNet dpn, CoverabilityGraph cg)
        {
            await Task.Run(cg.GenerateGraph);
            var soundnessProperties = RelaxedLazySoundnessAnalyzer.CheckSoundness(dpn, cg);
            
            return GraphToVisualize.FromCoverabilityGraph(cg, soundnessProperties);
        }

        private async Task<CoverabilityTreeToVisualize> ConstructAndAnalyzeCoverabilityTree
            (CoverabilityTree ct)
        {
            await Task.Run(ct.GenerateGraph);
            return CoverabilityTreeToVisualize.FromCoverabilityTree(ct);
        }
        
        private async Task<GraphToVisualize> ConstructAndAnalyzeCoverabilityGraph
            (CoverabilityGraph cg)
        {
            await Task.Run(cg.GenerateGraph);
            var soundnessProperties = SoundnessAnalyzer.CheckSoundness(currentDisplayedNet, cg);
            return GraphToVisualize.FromCoverabilityGraph(cg, soundnessProperties);
        }

        private void TransformModelToRefinedItem_Click(object sender, RoutedEventArgs e)
        {
            var dpnTransformation = new TransformerToRefined();
            
            (currentDisplayedNet, _) = dpnTransformation.TransformUsingCg(currentDisplayedNet);

            graphControl.Graph = dpnParser.FormGraphBasedOnDPN(currentDisplayedNet);
        }
        
        private void TransformModelToTauItem_Click(object sender, RoutedEventArgs e)
        {
            var dpnTransformation = new TransformerToTau();
            
            currentDisplayedNet = dpnTransformation.Transform(currentDisplayedNet);

            graphControl.Graph = dpnParser.FormGraphBasedOnDPN(currentDisplayedNet);
        }

        private async void ConstructLtsItem_Click(object sender, RoutedEventArgs e)
        {
            if (currentDisplayedNet != null)
            {
                var lts = new ClassicalLabeledTransitionSystem(currentDisplayedNet);

                var ltsToVisualize = await CheckSoundness(currentDisplayedNet, lts);
                VisualizeConstraintGraph(ltsToVisualize);
            }
        }

        private void OpenCG_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "CG files (*.cgml) | *.cgml"
            };
            if (ofd.ShowDialog() == true)
            {
                using (var fs = new FileStream(ofd.FileName, FileMode.Open))
                {
                    var cgmlParser = new CgmlParser();
                    var xDocument = XDocument.Load(fs);

                    var constraintGraphToVisualize = cgmlParser.Deserialize(xDocument);

                    var constraintGraphWindow = new LtsWindow(constraintGraphToVisualize, isOpenedFromFile: true);
                    constraintGraphWindow.Owner = this;
                    constraintGraphWindow.Show();
                }
            }
        }

        private void TransformModelToRepairedItem_Click(object sender, RoutedEventArgs e)
        {
            var dpnRepairment = new Repairment();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            (currentDisplayedNet, var repairSteps, var result) = dpnRepairment.RepairDpn(currentDisplayedNet);

            stopwatch.Stop();

            MessageBox.Show(result ? $"Success! Time spent: {stopwatch.ElapsedMilliseconds} ms. Repair steps: {repairSteps}." : "Failure!");
            graphControl.Graph = dpnParser.FormGraphBasedOnDPN(currentDisplayedNet);
        }

        private void SaveDpn_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new SaveFileDialog()
            {
                Filter = "Model files (*.pnmlx) | *.pnmlx",
            };
            if (ofd.ShowDialog() == true)
            {
                var xDocument = pnmlParser.SerializeDpn(currentDisplayedNet);
                xDocument.Save(ofd.FileName);
            }
        }
    }
}
